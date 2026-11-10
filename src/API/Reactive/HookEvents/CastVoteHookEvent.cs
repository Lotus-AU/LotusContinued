using Hazel;
using InnerNet;
using VentLib.Utilities.Optionals;

namespace Lotus.API.Reactive.HookEvents;

public class CastVoteHookEvent: INetworkedHookEvent
{
    public PlayerControl Voter;
    public Optional<PlayerControl> Vote;
    public bool JudgeVote;

    CastVoteHookEvent()
    {

    }

    public CastVoteHookEvent(PlayerControl voter, Optional<PlayerControl> vote, bool isOverrule = false)
    {
        this.Voter = voter;
        this.Vote = vote;
        this.JudgeVote = isOverrule;
    }


    public void Read(MessageReader reader)
    {
        Voter = reader.ReadNetObject<PlayerControl>();
        if (reader.ReadBoolean()) Vote = Optional<PlayerControl>.Of(reader.ReadNetObject<PlayerControl>());
        else Vote = Optional<PlayerControl>.Null();
        JudgeVote = reader.ReadBoolean();
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(Voter);
        if (Vote.Exists())
        {
            writer.Write(true);
            writer.WriteNetObject(Vote.Get());
        } else writer.Write(false);
        writer.Write(JudgeVote);
    }
}