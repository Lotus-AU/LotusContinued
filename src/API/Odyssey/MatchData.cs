using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Extensions;
using Lotus.Managers.History;
using Lotus.Roles;
using Lotus.Roles.Exceptions;
using Lotus.Roles.Overrides;
using Lotus.RPC;
using Lotus.Server;
using Lotus.Statuses;
using UnityEngine.ProBuilder;
using VentLib;
using VentLib.Networking.RPC;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.API.Odyssey;

public class MatchData
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(MatchData));
    internal ulong MatchID;

    public GameHistory GameHistory = new();
    public DateTime StartTime = DateTime.Now;


    public Dictionary<ulong, FrozenPlayer> FrozenPlayers = new();
    public VanillaRoleTracker VanillaRoleTracker = new();

    public HashSet<byte> UnreportableBodies = new();
    public int EmergencyButtonsUsed = 0;
    public int MeetingsCalled = 0;

    private static readonly Func<RemoteList<GameOptionOverride>> OptionOverrideListSupplier = GetGlobalOptions;

    public RoleData Roles = new();

    public FrozenPlayer? GetFrozenPlayer(PlayerControl? player)
    {
        return player == null || !FrozenPlayers.ContainsKey(player.GetGameID()) ? null : FrozenPlayers[player.GetGameID()];
    }

    public void RegenerateFrozenPlayers(PlayerControl? specificPlayer = null)
    {
        if (specificPlayer != null)
        {
            FrozenPlayer frozenPlayer = new(specificPlayer);
            Game.MatchData.FrozenPlayers[frozenPlayer.GameID] = frozenPlayer;
            return;
        }
        Players.GetAllPlayers().Where(p => p.Data != null && !p.Data.Disconnected).ForEach(p =>
        {
            FrozenPlayer frozenPlayer = new(p);
            Game.MatchData.FrozenPlayers[frozenPlayer.GameID] = frozenPlayer;
        });
    }

    public void Cleanup()
    {
        UnreportableBodies.Clear();
        Roles = new RoleData();
        GameHistory = new GameHistory();
        VanillaRoleTracker = new VanillaRoleTracker();
    }

    public class RoleData
    {
        public Dictionary<byte, CustomRole> MainRoles = new();
        public Dictionary<byte, List<CustomRole>> SubRoles = new();


        private readonly Dictionary<byte, RemoteList<GameOptionOverride>> rolePersistentOverrides = new();

        public Remote<GameOptionOverride> AddOverride(byte playerId, GameOptionOverride @override)
        {
            return rolePersistentOverrides.GetOrCompute(playerId, OptionOverrideListSupplier).Add(@override);
        }

        public IEnumerable<GameOptionOverride> GetOverrides(byte playerId)
        {
            return rolePersistentOverrides.GetOrCompute(playerId, OptionOverrideListSupplier);
        }

        public IEnumerable<CustomRole> GetRoleDefinitions(byte playerId)
        {
            CustomRole primaryRoleDefinition = GetMainRole(playerId);
            return new List<CustomRole> { primaryRoleDefinition }.Concat(SubRoles.GetOrCompute(playerId, () => new List<CustomRole>()));
        }

        public CustomRole SetMainRole(byte playerId, CustomRole roleDefinition) => MainRoles[playerId] = roleDefinition;
        public void AddSubrole(byte playerId, CustomRole secondaryRoleDefinition) => SubRoles.GetOrCompute(playerId, () => new List<CustomRole>()).Add(secondaryRoleDefinition);

        public CustomRole GetMainRole(byte playerId) => MainRoles.GetOptional(playerId).OrElseGet(() => throw new NoSuchRoleException($"Player ID ({playerId}) does not have a primary role."));
        public List<CustomRole> GetSubroles(byte playerId) => SubRoles.GetOrCompute(playerId, () => new List<CustomRole>());
    }
    public void AssignRole(PlayerControl player, CustomRole roleDefinition, bool sendToClient = false)
    {
        CustomRole assigned = this.Roles.SetMainRole(player.PlayerId, roleDefinition = roleDefinition.Instantiate(player));
        this.FrozenPlayers.GetOptional(player.GetGameID()).IfPresent(fp => fp.MainRole = assigned);
        log.Debug($"{roleDefinition.EnglishRoleName} was assigned to {player.name}.");
        if (Game.State is GameState.InLobby or GameState.InIntro) player.GetTeamInfo().MyRole = roleDefinition.RealRole;
        if (sendToClient) assigned.Assign();

    }

    public void AssignSubRole(PlayerControl player, CustomRole role, bool sendToClient = false)
    {
        CustomRole instantiated = role.Instantiate(player);
        this.Roles.AddSubrole(player.PlayerId, instantiated);
        log.Debug($"{role.EnglishRoleName} was added as a Subrole to {player.name}.");
        if (sendToClient) role.Assign();
    }

    public RemoteList<IStatus>? GetStatuses(PlayerControl player)
    {
        return this.FrozenPlayers.GetOptional(player.GetGameID()).Map(fp => fp.Statuses).OrElse(null!);
    }

    public Remote<IStatus>? AddStatus(PlayerControl player, IStatus status, PlayerControl? infector = null)
    {
        return this.FrozenPlayers.GetOptional(player.GetGameID()).Map(fp => fp.Statuses).Transform(statuses =>
        {
            if (statuses.Any(s => s.Name == status.Name)) return null!;
            Remote<IStatus> remote = statuses.Add(status);
            Hooks.ModHooks.StatusReceivedHook.Propagate(new PlayerStatusReceivedHook(player, status, infector));
            return remote;
        }, () => null!);
    }

    // TODO make way better
    public static RemoteList<GameOptionOverride> GetGlobalOptions()
    {
        RemoteList<GameOptionOverride> globalOverrides = new() { new GameOptionOverride(Override.ShapeshiftCooldown, 0.1f) };
        if (AUSettings.ConfirmImpostor()) globalOverrides.Add(new GameOptionOverride(Override.ConfirmEjects, false));
        return globalOverrides;
    }
}