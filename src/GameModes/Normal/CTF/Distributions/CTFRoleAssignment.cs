using System.Collections.Generic;
using System.Linq;
using Lotus.Extensions;
using Lotus.GameModes.Normal.CTF.Factions;
using UnityEngine;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.CTF.Distributions;

public class CTFRoleAssignment
{
    public void AssignRoles(List<PlayerControl> allPlayers)
    {
        allPlayers.Shuffle();
        int teamOneCount = Mathf.CeilToInt(allPlayers.Count / 2);

        IEnumerable<PlayerControl> teamOne = allPlayers.Take(teamOneCount); // Team Blue
        IEnumerable<PlayerControl> teamTwo = allPlayers.Skip(teamOneCount); // Team Red

        // Steal ColorFaction from ColorWars
        CTFTeamFaction blueFaction = new(1, Color.blue);
        teamOne.ForEach(p =>
        {
            CTFGamemode.Instance.Assign(p, CTFRoles.Instance.Static.Striker);
            p.PrimaryRole().RoleColor = Color.blue;
            p.PrimaryRole().Faction = blueFaction;
            p.RpcSetColor(1);
        });

        CTFTeamFaction redFaction = new(0, Color.red);
        teamTwo.ForEach(p =>
        {
            CTFGamemode.Instance.Assign(p, CTFRoles.Instance.Static.Striker);
            p.PrimaryRole().RoleColor = Color.red;
            p.PrimaryRole().Faction = redFaction;
            p.RpcSetColor(0);
        });
    }
}