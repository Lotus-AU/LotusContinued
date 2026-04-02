using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Roles;
using Lotus.Extensions;
using Lotus.Options;


namespace Lotus.Victory.Conditions;

public class HNSImpostorWin: IFactionWinCondition
{
    private static readonly List<IFaction> ImpostorFaction = new() { FactionInstances.Impostors };

    public List<IFaction> Factions() => ImpostorFaction;

    public bool IsConditionMet(out List<IFaction> factions)
    {
        factions = ImpostorFaction;

        if (Game.State is not (GameState.Roaming or GameState.InMeeting)) return false;

        int aliveImpostors = 0;
        int aliveOthers = 0;

        foreach (CustomRole role in Players.GetAlivePlayers().Select(p => p.PrimaryRole()))
        {
            if (role.Faction.Relationship(FactionInstances.Impostors) is Relation.FullAllies or Relation.SharedWinners) aliveImpostors++;
            else aliveOthers++;

        }

        return aliveOthers <= 0;
    }

    public WinReason GetWinReason() => new(ReasonType.FactionLastStanding);
}