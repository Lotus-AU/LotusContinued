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

public class HNSCrewmateWin: IFactionWinCondition
{
    private static readonly List<IFaction> CrewmateFaction = new() { FactionInstances.Crewmates };

    private LogicGameFlowHnS logicGameFlowHnS;

    public HNSCrewmateWin()
    {
        logicGameFlowHnS = GameManager.Instance.LogicFlow.Cast<LogicGameFlowHnS>();
    }

    public List<IFaction> Factions() => CrewmateFaction;

    public bool IsConditionMet(out List<IFaction> factions)
    {
        factions = CrewmateFaction;

        if (Game.State is not GameState.Roaming) return false;

        int aliveCrew = 0;

        foreach (CustomRole role in Players.GetAlivePlayers().Select(p => p.PrimaryRole()))
            if (role.Faction.Relationship(FactionInstances.Crewmates) is not Relation.None) aliveCrew++;


        return aliveCrew > 0 && logicGameFlowHnS.AllTimersExpired();
    }

    public WinReason GetWinReason() => new(ReasonType.FactionLastStanding);
}