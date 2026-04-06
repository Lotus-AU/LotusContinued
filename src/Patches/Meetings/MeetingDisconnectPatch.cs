using HarmonyLib;
using Lotus.API;
using Lotus.Utilities;
using Lotus.Extensions;
using UnityEngine;
using System.Linq;
using Lotus.API.Vanilla.Meetings;
using VentLib.Utilities.Extensions;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleDisconnect), typeof(PlayerControl), typeof(DisconnectReasons))]
class MeetingDisconnectPatch
{
    public static void Postfix(MeetingHud __instance, PlayerControl pc, DisconnectReasons reason)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            MeetingDelegate.Instance.CurrentVotes().ForEach(kvp =>
            {
                var allVotes = kvp.Value.FindAll(v => v.OrElseGet(() => 255) == pc.PlayerId);
                allVotes.ForEach(v => kvp.Value.Remove(v));
            });
            return;
        }

        PlayerVoteArea playerVoteArea = __instance.playerStates.First(pv => pv.TargetPlayerId == pc.PlayerId);
        playerVoteArea.AmDead = true;
        playerVoteArea.Overlay.gameObject.SetActive(true);
    }
}