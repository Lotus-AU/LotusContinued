using HarmonyLib;
using Lotus.Logging;
using UnityEngine;
using VentLib.Utilities.Extensions;

namespace Lotus.Patches.Hud;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetMapAndInfoButtonsEnabled))]
class SetMapAndInfoButtonsEnabledPatch
{
    public static void Postfix(HudManager __instance, bool enabled)
    {
        if (__instance.MatchInfoButton)
        {
            __instance.MatchInfoButton.gameObject.SetActive(false);
            __instance.MatchInfoButton.gameObject.
                FindChildOrEmpty<GameObject>("Inactive", true)
                .IfPresent(go => go.SetActive(false));
            __instance.MatchInfoButton.enabled = false;

        }
    }
}