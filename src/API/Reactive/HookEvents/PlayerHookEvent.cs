using Hazel;
using InnerNet;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerHookEvent : INetworkedHookEvent
{
    public PlayerControl Player;

    public PlayerHookEvent()
    {

    }

    public PlayerHookEvent(PlayerControl player)
    {
        Player = player;
    }


    public virtual void Read(MessageReader reader)
    {
        Player = reader.ReadNetObject<PlayerControl>();
    }

    public virtual void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(Player);
    }
}