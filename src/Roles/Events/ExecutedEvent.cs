using Lotus.API.Odyssey;
using Lotus.Managers.History.Events;

namespace Lotus.Roles.Events;

public class ExecutedEvent: TargetedAbilityEvent, IRoleEvent
{
    public ExecutedEvent(PlayerControl killer, PlayerControl victim) : base(killer, victim)
    {

    }

    public override string Message()
    {
        return $"{Game.GetName(Player())} executed {Game.GetName(Target())}";
    }
}

public class ExecutedDeathEvent: DeathEvent
{
    ExecutedDeathEvent()
    {

    }

    public ExecutedDeathEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
    {

    }

    public override string SimpleName() => ModConstants.DeathNames.Executed;
}