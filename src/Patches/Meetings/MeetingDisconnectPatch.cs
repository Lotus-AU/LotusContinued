using HarmonyLib;
using Lotus.API;
using Lotus.Utilities;
using Lotus.Extensions;
using UnityEngine;
using System.Linq;
using InnerNet;
using Lotus.API.Vanilla.Meetings;
using VentLib.Utilities.Extensions;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleDisconnect), typeof(PlayerControl), typeof(DisconnectReasons))]
class MeetingDisconnectPatch
{
    public static void Postfix(MeetingHud __instance, PlayerControl pc, DisconnectReasons reason)
    {
        if (__instance.playerStates == null) return;
        if (!pc) return;
        if (!GameData.Instance) return;
        if (!AmongUsClient.Instance.AmHost) return;

        // Original OnDisconnect function will send clear vote rpcs to these players to allow them to vote again.
        MeetingDelegate.Instance.CurrentVotes().ForEach(kvp =>
        {
            var allVotes = kvp.Value.FindAll(v => v.OrElse(255) == pc.PlayerId);
            allVotes.ForEach(v => kvp.Value.Remove(v));
        });
        var currentOverrules = MeetingDelegate.Instance.CurrentOverrules();
        for (int i = currentOverrules.Count - 1; i >= 0; i--)
        {
            JudgeOverrule judgeOverrule = currentOverrules[i];
            if (judgeOverrule.JudgePlayerId == pc.PlayerId)
            {
                currentOverrules.RemoveAt(i);
                break;
            }
            if (judgeOverrule.OverruledPlayerId == pc.PlayerId)
                judgeOverrule.OverruledPlayerId = PlayerId.Invalid;
        }
    }
}