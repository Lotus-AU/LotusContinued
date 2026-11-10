using Hazel;
using VentLib.Networking.Interfaces;

namespace Lotus.API.Reactive;

public interface INetworkedHookEvent: IHookEvent, IRpcWritable//, IRpcReadable<INetworkedHookEvent>
{
    void Read(MessageReader reader);
}