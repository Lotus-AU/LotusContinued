using System;
using HarmonyLib;
using UnityEngine;
using Lotus.GUI;
using Lotus.Utilities;

namespace Lotus.Options.Patches;

[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
public static class GameSettingsStartPatch
{
    public static void Postfix(GameSettingMenu __instance)
    {
        Transform background = __instance.transform.Find("Background");
        SpriteRenderer plBackground = background.parent.gameObject.QuickComponent<SpriteRenderer>("PLBackground",
            background.localPosition + new Vector3(0f, 0.2f, 0f));
        plBackground.sprite = LotusAssets.LoadSprite("PLBackground-Upscale.png", OperatingSystem.IsWindows() ? 190 : 155);
        plBackground.gameObject.layer = LayerMask.NameToLayer("UI");
        background.gameObject.SetActive(false);
    }
}