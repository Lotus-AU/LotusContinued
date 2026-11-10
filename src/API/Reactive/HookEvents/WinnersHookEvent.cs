using System.Collections.Generic;
using Hazel;
using Lotus.API.Player;
using VentLib.Utilities.Extensions;

namespace Lotus.API.Reactive.HookEvents;

public class WinnersHookEvent: INetworkedHookEvent
{
    public List<FrozenPlayer> Winners;

    WinnersHookEvent()
    {

    }

    public WinnersHookEvent(List<FrozenPlayer> winners)
    {
        Winners = winners;
    }


    public void Read(MessageReader reader)
    {
        Winners = reader.ReadList<FrozenPlayer>();
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteList(Winners);
    }
}