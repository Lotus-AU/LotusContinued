using System;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.API;
using Lotus.API.Vanilla.Meetings;
using Lotus.Extensions;
using Lotus.Roles;
using Lotus.Roles.Internals.Enums;
using VentLib.Utilities.Attributes;
using Hazel;
using Lotus.Roles.Operations;
using InnerNet;
using Lotus.Chat;
using Lotus.Options;
using UnityEngine.ProBuilder;
using VentLib.Localization;

namespace Lotus.Patches.Systems;

[LoadStatic]
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(byte))]
public static class SabotagePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(SabotagePatch));

    public static float SabotageCountdown = -1;
    public static ISabotage? CurrentSabotage;

    static SabotagePatch()
    {
        Hooks.GameStateHooks.GameStartHook.Bind(nameof(SabotagePatch), _ => CurrentSabotage = null);
    }

    internal static bool Prefix(ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] byte amount)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        ActionHandle handle = ActionHandle.NoInit();
        ISystemType systemInstance;
        log.Trace($"Update System: {systemType} | Player: {player.name} | Amount: {amount}");
        switch (systemType)
        {
            case SystemTypes.Sabotage:
                if (Game.CurrentGameMode.BlockedActions().HasFlag(GameModes.BlockableGameAction.CallSabotage)) return false;
                if (player.PrimaryRole() is not ISabotagerRole sabotager || !sabotager.CanSabotage()) return false;
                if (player.PrimaryRole().RoleAbilityFlags.HasFlag(RoleAbilityFlag.CannotSabotage)) return false;
                if (MeetingPrep.Prepped) return false;

                if (!player.IsAlive() && GeneralOptions.GameplayOptions.BlockDeadFromSabotaging)
                {
                    ChatHandlers.NotPermitted(Localizer.Translate("Announcements.CannotSabotageMessage")).Send(player);
                    return false;
                }

                SabotageCountdown = -1;
                SabotageType sabotage = (SystemTypes)amount switch
                {
                    SystemTypes.Electrical => SabotageType.Lights,
                    SystemTypes.Comms => SabotageType.Communications,
                    SystemTypes.LifeSupp => SabotageType.Oxygen,
                    SystemTypes.Reactor => AUSettings.MapId() == 4 ? SabotageType.Helicopter : SabotageType.Reactor,
                    SystemTypes.Laboratory => SabotageType.Reactor,
                    _ => throw new Exception($"Invalid Sabotage Type:{(SystemTypes)amount} {amount}")
                };

                if (GeneralOptions.SabotageOptions.IsSubscribed(Game.CurrentGameMode.MainTab()))
                    if (GeneralOptions.SabotageOptions.DisabledSabotages.HasFlag(sabotage))
                        return false;

                ISabotage sabo = ISabotage.From(sabotage, player);
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotageStarted, player, sabo);
                if (!handle.IsCanceled) CurrentSabotage = sabo;
                else return false;

                Hooks.SabotageHooks.SabotageCalledHook.Propagate(new SabotageHookEvent(sabo));
                log.Debug($"Sabotage Started: {sabo}");
                Game.SyncAll();
                break;
            case SystemTypes.Electrical:
                if (amount > 64) return true;
                if (CurrentSabotage?.SabotageType() != SabotageType.Lights) break;
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                SwitchSystem electrical = systemInstance!.Cast<SwitchSystem>();
                byte currentSwitches = electrical.ActualSwitches;
                if (amount.HasBit(128))
                    currentSwitches ^= (byte)(amount & 31U);
                else
                    currentSwitches ^= (byte)(1U << amount);
                if (currentSwitches != electrical.ExpectedSwitches)
                {
                    handle = RoleOperations.Current.Trigger(LotusActionType.SabotagePartialFix, player, CurrentSabotage);
                    Hooks.SabotageHooks.SabotagePartialFixHook.Propagate(new SabotageHookEvent(CurrentSabotage));
                    break;
                }
                log.Info($"Electrical Sabotage Fixed by {player.name}", "SabotageFix");
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotageFixed, player, CurrentSabotage);
                Hooks.SabotageHooks.SabotageFixedHook.Propagate(new SabotageFixHookEvent(player, CurrentSabotage));
                CurrentSabotage = null;
                break;
            case SystemTypes.Comms:
                if (CurrentSabotage?.SabotageType() != SabotageType.Communications) break;
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                if (systemInstance.TryCast<HudOverrideSystemType>() != null && amount == 0)
                {
                    RoleOperations.Current.Trigger(LotusActionType.SabotagePartialFix, player, CurrentSabotage);
                    handle = RoleOperations.Current.Trigger(LotusActionType.SabotageFixed, player, CurrentSabotage);
                    Hooks.SabotageHooks.SabotageFixedHook.Propagate(new SabotageFixHookEvent(player, CurrentSabotage));
                    CurrentSabotage = null;
                }
                else if (systemInstance.TryCast<HqHudSystemType>() != null) // Mira has a special communications which requires two people
                {
                    HqHudSystemType miraComms = systemInstance.Cast<HqHudSystemType>(); // Get mira comm instance
                    byte commsNum = (byte)(amount & 15U); // Convert to 0 or 1 for respective console
                    if (miraComms.CompletedConsoles.Contains(commsNum)) break; // Negative check if console has already been fixed (refreshes periodically)

                    handle = RoleOperations.Current.Trigger(LotusActionType.SabotagePartialFix, player, CurrentSabotage);
                    // Send partial fix action
                    Hooks.SabotageHooks.SabotagePartialFixHook.Propagate(new SabotageHookEvent(CurrentSabotage));
                    // If there's more than 1 already fixed then comms is fixed totally
                    if (miraComms.NumComplete == 0) break;
                    handle = RoleOperations.Current.Trigger(LotusActionType.SabotageFixed, player, CurrentSabotage);
                    Hooks.SabotageHooks.SabotageFixedHook.Propagate(new SabotageFixHookEvent(player, CurrentSabotage));
                    CurrentSabotage = null;
                }
                if (CurrentSabotage == null)
                    log.Info($"Communications Sabotage Fixed by {player.name}", "SabotageFix");
                break;
            case SystemTypes.LifeSupp:
                if (CurrentSabotage?.SabotageType() != SabotageType.Oxygen) break;
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                LifeSuppSystemType oxygen = systemInstance!.Cast<LifeSuppSystemType>();
                int o2Num = amount & 3;
                if (oxygen.CompletedConsoles.Contains(o2Num)) break;
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotagePartialFix, player, CurrentSabotage);
                Hooks.SabotageHooks.SabotagePartialFixHook.Propagate(new SabotageHookEvent(CurrentSabotage));
                if (oxygen.UserCount == 0) break;
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotageFixed, player, CurrentSabotage);
                Hooks.SabotageHooks.SabotageFixedHook.Propagate(new SabotageFixHookEvent(player, CurrentSabotage));
                CurrentSabotage = null;
                log.Info($"Oxygen Sabotage Fixed by {player.name}", "SabotageFix");
                break;
            case SystemTypes.Reactor when CurrentSabotage?.SabotageType() is SabotageType.Helicopter:
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                HeliSabotageSystem heliSabotage = systemInstance!.Cast<HeliSabotageSystem>();
                int heliNum = amount & 3;
                if (heliSabotage.CompletedConsoles.Contains((byte)heliNum)) break;
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotagePartialFix, player, CurrentSabotage);
                Hooks.SabotageHooks.SabotagePartialFixHook.Propagate(new SabotageHookEvent(CurrentSabotage));
                if (heliSabotage.UserCount == 0) break;
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotageFixed, player, CurrentSabotage);
                Hooks.SabotageHooks.SabotageFixedHook.Propagate(new SabotageFixHookEvent(player, CurrentSabotage));
                CurrentSabotage = null;
                log.Info($"Helicopter Sabotage Fixed by {player.name}", "SabotageFix");
                break;
            case SystemTypes.Laboratory:
            case SystemTypes.Reactor:
                if (CurrentSabotage?.SabotageType() != SabotageType.Reactor) break;
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                ReactorSystemType? reactor = systemInstance!.TryCast<ReactorSystemType>();
                if (reactor == null) break;
                int reactNum = amount & 3;
                if (reactor.UserConsolePairs.ToList().Any(p => p.Item2 == reactNum)) break;
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotagePartialFix, player, CurrentSabotage);
                Hooks.SabotageHooks.SabotagePartialFixHook.Propagate(new SabotageHookEvent(CurrentSabotage));
                if (reactor.UserCount == 0) break;
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotageFixed, player, CurrentSabotage);
                Hooks.SabotageHooks.SabotageFixedHook.Propagate(new SabotageFixHookEvent(player, CurrentSabotage));
                CurrentSabotage = null;
                log.Info($"Reactor Sabotage Fixed by {player.name}", "SabotageFix");
                break;
            case SystemTypes.Doors:
                int doorIndex = amount & 31;
                DoorSabotage doorSabotage = new(null, doorIndex);
                RoleOperations.Current.Trigger(LotusActionType.SabotagePartialFix, player, doorSabotage);
                handle = RoleOperations.Current.Trigger(LotusActionType.SabotageFixed, player, doorSabotage);
                Hooks.SabotageHooks.SabotageFixedHook.Propagate(new SabotageFixHookEvent(player, doorSabotage));
                break;
            default:
                return true;
        }

        Game.SyncAll();
        return !handle.IsCanceled;
    }
    internal static void LogItem(string message) => log.Trace(message);
}


[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader))]
class WriterSabotagePatch
{
    internal static SystemTypes[] WatchedSystems = { SystemTypes.Reactor, SystemTypes.Doors, SystemTypes.Sabotage, SystemTypes.Electrical, SystemTypes.LifeSupp, SystemTypes.Laboratory, SystemTypes.Comms };
    internal static bool Prefix(ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!WatchedSystems.Contains(systemType))
        {
            SabotagePatch.LogItem($"Skipped Update System: {systemType} | Player: {player.name}");
            return true;
        }

        byte amount = reader.ReadByte();
        bool flag = SabotagePatch.Prefix(__instance, systemType, player, amount);
        if (!flag) return false;
        MessageWriter writer = MessageWriter.Get(0);
        writer.Write(amount);
        MessageReader newReader = MessageReader.Get(writer.ToByteArray(false));

        ISystemType systemType2;
        if (__instance.Systems.TryGetValue(systemType, out systemType2))
        {
            __instance.logger.Info(string.Format("Player {0} modifying system {1}", player.PlayerId, systemType), null);
            systemType2.UpdateSystem(player, newReader);
        }
        writer.Recycle();

        return false;
    }
}