using Hazel;
using InnerNet;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerShapeshiftHookEvent : INetworkedHookEvent
{
    public PlayerControl Player;
    public NetworkedPlayerInfo Target;
    public bool Reverted;

    PlayerShapeshiftHookEvent()
    {

    }

    public PlayerShapeshiftHookEvent(PlayerControl player, NetworkedPlayerInfo target, bool reverted)
    {
        Player = player;
        Target = target;
        Reverted = reverted;
    }


    public void Read(MessageReader reader)
    {
        Player = reader.ReadNetObject<PlayerControl>();
        Target = reader.ReadNetObject<NetworkedPlayerInfo>();
        Reverted = reader.ReadBoolean();
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(Player);
        writer.WriteNetObject(Target);
        writer.Write(Reverted);
    }
}