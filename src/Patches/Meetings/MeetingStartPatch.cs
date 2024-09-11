using System;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Meetings;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.Managers;
using Lotus.Options.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Utilities;
using Lotus.Options;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using Lotus.Options.General;
using Lotus.GameModes.Standard;

namespace Lotus.Patches.Meetings;


[LoadStatic]
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public class MeetingStartPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(MeetingStartPatch));
    private static int buttonsRemaining = 255;

    static MeetingStartPatch()
    {
        PluginDataManager.TemplateManager.RegisterTag("meeting-first", "The template to show during the first meeting.");
        PluginDataManager.TemplateManager.RegisterTag("meeting-subsequent", "The template to show during all meetings after the first.");
        PluginDataManager.TemplateManager.RegisterTag("meeting-start", "The template to show during each meeting.");
    }

    public static void Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        log.Info("------------Meeting Start------------", "Phase");

        MeetingDelegate meetingDelegate = MeetingPrep.PrepMeeting()!;
        PlayerControl reporter = Utils.GetPlayerById(__instance.reporterId)!;


        Players.GetPlayers().Do(p =>
        {
            ActionHandle handle = ActionHandle.NoInit();
            try
            {
                if (Game.MatchData.MeetingsCalled == 0)
                    PluginDataManager.TemplateManager.GetTemplates("meeting-first")
                        ?.ForEach(t => t.SendMessage(PlayerControl.LocalPlayer, p));
                else
                    PluginDataManager.TemplateManager.GetTemplates("meeting-subsequent")
                        ?.ForEach(t => t.SendMessage(PlayerControl.LocalPlayer, p));

                PluginDataManager.TemplateManager.GetTemplates("meeting-start")
                    ?.ForEach(t => t.SendMessage(PlayerControl.LocalPlayer, p));
            }
            catch (Exception ex)
            {
                log.Exception("Error Sending Template Information!", ex);
            }
            finally
            {
                // RoleOperations.Current.TriggerFor(p, LotusActionType.RoundEnd, null, handle, meetingDelegate, false);
            }
        });

        Hooks.MeetingHooks.MeetingCalledHook.Propagate(new MeetingHookEvent(reporter, MeetingPrep.Reported, meetingDelegate));
        Hooks.GameStateHooks.RoundEndHook.Propagate(new GameStateHookEvent(Game.MatchData, ProjectLotus.GameModeManager.CurrentGameMode));
        Game.MatchData.MeetingsCalled++;

        Game.SyncAll(); // This syncs up all the cooldowns to fix doubling after meeting

        if (!GeneralOptions.MeetingOptions.SyncMeetingButtons || Game.CurrentGameMode is not StandardGameMode) return;

        if (buttonsRemaining != Game.MatchData.EmergencyButtonsUsed) return;
        buttonsRemaining = Game.MatchData.EmergencyButtonsUsed;

        int remainingButtons = GeneralOptions.MeetingOptions.MeetingButtonPool - Game.MatchData.EmergencyButtonsUsed;

        ChatHandler.Of(MeetingOptions.MeetingOptionTranslations.ButtonsRemainingMessage.Formatted(remainingButtons))
            .Title(t => t.PrefixSuffix("⚠").Color(ModConstants.Palette.InvalidUsage).Text(MeetingOptions.MeetingOptionTranslations.SingleMeetingPool).Build())
            .Send();
    }

    public static void Postfix(MeetingHud __instance)
    {
        /*SoundManager.Instance.ChangeMusicVolume(0f);*/
        if (AmongUsClient.Instance.AmHost) __instance.playerStates.ToArray()
            .FirstOrOptional(ps => ps.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId)
            .IfPresent(voteArea => voteArea.NameText.text = PlayerControl.LocalPlayer.NameModel().Render(sendToPlayer: false, force: true));
    }
}