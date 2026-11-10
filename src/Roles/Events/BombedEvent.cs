using Lotus.Managers.History.Events;

namespace Lotus.Roles.Events;

public class BombedEvent : DeathEvent
{
    BombedEvent()
    {

    }

    public BombedEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
    {
    }

    public override string SimpleName() => ModConstants.DeathNames.Bombed;
}