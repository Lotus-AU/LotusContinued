using System.Reflection;
using System.Text;
using HarmonyLib;
using Lotus.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentLib.Utilities.Harmony.Attributes;
using Object = UnityEngine.Object;

namespace Lotus.GUI.Patches;

public static class CreditsControllerPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(CreditsControllerPatch));

    private static string GetCreditsText()
    {
        string creditsText = "";

        System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Lotus.assets.Credits.plcredits.txt")!;
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);

        creditsText = Encoding.UTF8.GetString(buffer);
        return creditsText;
    }
    private static void PassCreditsController(GameObject mainObject)
    {
        mainObject.transform.parent.FindChild("FollowUs").gameObject.SetActive(false); // nobody is following my facebook!!!!!! 😡😡
        // mainObject.transform.localPosition -= new Vector3(0, 5, 0);

        // now we have to add a sprite renderer.
        var creditsBG = new GameObject("CreditsBackground");
        creditsBG.transform.SetParent(mainObject.transform.parent);
        creditsBG.transform.localPosition = mainObject.transform.localPosition;
        creditsBG.transform.localScale = Vector3.one;

        creditsBG.transform.localPosition += new Vector3(0.1f, 0, 0);

        var renderer = creditsBG.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetLoader.LoadLotusSprite("Credits.Images.background.png", 180);
    }

    [QuickPrefix(typeof(CreditsController), nameof(CreditsController.LoadCredits))]
    public static void LoadCreditsPrefix(CreditsController __instance) => __instance.CSVCredits = new TextAsset(GetCreditsText());

    [QuickPrefix(typeof(CreditsController), nameof(CreditsController.Start))]
    public static bool StartPrefix(CreditsController __instance)
    {
        __instance.initialDelay = 1f;
        __instance.remainingDelay = __instance.initialDelay;
        log.Debug("First Credit: " + __instance.credits[0].columns[0]);
        GameObject mainObject = __instance.gameObject;
        PassCreditsController(mainObject);
        for (int i = 0; i < __instance.credits.Count; i++)
        {
            CreditsController.FormatStruct format = __instance.GetFormat(__instance.credits[i].format);
            GameObject gameObject2 = Object.Instantiate<GameObject>(__instance.creditPanelPrefab, __instance.creditMainPanel.transform);
            for (int j = 0; j < format.formatPrefabs.Count; j++)
            {
                GameObject gameObject3 = Object.Instantiate<GameObject>(format.formatPrefabs[j], gameObject2.transform);
                if (format.creditType == CreditsController.CreditType.TEXT)
                {
                    string text = __instance.credits[i].columns[j].Trim();
                    if (text.IsNullOrWhiteSpace())
                    {
                        gameObject3.GetComponent<TextMeshProUGUI>().text = string.Empty;
                    }
                    else
                    {
                        TextMeshProUGUI component = gameObject3.GetComponent<TextMeshProUGUI>();
                        component.text += text;
                    }
                }
                else if (format.creditType == CreditsController.CreditType.IMAGE)
                {
                    string creditsPath = __instance.credits[i].columns[j];
                    if (!creditsPath.Contains("Lotus.assets"))
                        gameObject3.GetComponent<Image>().sprite = Resources.Load<Sprite>("Credits/" + __instance.credits[i].columns[j]);
                    else
                    {
                        string[] assetSplit = creditsPath.Split(";;");
                        gameObject3.GetComponent<Image>().sprite = AssetLoader.LoadSprite(assetSplit[1], float.Parse(assetSplit[0]), true);
                    }
                    gameObject3.GetComponent<Image>().preserveAspect = true;
                }
            }
        }
        return false;
    }
}