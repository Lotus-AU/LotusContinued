using AmongUs.Data;
using HarmonyLib;
using Discord;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.Network;
using UnityEngine;

namespace Lotus.Discord.Patches;

[HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
public class DiscordPatch
{
    private static string _details = "";
    private static string _state = "";

    private static string _gameCode = "";
    private static string _gameRegion = "";

    public static void Prefix(ref Activity activity)
    {
        activity.Assets = new ActivityAssets
        {
            LargeImage = "https://avatars.githubusercontent.com/u/173427715?s=1400&v=4",
            LargeText = $"{ProjectLotus.ModName}" + (ProjectLotus.DevVersion ? $" v{ProjectLotus.CompileVersion} [DEV]" : "v" + ProjectLotus.VisibleVersion),
            SmallImage = "https://lotusau.top/lilypad/rpc_icon.png",
            SmallText = $"Among Us v{GetVersion()}",
        };

        string details = $"{ProjectLotus.ModName} " + (ProjectLotus.DevVersion ? ProjectLotus.DevVersionStr : $"v{ProjectLotus.VisibleVersion}");
        string state = activity.State;

        bool inMenuOrFreeplay = activity.State is "In Menus" or "In Freeplay";
        bool disabled = DataManager.Settings.Gameplay.StreamerMode;

        if (!disabled && !inMenuOrFreeplay && AmongUsClient.Instance.GameId != 32)
        {
            string gameCode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
            string gameRegion = ConnectionManager.GetRegionAbbreviation(ServerManager.Instance.CurrentRegion.Name);

            details = $"Lotus v{ProjectLotus.VisibleVersion}";
            if (gameCode != "" || gameRegion != "")
                details += $" - ({gameCode}) | ({gameRegion})";

            state = GetStateMessage(Game.State);
            if (Game.State is not GameState.InLobby)
                state += $" - ({GameData.Instance.PlayerCount}/{GameManager.Instance.LogicOptions.MaxPlayers})";
        }

        activity.Details = details;

        if (!disabled)
            activity.State = state;
    }

    private static string GetStateMessage(GameState state)
    {
        return state switch
        {
            GameState.Roaming => $"Roaming {(MapNames)ProjectLotus.NormalOptions.MapId}",
            GameState.InIntro => $"Roaming {(MapNames)ProjectLotus.NormalOptions.MapId}",
            GameState.InLobby => "Waiting in Lobby",
            _ => "Idle"
        };
    }

    private static string GetVersion() => ReferenceDataManager.Instance?.Refdata?.userFacingVersion ?? Application.version;
}