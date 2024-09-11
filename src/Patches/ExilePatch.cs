using System;
using System.Collections.Generic;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles.Internals;
using Lotus.Managers.History.Events;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.API.Player;
using VentLib.Utilities.Extensions;
using Lotus.RPC;
using VentLib.Utilities;

namespace Lotus.Patches;

static class ExileControllerWrapUpPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ExileControllerWrapUpPatch));

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    class BaseExileControllerPatch
    {
        public static void Postfix(ExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.initData);
            }
            catch (Exception ex)
            {
                log.Exception(ex);
            }
            finally
            {
                WrapUpFinalizer();
            }
        }
    }

    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
    class AirshipExileControllerPatch
    {
        public static void Postfix(AirshipExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.initData);
            }
            catch (Exception ex)
            {
                log.Exception(ex);
            }
            finally
            {
                WrapUpFinalizer();
            }
        }
    }
    static void WrapUpPostfix(ExileController.InitProperties? exiled)
    {
        if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません;
        FallFromLadder.Reset();

        if (exiled == null) return;
        if (exiled.networkedPlayer == null) return;

        PlayerControl exiledPlayer = exiled.networkedPlayer.Object;

        RoleOperations.Current.Trigger(LotusActionType.Exiled, exiledPlayer);

        Hooks.PlayerHooks.PlayerExiledHook.Propagate(new PlayerHookEvent(exiledPlayer!));
        Hooks.PlayerHooks.PlayerDeathHook.Propagate(new PlayerDeathHookEvent(exiledPlayer!, new ExiledEvent(exiledPlayer!, new List<PlayerControl>(), new List<PlayerControl>())));
    }

    static void WrapUpFinalizer()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            MeetingDelegate.Instance.BlackscreenResolver.FixBlackscreens(BeginRoundStart);
            log.Debug("Blackscreen Resolver ran without issues!");
        }
        catch (Exception ex)
        {
            log.Fatal("Error occured during Blackscreen Resolver.");
            log.Exception(ex);
            BeginRoundStart();
        }
    }

    /// <summary>
    /// Called after Clear Blackscreen is done processing
    /// </summary>
    private static void BeginRoundStart()
    {
        Players.GetAlivePlayers().ForEach(p => Async.Schedule(ReverseEngineeredRPC.UnshfitButtonTrigger(p), NetUtils.DeriveDelay(1f)));

        try
        {
            Game.RenderAllForAll(force: true);
        }
        catch (Exception exception)
        {
            log.Exception(exception);
        }

        Game.State = GameState.Roaming;
        ActionHandle handle = ActionHandle.NoInit();
        log.Debug("Triggering RoundStart Action!!");
        RoleOperations.Current.TriggerForAll(LotusActionType.RoundStart, null, handle, false);
        Hooks.GameStateHooks.RoundStartHook.Propagate(new GameStateHookEvent(Game.MatchData, ProjectLotus.GameModeManager.CurrentGameMode));
        Game.SyncAll();
    }
}

[HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
class PolusExileHatFixPatch
{
    public static void Prefix(PbExileController __instance)
    {
        __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
    }
}