using Hazel;
using InnerNet;
using Lotus.API.Vanilla.Sabotages;
using VentLib.Utilities.Optionals;

namespace Lotus.API.Reactive.HookEvents;

public class SabotageFixHookEvent : SabotageHookEvent
{
    public Optional<PlayerControl> Fixer;

    public SabotageFixHookEvent()
    {

    }

    public SabotageFixHookEvent(PlayerControl? fixer, ISabotage sabotage) : base(sabotage)
    {
        Fixer = fixer == null ? Optional<PlayerControl>.Null() : Optional<PlayerControl>.Of(fixer);
    }


    public override void Read(MessageReader reader)
    {
        Sabotage = ISabotage.From((SabotageType)reader.ReadByte());
        if (reader.ReadBoolean()) Fixer = Optional<PlayerControl>.Of(reader.ReadNetObject<PlayerControl>());
        else Fixer = Optional<PlayerControl>.Null();
    }

    public override void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.Write((byte)Sabotage.SabotageType());
        if (Fixer.Exists())
        {
            writer.Write(true);
            writer.WriteNetObject(Fixer.Get());
        } else writer.Write(false);
    }
}