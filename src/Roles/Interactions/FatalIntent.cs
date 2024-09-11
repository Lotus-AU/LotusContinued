using System;
using System.Collections.Generic;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Roles.Internals.Enums;
using VentLib.Utilities.Optionals;
using Lotus.Roles.Operations;

namespace Lotus.Roles.Interactions;

public class FatalIntent : IFatalIntent
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(FatalIntent));
    private Func<IDeathEvent>? causeOfDeath;
    private bool ranged;

    public FatalIntent(bool ranged = false, Func<IDeathEvent>? causeOfDeath = null)
    {
        this.ranged = ranged;
        this.causeOfDeath = causeOfDeath;
    }

    public Optional<IDeathEvent> CauseOfDeath() => Optional<IDeathEvent>.Of(causeOfDeath?.Invoke() ?? null);

    public bool IsRanged() => ranged;

    public virtual void Action(PlayerControl initiator, PlayerControl target)
    {
        Optional<IDeathEvent> deathEvent = CauseOfDeath();
        initiator.PrimaryRole().SyncOptions();

        KillTarget(initiator, target);
        Game.MatchData.GameHistory.SetCauseOfDeath(target.PlayerId,
        deathEvent.OrElse(initiator.PlayerId == target.PlayerId
            ? new SuicideEvent(initiator)
            : new DeathEvent(target, initiator)
        ));
        Game.MatchData.RegenerateFrozenPlayers(target);

        if (!target.IsAlive()) return;
        log.Debug($"After executing the fatal action. The target \"{target.name}\" was still alive. Killer: {initiator.name}");
        RoleOperations.Current.Trigger(LotusActionType.SuccessfulAngelProtect, initiator, target);
        Game.MatchData.GameHistory.ClearCauseOfDeath(target.PlayerId);
        Game.MatchData.RegenerateFrozenPlayers(target);
    }

    public void KillTarget(PlayerControl initiator, PlayerControl target)
    {
        ProtectedRpc.CheckMurder(IsRanged() ? target : initiator, target);
    }

    public void Halted(PlayerControl actor, PlayerControl target)
    {
        actor.RpcMark(target);
    }

    private Dictionary<string, object?>? meta;
    public object? this[string key]
    {
        get => (meta ?? new Dictionary<string, object?>()).GetValueOrDefault(key);
        set => (meta ?? new Dictionary<string, object?>())[key] = value;
    }
}