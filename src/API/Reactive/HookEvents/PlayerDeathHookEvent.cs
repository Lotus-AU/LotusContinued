using Hazel;
using InnerNet;
using Lotus.Managers.History.Events;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerDeathHookEvent: PlayerHookEvent
{
    public IDeathEvent CauseOfDeath;

    public PlayerDeathHookEvent()
    {

    }

    public PlayerDeathHookEvent(PlayerControl player, IDeathEvent causeOfDeath) : base(player)
    {
        CauseOfDeath = causeOfDeath;
    }


    public override void Read(MessageReader reader)
    {
        base.Read(reader);
        CauseOfDeath = IDeathEvent.ReadDeathEvent(reader);
    }

    public override void Write(MessageWriter writer)
    {
        base.Write(writer);
        CauseOfDeath.Write(writer);
    }
}