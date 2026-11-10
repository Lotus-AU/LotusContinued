using Hazel;
using InnerNet;
using Lotus.API.Vanilla.Meetings;

namespace Lotus.API.Reactive.HookEvents;

public class MeetingHookEvent : INetworkedHookEvent
{
    public PlayerControl Caller;
    public NetworkedPlayerInfo? Reported;
    public MeetingDelegate Delegate;

    MeetingHookEvent()
    {

    }

    public MeetingHookEvent(PlayerControl caller, NetworkedPlayerInfo? reporter, MeetingDelegate meetingDelegate)
    {
        Caller = caller;
        Reported = reporter;
        Delegate = meetingDelegate;
    }

    public void Read(MessageReader reader)
    {
        Caller = reader.ReadNetObject<PlayerControl>();
        if (reader.ReadBoolean()) Reported = reader.ReadNetObject<NetworkedPlayerInfo>();
        Delegate = MeetingDelegate.Instance;
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(Caller);
        if (Reported)
        {
            writer.Write(true);
            writer.WriteNetObject(Reported);
        } else writer.Write(false);
    }
}