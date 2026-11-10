using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Logging;
using Lotus.Patches;
using Lotus.RPC.CustomObjects;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Victory.Patches;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
class EndGamePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(EndGamePatch));

    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        Game.Cleanup();
        CustomNetObject.Reset();
        Game.MatchData.EndTime = DateTime.Now;

        log.Info("-----------Game End----------- Phase");
    }
}