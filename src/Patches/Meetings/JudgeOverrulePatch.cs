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

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.SetJudgeOverrule))]
public class JudgeOverrulePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(JudgeOverrulePatch));

    public static bool Prefix(MeetingHud __instance, PlayerId judgePlayerId, PlayerId targetPlayerId, ushort overruleNonce)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        // PlayerControl voter = Utils.GetPlayerById(srcPlayerId)!;
        // Optional<PlayerControl> voted = Utils.PlayerById(suspectPlayerId);
        // log.Trace($"{voter.GetNameWithRole()} voted for {voted.Transform(v => v.name, () => "No one")}");
        PlayerControl judge = Utils.GetPlayerById(judgePlayerId)!;
        if (!judge || !judge.Data)
        {
            log.Debug($"Could not find judge with ID {judgePlayerId}");
            return false;
        }
        if (judge.Data.Disconnected)
        {
            log.Debug($"Judge at ID {judgePlayerId} already disconnected.");
            return false;
        }
        PlayerControl target = Utils.GetPlayerById(targetPlayerId)!;
        if (!target || !target.Data)
        {
            log.Debug($"Could not find target with ID {targetPlayerId}");
            return false;
        }
        if (target.Data.Disconnected)
        {
            log.Debug($"Target at ID {targetPlayerId} already disconnected.");
            return false;
        }

        if (overruleNonce < 1)
        {
            log.Debug($"Invalid Overrule ID {overruleNonce}.");
            return false;
        }
        log.Trace($"{judge.GetNameWithRole()} wants to overrule vote for {target.GetNameWithRole()}");

        ActionHandle handle = RoleOperations.Current.Trigger(LotusActionType.Overrule, judge, target, overruleNonce, MeetingDelegate.Instance);

        if (!handle.IsCanceled)
        {
            Hooks.MeetingHooks.CastVoteHook.Propagate(new CastVoteHookEvent(judge, Optional<PlayerControl>.NonNull(target), true));
            MeetingDelegate.Instance.AddOverrule(judge, target, overruleNonce);
            Game.MatchData.GameHistory.AddEvent(new VotePlayerEvent(judge, Optional<PlayerControl>.NonNull(target), true));
            return true;
        }

        __instance.playerStates.ToArray().FirstOrDefault(state => state.PlayerId.Value == judgePlayerId)?.UnsetVote();
        Game.MatchData.GameHistory.AddEvent(new VotePlayerEvent(judge, Optional<PlayerControl>.NonNull(target), true, true));

        log.Debug($"Canceled Overrule from {judge.GetNameWithRole()}");
        Async.Schedule(() => ClearVote(__instance, judge), NetUtils.DeriveDelay(0.4f));
        Async.Schedule(() => ClearVote(__instance, judge), NetUtils.DeriveDelay(0.6f));
        return false;
    }

    public static void ClearVote(MeetingHud hud, PlayerControl target)
    {
        if (MeetingDelegate.Instance.AlreadyCompleted) return;
        log.Trace($"Clearing overrule for: {target.GetNameWithRole()}");
        hud.playerStates.Where(ps => ps.PlayerId.Value == target.PlayerId).ForEach(ps => ps.VotedForId = byte.MaxValue);
        RpcV3.Immediate(hud.NetId, RpcCalls.ClearVote).Write(target.PlayerId).Send(target.OwnerId);
    }
}
