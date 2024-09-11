using System;
using System.Collections.Generic;
using HarmonyLib;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Managers;
using Lotus.Roles.Managers.Interfaces;
using Lotus.Options;
using VentLib.Utilities.Extensions;
using UnityEngine;
using Lotus.Utilities;
// using Lotus.RPC.CustomObjects;
using Lotus.Patches.Systems;

namespace Lotus.Patches;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
class CoStartGamePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(CoStartGamePatch));

    public static void Prefix(AmongUsClient __instance)
    {
        SetRolePatch.RoleAssigned = new();
        Players.GetPlayers().ForEach(player =>
        {
            if (player == null) return;
            SetRolePatch.RoleAssigned[player.PlayerId] = false;
            if (!GeneralOptions.MiscellaneousOptions.ColoredNameMode) return;
            string colorName = player.Data.ColorName.Trim('(', ')');
            player.RpcSetName(colorName);
            Api.Local.SetName(player, colorName, true);
        });

        Game.Setup();
        Game.CurrentGameMode.Setup();
        RpcSetTasksPatch.OnGameStart();
        log.Trace("Setup Game!");
    }

    public static void Postfix(AmongUsClient __instance)
    {
        ProjectLotus.ResetCamPlayerList = new List<byte>();
        // CustomNetObject.Reset();
        FallFromLadder.Reset();

        try
        {
            Game.State = GameState.InIntro;
            Players.GetPlayers().Do(p => Game.MatchData.Roles.MainRoles[p.PlayerId] = IRoleManager.Current.FallbackRole());
            Players.GetPlayers().Do(p => Game.MatchData.Roles.SubRoles[p.PlayerId] = new List<Roles.CustomRole>());
        }
        catch (Exception exception)
        {
            FatalErrorHandler.ForceEnd(exception, "Setup Phase");
        }
    }
}