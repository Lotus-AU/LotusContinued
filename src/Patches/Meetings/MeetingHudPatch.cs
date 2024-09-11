using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Vanilla.Meetings;
using Lotus.Server;
using Lotus.Options;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Patches.Meetings;


[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}


[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
class MeetingHudOnDestroyPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(MeetingHudOnDestroyPatch));

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        log.Debug("------------End of Meeting------------", "Phase");
        MeetingDelegate meetingDelegate = MeetingDelegate.Instance;

        if (meetingDelegate.ExiledPlayer != null && meetingDelegate.ExiledPlayer.Object != null)
            meetingDelegate.CheckAndSetConfirmEjectText(meetingDelegate.ExiledPlayer.Object);

        /*Players.GetPlayers().Where(p =>
        {
            p.RpcRevertShapeshift(false);
            return p.PlayerId != meetingDelegate.ExiledPlayer?.PlayerId;
        }).ForEach(p => p.RpcSetName(p.name));*/

        meetingDelegate.BlackscreenResolver.OnMeetingEnd();
        Async.Schedule(PostMeetingSetups, NetUtils.DeriveDelay(0.5f));
    }


    private static void PostMeetingSetups()
    {
        bool randomSpawn = GeneralOptions.MayhemOptions.RandomSpawn;

        Players.GetPlayers().ForEach(p =>
        {
            if (randomSpawn) Game.RandomSpawn.Spawn(p);
        });
    }
}