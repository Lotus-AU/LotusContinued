using System.Collections.Generic;
using HarmonyLib;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Patches;

static class WrongGameTypePatch
{
    private static readonly List<AmongUs.GameOptions.GameModes> lastGamemodes = [];

    [QuickPostfix(typeof(GameOptionsManager), nameof(GameOptionsManager.SwitchGameMode))]
    public static void LogLastThreeGameModes(GameOptionsManager __instance, AmongUs.GameOptions.GameModes gameMode)
    {
        if (gameMode is AmongUs.GameOptions.GameModes.None) return;
        lastGamemodes.Add(gameMode);
        if (lastGamemodes.Count > 3) lastGamemodes.RemoveAt(0);
    }

    [HarmonyPatch(typeof(GameManagerCreator), nameof(GameManagerCreator.CreateGameManager))]
    static class PatchGameManager
    {
        static void Prefix(ref AmongUs.GameOptions.GameModes mode)
        {
            if (!lastGamemodes[1].IsHns() && lastGamemodes[2].IsHns())
            {
                if (mode is AmongUs.GameOptions.GameModes.HideNSeek) mode = AmongUs.GameOptions.GameModes.Normal;
                else mode = AmongUs.GameOptions.GameModes.NormalFools;
                StaticLogger.Debug("overwrote hns mode to standard");
                GameOptionsManager.Instance.SwitchGameMode(mode);
            }
            else if (lastGamemodes[1].IsHns() && !lastGamemodes[2].IsHns())
            {
                if (mode is AmongUs.GameOptions.GameModes.Normal) mode = AmongUs.GameOptions.GameModes.HideNSeek;
                else mode = AmongUs.GameOptions.GameModes.SeekFools;
                StaticLogger.Debug("overwrote standard mode to hns");
                GameOptionsManager.Instance.SwitchGameMode(mode);
            }
        }

    }

    private static bool IsHns(this AmongUs.GameOptions.GameModes mode) =>
        mode is AmongUs.GameOptions.GameModes.HideNSeek or AmongUs.GameOptions.GameModes.SeekFools;
}

