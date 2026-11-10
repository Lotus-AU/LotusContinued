using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using Lotus.API;
using Lotus.API.Player;
using Lotus.API.Vanilla.Meetings;
using Lotus.Utilities;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
class MeetingUpdatePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(MeetingUpdatePatch));

    public static void Postfix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
            {
                PlayerControl? player = Utils.GetPlayerById(x.PlayerId.Value);
                ProtectedRpc.CheckMurder(PlayerControl.LocalPlayer, player);

                log.High($"Execute: {player.GetNameWithRole()}", "Execution");
                // __instance.HandleDisconnectedPlayerVoteArea(player, DisconnectReasons.Custom);
                if (SoundManager.Instance) SoundManager.Instance.PlaySoundImmediate(player.KillSfx, false);
            });

        // ForceSkipAll has been inlined im pretty sure.
        var state = __instance.state;
        if (state != MeetingHud.MeetingStates.NotVoted && state != MeetingHud.MeetingStates.Voted) return;
        int votingTime = AUSettings.VotingTime();
        if (votingTime < 0) return;
        int discussionTime = AUSettings.DiscussionTime();
        if ((__instance.discussionTimer - discussionTime) < votingTime) return;
        // __instance.ForceSkipAll();
        var currentVotes = MeetingDelegate.Instance.CurrentVotes();
        Players.GetPlayers(PlayerFilter.Alive).ForEach(p =>
        {
            if (currentVotes.GetOrCompute(p.PlayerId, () => []).Any()) return;
            MeetingDelegate.Instance.CastVote(p.PlayerId, Optional<byte>.NonNull(255));
            __instance.playerStates.First(ps => ps.PlayerId.Value == p.PlayerId).VotedForId = new PlayerId(255);
        });
    }
}