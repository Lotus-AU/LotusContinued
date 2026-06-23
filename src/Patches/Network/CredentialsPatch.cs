using System;
using System.Linq;
using HarmonyLib;
using Lotus.Addons;
using Lotus.Managers.Date;
using TMPro;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using Object = UnityEngine.Object;

namespace Lotus.Patches.Network;

[Localized("PingDisplay")]
[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public class VersionShowerStartPatch
{
    [Localized("AddonsLoaded")]
    private static string addonsLoaded = "{0} Addons Loaded";


    //private static readonly ColorGradient LotusGradient = new(new Color(1f, 0.93f, 0.98f), new Color(1f, 0.57f, 0.73f));
    //private static readonly ColorGradient LotusGradient = new(new Color(1f, 0.76f, 0.44f), new Color(0.59f, 0.92f, 0.89f));
    private static Color _endColor = new Color(0.59f, 0.92f, 0.89f);
    private static Color _startColor = new Color(1f, 0.58f, 0.36f);
    private static bool _init = false;

    public static TextMeshPro? SpecialEventText;
    static void Postfix(VersionShower __instance)
    {
        ProjectLotus.CredentialsText = "\r\n";
#if DEBUG
        ProjectLotus.CredentialsText += $"v{ProjectLotus.PluginVersion}";
        ProjectLotus.CredentialsText += $" ({ProjectLotus.DevVersionStr})\n";
#endif

        ProjectLotus.CredentialsText += $"{_startColor.Colorize(ProjectLotus.ModName)}";
#if DEBUG
        ProjectLotus.CredentialsText += $": {_endColor.Colorize($"{ProjectLotus.Instance.Version().Branch}({ProjectLotus.Instance.Version().CommitNumber})")}";
#else
        ProjectLotus.CredentialsText += $" v{ProjectLotus.PluginVersion}";
#endif

        int addonCount = AddonManager.Addons.Count;
        if (addonCount > 0)
            ProjectLotus.CredentialsText += $"\r\n{new Color(1f, 0.67f, 0.37f).Colorize($"{addonCount} {addonsLoaded}")}";


        var credentials = Object.Instantiate(__instance.text);
        credentials.fontSize = 1.5f;
        credentials.text = ProjectLotus.CredentialsText;
        credentials.alignment = TextAlignmentOptions.TopRight;
        credentials.transform.position = OperatingSystem.IsWindows() ? new Vector3(3.3f, 3f, 0) :  new Vector3(4.3f, 3f, 0);


        if (SpecialEventText == null)
        {
            SpecialEventText = Object.Instantiate(__instance.text);
            SpecialEventText.text = "";
            SpecialEventText.color = Color.white;
            SpecialEventText.fontSize += 2.5f;
            SpecialEventText.alignment = TMPro.TextAlignmentOptions.Top;
            SpecialEventText.transform.position = new Vector3(0, 0.5f, 0);
        }

        /*SpecialEventText.enabled = SplashPatch.AmongUsLogo != null;*/
        if (!_init)
            ISpecialDate.CheckDates();
        _init = true;
    }
}

[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();
    }

    public static void Postfix(ModManager __instance)
    {
        var offsetY = HudManager.InstanceExists ? 1.8f : 0.9f;
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
            __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
            new Vector3(0.4f, offsetY, __instance.localCamera.nearClipPlane + 0.1f));
    }
}