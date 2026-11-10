using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.Victory.Conditions;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Collections;

namespace Lotus.Victory;

public class WinDelegate
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(WinDelegate));

    private readonly List<IWinCondition> winConditions = new() { new FallbackCondition() };
    private readonly RemoteList<Action<WinDelegate>> winNotifiers = new();

    private IWinCondition? winCondition;

    private List<PlayerControl> winners = new();
    private List<PlayerControl> additionalWinners = new();
    private WinReason winReason;
    private bool forcedWin;
    private bool forcedCancel;


    public List<PlayerControl> GetWinners() => winners;
    public List<PlayerControl> GetAdditionalWinners() => additionalWinners;
    public IEnumerable<PlayerControl> GetAllWinners() => winners.Concat(additionalWinners);

    public WinReason GetWinReason() => winReason;

    public void SetWinReason(WinReason reason) => winReason = reason;
    public void SetWinners(List<PlayerControl> winners) => this.winners = winners;

    public IWinCondition? WinCondition() => winCondition;

    public bool IsGameOver()
    {
        if (forcedWin)
        {
            log.Info($"Triggering Game Win by Force, winners={winners.Where(p => p != null).Select(p => p.name).Join()}, reason={winReason}", "WinCondition");
            winNotifiers.ForEach(notify => notify(this));
            return true;
        }

        IWinCondition? condition = winConditions.FirstOrDefault(con => con.IsConditionMet(out winners));
        if (condition == null) return false;
        if (winners == null!)
        {
            log.Warn("The list of winners was null. Please do ensure that the winner list is not null if the win condition is actually met.");
            return false;
        }

        winCondition = condition;
        winNotifiers.ForEach(notify => notify(this));

        if (winCondition.GetWinReason().ReasonType is ReasonType.HostForceEnd)
        {
            winners = [];
            additionalWinners = [];
        }

        if (forcedCancel)
        {
            winCondition = null;
            forcedCancel = false;
            return false;
        }

        winReason = condition.GetWinReason();
        log.Info($"Triggering Win by \"{condition.GetType()}\", winners={winners.Where(p => p != null).Select(p => p.name).StrJoin()}, reason={winReason}", "WinCondition");
        return true;
    }

    /// <summary>
    /// Adds a consumer which gets triggered when the game has detected a possible win. This allows for pre-win interactions
    /// as well as the possibility to cancel a game win via CancelGameWin() or to modify the game winners
    /// </summary>
    /// <param name="consumer"></param>
    public Remote<Action<WinDelegate>> AddSubscriber(Action<WinDelegate> consumer)
    {
        return winNotifiers.Add(consumer);
    }

    public void AddAdditionalWinner(PlayerControl winner)
    {
        if (winner == null) return;
        if (additionalWinners.All(p => p.PlayerId != winner.PlayerId)) additionalWinners.Add(winner);
        if (winners.All(p => p.PlayerId != winner.PlayerId)) winners.Add(winner);
    }

    public void AddWinCondition(IWinCondition condition)
    {
        winConditions.Add(condition);
        winConditions.Sort();
    }

    public void RemoveWinner(PlayerControl player) => RemoveWinner(player.PlayerId);

    public void RemoveWinner(byte playerId)
    {
        winners.RemoveAll(p => p.PlayerId == playerId);
        additionalWinners.RemoveAll(p => p.PlayerId == playerId);
    }

    public void ForceGameWin(List<PlayerControl> forcedWinners, WinReason reason, IWinCondition? win = null)
    {
        this.winners = forcedWinners;
        this.winCondition = win;
        this.winReason = reason;
        forcedWin = true;
    }

    public void CancelGameWin()
    {
        forcedCancel = true;
    }
}