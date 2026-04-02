using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.Extensions;
using Lotus.GameModes.Normal.Colorwars.Factions;
using Lotus.Logging;
using Lotus.Options;
using UnityEngine;
using VentLib.Ranges;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Colorwars.Distributions;

public class ColorwarsRoleAssignment
{
    public void AssignRoles(List<PlayerControl> allPlayers)
    {
        List<PlayerControl> unassignedPlayers = new(allPlayers);
        unassignedPlayers.Shuffle();

        List<List<PlayerControl>> teams = ExtraGamemodeOptions.ColorwarsOptions.CustomTeams ? CreateManualTeams(unassignedPlayers) : CreateRandomTeams(unassignedPlayers);

        if (unassignedPlayers.Count > 0) DevLogger.Log($"{unassignedPlayers.Count} unassigned players.");

        teams.ForEach((t, i) =>
        {
            if (t.Count == 0) return;
            Color factionColor = (Color)Palette.PlayerColors[t.First().cosmetics.bodyMatProperties.ColorId];
            ColorFaction teamFaction = new(i, factionColor);
            t.ForEach(p =>
            {
                ColorwarsGamemode.Instance.Assign(p, ColorwarsRoles.Instance.Static.Painter);
                p.PrimaryRole().Faction = teamFaction;
                p.PrimaryRole().RoleColor = factionColor;
            });
        });
    }

    private static List<List<PlayerControl>> CreateRandomTeams(List<PlayerControl> allPlayers)
    {
        List<List<PlayerControl>> teams = new();
        List<byte> colorCodes = new IntRangeGen(0, ModConstants.ColorNames.Length - 1).AsEnumerable().Select(i => (byte)i).ToList();

        int teamSize = ExtraGamemodeOptions.ColorwarsOptions.TeamSize;

        while (allPlayers.Count >= teamSize)
        {
            CreateTeam(allPlayers.Take(teamSize).ToList());
            allPlayers.RemoveRange(0, teamSize);
        }
        if (teams.Count == 0)
        {
            // Teamsize is not enough for one team.
            // So just do FFA.
            allPlayers.ForEach(p =>
            {
                CreateTeam(new List<PlayerControl>() { p });
            });
            allPlayers.Clear();
        }

        if (allPlayers.Count > 0)
        {
            if (ExtraGamemodeOptions.ColorwarsOptions.PreferSmall || teams.Count == 1)
            {
                // Host prefers teams to be smaller than maximum so we just make these last few players their own team.
                CreateTeam(allPlayers);
            }
            else
            {
                // Host wants all teams to have at least the minimum. So we add these to random teams.
                allPlayers.ForEach(p =>
                {
                    List<PlayerControl> lowestTeam = teams.OrderBy(t => t.Count).First();
                    lowestTeam.Add(p);
                    p.RpcSetColor((byte)p.cosmetics.bodyMatProperties.ColorId);
                });
            }
        }

        void CreateTeam(List<PlayerControl> players)
        {
            byte colorId = colorCodes.PopRandom();
            players.Do(p => p.RpcSetColor(colorId));
            teams.Add(players);
        }

        return teams;
    }

    private static List<List<PlayerControl>> CreateManualTeams(List<PlayerControl> players)
    {
        // Start with random teams in case a player doesn't have a team set.
        List<List<PlayerControl>> teams = new();
        List<byte> colorCodes = new IntRangeGen(0, ModConstants.ColorNames.Length - 1).AsEnumerable().Select(i => (byte)i).ToList();

        Dictionary<int, byte> teamToColor = new();

        int maxTeam = ColorwarsGamemode.Instance.PlayerToTeam.OrderBy(kvp => kvp.Value).Last().Value;
        for (int i = 0; i < maxTeam; i++)
        {
            teams.Add(new List<PlayerControl>());
        }

        ColorwarsGamemode.Instance.PlayerToTeam.ForEach(kvp =>
        {
            PlayerControl? targetPlayer = players.FirstOrDefault(p => p.PlayerId == kvp.Key);
            if (targetPlayer == null) return;
            players.Remove(targetPlayer);

            int teamNum = kvp.Value;
            if (!teamToColor.TryGetValue(teamNum, out byte teamColorId))
            {
                teamToColor[teamNum] = teamColorId = colorCodes.PopRandom();
            }


            teams[teamNum].Add(targetPlayer);
            targetPlayer.RpcSetColor(teamColorId);
        });

        if (players.Count > 0)
        {
            // If we are left with players, just make them their own team.
            byte colorId = colorCodes.PopRandom();
            players.Do(p => p.RpcSetColor(colorId));
            teams.Add(players);
        }

        return teams;
    }
}