using Hazel;
using Lotus.API.Vanilla.Sabotages;

namespace Lotus.API.Reactive.HookEvents;

public class SabotageHookEvent: INetworkedHookEvent
{
    public ISabotage Sabotage;

    public SabotageHookEvent()
    {

    }

    public SabotageHookEvent(ISabotage sabotage)
    {
        Sabotage = sabotage;
    }


    public virtual void Read(MessageReader reader)
    {
        Sabotage = ISabotage.From((SabotageType)reader.ReadByte());
    }

    public virtual void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.Write((byte)Sabotage.SabotageType());
    }
}