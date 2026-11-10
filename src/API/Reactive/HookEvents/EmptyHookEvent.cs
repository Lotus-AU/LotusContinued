using Hazel;
using Il2CppSystem;
using VentLib.Networking.Interfaces;

namespace Lotus.API.Reactive.HookEvents;

public class EmptyHookEvent: INetworkedHookEvent
{
    public static readonly EmptyHookEvent Hook = new();

    public EmptyHookEvent()
    {

    }

    public void Read(MessageReader reader) {}

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
    }
}