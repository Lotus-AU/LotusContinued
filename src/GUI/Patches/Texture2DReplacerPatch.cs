using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Il2CppInterop.Runtime;
using Lotus.Extensions;
using Lotus.Logging;
using UnityEngine;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;
using VentLib.Utilities.Optionals;

namespace Lotus.GUI.Patches;

public class Texture2DReplacerPatch
{
    public static readonly string[] ReplacedGraphics =
    [
        "GameSettingReworkAssets", "LobbyReworkAssets", "MainMenuSprites",
        "OldButtons", "Online_EnterCode_CreateLobby", "UiButtons"
    ];

    public const string StartPath = "Replaced/";

    public static void CheckForTextures()
    {
        Resources.FindObjectsOfTypeAll(Il2CppType.Of<Texture2D>())
            .ForEach(obj =>
            {
                if (!obj.TryCast<Texture2D>(out var texture)) return;
                if (texture == null) return;
                if (!ReplacedGraphics.Contains(texture.name)) return;

                Sprite spriteTexture = LotusAssets.LoadAsset<Sprite>(StartPath + texture.name + ".png");
                if (spriteTexture == null || spriteTexture.texture == null) return;
                DevLogger.Log($"Replacing {texture.name}.");

                if (OperatingSystem.IsAndroid()) ReplaceTextureAndroid(spriteTexture, texture);
                else ReplaceTextureWindows(spriteTexture, texture);
            });
    }

    private static void ReplaceTextureAndroid(Sprite spriteTexture, Texture2D texture)
    {
        try
        {
            Texture2D src = spriteTexture.texture;

            RenderTexture rt = RenderTexture.GetTemporary(
                texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            Graphics.CopyTexture(rt, 0, 0, texture, 0, 0);
            RenderTexture.ReleaseTemporary(rt);

            texture.filterMode = FilterMode.Bilinear;
            texture.name += "_Replaced";
        }
        catch (Exception e)
        {
            DevLogger.Log($"Error replacing texture: {e.Message}");
            throw;
        }
    }

    private static void ReplaceTextureWindows(Sprite spriteTexture, Texture2D texture)
    {
        try
        {
            Graphics.CopyTexture(spriteTexture.texture, texture);
            texture.filterMode = FilterMode.Bilinear;

            texture.name += "_Replaced"; // Makes it so we don't override the same texture twice
        }
        catch (Exception)
        {
            DevLogger.Log("Error replacing texture.");
            throw;
        }
    }

    [QuickPostfix(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    private static void SettingsStartPostfix(GameSettingMenu __instance)
    {
        __instance.FindChild<SpriteRenderer>("LeftSideTint", true).gameObject.SetActive(false);
        CheckForTextures();
    }

    [QuickPostfix(typeof(HudManager), nameof(HudManager.Start))] private static void HudManagerPostfix(HudManager __instance) => CheckForTextures();
    [QuickPostfix(typeof(AccountManager), nameof(AccountManager.Awake))] private static void AccountManagerPostfix(AccountManager __instance) => CheckForTextures();
    [QuickPostfix(typeof(MainMenuManager), nameof(MainMenuManager.Awake))] private static void MainMenuManagerPostfix(MainMenuManager __instance) => CheckForTextures();
    [QuickPostfix(typeof(ProgressTracker), nameof(ProgressTracker.Start))] private static void ProgressTrackerPostfix(ProgressTracker __instance) => CheckForTextures();
    [QuickPostfix(typeof(LobbyViewSettingsPane), nameof(LobbyViewSettingsPane.Awake))] private static void LobbyViewSettingsPanePostfix(LobbyViewSettingsPane __instance) => CheckForTextures();
}