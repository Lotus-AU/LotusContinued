using System.Collections.Generic;
using Hazel;
using InnerNet;
using VentLib.Utilities.Extensions;

namespace Lotus.API.Reactive.HookEvents;

public class ExiledHookEvent : INetworkedHookEvent
{
    public NetworkedPlayerInfo ExiledPlayer;
    public List<byte> Voters;

    public ExiledHookEvent()
    {

    }

    public ExiledHookEvent(NetworkedPlayerInfo exiledPlayer, List<byte> voters)
    {
        ExiledPlayer = exiledPlayer;
        Voters = voters;
    }


    public void Read(MessageReader reader)
    {
        ExiledPlayer = reader.ReadNetObject<NetworkedPlayerInfo>();
        Voters = reader.ReadList<byte>();
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(ExiledPlayer);
        writer.WriteList(Voters);
    }
}