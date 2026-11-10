using System.Linq;
using HarmonyLib;
using InnerNet;
using Lotus.API.Player;
using Lotus.API.Vanilla.Meetings;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ForceSkipAll))]
public class ForceSkipAllPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ForceSkipAllPatch));
    public static void Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var currentVotes = MeetingDelegate.Instance.CurrentVotes();
        Players.GetPlayers(PlayerFilter.Alive).ForEach(p =>
        {
            if (currentVotes.GetOrCompute(p.PlayerId, () => []).Any()) return;
            MeetingDelegate.Instance.CastVote(p.PlayerId, Optional<byte>.NonNull(253));
            __instance.playerStates.First(ps => ps.PlayerId.Value == p.PlayerId).VotedForId = new PlayerId(253);
        });
    }
}