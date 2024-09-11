using System;
using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Managers.History.Events;
using Lotus.Roles.Internals;
using Lotus.Extensions;
using Lotus.Managers.History;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Server;
using Lotus.Utilities;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;
using VentLib.Utilities.Optionals;
using Lotus.Roles.Managers.Interfaces;

namespace Lotus.Patches.Actions;


public static class MurderPatches
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(MurderPatches));

    public static PlayerControl LastAttacker = null!;
    private static readonly Dictionary<byte, FixedUpdateLock> MurderLocks = new();
    private static readonly Func<FixedUpdateLock> TimeoutSupplier = () => new FixedUpdateLock(0.25f);

    public static bool Lock(byte player) => MurderLocks.GetOrCompute(player, TimeoutSupplier).AcquireLock(NetUtils.DeriveDelay(0.25f));

    [QuickPrefix(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        //if (__instance.IsHost()) return true;
        if (!AmongUsClient.Instance.AmHost) return false;
        if (__instance == null || target == null) return false;

        log.Debug($"(CheckMurder) {__instance.GetNameWithRole()} => {target.GetNameWithRole()}");

        if (__instance.Data.Disconnected || !target)
        {
            log.Trace($"Unable to kill {target.name}. Bad Kill.", "CheckMurder");
            return false;
        }

        if (target.Data == null || target.inVent || target.inMovingPlat || target.Data.Disconnected)
        {
            log.Trace($"Unable to kill {target.name}. Invalid Status", "CheckMurder");
            return false;
        }
        if (!target.IsAlive())
        {
            log.Trace($"Unable to kill {target.name}. Player is already dead.", "CheckMurder");
            return false;
        }
        if (MeetingHud.Instance != null)
        {
            log.Trace($"Unable to kill {target.name}. There is currently a meeting.", "CheckMurder");
            return false;
        }

        if (__instance.PlayerId == target.PlayerId) return false;

        if (!MurderLocks.GetOrCompute(__instance.PlayerId, TimeoutSupplier).IsUnlocked())
        {
            log.Trace("Kill was canceled because it's been 0.25 seconds since the last kill.");
            return false;
        }
        Lock(__instance.PlayerId);

        RoleOperations.Current.Trigger(LotusActionType.Attack, __instance, target);
        return false;
    }

    [QuickPrefix(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public static void SaveAttacker(PlayerControl __instance, PlayerControl target, MurderResultFlags resultFlags)
    {
        if (resultFlags.HasFlag(MurderResultFlags.Succeeded) || resultFlags.HasFlag(MurderResultFlags.DecisionByHost) && target.protectedByGuardianId > -1)
            LastAttacker = __instance;
    }

    [QuickPostfix(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public static void MurderPlayer(PlayerControl __instance, PlayerControl target, MurderResultFlags resultFlags)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!target.Data.IsDead) return;
        if (resultFlags.HasFlag(MurderResultFlags.FailedError)) return;
        if (Game.MatchData.GetFrozenPlayer(target)?.Status is not PlayerStatus.Alive) return;

        MurderPatches.Lock(__instance.PlayerId);

        log.Trace($"(MurderPlayer) {__instance.GetNameWithRole()} => {target.GetNameWithRole()}{(target.protectedByGuardianId > -1 ? "(Protected)" : "")}");

        IDeathEvent deathEvent = Game.MatchData.GameHistory.GetCauseOfDeath(target.PlayerId)
            .OrElseGet(() => __instance.PlayerId == target.PlayerId
                ? new SuicideEvent(__instance)
                : new DeathEvent(target, __instance)
            );

        Game.MatchData.GameHistory.AddEvent(deathEvent);
        Game.MatchData.GameHistory.SetCauseOfDeath(target.PlayerId, deathEvent);

        Game.MatchData.RegenerateFrozenPlayers(target);

        RoleOperations.Current.TriggerForAll(LotusActionType.PlayerDeath, target, __instance, deathEvent.Instigator(), deathEvent);

        PlayerControl killer = deathEvent.Instigator().FlatMap(k => new UnityOptional<PlayerControl>(k.MyPlayer)).OrElse(__instance);
        PlayerMurderHookEvent playerMurderHookEvent = new(killer, target, deathEvent);
        Hooks.PlayerHooks.PlayerMurderHook.Propagate(playerMurderHookEvent);
        Hooks.PlayerHooks.PlayerDeathHook.Propagate(playerMurderHookEvent);

        Async.Schedule(() =>
        {
            if (target != null) target.SetChatName(target.name);
        }, 0.1f);
    }
}