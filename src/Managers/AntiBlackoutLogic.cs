using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Utilities;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using static GameData;

namespace Lotus.Managers;

public static class AntiBlackoutLogic
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(AntiBlackoutLogic));

    public static HashSet<byte> PatchedDataLegacy(byte exiledPlayer)
    {
        log.Debug("Patching GameData", "AntiBlackout");
        IEnumerable<PlayerControl> players = PlayerControl.AllPlayerControls.ToArray().Sorted(p => p.IsHost());
        VanillaRoleTracker roleTracker = Game.MatchData.VanillaRoleTracker;

        HashSet<byte> unpatchable = new();

        NetworkedPlayerInfo[] allPlayers = Instance.AllPlayers.ToArray();

        foreach (PlayerControl player in players)
        {
            if (player.IsHost() || player.IsModded()) continue;
            log.Debug($"Patching For: {player.name} ({player.PrimaryRole().RoleName})");
            ReviveEveryone(exiledPlayer);

            bool wasImpostor = roleTracker.GetAllImpostorIds(player.PlayerId).Contains(0);
            HashSet<byte> impostorIds = roleTracker.GetAllImpostorIds(player.PlayerId).Where(id => exiledPlayer != id && id != 0).ToHashSet();
            NetworkedPlayerInfo[] impostorInfo = allPlayers.Where(info => impostorIds.Contains(info.PlayerId)).ToArray();
            log.Debug($"Impostors: {impostorInfo.Select(i => i.Object).Where(o => o != null).Select(o => o.name).Fuse()}");

            HashSet<byte> crewIds = roleTracker.GetAllCrewmateIds(player.PlayerId).Where(id => exiledPlayer != id).ToHashSet();
            NetworkedPlayerInfo[] crewInfo = allPlayers.Where(info => crewIds.Contains(info.PlayerId)).ToArray();
            log.Debug($"Crew: {crewInfo.Select(i => i.Object).Where(o => o != null).Select(o => o.name).Fuse()}");

            int aliveImpostorCount = impostorInfo.Length;
            int aliveCrewCount = crewInfo.Length;

            if (player.PlayerId == exiledPlayer) { }
            else if (player.IsAlive() && player.GetVanillaRole().IsImpostor()) aliveImpostorCount++;
            else if (player.IsAlive()) aliveCrewCount++;
            if (wasImpostor && PlayerControl.LocalPlayer.GetVanillaRole().IsImpostor() && PlayerControl.LocalPlayer.PlayerId != exiledPlayer) aliveImpostorCount++;

            log.Debug($"Alive Crew: {aliveCrewCount} | Alive Impostors: {aliveImpostorCount}");

            bool IsFailure()
            {
                bool failure = false;
                if (aliveCrewCount == 0) failure = unpatchable.Add(player.PlayerId);
                if (aliveImpostorCount == 0) failure |= unpatchable.Add(player.PlayerId);
                return failure;
            }


            // Go until failure, or aliveCrew > aliveImpostor
            int index = 0;
            while (!IsFailure() && index < impostorInfo.Length)
            {
                if (aliveCrewCount > aliveImpostorCount) break;
                NetworkedPlayerInfo info = impostorInfo[index++];
                if (info.Object != null) log.Debug($"Set {info.Object.name} => Disconnect = true | Impostors: {aliveImpostorCount - 1} | Crew: {aliveCrewCount}");
                info.Disconnected = true;
                aliveImpostorCount--;
            }

            // No matter what, if crew is less than impostor alive, we're unpatchable
            if (aliveCrewCount <= aliveImpostorCount) unpatchable.Add(player.PlayerId);


            GeneralRPC.SendGameData(player.GetClientId());
        }

        return unpatchable;
    }

    public static Dictionary<byte, (bool isDead, bool isDisconnected)> PatchedData(PlayerControl curPlayer, byte exiledPlayer,
        Dictionary<byte, (bool isDead, bool isDisconnected)> replacedPlayers)
    {
        log.Debug($"Patching GameData ({curPlayer.name}) ({curPlayer.PrimaryRole().RoleName})");

        if (curPlayer.IsHost() || curPlayer.IsModded()) return replacedPlayers;

        VanillaRoleTracker.TeamInfo myTeamInfo = curPlayer.GetTeamInfo();
        NetworkedPlayerInfo[] allPlayers = Instance.AllPlayers.ToArray();
        HashSet<byte> impostorIds = myTeamInfo.Impostors.Where(id => exiledPlayer != id && id != 0).ToHashSet();
        HashSet<byte> crewIds = myTeamInfo.Crewmates.Where(id => exiledPlayer != id).ToHashSet();

        List<NetworkedPlayerInfo> aliveImpostors = allPlayers.Where(p => impostorIds.Contains(p.PlayerId) && !p.IsDead).ToList();
        List<NetworkedPlayerInfo> aliveCrewmates = allPlayers.Where(p => crewIds.Contains(p.PlayerId) && !p.IsDead).ToList();

        bool isCurPlayerImpostor = curPlayer.GetVanillaRole().IsImpostor();
        if (curPlayer.PlayerId != exiledPlayer && curPlayer.IsAlive())
        {
            if (isCurPlayerImpostor) aliveImpostors.Add(curPlayer.Data);
            else aliveCrewmates.Add(curPlayer.Data);
        }
        log.Debug($"Impostors: {aliveImpostors.Where(o => o != null).Select(o => o.name).Fuse()}");
        log.Debug($"Crew: {aliveCrewmates.Where(o => o != null).Select(o => o.name).Fuse()}");
        log.Debug($"Alive Crew: {aliveCrewmates.Count} | Alive Impostors: {aliveImpostors.Count} | Is Impostor: {isCurPlayerImpostor}");

        bool isBlackScreenLikely = aliveImpostors.Count >= aliveCrewmates.Count || aliveImpostors.Count == 0;
        if (!isBlackScreenLikely) return replacedPlayers;

        if (aliveImpostors.Count == 0 && impostorIds.Count > 0)
        {
            NetworkedPlayerInfo? firstImpostor = allPlayers.FirstOrDefault(p => impostorIds.Contains(p.PlayerId));
            if (firstImpostor != null)
            {
                aliveImpostors.Add(firstImpostor);
                replacedPlayers[firstImpostor.PlayerId] = (false, false);
            }
            else throw new System.Exception("unpatchable.");
        }

        while (aliveImpostors.Count >= aliveCrewmates.Count)
        {
            var leftoverCrewmates = allPlayers.Where(p => crewIds.Contains(p.PlayerId) && !aliveCrewmates.Contains(p));
            if (!leftoverCrewmates.Any()) break;

            NetworkedPlayerInfo randomCrewmate = leftoverCrewmates.First();
            aliveCrewmates.Add(randomCrewmate);
            replacedPlayers[randomCrewmate.PlayerId] = (false, false);
        }

        if (aliveImpostors.Count >= aliveCrewmates.Count || aliveImpostors.Count == 0) throw new System.Exception("unpatchable.");

        return replacedPlayers;
    }

    private static void ReviveEveryone(byte exiledPlayer)
    {
        foreach (var info in Instance.AllPlayers)
        {
            if (info == null) continue;
            info.IsDead = false;
            info.Disconnected = false;
            try
            {
                if (info.PlayerId != exiledPlayer && info.Object != null) info.PlayerName = info.Object.name;
                else if (info.PlayerId != exiledPlayer) info.PlayerName = info.PlayerName.RemoveHtmlTags();
            }
            catch { }
        }
    }
}