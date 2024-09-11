using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.Factions.Neutrals;
using Lotus.Managers;
using Lotus.Roles.Subroles;
using Lotus.Victory.Conditions;
using Lotus.API;
using Lotus.Extensions;
using Lotus.API.Player;

namespace Lotus.GameModes.Standard.Conditions;
public class SoloPassiveWinCondition : IWinCondition
{
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null!;
        List<PlayerControl> allPlayers = Players.GetAllPlayers().ToList();
        if (allPlayers.Count != 1) return false;

        PlayerControl lastPlayer = allPlayers[0];
        winners = new List<PlayerControl> { lastPlayer };
        return lastPlayer.PrimaryRole().Faction is Neutral;
    }

    public WinReason GetWinReason() => new(ReasonType.FactionLastStanding);
}