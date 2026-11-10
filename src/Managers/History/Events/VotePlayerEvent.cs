using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Roles;
using VentLib.Utilities.Optionals;

namespace Lotus.Managers.History.Events;

public class VotePlayerEvent: IHistoryEvent
{
    private FrozenPlayer? voter;
    private CustomRole voterRole;
    private Optional<FrozenPlayer> votedFor;
    private Optional<CustomRole> votedRole;

    private bool isJudge;
    private bool wasCancelled;

    private Timestamp timestamp = new();

    public VotePlayerEvent(PlayerControl voter, Optional<PlayerControl> votedFor, bool isJudge = false,
        bool wasCancelled = false)
    {
        this.voter = Game.MatchData.GetFrozenPlayer(voter);
        voterRole = voter.PrimaryRole();
        if (votedFor.Exists())
        {
            this.votedFor = Optional<FrozenPlayer>.Of(Game.MatchData.GetFrozenPlayer(votedFor.Get()));
            votedRole = Optional<CustomRole>.NonNull(votedFor.Get().PrimaryRole());
        }
        else
        {
            this.votedFor = Optional<FrozenPlayer>.Null();
            votedRole = Optional<CustomRole>.Null();
        }

        this.isJudge = isJudge;
        this.wasCancelled = wasCancelled;
    }

    public FrozenPlayer Player() => voter!;
    public Optional<CustomRole> RelatedRole() => Optional<CustomRole>.NonNull(voterRole);

    public Optional<FrozenPlayer> Target() => votedFor;
    public Optional<CustomRole> TargetRole() => votedRole;

    public Timestamp Timestamp() => timestamp;

    public bool IsCompletion() => wasCancelled;

    public virtual string Message()
    {
        return $"{Game.GetName(voter)}'s" +
               $"{votedFor.Transform(f => $"{(wasCancelled ? "vote" : "voted")} for {Game.GetName(f)}",
                   () => $"{(wasCancelled ? "skip vote" : "skipped their vote")}")}{(wasCancelled ? " was cancelled" : ".")}";
    }
}