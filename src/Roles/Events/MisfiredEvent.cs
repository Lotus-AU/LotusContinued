using Lotus.API.Odyssey;
using Lotus.Managers.History.Events;

namespace Lotus.Roles.Events;

public class MisfiredEvent: DeathEvent
{
    MisfiredEvent()
    {

    }

    public MisfiredEvent(PlayerControl deadPlayer) : base(deadPlayer, deadPlayer)
    {
    }

    public override string SimpleName() => ModConstants.DeathNames.Misfired;

    public override string Message() => $"{Game.GetName(Player())} misfired.";
}