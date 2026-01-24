using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Factions.Crew;
using Lotus.Factions.Impostors;
using Lotus.GUI.Name.Interfaces;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Managers.Interfaces;
using Lotus.Utilities;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.API.Player;

public static class Players
{
    public static IEnumerable<PlayerControl> GetPlayers(PlayerFilter filter = PlayerFilter.None)
    {
        IEnumerable<PlayerControl> players = GetAllPlayers();
        if (filter.HasFlag(PlayerFilter.NonPhantom)) players = players.Where(p => !(p.PrimaryRole() is IPhantomRole phantomRole) || phantomRole.IsCountedAsPlayer());
        if (filter.HasFlag(PlayerFilter.Alive)) players = players.Where(p => p.IsAlive());
        if (filter.HasFlag(PlayerFilter.Dead)) players = players.Where(p => !p.IsAlive());
        if (filter.HasFlag(PlayerFilter.Impostor)) players = players.Where(p => p.PrimaryRole().Faction.GetType() == typeof(ImpostorFaction)
            && p.PrimaryRole().SpecialType is SpecialType.None);
        if (filter.HasFlag(PlayerFilter.Crewmate)) players = players.Where(p => p.PrimaryRole().Faction is Crewmates);
        // if (filter.HasFlag(PlayerFilter.Neutral)) players = players.Where(p => p.PrimaryRole().Metadata.Get(LotusKeys.AuxiliaryRoleType) is SpecialType.Neutral);
        // if (filter.HasFlag(PlayerFilter.NeutralKilling)) players = players.Where(p => p.PrimaryRole().Metadata.Get(LotusKeys.AuxiliaryRoleType) is SpecialType.NeutralKilling);
        if (filter.HasFlag(PlayerFilter.Neutral)) players = players.Where(p => p.PrimaryRole().SpecialType is SpecialType.Neutral);
        if (filter.HasFlag(PlayerFilter.NeutralKilling)) players = players.Where(p => p.PrimaryRole().SpecialType is SpecialType.NeutralKilling);
        if (filter.HasFlag(PlayerFilter.Undead)) players = players.Where(p => p.PrimaryRole().SpecialType is SpecialType.Undead);
        return players;
    }

    public static void SendPlayerData(NetworkedPlayerInfo playerInfo, int clientId = -1, bool autoSetName = true)
    {
        INameModel? nameModel = playerInfo.Object != null ? playerInfo.Object.NameModel() : null;
        GetPlayers().ForEach(p =>
        {
            int playerClientId = p.GetClientId();
            if (clientId != -1 && playerClientId != clientId) return;

            MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
            messageWriter.StartMessage(6);
            messageWriter.Write(AmongUsClient.Instance.GameId);
            messageWriter.WritePacked(playerClientId);
            {
                messageWriter.StartMessage(1);
                messageWriter.WritePacked(playerInfo.NetId);
                string name = playerInfo.PlayerName;
                if (autoSetName) playerInfo.PlayerName = nameModel?.RenderFor(p, sendToPlayer: false, force: true) ?? name;
                playerInfo.Serialize(messageWriter, false);
                messageWriter.EndMessage();

                if (autoSetName) playerInfo.PlayerName = name;
            }

            messageWriter.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(messageWriter);
            messageWriter.Recycle();
        });
        // string name = playerInfo.Object.name;
        // GetPlayers().ForEach(p =>
        // {
        //     int playerClientId = p.GetClientId();
        //     if (clientId != -1 && playerClientId != clientId) return;
        //     string uniqueName = "";
        //     if (autoSetName) uniqueName = nameModel?.RenderFor(p, sendToPlayer: false, force: true) ?? name;
        //     RpcV3.Immediate(playerInfo.Object.NetId, RpcCalls.SetName).Write(playerInfo.NetId).Write(uniqueName).Send(p.GetClientId());
        // });
        // if (autoSetName) playerInfo.PlayerName = name;
    }

    public static PlayerControl? FindPlayerById(byte playerId) => Utils.GetPlayerById(playerId);
    public static Optional<PlayerControl> PlayerById(byte playerId) => Utils.PlayerById(playerId);


    public static IEnumerable<PlayerControl> GetAllPlayers() => PlayerControl.AllPlayerControls.ToArray();
    public static IEnumerable<PlayerControl> GetAlivePlayers() => GetPlayers(PlayerFilter.Alive);

    public static IEnumerable<CustomRole> GetAliveRoles(bool includePhantomRoles = false) => GetPlayers(!includePhantomRoles ? (PlayerFilter.NonPhantom | PlayerFilter.Alive) : PlayerFilter.Alive).Select(p => p.PrimaryRole());
    public static IEnumerable<CustomRole> GetAllRoles(bool includeEmptyRoles = false) => GetPlayers().Select(p => p.PrimaryRole()).Where(r => includeEmptyRoles || r.GetType() != IRoleManager.Current.FallbackRole().GetType());

    public static IEnumerable<PlayerControl> GetDeadPlayers(bool disconnected = false) => GetAllPlayers().Where(p => p.Data.IsDead || (disconnected && p.Data.Disconnected));
    public static List<PlayerControl> GetAliveImpostors()
    {
        return GetAlivePlayers().Where(p => p.PrimaryRole().Faction is ImpostorFaction).ToList();
    }

    public static IEnumerable<PlayerControl> FindAlivePlayersWithRole(params CustomRole[] roles) =>
        GetAllPlayers()
            .Where(p => roles.Any(r => r.GetType() == p.PrimaryRole().GetType()) || p.GetSubroles().Any(s => s.GetType() == roles.GetType()));

    public static void SyncAll() => GetPlayers().ForEach(p => p.SyncAll());

    public static IEnumerable<int> GetAllClientIds() => GetAllPlayers().Select(p => p.GetClientId());
}

[Flags]
public enum PlayerFilter
{
    None = 1,
    NonPhantom = 2,
    Alive = 4,
    Dead = 8,
    Impostor = 16,
    Crewmate = 32,
    Neutral = 64,
    NeutralKilling = 128,
    Undead = 256
}