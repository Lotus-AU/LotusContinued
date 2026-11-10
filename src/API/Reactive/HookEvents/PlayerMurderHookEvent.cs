using Hazel;
using InnerNet;
using Lotus.Managers.History.Events;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerMurderHookEvent: PlayerDeathHookEvent
{
    public PlayerControl Killer;
    public PlayerControl Victim;

    public PlayerMurderHookEvent()
    {

    }

    public PlayerMurderHookEvent(PlayerControl killer, PlayerControl victim, IDeathEvent causeOfDeath) : base(victim, causeOfDeath)
    {
        Killer = killer;
        Victim = victim;
    }


    public override void Read(MessageReader reader)
    {
        base.Read(reader);
        Killer =  reader.ReadNetObject<PlayerControl>();
        Victim = Player;
    }

    public override void Write(MessageWriter writer)
    {
        base.Write(writer);
        writer.WriteNetObject(Killer);
    }
}