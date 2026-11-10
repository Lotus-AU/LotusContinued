using Lotus.API.Odyssey;

namespace Lotus.Managers.History.Events;

public class SuicideEvent : DeathEvent
{
    SuicideEvent()
    {

    }

    public SuicideEvent(PlayerControl player) : base(player, player)
    {
    }

    public override string SimpleName() => ModConstants.DeathNames.Suicide;

    public override string Message() => $"{Game.GetName(Player())} suicided.";
}