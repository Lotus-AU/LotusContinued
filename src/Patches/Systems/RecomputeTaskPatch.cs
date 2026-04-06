using System;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Roles.Interfaces;
using Lotus.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Patches.Systems;

[HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
public class RecomputeTaskPatch
{
    public static bool Prefix(GameData __instance)
    {
        if (!Game.InGameStates.Contains(Game.State)) return true;
        __instance.TotalTasks = 0;
        __instance.CompletedTasks = 0;
        __instance.AllPlayers.ToArray()
            .Where(Utils.HasTasks)
            .SelectMany(p => p?.Tasks?.ToArray() ?? Array.Empty<NetworkedPlayerInfo.TaskInfo>())
            .ForEach(task =>
            {
                __instance.TotalTasks++;
                if (task.Complete) __instance.CompletedTasks++;
            });

        return false;
    }
}