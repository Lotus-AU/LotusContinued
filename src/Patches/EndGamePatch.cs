using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Logging;
// using Lotus.RPC.CustomObjects;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Patches;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
class EndGamePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(EndGamePatch));

    public static Dictionary<byte, string> SummaryText = new();

    public static string KillLog = "";

    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Game.Cleanup();
        // CustomNetObject.Reset();

        SummaryText = new();
        /*foreach (var id in TOHPlugin.PlayerStates.Keys)
            SummaryText[id] = Utils.SummaryTexts(id, disableColor: false);*/
        KillLog = ": ";/*GetString("KillLog") + ":";*/
        /*foreach (var kvp in TOHPlugin.PlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
        {
            var date = kvp.Value.RealKiller.Item1;
            if (date == DateTime.MinValue) continue;
            var killerId = kvp.Value.GetRealKiller();
            var targetId = kvp.Key;
            /*KillLog += $"\n{date:T} {TOHPlugin.AllPlayerNames[targetId]}({Utils.GetDisplayRoleName(targetId)}{Utils.GetSubRolesText(targetId)}) [{Utils.GetVitalText(kvp.Key)}]";#1#
            if (killerId != byte.MaxValue && killerId != targetId)
                KillLog += $"\n\t\t⇐ {TOHPlugin.AllPlayerNames[killerId]}({Utils.GetDisplayRoleName(killerId)}{Utils.GetSubRolesText(killerId)})";
        }*/
        SelectRolesPatch.desyncedIntroText = new();

        KillLog = "asdoksdoksadpsako";
        log.Info("-----------ゲーム終了----------- Phase");
    }
}