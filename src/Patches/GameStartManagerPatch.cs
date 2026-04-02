using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using Lotus.API;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Options.General;
using Lotus.Options;
using UnityEngine;
using VentLib.Utilities.Extensions;
using VentLib.Utilities;
using Lotus.Logging;
using Lotus.Roles;
using Lotus.Roles.Managers.Interfaces;
using Lotus.GenTranslations;
using VentLib.Localization.Attributes;

namespace Lotus.Patches;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class GameStartManagerUpdatePatch
{
    public static void Prefix(GameStartManager __instance)
    {
        __instance.MinPlayers = 1;
    }
}
//タイマーとコード隠し
public static class GameStartManagerPatch
{
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public class GameStartManagerStartPatch
    {
        public static TMPro.TextMeshPro HideName;
        public static void Postfix(GameStartManager __instance)
        {
            __instance.GameRoomNameCode.text = GameCode.IntToGameName(AmongUsClient.Instance.GameId);

            HideName = Object.Instantiate(__instance.GameRoomNameCode, __instance.GameRoomNameCode.transform);
            HideName.text = Color.white.Colorize($"******");

            // Make Public Button
            /*if ((ModUpdater.isBroken || ModUpdater.hasUpdate || !TOHPlugin.AllowPublicRoom) && !ModUpdater.ForceAccept)
            {
                __instance.MakePublicButton.color = Palette.DisabledClear;
                __instance.privatePublicText.color = Palette.DisabledClear;
            }*/
        }
    }
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public class GameStartManagerUpdatePatch
    {
        public static void Prefix(GameStartManager __instance)
        {
            // Lobby code
            if (DataManager.Settings.Gameplay.StreamerMode && AmongUsClient.Instance.NetworkMode is NetworkModes.OnlineGame)
            {
                __instance.GameRoomNameCode.color = new(255, 255, 255, 0);
                GameStartManagerStartPatch.HideName.enabled = true;
            }
            else
            {
                __instance.GameRoomNameCode.color = new(255, 255, 255, 255);
                GameStartManagerStartPatch.HideName.enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
    public static class HiddenTextPatch
    {
        private static void Postfix(TextBoxTMP __instance)
        {
            if (__instance.name == "GameIdText") __instance.outputText.text = new string('*', __instance.text.Length);
        }
    }
}
[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
public class GameStartRandomMap
{
    public static bool Prefix(GameStartManager __instance)
    {
        if (__instance.startState != GameStartManager.StartingStates.NotStarting)
        {
            return false;
        }
        if (GeneralOptions.AdminOptions.HostGM) LogManager.SendInGame("[Info] GM is Enabled");

        PetWarning.CheckForPets();
        HostRoleOptionsWarning.CheckForOptions();

        __instance.ReallyBegin(false);
        return false;
    }
    public static bool Prefix(GameStartRandomMap __instance)
    {
        AUSettings.StaticOptions.SetFloat(FloatOptionNames.ProtectionDurationSeconds, 3600f);
        AUSettings.StaticOptions.SetBool(BoolOptionNames.ImpostorsCanSeeProtect, false);
        if (!GeneralOptions.MiscellaneousOptions.UseRandomMap) return true;

        List<byte> randomMaps = new();

        AuMap map = GeneralOptions.MiscellaneousOptions.RandomMaps;
        if (map.HasFlag(AuMap.Skeld)) randomMaps.Add(0);
        if (map.HasFlag(AuMap.Mira)) randomMaps.Add(1);
        if (map.HasFlag(AuMap.Polus)) randomMaps.Add(2);
        if (map.HasFlag(AuMap.Airship)) randomMaps.Add(4);

        if (randomMaps.Count == 0) return true;

        AUSettings.StaticOptions.SetByte(ByteOptionNames.MapId, randomMaps.GetRandom());
        return true;
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
class ResetStartStatePatch
{
    public static void Prefix()
    {
        if (GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown)
        {
            PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameOptionsManager.Instance.CurrentGameOptions, false));
        }
    }
}
[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
class UnrestrictedNumImpostorsPatch
{
    public static bool Prefix(ref int __result)
    {
        __result = ProjectLotus.NormalOptions.NumImpostors;
        return false;
    }
}