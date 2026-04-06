using System.Linq;
using HarmonyLib;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Meetings;
using Lotus.Extensions;
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

    public static bool Prefix(MeetingHud __instance, byte srcPlayerId, byte suspectPlayerId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        PlayerControl voter = Utils.GetPlayerById(srcPlayerId)!;
        Optional<PlayerControl> voted = Utils.PlayerById(suspectPlayerId);
        log.Trace($"{voter.GetNameWithRole()} voted for {voted.Transform(v => v.name, () => "No one")}");

        ActionHandle handle = RoleOperations.Current.Trigger(LotusActionType.Vote, voter, voted, MeetingDelegate.Instance);

        if (!handle.IsCanceled)
        {
            Hooks.MeetingHooks.CastVoteHook.Propagate(new CastVoteHookEvent(voter, voted));
            MeetingDelegate.Instance.CastVote(voter, voted);
            return true;
        }

        if (handle.Cancellation is ActionHandle.CancelType.Soft)
        {
            MeetingDelegate.Instance.CastVote(voter.PlayerId, Optional<byte>.NonNull(254));
            return false;
        }

        __instance.playerStates.ToArray().FirstOrDefault(state => state.TargetPlayerId == srcPlayerId)?.UnsetVote();

        log.Debug($"Canceled Vote from {voter.GetNameWithRole()}");
        Async.Schedule(() => ClearVote(__instance, voter), NetUtils.DeriveDelay(0.4f));
        Async.Schedule(() => ClearVote(__instance, voter), NetUtils.DeriveDelay(0.6f));
        return false;
    }

    public static void ClearVote(MeetingHud hud, PlayerControl target)
    {
        if (MeetingDelegate.Instance.AlreadyCompleted) return;
        log.Trace($"Clearing vote for: {target.GetNameWithRole()}");
        hud.playerStates.Where(ps => ps.TargetPlayerId == target.PlayerId).ForEach(ps => ps.VotedFor = byte.MaxValue);
        RpcV3.Immediate(hud.NetId, RpcCalls.ClearVote).Send(target.OwnerId);
    }
}
