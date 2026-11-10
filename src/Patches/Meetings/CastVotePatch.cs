using System.Linq;
using HarmonyLib;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Meetings;
using Lotus.Extensions;
using Lotus.Managers.History.Events;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Utilities;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
public class CastVotePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(CastVotePatch));

    public static bool Prefix(MeetingHud __instance, PlayerId srcPlayerId, PlayerId suspectPlayerId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        PlayerControl voter = Utils.GetPlayerById(srcPlayerId)!;
        Optional<PlayerControl> voted = Utils.PlayerById(suspectPlayerId);
        log.Trace($"{voter.GetNameWithRole()} voted for {voted.Transform(v => v.name, () => "No one")}");

        ActionHandle handle = RoleOperations.Current.Trigger(LotusActionType.Vote, voter, voted, MeetingDelegate.Instance);

        if (!handle.IsCanceled)
        {
            Hooks.MeetingHooks.CastVoteHook.Propagate(new CastVoteHookEvent(voter, voted));
            MeetingDelegate.Instance.CastVote(voter, suspectPlayerId);
            Game.MatchData.GameHistory.AddEvent(new VotePlayerEvent(voter, voted));
            return true;
        }

        if (handle.Cancellation is ActionHandle.CancelType.Soft)
        {
            MeetingDelegate.Instance.CastVote(voter.PlayerId, Optional<byte>.NonNull(254));
            Game.MatchData.GameHistory.AddEvent(new VotePlayerEvent(voter, voted, wasCancelled: true));
            return false;
        }

        __instance.playerStates.ToArray().FirstOrDefault(state => state.PlayerId.Value == srcPlayerId)?.UnsetVote();
        Game.MatchData.GameHistory.AddEvent(new VotePlayerEvent(voter, voted, wasCancelled: true));

        log.Debug($"Canceled Vote from {voter.GetNameWithRole()}");
        Async.Schedule(() => ClearVote(__instance, voter), NetUtils.DeriveDelay(0.4f));
        Async.Schedule(() => ClearVote(__instance, voter), NetUtils.DeriveDelay(0.6f));
        return false;
    }

    public static void ClearVote(MeetingHud hud, PlayerControl target)
    {
        if (MeetingDelegate.Instance.AlreadyCompleted) return;
        log.Trace($"Clearing vote for: {target.GetNameWithRole()}");
        hud.playerStates.Where(ps => ps.PlayerId.Value == target.PlayerId).ForEach(ps => ps.VotedForId = byte.MaxValue);
        RpcV3.Immediate(hud.NetId, RpcCalls.ClearVote).Write(target.PlayerId).Send(target.OwnerId);
    }
}
