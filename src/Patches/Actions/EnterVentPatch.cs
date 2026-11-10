using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Lotus.API.Odyssey;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.API.Stats;
using Lotus.Extensions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using UnityEngine;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using Lotus.Patches.Client;

namespace Lotus.Patches.Actions;

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
public static class EnterVentPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(EnterVentPatch));
    private static Dictionary<byte, int> LastEnteredVent = [];

    public static int GetLastEnteredVent(this PlayerControl player) => LastEnteredVent.GetValueOrDefault(player.PlayerId, -1);

    public static void Reset() => LastEnteredVent = [];

    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        log.Trace($"{pc.GetNameWithRole()} Entered Vent (ID: {__instance.Id})", "CoEnterVent");
        CustomRole role = pc.PrimaryRole();

        if (Game.CurrentGameMode.BlockedActions().HasFlag(GameModes.BlockableGameAction.EnterVent) || !UseVentPatch.CanUseVent(pc, role, __instance))
        {
            log.Trace($"{pc.GetNameWithRole()} cannot enter vent. Booting.");
            Async.Schedule(() => pc.MyPhysics.RpcBootFromVent(__instance.Id), 0.01f);
            return;
        }

        ActionHandle vented = ActionHandle.NoInit();
        RoleOperations.Current.Trigger(LotusActionType.VentEntered, pc, vented, __instance);
        if (vented.IsCanceled) Async.Schedule(() => pc.MyPhysics.RpcBootFromVent(__instance.Id), 0.4f);
        else
        {
            VanillaStatistics.TimesVented.Update(pc.PlayerId, i => i + 1);
            LastEnteredVent[pc.PlayerId] = __instance.Id;
        }
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
class ExitVentPatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        ActionHandle exitVent = ActionHandle.NoInit();
        RoleOperations.Current.Trigger(LotusActionType.VentExit, pc, exitVent, __instance);
        if (exitVent.IsCanceled)
            Async.Schedule(() => RpcV3.Immediate(pc.MyPhysics.NetId, RpcCalls.EnterVent, SendOption.Reliable).WritePacked(__instance.Id).Send(), 0.5f);
    }
}
