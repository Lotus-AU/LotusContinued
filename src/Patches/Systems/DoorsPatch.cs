using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Chat;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;

namespace Lotus.Patches.Systems;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
public class DoorsPatch
{
    public static bool Prefix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes room)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (Game.CurrentGameMode.BlockedActions().HasFlag(GameModes.BlockableGameAction.CloseDoors)) return false;
        if (GeneralOptions.GameplayOptions.IsSubscribed(Game.CurrentGameMode.MainTab()))
            if (GeneralOptions.GameplayOptions.BlockDeadFromSabotaging &&
                GeneralOptions.GameplayOptions.BlockDoorsWhenAllSabotagersAreDead)
                if (Players.GetAliveRoles().All(r =>
                        r.RoleAbilityFlags.HasFlag(RoleAbilityFlag.CannotSabotage) ||
                        r is not ISabotagerRole || r is ISabotagerRole sabotagerRole && !sabotagerRole.CanSabotage()))
                    // was thinking of sending a message to the player but then I forgot we don't know WHO is sabotaging.
                    return false;
        if (GeneralOptions.SabotageOptions.IsSubscribed(Game.CurrentGameMode.MainTab()))
            if (GeneralOptions.SabotageOptions.DisabledSabotages.HasFlag(SabotageType.Door))
                return false;

        ISabotage sabotage = new DoorSabotage(room);

        ActionHandle handle = RoleOperations.Current.Trigger(LotusActionType.SabotageStarted, null, sabotage, PlayerControl.LocalPlayer);
        if (handle.IsCanceled) return false;

        Hooks.SabotageHooks.SabotageCalledHook.Propagate(new SabotageHookEvent(sabotage));
        return true;
    }
}