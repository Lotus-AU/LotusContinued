using System;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Processes;
using Lotus.API.Reactive;
using Lotus.Victory;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.GameModes.Standard;
using Lotus.Options;
using HarmonyLib;

namespace Lotus.API.Vanilla.Meetings;

public class MeetingPrep
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(MeetingPrep));

    internal static DateTime MeetingCalledTime = DateTime.Now;
    internal static NetworkedPlayerInfo? Reported;

    private static MeetingDelegate _meetingDelegate = null!;
    public static bool Prepped;

    private const string MeetingPrepHookKey = nameof(MeetingPrepHookKey);

    static MeetingPrep()
    {
        Hooks.GameStateHooks.GameStartHook.Bind(MeetingPrepHookKey, _ => Prepped = false);
        Hooks.GameStateHooks.RoundStartHook.Bind(MeetingPrepHookKey, _ => Prepped = false);
    }

    /// <summary>
    /// This API is a little bit strange, but basically if you provide the report the meeting will actually be called. Otherwise this guarantees meeting prep has been done and returns the most recent meeting delegate.
    /// </summary>
    /// <param name="reporter">Optional player, if provided, uses rpc to call meeting</param>
    /// <param name="deadBody">Optional reported body</param>
    /// <returns>the current meeting delegate</returns>
    public static MeetingDelegate? PrepMeeting(PlayerControl? reporter = null, NetworkedPlayerInfo? deadBody = null, bool checkReportBodyCancel = true)
    {
        if (!Prepped) _meetingDelegate = new MeetingDelegate();
        if (Prepped || !AmongUsClient.Instance.AmHost) return _meetingDelegate;
        if (Game.CurrentGameMode is StandardGameMode && deadBody == null && GeneralOptions.MeetingOptions.SyncMeetingButtons)
        {
            if (Game.MatchData.EmergencyButtonsUsed >= GeneralOptions.MeetingOptions.MeetingButtonPool)
            {
                _meetingDelegate = null!;
                log.Debug($"{reporter?.name ?? "null player"}'s meeting was canceled because there are no more meetings. {Game.MatchData.EmergencyButtonsUsed} >= {GeneralOptions.MeetingOptions.MeetingButtonPool}");
                return _meetingDelegate;
            }
        }
        if (checkReportBodyCancel)
        {
            ActionHandle handle = ActionHandle.NoInit();
            if (reporter != null) RoleOperations.Current.TriggerForAll(LotusActionType.ReportBody, reporter, handle, Optional<NetworkedPlayerInfo>.Of(deadBody));
            if (handle.IsCanceled) return null;
        }

        if (deadBody == null) Game.MatchData.EmergencyButtonsUsed += 1;

        Game.State = GameState.InMeeting;

        NameUpdateProcess.Paused = true;

        if (reporter != null)
            Async.Schedule(() =>
            {
                QuickStartMeeting(reporter);
                /*Async.Schedule(() => Players.GetPlayers().ForEach(p => p.CRpcRevertShapeshift(false)), 0.1f);*/
            }, 0.1f);
        Players.GetPlayers().Do(p =>
        {
            ActionHandle handle = ActionHandle.NoInit();
            try
            {
                RoleOperations.Current.TriggerFor(p, LotusActionType.RoundEnd, null, handle, _meetingDelegate, false);
            }
            catch
            {
                // ignored. just so a meeting isn't forcefully stopped
            }
        });

        Game.RenderAllForAll(GameState.InMeeting, true);
        Async.Schedule(FixChatNames, 5f);

        log.Trace("Finished Prepping", "MeetingPrep");
        Prepped = true;

        CheckEndGamePatch.Deferred = true;
        Game.SyncAll();
        return _meetingDelegate;
    }

    private static void QuickStartMeeting(PlayerControl reporter)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MeetingCalledTime = DateTime.Now;
        if (CheckEndGamePatch.ForceCheckEndGame()) return;
        MeetingRoomManager.Instance.AssignSelf(reporter, Reported);
        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
        reporter.RpcStartMeeting(Reported);

    }

    private static void FixChatNames() => Players.GetPlayers().ForEach(p => p.RpcSetName(p.name));
}