using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using Lotus.Victory.Conditions;
using VentLib.Localization.Attributes;

namespace Lotus.GameModes.HideAndSeek.ETF.Conditions;

public class ETFFugitiveWin: IWinCondition
{
    [Localized($"{ModConstants.Localization.WinConditions}.{nameof(PlayersEscaped)}")]
    public static string PlayersEscaped = "Some Players Escaped";

    [Localized($"{ModConstants.Localization.WinConditions}.{nameof(WardensLeft)}")]
    public static string WardensLeft = "All Wardens Left";

    private LogicGameFlowHnS logicGameFlowHnS;
    private WinState winState;

    public ETFFugitiveWin()
    {
        logicGameFlowHnS = GameManager.Instance.LogicFlow.Cast<LogicGameFlowHnS>();
    }

    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null!;

        if (Game.State is not GameState.Roaming) return false;

        if (logicGameFlowHnS.currentHideTime <= 0f)
        {
            List<Fugitive> escapedPlayers =
                Players.GetAllRoles().OfType<Fugitive>().Where(r => r.Escaped()).ToList();
            if (!escapedPlayers.Any()) return false;
            winners = escapedPlayers.Select(r => r.MyPlayer).ToList();

            return true;
        }

        List<Fugitive> inGameFugitives = Players.GetAllRoles().OfType<Fugitive>().ToList();

        if (inGameFugitives.All(r => r.Escaped() || r.Died() || r.Captured())) // if everyone escaped or died.
        {
            if (!inGameFugitives.Any(r => r.Escaped())) return false; // if not one escaped we dont win
            winState = WinState.Escape;
            winners = inGameFugitives.Where(r => r.Escaped()).Select(r => r.MyPlayer).ToList();
            return true;
        }

        if (Players.GetAllRoles().OfType<Warden>().Any()) return false;
        winState =  WinState.ImpostorDisconnect;
        winners = Players.GetAllPlayers().ToList();
        return true;
    }

    public WinReason GetWinReason() => winState switch
    {
        WinState.Escape => new WinReason(ReasonType.GameModeSpecificWin, PlayersEscaped),
        WinState.ImpostorDisconnect => new WinReason(ReasonType.NoWinCondition, WardensLeft),
        _ => throw new NotSupportedException($"{(int)winState} ({winState.ToString()}) is not supported.")
    };

    private enum WinState
    {
        Escape,
        ImpostorDisconnect
    }
}