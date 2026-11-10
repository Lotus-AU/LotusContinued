using Lotus.API.Odyssey;
using Lotus.Managers.History.Events;
using Lotus.API;

namespace Lotus.Roles.Events;

public class CursedEvent : TargetedAbilityEvent, IRoleEvent
{
    public CursedEvent(PlayerControl killer, PlayerControl victim) : base(killer, victim)
    {
    }

    public override string Message()
    {
        return $"{Game.GetName(Player())} cursed {Game.GetName(Target())}.";
    }
}

public class CursedDeathEvent : DeathEvent
{
    CursedDeathEvent()
    {

    }

    public CursedDeathEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
    {
    }

    public override string SimpleName() => ModConstants.DeathNames.Cursed;

    public override string Message()
    {
        string baseMessage = $"{Game.GetName(Player())}'s curse";
        return $"{Game.GetName(Player())}'s curse {Instigator().Map(klr => baseMessage + $"from {klr.Name}").OrElse("")} got the best of them.";
    }
}