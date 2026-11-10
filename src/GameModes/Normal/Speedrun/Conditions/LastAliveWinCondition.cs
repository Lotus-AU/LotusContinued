using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.GameModes.Normal.Standard.Conditions;
using Lotus.Victory.Conditions;

namespace Lotus.GameModes.Normal.Speedrun.Conditions;

public class LastAliveWinCondition: IWinCondition
{
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null;

        var alivePlayers = Players.GetAlivePlayers().ToList();

        if (alivePlayers.Count > 1) return false;
        winners = alivePlayers;
        return true;
    }
    public WinReason GetWinReason() => new(ReasonType.SoloWinner, SoloKillingWinCondition.SoloWin);
}