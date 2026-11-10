using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using Lotus.Victory.Conditions;
using VentLib.Localization.Attributes;

namespace Lotus.GameModes.HideAndSeek.ETF.Conditions;

public class ETFWardenWin: IFactionWinCondition
{
    [Localized($"{ModConstants.Localization.WinConditions}.{nameof(AllPlayersCaptured)}")]
    public static string AllPlayersCaptured = "All Players Captured";
    [Localized($"{ModConstants.Localization.WinConditions}.{nameof(TimeRanOut)}")]
    public static string TimeRanOut = "Time Ran Out";

    private static readonly List<IFaction> ImpostorFaction = [FactionInstances.Impostors];
    public List<IFaction> Factions() => ImpostorFaction;

    private LogicGameFlowHnS logicGameFlowHnS;
    private WinState winState;

    public ETFWardenWin()
    {
        logicGameFlowHnS = GameManager.Instance.LogicFlow.Cast<LogicGameFlowHnS>();
    }

    public bool IsConditionMet(out List<IFaction> factions)
    {
        factions = ImpostorFaction;

        if (Game.State is not GameState.Roaming) return false;

        if (logicGameFlowHnS.currentHideTime <= 0f)
        {
            if (Players.GetAllRoles().OfType<Fugitive>().Any(r => r.Escaped())) return false; // if anyone escapes we don't win
            winState = WinState.TimeOut;
            return true;
        }

        if (Players.GetAllRoles().OfType<Fugitive>().Any(r => !r.Died() && !r.Captured())) return false;
        winState = WinState.Capture;
        return true;
    }

    public WinReason GetWinReason() => winState switch
    {
        WinState.Capture => new WinReason(ReasonType.FactionLastStanding, AllPlayersCaptured),
        WinState.TimeOut => new WinReason(ReasonType.GameModeSpecificWin, TimeRanOut),
        _ => throw new NotSupportedException($"{(int)winState} ({winState.ToString()}) is not supported.")
    };

    private enum WinState
    {
        TimeOut,
        Capture
    }
}