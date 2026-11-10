using HarmonyLib;
using Lotus.Logging;
using System.Diagnostics;
using AmongUs.GameOptions;
using VentLib.Networking.RPC;
using System.Collections.Generic;

namespace Lotus.Patches;
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
class SetRolePatch
{
    public static Dictionary<byte, bool> RoleAssigned = new();
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType, [HarmonyArgument(1)] bool canOverrideRole)
    {
        if (!ProjectLotus.AdvancedRoleAssignment) return true;
        if (!AmongUsClient.Instance.AmHost) return true;
        if (RoleAssigned[__instance.PlayerId] && !RoleManager.IsGhostRole(roleType))
        {
            DevLogger.Log($"Custom assigning {roleType} for {__instance.name} because they were already assigned a role.");
            __instance.StartCoroutine(__instance.CoSetRole(roleType, true));
            RpcV3.Immediate(__instance.NetId, RpcCalls.SetRole, Hazel.SendOption.Reliable).Write((ushort)roleType).Write(true).Send();
            return false;
        }
        if (!canOverrideRole)
        {
            __instance.RpcSetRole(roleType, true);
            return false;
        }
        if (RoleManager.IsGhostRole(roleType)) return true;
        RoleAssigned[__instance.PlayerId] = true;
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CoSetRole))]
class CoSetRolePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
    {
        if (!AmongUsClient.Instance.AmHost && !ProjectLotus.DevVersion) return;
        string invokerName = new StackTrace(5)?.GetFrame(0)?.GetMethod()?.Name ?? "null";
        DevLogger.Log($"{roleType} was assigned to {__instance.name} for me. (Invoker): {invokerName}");
    }
}