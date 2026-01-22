using System;
using System.Collections;
using AmongUs.Data;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Lotus.Extensions;
using Lotus.Logging;
using TMPro;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Object = UnityEngine.Object;
using UnityEngine.Networking;
using System.IO;
using System.Linq;
using Lotus.Managers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text;
using Lotus.Addons;
using VentLib.Localization;

namespace Lotus.Network.PrivacyPolicy.Patches;
public class PrivacyPolicyPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PrivacyPolicyPatch));
    public const string PrivacyPolicyLink = "https://beta.lotusau.top/privacy/";

    private static DateTimeOffset LatestPrivacyPolicy = DateTimeOffset.MinValue;
    private static EditablePrivacyPolicyInfo _privacyInfo = null!;
    private static InfoTextBox customScreen = null!;

    private static IDeserializer deserializer = new DeserializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();

    private static ISerializer serializer = new SerializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();


    private static bool wait = true;
    private static bool failedAddons;

    [QuickPrefix(typeof(EOSManager), nameof(EOSManager.RunLogin))]
    public static void PreLogin()
    {
        InfoTextBox infoTextBox = DestroyableSingleton<AccountManager>.Instance.transform.Find("InfoTextBox").GetComponent<InfoTextBox>();
        customScreen = Object.Instantiate(infoTextBox);
        customScreen.transform.parent = infoTextBox.transform.parent;
        customScreen.gameObject.SetActive(false);
        customScreen.transform.localPosition -= new Vector3(0, 0, 10);
    }

    [QuickPostfix(typeof(EOSManager), nameof(EOSManager.RunLogin))]
    public static void EditLoginCoroutine(EOSManager __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        __result = PatchedCoroutine(__instance).WrapToIl2Cpp();
    }

    public static bool EditPrivacyPolicy(PrivacyPolicyEditType editType, object newValue)
    {
        switch (editType)
        {
            case PrivacyPolicyEditType.AnonymousBugReports:
                _privacyInfo.AnonymousBugReports = (bool)newValue;
                return Change();
            case PrivacyPolicyEditType.ConnectWithAPI:
                _privacyInfo.ConnectWithAPI = (bool)newValue;
                return Change();
            case PrivacyPolicyEditType.LobbyDiscovery:
                _privacyInfo.LobbyDiscovery = (bool)newValue;
                return Change();
        }
        return false;
        bool Change()
        {
            _privacyInfo.ToRealInfo();
            SavePrivacyPolicy();
            return true;
        }
    }

    private static void SavePrivacyPolicy()
    {
        FileInfo privacyFile = PluginDataManager.HiddenDataDirectory.GetFile("privacyPolicyInfo.yaml");

        string emptyFile = serializer.Serialize(PrivacyPolicyInfo.Instance);
        using FileStream stream = privacyFile.Open(FileMode.Create);
        stream.Write(Encoding.UTF8.GetBytes(emptyFile));
        stream.Close();
    }

    private static IEnumerator CustomCoroutine(EOSManager __instance)
    {
        log.Info("running patched couroutine");
        __instance.announcementsVisible = false;
        if (DataManager.Player.Account.LoginStatus == EOSManager.AccountLoginStatus.Offline)
        {
            __instance.IsAllowedOnline(false);
        }
        if (__instance.hasRunLoginFlow)
        {
            yield return CustomPrivacyPolicy();
            DestroyableSingleton<AccountManager>.Instance.privacyPolicyBg.gameObject.SetActive(false);
            DestroyableSingleton<AccountManager>.Instance.waitingText.gameObject.SetActive(false);
            if (DataManager.Player.Account.LoginStatus != EOSManager.AccountLoginStatus.Offline)
            {
                __instance.IsAllowedOnline(true);
            }
            if (AddonManager.FailedAddons.IsEmpty() || failedAddons)
            {
                if (customScreen) customScreen.Destroy();
                yield break;
            }
            failedAddons = true;
            customScreen.SetOneButton();
            customScreen.titleTexxt.text = "⚠⚠⚠";
            customScreen.bodyText.text = Localizer
                .Translate("PingDisplay.AddonsFailed", "{0} Addon(s) Failed to Load: {1}")
                .Formatted(AddonManager.FailedAddons.Count, AddonManager.FailedAddons.StrJoin(string.Empty).Trim());
            customScreen.button1Text.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Okay, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
            customScreen.gameObject.SetActive(true);
            yield break;
        }
        __instance.hasRunLoginFlow = true;
        __instance.loginFlowFinished = false;
        yield return DestroyableSingleton<AccountManager>.Instance.PrivacyPolicy.Show();
        DestroyableSingleton<AccountManager>.Instance.privacyPolicyBg.gameObject.SetActive(false);
        yield return CustomPrivacyPolicy();
        if (__instance.platformInitialized)
        {
            __instance.StartInitialLoginFlow();
        }
        else
        {
            DestroyableSingleton<AccountManager>.Instance.SetDLLErrorMode();
            DestroyableSingleton<AccountManager>.Instance.SignInFail(EOSManager.EOS_ERRORS.InterfaceInitFail, new Action(__instance.ContinueInOfflineMode));
        }
        while (!__instance.HasFinishedLoginFlow())
        {
            yield return null;
        }

        if (AddonManager.FailedAddons.IsEmpty() || failedAddons)
        {
            if (customScreen) customScreen.Destroy();
            yield break;
        }
        failedAddons = true;

        customScreen.SetOneButton();
        customScreen.titleTexxt.text = "⚠⚠⚠";
        customScreen.bodyText.text = Localizer
            .Translate("PingDisplay.AddonsFailed", "{0} Addon(s) Failed to Load: {1}")
            .Formatted(AddonManager.FailedAddons.Count, AddonManager.FailedAddons.StrJoin(string.Empty).Trim());
        customScreen.button1Text.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Okay, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        customScreen.gameObject.SetActive(true);
        yield break;
    }

    // private static IEnumerator CustomPrivacyPolicy()
    // {
    //     // Update text
    //     log.Info("CustomPrivacyPolicy");
    //     customScreen.HyperLinkText.Text = PrivacyPolicyText;
    //     // customScreen.OnTextUpdated();
    //     customScreen.FindChild<TextMeshPro>("TitleText_TMP", true).text = "Project: Lotus Privacy Policy";

    //     // modify buttons
    //     if (customScreen.AcceptButton.TryCast(out PassiveButton acceptButton))
    //     {
    //         log.Info("is PassiveButton!");
    //         acceptButton.Modify(() =>
    //         {
    //             customScreen.GetComponent<TransitionOpen>().Close();
    //         });
    //     }
    //     if (customScreen.ManageDataButton.TryCast(out PassiveButton manageButton))
    //     {
    //         log.Info("is PassiveButton!");
    //         manageButton.Modify(() =>
    //         {
    //             customScreen.GetComponent<TransitionOpen>().Close();
    //         });
    //     }

    //     // yields until it closes
    //     int lastAccepted = DataManager.Player.Onboarding.LastAcceptedPrivacyPolicyVersion;
    //     DataManager.Player.Onboarding.LastAcceptedPrivacyPolicyVersion = 0;
    //     yield return customScreen.Show();
    //     DataManager.Player.Onboarding.LastAcceptedPrivacyPolicyVersion = lastAccepted;
    //     customScreen.Destroy();
    // }
    private static IEnumerator CustomPrivacyPolicy()
    {
        // Update text
        log.Info("CustomPrivacyPolicy");

        yield return TryLoadPrivacyInfo();

        bool hasUpdatedPrivacyPolicy = false;

        if (_privacyInfo != null)
        {
            DateTimeOffset lastAcceptedVersionTime = DateTimeOffset.FromUnixTimeSeconds(_privacyInfo.LastAcceptedPrivacyPolicyVersion);
            hasUpdatedPrivacyPolicy = lastAcceptedVersionTime < LatestPrivacyPolicy;
            if (!hasUpdatedPrivacyPolicy)
            {

                yield break;
            }
        }

        customScreen.SetText(hasUpdatedPrivacyPolicy ? PrivacyPolicyTranslations.UpdatedPrivacyPolicy : PrivacyPolicyTranslations.PrivacyPolicyText);
        customScreen.titleTexxt.text = PrivacyPolicyTranslations.PrivacyPolicyTitle;

        customScreen.button1Text.text = PrivacyPolicyTranslations.AcceptButtonText;
        customScreen.button2Text.text = PrivacyPolicyTranslations.DisagreeButtonText;

        int choice = 0;

        customScreen.button1.Modify(Close);
        customScreen.button2.Modify(() =>
        {
            choice = 1;
            Close();
        });

        customScreen.gameObject.SetActive(true);
        Async.Schedule(() =>
        {
            // customScreen.SetTwoButtons(); doesnt work for some reason. so we just have our own below
            (PassiveButton, TextMeshPro) thirdButtonInfo = customScreen.SetThreeButtons();
            customScreen.button2Trans = customScreen.button2.transform;

            customScreen.button2Trans.gameObject.SetActive(true);
            customScreen.button1Trans.localPosition = new Vector3(2f, customScreen.button1Trans.localPosition.y, 0);
            customScreen.button2Trans.localPosition = new Vector3(-2f, customScreen.button2Trans.localPosition.y, 0);
            thirdButtonInfo.Item1.Modify(() =>
            {
                choice = 2;
                Close();
            });
            thirdButtonInfo.Item2.text = PrivacyPolicyTranslations.CustomizeButtonText;
        }, 0.1f);
        while (customScreen.gameObject.activeSelf)
        {
            // yield until closed
            yield return null;
        }

        if (wait) yield return new WaitForSeconds(1);
        wait = false;

        if (choice == 2)
        {
            _privacyInfo = new() { LastAcceptedPrivacyPolicyVersion = LatestPrivacyPolicy.ToUnixTimeSeconds() };
            _privacyInfo.ToRealInfo();
            // create options menu behaviour
            DestroyableSingleton<MainMenuManager>.Instance.settingsButton.OnClick.Invoke();
            while (DestroyableSingleton<OptionsMenuBehaviour>.Instance.gameObject.activeSelf)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1);
        }

        customScreen.SetOneButton();
        customScreen.button1Text.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Okay, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        customScreen.titleTexxt.text = PrivacyPolicyTranslations.ManageData;
        customScreen.SetText(choice switch
        {
            0 => PrivacyPolicyTranslations.AcceptText,
            1 => PrivacyPolicyTranslations.DisagreeText,
            2 => PrivacyPolicyTranslations.CustomizeText,
            _ => throw new ArgumentException($"choice ({choice}) is not between 0-2!")
        });

        // rawww. coding this is so hard.
        // GameObject reportIdentification = CreatePolicyChoice("Bug Report Identification",
        //     "When you submit a bug report using /report, we collect your Among Us Friend Code and in-game name to identify the report sender and prevent abuse.",
        //     policyInfo.AnonymousBugReports ? "YES" : "NO", gameObj =>
        // {
        //     policyInfo.AnonymousBugReports = !policyInfo.AnonymousBugReports;
        //     gameObj.FindChild<TextMeshPro>("Button/Text_TMP").text = policyInfo.AnonymousBugReports ? "YES" : "NO";
        // });
        // GameObject lobbyDiscovery = CreatePolicyChoice("Automatic Lobby Discovery",
        //     "We collect your Game ID, Game Code, Region, Mod Name & Version, and Host Name to automatically post public lobbies to the \"Lobby Discovery\" service.",
        //     policyInfo.LobbyDiscovery ? "ON" : "OFF", gameObj =>
        // {
        //     policyInfo.LobbyDiscovery = !policyInfo.LobbyDiscovery;
        //     gameObj.FindChild<TextMeshPro>("Button/Text_TMP").text = policyInfo.LobbyDiscovery ? "ON" : "OFF";
        // });
        // GameObject consentChoice = CreatePolicyChoice("Consent to Data Collection", "Adjust your consent for what data we collect during gameplay.",
        //     policyInfo.ConnectWithAPI ? "YES" : "NO", gameObj =>
        // {
        //     policyInfo.ConnectWithAPI = !policyInfo.ConnectWithAPI;
        //     reportIdentification.SetActive(policyInfo.ConnectWithAPI);
        //     lobbyDiscovery.SetActive(policyInfo.ConnectWithAPI);
        //     gameObj.FindChild<TextMeshPro>("Button/Text_TMP").text = policyInfo.ConnectWithAPI ? "YES" : "NO";
        // });

        // consentChoice.transform.localPosition = new Vector3(-2.3f, .7f, 0);
        // reportIdentification.transform.localPosition = new Vector3(-2.3f, 0f, 0);
        // lobbyDiscovery.transform.localPosition = new Vector3(-2.3f, -7f, 0);

        customScreen.gameObject.SetActive(true);

        while (customScreen.gameObject.activeSelf)
        {
            // yield until closed
            yield return null;
        }

        EditablePrivacyPolicyInfo policyInfo;

        if (choice == 2) policyInfo = _privacyInfo!;
        else policyInfo = new()
        {
            LastAcceptedPrivacyPolicyVersion = LatestPrivacyPolicy.ToUnixTimeSeconds(),
            ConnectWithAPI = choice switch
            {
                2 => false,
                _ => choice == 0
            },
            AnonymousBugReports = choice switch
            {
                2 => false,
                _ => choice == 1
            },
            LobbyDiscovery = choice switch
            {
                2 => false,
                _ => choice == 0
            }
        };

        _privacyInfo = policyInfo;
        _privacyInfo.ToRealInfo();
        SavePrivacyPolicy();

        void Close() => customScreen.GetComponent<TransitionOpen>().Close();
        GameObject CreatePolicyChoice(string name, string description, string defaultText, Action<GameObject> onClick)
        {
            // this code is terrible. But no one said I was a UI designer.
            GameObject policyChoice = Object.Instantiate(customScreen.transform, customScreen.transform).gameObject;
            policyChoice.name = "PolicyChoice";
            policyChoice.GetComponents<Component>().ForEach(c =>
            {
                c.Destroy();
            });
            policyChoice.transform.DestroyChildren();
            policyChoice.gameObject.SetActive(true);

            // OnClick button
            PassiveButton button = Object.Instantiate(customScreen.button1, policyChoice.transform);
            button.transform.localPosition = Vector3.zero;
            button.transform.localScale = new Vector2(.15f, 1);
            button.FindChild<TextMeshPro>("Text_TMP").text = defaultText;
            button.name = "Button";
            button.Modify(() => onClick(policyChoice));

            // NameText
            TextMeshPro nameText = Object.Instantiate(customScreen.titleTexxt, policyChoice.transform);
            nameText.transform.localPosition = new Vector3(1f, 0, 0);
            nameText.name = "NameText";
            nameText.fontSizeMax = 3;
            nameText.text = name;

            TextMeshPro descriptionText = Object.Instantiate(customScreen.titleTexxt, policyChoice.transform);
            descriptionText.transform.localPosition = new Vector3(1f, 1f, 0);
            descriptionText.name = "DescriptionText";
            descriptionText.color = Color.gray;
            descriptionText.text = description;
            descriptionText.fontSizeMax = 1;

            return policyChoice;
        }
    }

    private static IEnumerator GetLatestPrivacyVersion()
    {
        if (_privacyInfo != null)
        {
            if (!_privacyInfo.ConnectWithAPI) yield break;
        }
        UnityWebRequest webRequest = new(ModConstants.WebsiteLink + "api/privacypolicy", UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };

        yield return webRequest.SendWebRequest();

        switch (webRequest.result)
        {
            case UnityWebRequest.Result.Success:
                long unixTimestamp;
                if (long.TryParse(webRequest.downloadHandler.text, out unixTimestamp))
                {
                    // Convert Unix timestamp to DateTime
                    DateTimeOffset dateTime;
                    try
                    {
                        dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
                        log.Info("Server returned Unix timestamp converted to DateTime: " + dateTime.DateTime);
                    }
                    catch (Exception e)
                    {
                        dateTime = DateTimeOffset.MinValue;
                        log.Exception($"Error occured while parsing server response. Resposnse: {webRequest.downloadHandler.text}", e);
                        log.Exception(e);
                    }
                    LatestPrivacyPolicy = dateTime;
                }
                else
                {
                    log.Exception($"Failed to parse the server response as Unix timestamp. Response: {webRequest.downloadHandler.text}");
                    LatestPrivacyPolicy = DateTimeOffset.MinValue;
                }
                break;
            default:
                LatestPrivacyPolicy = DateTimeOffset.MinValue;
                log.Info("Result: {0} - Error: {1} - ResponseCode: {2} - Server Response: {3}".Formatted(webRequest.result.ToString(),
                    webRequest.error, webRequest.responseCode, webRequest.downloadHandler.text));
                break;
        }
        yield break;
    }

    private static IEnumerator TryLoadPrivacyInfo()
    {
        FileInfo privacyFile = PluginDataManager.HiddenDataDirectory.GetFile("privacyPolicyInfo.yaml");
        if (!privacyFile.Exists) yield break;
        string content;
        using (StreamReader reader = new(privacyFile.Open(FileMode.OpenOrCreate))) content = reader.ReadToEnd();
        _privacyInfo = deserializer.Deserialize<EditablePrivacyPolicyInfo>(content);
        _privacyInfo.ToRealInfo();
        yield break;
    }

    private static IEnumerator PatchedCoroutine(EOSManager __instance)
    {
        yield return GetLatestPrivacyVersion();
        yield return CustomCoroutine(__instance);
    }

    private class EditablePrivacyPolicyInfo()
    {
        public bool ConnectWithAPI;
        public bool AnonymousBugReports;
        public bool LobbyDiscovery;
        public long LastAcceptedPrivacyPolicyVersion;
        public PrivacyPolicyInfo ToRealInfo() => new((ConnectWithAPI, LobbyDiscovery, AnonymousBugReports, LastAcceptedPrivacyPolicyVersion));

        public static EditablePrivacyPolicyInfo FromRealInfo() => new()
        {
            LastAcceptedPrivacyPolicyVersion = _privacyInfo.LastAcceptedPrivacyPolicyVersion,
            AnonymousBugReports = _privacyInfo.AnonymousBugReports,
            ConnectWithAPI = _privacyInfo.ConnectWithAPI,
            LobbyDiscovery = _privacyInfo.LobbyDiscovery,
        };
    }
}

public enum PrivacyPolicyEditType
{
    ConnectWithAPI,
    AnonymousBugReports,
    LobbyDiscovery
}

[Localized("PrivacyPolicy")]
public static class PrivacyPolicyTranslations
{
    [Localized(nameof(PrivacyPolicyText))] public static string PrivacyPolicyText = "Project Lotus has a few optional features that require sharing data with online services, like Discord lobbies, cheater ban lists and bug reports. Check our <link=https://beta.lotusau.top/privacy/>Privacy Policy</link> for more details. Do you want to enable these extra features and allow us to process your data?";
    [Localized(nameof(UpdatedPrivacyPolicy))] public static string UpdatedPrivacyPolicy = "We have revised our <link=https://beta.lotusau.top/privacy/>Privacy Policy</link>. We encourage you to review it and update your preferences accordingly. Do you still allow us to process your data?";
    [Localized(nameof(DisagreeText))] public static string DisagreeText = "All online requests have been turned off. You can enable specific ones through the Settings menu.";
    [Localized(nameof(AcceptText))] public static string AcceptText = "All online requests have been turned on. You can disable specific ones through the Settings menu.";
    [Localized(nameof(CustomizeText))] public static string CustomizeText = "Your preferences have been saved. You can edit them any time through the Settings menu.";
    [Localized(nameof(PrivacyPolicyTitle))] public static string PrivacyPolicyTitle = "Project: Lotus Privacy Policy";
    [Localized(nameof(CustomizeButtonText))] public static string CustomizeButtonText = "Customize";
    [Localized(nameof(ManageData))] public static string ManageData = "Manage Data Collection";
    [Localized(nameof(DisagreeButtonText))] public static string DisagreeButtonText = "No";
    [Localized(nameof(AcceptButtonText))] public static string AcceptButtonText = "Yes";
}