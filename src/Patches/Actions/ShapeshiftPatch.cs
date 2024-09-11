using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Roles.Internals;
using Lotus.Extensions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.RPC;
using VentLib.Utilities;
using Priority = HarmonyLib.Priority;
using Lotus.Roles;
using Lotus.API.Player;
using VentLib.Utilities.Extensions;

namespace Lotus.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
public static class ShapeshiftPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ShapeshiftPatch));

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
    {
        string invokerName = new StackTrace(5)?.GetFrame(0)?.GetMethod()?.Name;
        log.Debug($"(ShapeshiftEvent) Shapeshift Cause (Invoker): {invokerName}");
        if (invokerName is "RpcShapeshiftV2" or "RpcRevertShapeshiftV2" or "<Shapeshift>b__0" or "<RevertShapeshift>b__0") return true;
        if (invokerName is "CRpcShapeshift" or "CRpcRevertShapeshift" or "<Shapeshift>b__0" or "<RevertShapeshift>b__0") return true;
        log.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");
        if (!AmongUsClient.Instance.AmHost) return true;

        var shapeshifter = __instance;
        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

        // we do not check if the role is not ss because of desync roles.
        // yes this can be exploited for hackers, but we'll tackle that problem when we get there.
        if (!target || target.Data == null || shapeshifter.Data.IsDead || shapeshifter.Data.Disconnected)
        {
            int num = target ? ((int)target.PlayerId) : -1;
            log.Warn(string.Format("Bad shapeshift from {0} to {1}", shapeshifter.name, num));
            shapeshifter.RpcRejectShapeshift();
            return false;
        }
        if (target.IsMushroomMixupActive() && shouldAnimate)
        {
            log.Warn($"Tried to shapeshift while mushroom mixup was active {shapeshifter.name}");
            shapeshifter.RpcRejectShapeshift();
            return false;
        }
        if (MeetingHud.Instance && shouldAnimate)
        {
            log.Warn($"Tried to shapeshift while a meeting was starting. {shapeshifter.name}");
            shapeshifter.RpcRejectShapeshift();
            return false;
        }
        if (shapeshifter.PrimaryRole().RealRole != AmongUs.GameOptions.RoleTypes.Shapeshifter)
        {
            log.Fatal($"Tried to shapeshift while their realrole is not Shapeshifter. {shapeshifter.name}");
            shapeshifter.RpcRejectShapeshift();
            return false;
        }

        ActionHandle handle = ActionHandle.NoInit();
        RoleOperations.Current.Trigger(shapeshifting ? LotusActionType.Shapeshift : LotusActionType.Unshapeshift, __instance, handle, target);
        shapeshifter.SyncAll(); // sync new cooldowns and durations

        bool unshiftTrigger = shapeshifter.PrimaryRole().RoleAbilityFlags.HasFlag(RoleAbilityFlag.UsesUnshiftTrigger);

        if (handle.IsCanceled)
        {
            shapeshifter.RpcRejectShapeshift();
            if (handle.Cancellation is ActionHandle.CancelType.Normal)
            {
                // Reset cooldown if the cancelType is normal.
                // Async.Schedule(shapeshifter.RpcResetAbilityCooldown, NetUtils.DeriveDelay(0.1f));
                if (shapeshifting) shapeshifter.RpcShapeshift(shapeshifter, false);
                else shapeshifter.RpcShapeshift(Players.FindPlayerById(ShapeshiftFixPatch.GetShapeshifted(shapeshifter)), false);
            }
            return false;
        }

        if (unshiftTrigger)
        {
            Async.Execute(ReverseEngineeredRPC.UnshfitButtonTrigger(shapeshifter));
            return false;
        }
        Hooks.PlayerHooks.PlayerShapeshiftHook.Propagate(new PlayerShapeshiftHookEvent(__instance, target.Data, !shapeshifting));
        shapeshifter.RpcShapeshift(target, shouldAnimate);
        return false;
    }
}

[HarmonyPriority(Priority.LowerThanNormal)]
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
public static class ShapeshiftFixPatch
{
    private static Dictionary<byte, byte> _shapeshifted = new();

    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
    {
        if (target.PlayerId == __instance.PlayerId) _shapeshifted.Remove(__instance.PlayerId);
        else _shapeshifted[__instance.PlayerId] = target.PlayerId;
        if (!shouldAnimate) // dont need to wait because no ss anim
        {
            var nameModel = __instance.NameModel();
            if (Game.State is not GameState.InMeeting) Players.GetAllPlayers().ForEach(p => nameModel.RenderFor(p));
            return;
        }
        Async.Schedule(() =>
        {
            var nameModel = __instance.NameModel();
            if (Game.State is not GameState.InMeeting) Players.GetAllPlayers().ForEach(p => nameModel.RenderFor(p));
        }, 1.2f);
    }

    public static bool IsShapeshifted(this PlayerControl player) => _shapeshifted.ContainsKey(player.PlayerId);
    public static byte GetShapeshifted(this PlayerControl player) => _shapeshifted.GetValueOrDefault(player.PlayerId, (byte)255);
}