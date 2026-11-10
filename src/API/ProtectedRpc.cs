using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Impostors;
using Lotus.Network;
using Lotus.Patches.Actions;
using Lotus.Roles.Overrides;
using Lotus.RPC.CustomObjects;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.API;

public class ProtectedRpc
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ProtectedRpc));

    public static void CheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost) return;
        log.Trace($"Protected Check Murder ({killer.name} => {target.name})");
        if (target == null) return;
        NetworkedPlayerInfo data = target.Data;
        if (data == null)
        {
            log.Trace("Kill was canceled because target's data is null.");
            return;
        }

        if (target is {PlayerId: 254, notRealPlayer: true})
        {
            Optional<CustomNetObject> netObject = CustomNetObject.ObjectFromPlayer(target);
            if (netObject.Exists())
            {
                log.Trace($"Kill was canceled because they are trying to kill a CustomNetObject.");
                return;
            }
        }

        if (MeetingHud.Instance)
        {
            killer.RpcVaporize(target);
            RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.Succeeded).Send(target.GetClientId());
            target.Data.IsDead = true;
            return;
        }

        RoleTypes realRole = killer.PrimaryRole().RealRole;

        if (realRole is not RoleTypes.Viper)
        {
            if (!ConnectionManager.IsVanillaServer) target.SetPet("pet_EmptyPet");
            killer.MurderPlayer(target, MurderResultFlags.Succeeded);

            if (!ConnectionManager.IsVanillaServer)
                RpcV3.Immediate(target.NetId, RpcCalls.SetPetStr).Write("").Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr)).Send();
            RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.Succeeded).Send();
            target.Data.IsDead = true;
            return;
        }

        // innersloth decided that the client must see the impostor as Viper for the body to spawn with acid
        // so we have to use a set role RPC. and we need to make sure the role's dissolve time is sent to all clients.

        RoleTypes finalRole = killer.IsAlive() ? RoleTypes.Crewmate : RoleTypes.ImpostorGhost;
        float dissolveTime = killer.PrimaryRole().GetOverride(Override.ViperDissolveTime)?.GetValue() as float? ??
                             AUSettings.ViperDissolveTime();

        var desyncedWriter = RpcV3.Mass(SendOption.Reliable)
            .Start(killer.NetId, RpcCalls.SetRole).Write((ushort)RoleTypes.Viper).Write(ProjectLotus.AdvancedRoleAssignment).End();
        if (!ConnectionManager.IsVanillaServer)
            desyncedWriter.Start(target.NetId, RpcCalls.SetPetStr).Write("").Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr)).End();
        desyncedWriter
            .Start(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.Succeeded).End()
            .Start(killer.NetId, RpcCalls.SetRole).Write((ushort)finalRole).Write(ProjectLotus.AdvancedRoleAssignment).End();

        var regularWriter = RpcV3.Mass(SendOption.Reliable);
        if (!ConnectionManager.IsVanillaServer)
            regularWriter.Start(target.NetId, RpcCalls.SetPetStr).Write("").Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr)).End();
        regularWriter.Start(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.Succeeded).End();

        List<Remote<GameOptionOverride>> remoteOverrides = [];

        foreach (PlayerControl otherPlayer in Players.GetAllPlayers())
        {
            if (killer.PlayerId == otherPlayer.PlayerId)
            {
                // skip player doing killing
                HandleNormalPlayer(otherPlayer);
                continue;
            }
            remoteOverrides.Add(Game.MatchData.Roles.AddOverride(otherPlayer.PlayerId,
                new GameOptionOverride(Override.ViperDissolveTime, dissolveTime)));
            otherPlayer.SyncAll();
            if (otherPlayer.GetTeamInfo().Impostors.Contains(killer.PlayerId))
            {
                // we see them as impostor (Viper)
                HandleNormalPlayer(otherPlayer);
                continue;
            }

            desyncedWriter.Send(otherPlayer.OwnerId);
        }
        target.Data.IsDead = true;
        Async.Schedule(() => remoteOverrides.RemoveAll(r => r.Delete()), NetUtils.DeriveDelay(0.5f));
        return;

        void HandleNormalPlayer(PlayerControl player)
        {
            if (player.AmOwner)
            {
                target.SetPet("pet_EmptyPet");
                killer.MurderPlayer(target, MurderResultFlags.Succeeded);
                return;
            }
            regularWriter.Send(player.OwnerId);
        }
    }
}