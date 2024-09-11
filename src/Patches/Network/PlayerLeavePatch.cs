using System.Collections.Generic;
using HarmonyLib;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
// using Lotus.RPC.CustomObjects;

namespace Lotus.Patches.Network;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
class PlayerLeavePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PlayerLeavePatch));

    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        log.Debug($"{data.PlayerName} (ClientID={data.Id}) left the game. (Reason={reason})", "SessionEnd");
        if (!AmongUsClient.Instance.AmHost) return;
        if (Game.State is GameState.InLobby)
        {
            PlayerJoinPatch.CheckAutostart();
            Hooks.PlayerHooks.PlayerDisconnectHook.Propagate(new PlayerHookEvent(data.Character));
            return;
        }

        //Game.NameModels.Remove(data.Character.PlayerId);

        if (Game.State is not (GameState.InLobby or GameState.InIntro))
        {
            RoleOperations.Current.Trigger(LotusActionType.Disconnect, data.Character);
            Game.MatchData.Roles.MainRoles.GetValueOrDefault(data.Character.PlayerId)?.HandleDisconnect();
            Game.MatchData.Roles.SubRoles.GetValueOrDefault(data.Character.PlayerId)?.ForEach(r => r.HandleDisconnect());
            try
            {
                Game.MatchData.RegenerateFrozenPlayers(data.Character);
            }
            catch { }
            // CustomNetObject.DespawnOnQuit(data.Character.PlayerId);
        }
        Hooks.PlayerHooks.PlayerDisconnectHook.Propagate(new PlayerHookEvent(data.Character));
        data.Character.Data.PlayerName = data.Character.name;
    }
}