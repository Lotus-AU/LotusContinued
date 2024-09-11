using System.Collections.Generic;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Stats;
using Lotus.Factions;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Victory.Conditions;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Roles.Internals;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Jester : CustomRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Jester));
    private bool canUseVents;
    private bool impostorVision;
    private int meetingThreshold;
    private bool cantCallMeetings;


    [UIComponent(UI.Counter)]
    public string MeetingCounter() => meetingThreshold > 0 ? RoleUtils.Counter(Game.MatchData.MeetingsCalled, meetingThreshold, RoleColor) : "";

    [RoleAction(LotusActionType.Exiled)]
    public void JesterWin()
    {
        if (Game.MatchData.MeetingsCalled < meetingThreshold) return;
        log.Fatal("Forcing Win by Jester");
        ManualWin jesterWin = new(MyPlayer, new WinReason(ReasonType.SoloWinner, TranslationUtil.Colorize(Translations.WinConditionName, RoleColor)), 999);
        jesterWin.Activate();
    }

    [RoleAction(LotusActionType.ReportBody)]
    public void CheckCallMeeting(Optional<NetworkedPlayerInfo> deadBody, ActionHandle handle)
    {
        // Cancel if the jester can't call emergency meetings
        if (!deadBody.Exists() && cantCallMeetings) handle.Cancel();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(opt =>
                opt.KeyName("Has Impostor Vision", Translations.Options.ImpostorVision)
                    .Bind(v => impostorVision = (bool)v).AddOnOffValues().Build())
            .SubOption(opt => opt.KeyName("Can Use Vents", Translations.Options.CanUseVents)
                .Bind(v => canUseVents = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub.KeyName("Can't Call Emergency Meetings", Translations.Options.CantCallEmergencyMeetings)
                .BindBool(b => cantCallMeetings = b)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub.KeyName("Minimum Meetings Before Win", Translations.Options.MeetingsBeforeWinning)
                .AddIntRange(0, 30)
                .BindInt(i => meetingThreshold = i)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
            .Faction(FactionInstances.Neutral)
            .VanillaRole(canUseVents ? RoleTypes.Engineer : RoleTypes.Crewmate)
            .SpecialType(SpecialType.Neutral)
            .RoleFlags(RoleFlag.CannotWinAlone)
            .RoleColor(new Color(0.93f, 0.38f, 0.65f))
            .OptionOverride(Override.CrewLightMod, () => AUSettings.ImpostorLightMod(), () => impostorVision)
            .OptionOverride(Override.EngVentDuration, 100f)
            .OptionOverride(Override.EngVentCooldown, 0.1f);
    }

    public override List<Statistic> Statistics() => new() { VanillaStatistics.TimesVented };

    [Localized(nameof(Jester))]
    private static class Translations
    {
        [Localized(nameof(WinConditionName))]
        public static string WinConditionName = "Jester::0 Exiled";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(ImpostorVision))]
            public static string ImpostorVision = "Has Impostor Vision";

            [Localized(nameof(CanUseVents))]
            public static string CanUseVents = "Can Use Vents";

            [Localized(nameof(CantCallEmergencyMeetings))]
            public static string CantCallEmergencyMeetings = "Can't Call Emergency Meetings";

            [Localized(nameof(MeetingsBeforeWinning))]
            public static string MeetingsBeforeWinning = "Minimum Meetings Before Ability to Win";
        }
    }
}