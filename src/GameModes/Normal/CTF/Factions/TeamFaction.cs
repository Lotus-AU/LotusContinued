using Lotus.GameModes.Normal.Colorwars.Factions;
using UnityEngine;

namespace Lotus.GameModes.Normal.CTF.Factions;

public class CTFTeamFaction : ColorFaction
{
    public CTFTeamFaction(int teamId, Color teamColor) : base(teamId, teamColor)
    {

    }

    public override bool CanSeeRole(PlayerControl player) => true;
}