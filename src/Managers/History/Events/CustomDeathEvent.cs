using Hazel;

namespace Lotus.Managers.History.Events;

public class CustomDeathEvent : DeathEvent
{
    private string deathType;

    CustomDeathEvent()
    {

    }

public CustomDeathEvent(PlayerControl deadPlayer, PlayerControl? killer, string deathType) : base(deadPlayer, killer)
    {
        this.deathType = deathType;
    }

    public override string SimpleName() => deathType;

    public override void Read(MessageReader reader)
    {
        base.Read(reader);
        deathType = reader.ReadString();
    }

    public override void Write(MessageWriter writer)
    {
        base.Write(writer);
        writer.Write(deathType);
    }
}