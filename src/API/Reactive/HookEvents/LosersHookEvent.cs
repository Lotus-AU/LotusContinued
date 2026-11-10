using System.Collections.Generic;
using Hazel;
using Lotus.API.Player;
using VentLib.Utilities.Extensions;

namespace Lotus.API.Reactive.HookEvents;

public class LosersHookEvent: INetworkedHookEvent
{
    public List<FrozenPlayer> Losers;

    LosersHookEvent()
    {

    }

    public LosersHookEvent(List<FrozenPlayer> losers)
    {
        Losers = losers;
    }


    public void Read(MessageReader reader)
    {
        Losers = reader.ReadList<FrozenPlayer>();
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteList(Losers);
    }
}