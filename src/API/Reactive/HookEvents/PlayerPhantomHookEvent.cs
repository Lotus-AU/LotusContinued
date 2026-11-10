using Hazel;
using InnerNet;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerPhantomHookEvent: INetworkedHookEvent
{
    public PlayerControl Player { get; private set; } = null!;
    public bool IsInvisible { get; private set; }

    PlayerPhantomHookEvent()
    {
    }

    public PlayerPhantomHookEvent(PlayerControl player, bool isInvisible)
    {
        Player = player;
        IsInvisible = isInvisible;
    }

    public void Read(MessageReader reader)
    {
        Player =  reader.ReadNetObject<PlayerControl>();
        IsInvisible = reader.ReadBoolean();
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(Player);
        writer.Write(IsInvisible);
    }
}