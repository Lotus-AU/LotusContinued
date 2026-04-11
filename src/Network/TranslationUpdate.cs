using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using VentLib;
using VentLib.Localization;
using System.Net.Http;
using System.Threading.Tasks;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.GUI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Object = System.Object;

namespace Lotus.Network;

public static class TranslationUpdate
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(TranslationUpdate));

    public static IEnumerator CheckIfCanDownloadTranslations()
    {
        DirectoryInfo lotusLanguageFiles =
            LocalizerSettings.LanguageDirectory.GetDirectory(
                AssemblyUtils.GetAssemblyRefName(Assembly.GetExecutingAssembly()));
        if (OperatingSystem.IsWindows())
        {
            log.Info("Checking language files.");
            if (!lotusLanguageFiles.Exists) lotusLanguageFiles.Create();
            log.Info($"language file count: {lotusLanguageFiles.GetFiles().Length}");
            if (lotusLanguageFiles.GetFiles().Length > 2) yield break; // has language files (more than temp and english)
        }

        log.Info("Downloading...");

        using HttpClient httpClient = new();

        Task<byte[]> downloadTask = httpClient.GetByteArrayAsync(
            "https://github.com/Lotus-AU/Languages/archive/refs/heads/new_translations.zip"
        );

        yield return new WaitUntil((Func<bool>)(() => downloadTask.IsCompleted));

        lotusLanguageFiles
            .GetFiles()
            .FirstOrOptional(f => f.Name.StartsWith("lang_Template"))
            .IfPresent(f => f.Delete());

        IEnumerator Fallback()
        {
            string fallbackEnglishLanguage = LotusAssets.LoadAsset<TextAsset>("Fallback/lang_English.yaml").text;
            string destPath = Path.Combine(lotusLanguageFiles.FullName, "lang_English.yaml");
            bool fileAlreadyExists = File.Exists(destPath);
            File.WriteAllText(destPath, fallbackEnglishLanguage);

            Localizer.Reload(Assembly.GetExecutingAssembly());

            InfoTextBox amTextBox = DestroyableSingleton<AccountManager>.Instance.transform.Find("InfoTextBox").GetComponent<InfoTextBox>();
            InfoTextBox warningScreen = UnityEngine.Object.Instantiate(amTextBox);
            warningScreen.transform.parent = amTextBox.transform.parent;
            warningScreen.gameObject.SetActive(false);
            warningScreen.transform.localPosition -= new Vector3(0, 0, 10);

            warningScreen.SetOneButton();
            warningScreen.titleTexxt.text = Localizer.Translate("TranslationUpdate.ErrorTitle", "Translation Update Error");
            warningScreen.bodyText.text = Localizer.Translate("TranslationUpdate.ErrorMessage", "An error occurred while downloading the translation files. A fallback file has been used. It will try to redownload the files again on next launch.");
            warningScreen.button1Text.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Okay, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
            warningScreen.button1Text.gameObject.SetActive(fileAlreadyExists);
            warningScreen.gameObject.SetActive(true);
            while (warningScreen.gameObject.activeSelf) yield return null;
        }

        if (downloadTask.IsFaulted)
        {
            if (lotusLanguageFiles.GetFiles().Length > 2) yield break; // we already have language files so don't warn user.
            yield return Fallback();
            yield break;
        }

        bool errorOccured = false;

        try
        {
            byte[] zipData = downloadTask.Result;

            using MemoryStream zipStream = new(zipData);
            using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
            int updatedCount = 0;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                if (entry.Name.StartsWith("README")) continue;

                string destPath = Path.Combine(lotusLanguageFiles.FullName, entry.Name);
                if (File.Exists(destPath) && File.GetLastWriteTime(destPath) >= entry.LastWriteTime.LocalDateTime)
                    continue;
                updatedCount++;

                using Stream entryStream = entry.Open();
                using FileStream fileStream = File.Create(destPath);
                entryStream.CopyTo(fileStream);
            }
            if (updatedCount == 0) yield break;
        }
        catch (Exception exception)
        {
            log.Exception(exception);
            errorOccured = true;
        }

        if (errorOccured)
        {
            lotusLanguageFiles.Delete(true);
            lotusLanguageFiles.Create();
            yield return Fallback();
            yield break;
        }

        Localizer.Reload(Assembly.GetExecutingAssembly());

        InfoTextBox infoTextBox = DestroyableSingleton<AccountManager>.Instance.transform.Find("InfoTextBox").GetComponent<InfoTextBox>();
        InfoTextBox customScreen = UnityEngine.Object.Instantiate(infoTextBox);
        customScreen.transform.parent = infoTextBox.transform.parent;
        customScreen.gameObject.SetActive(false);
        customScreen.transform.localPosition -= new Vector3(0, 0, 10);


        customScreen.SetOneButton();
        customScreen.titleTexxt.text = Localizer.Translate("TranslationUpdate.UpdateTitle", "Translation Update");
        customScreen.bodyText.text = Localizer.Translate("TranslationUpdate.UpdateMessage", "The translation files have been downloaded. Please restart the game for them to apply and take effect correctly.");
        customScreen.button1Trans.gameObject.SetActive(false);
        customScreen.gameObject.SetActive(true);
        while (true)
        {
            yield return null;
        }
    }
}