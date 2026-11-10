using Hazel;
using InnerNet;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerMessageHookEvent : INetworkedHookEvent
{
    public PlayerControl Player;
    public string Message;

    PlayerMessageHookEvent()
    {

    }

    public PlayerMessageHookEvent(PlayerControl player, string message)
    {
        Player = player;
        Message = message;
    }


    public void Read(MessageReader reader)
    {
        Player = reader.ReadNetObject<PlayerControl>();
        Message = reader.ReadString();
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(Player);
        writer.Write(Message);
    }
}