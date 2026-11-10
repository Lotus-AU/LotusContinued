using System.Collections.Generic;
using Hazel;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers.History.Events;

public class ExiledEvent : DeathEvent
{
    private List<PlayerControl> voters;
    private List<PlayerControl> abstainers;

    ExiledEvent()
    {

    }

    public ExiledEvent(PlayerControl deadPlayer, List<PlayerControl> voters, List<PlayerControl> abstainers) : base(deadPlayer, null)
    {
        this.voters = voters;
        this.abstainers = abstainers;
    }

    public List<PlayerControl> Abstainers() => abstainers;

    public List<PlayerControl> Voters() => voters;

    public override string SimpleName() => ModConstants.DeathNames.Exiled;

    public override void Read(MessageReader reader)
    {
        base.Read(reader);
        voters = reader.ReadList<PlayerControl>();
        abstainers = reader.ReadList<PlayerControl>();
    }

    public override void Write(MessageWriter writer)
    {
        base.Write(writer);
        writer.WriteList(voters);
        writer.WriteList(abstainers);
    }
}