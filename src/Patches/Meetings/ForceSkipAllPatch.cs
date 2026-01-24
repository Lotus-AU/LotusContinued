using System.Linq;
using HarmonyLib;
using Lotus.API.Player;
using Lotus.API.Vanilla.Meetings;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ForceSkipAll))]
public class ForceSkipAllPatch
{
    public static void Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var currentVotes = MeetingDelegate.Instance.CurrentVotes();
        Players.GetPlayers(PlayerFilter.Alive).ForEach(p =>
        {
            if (currentVotes.GetOrCompute(p.PlayerId, () => []).Count > 0) return;
            MeetingDelegate.Instance.CastVote(p.PlayerId, Optional<byte>.NonNull(254));
            __instance.playerStates.First(ps => ps.TargetPlayerId == p.PlayerId).VotedFor = 254;
        });
    }
}