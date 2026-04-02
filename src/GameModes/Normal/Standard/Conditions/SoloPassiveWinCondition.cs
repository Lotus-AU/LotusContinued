using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions.Neutrals;
using Lotus.Victory.Conditions;

namespace Lotus.GameModes.Normal.Standard.Conditions;
public class SoloPassiveWinCondition : IWinCondition
{
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null!;
        List<PlayerControl> allPlayers = Players.GetAllPlayers().ToList();
        if (allPlayers.Count != 1) return false;

        PlayerControl lastPlayer = allPlayers[0];
        winners = new List<PlayerControl> { lastPlayer };
        return lastPlayer.PrimaryRole().Faction is INeutralFaction;
    }

    public WinReason GetWinReason() => new(ReasonType.FactionLastStanding);
}