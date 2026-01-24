using HarmonyLib;
using Lotus.API.Vanilla.Meetings;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.GetVotesRemaining))]
public class GetVotesRemainingPatch
{
    public static bool Prefix(MeetingHud __instance, ref int __result)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        __result = MeetingDelegate.Instance.GetPlayersRemainingToVote();
        return false;
    }
}