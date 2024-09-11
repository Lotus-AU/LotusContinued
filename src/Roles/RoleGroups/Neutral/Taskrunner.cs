extern alias JBAnnotations;
using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Vanilla.Meetings;
using Lotus.Factions;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Options;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Victory.Conditions;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.API.Player;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Taskrunner : Crewmate, IPhantomRole
{
    private bool immuneToRangedInteractions;
    private int phantomInteractionThreshold;
    private int phantomWarningThreshold;
    private bool isRevealed;

    [NewOnSetup]
    private List<Remote<IndicatorComponent>> indicatorComponents = null!;

    [RoleAction(LotusActionType.Interaction)]
    private void PhantomInteraction(Interaction interaction, ActionHandle handle)
    {
        if (!immuneToRangedInteractions && interaction is IRangedInteraction) return;
        if ((TotalTasks - TasksComplete) > phantomInteractionThreshold) handle.Cancel();
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    private void ClearComponents() => indicatorComponents.ForEach(c => c.Delete());

    [RoleAction(LotusActionType.Vote, ActionFlag.GlobalDetector, priority: Priority.Last)]
    private void ClearVotesAgainstPhantom(Optional<PlayerControl> player, MeetingDelegate meetingDelegate, ActionHandle handle)
    {
        if (!isRevealed) return;
        if (!player.Exists() || player.Get().PlayerId != MyPlayer.PlayerId) return;
        handle.Cancel();
    }

    [RoleAction(LotusActionType.VotingComplete, priority: Priority.Last)]
    private void ClearVotedOut(MeetingDelegate meetingDelegate)
    {
        if (meetingDelegate.ExiledPlayer == null) return;
        if (meetingDelegate.ExiledPlayer.PlayerId != MyPlayer.PlayerId) return;
        meetingDelegate.ExiledPlayer = null;
        PhantomReveal();
    }

    public override bool TasksApplyToTotal() => false;

    public bool IsCountedAsPlayer() => false;

    protected override void OnTaskComplete(Optional<NormalPlayerTask> _)
    {
        if (!MyPlayer.IsAlive()) return;
        if (TotalTasks == TasksComplete) ManualWin.Activate(MyPlayer, ReasonType.SoloWinner, 999);
        if ((TotalTasks - TasksComplete) > phantomWarningThreshold) return;
        PhantomReveal();
    }

    private void PhantomReveal()
    {
        if (isRevealed) return;
        MyPlayer.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(new LiveString("⚠", RoleColor), Game.InGameStates));

        Players.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).ForEach(p =>
        {
            LiveString liveString = new(() => RoleUtils.CalculateArrow(p, MyPlayer, RoleColor));
            var remote = p.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(liveString, GameState.Roaming, viewers: p));
            indicatorComponents.Add(remote);
        });
        isRevealed = true;
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
         AddTaskOverrideOptions(base.RegisterOptions(optionStream)
             .Tab(DefaultTabs.NeutralTab)
             .SubOption(sub => sub.KeyName("Immune to Ranged Interactions", Translations.Options.ImmuneToRangedInteractions)
                 .AddOnOffValues()
                 .BindBool(b => immuneToRangedInteractions = b)
                 .Build())
             .SubOption(opt =>
                opt.KeyName("Remaining Task Warning", Translations.Options.RemainingTaskWarning)
                .BindInt(v => phantomInteractionThreshold = v)
                .AddIntRange(1, 40, 1)
                .Build())
            .SubOption(opt =>
                opt.KeyName("Remaining Tasks for Targetability", Translations.Options.RemainingTaskTargetable)
                .BindInt(v => phantomWarningThreshold = v)
                .AddIntRange(1, 40, 1)
                .Build()));

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier
        .RoleColor(new Color(0.4f, 0.16f, 0.38f))
        .SpecialType(SpecialType.Neutral)
        .Faction(FactionInstances.Neutral)
        .RoleFlags(RoleFlag.CannotWinAlone);

    [Localized(nameof(Taskrunner))]
    private static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(ImmuneToRangedInteractions))]
            public static string ImmuneToRangedInteractions = "Immune to Ranged Interactions";

            [Localized(nameof(RemainingTaskWarning))]
            public static string RemainingTaskWarning = "Remaining Task Warning";

            [Localized(nameof(RemainingTaskTargetable))]
            public static string RemainingTaskTargetable = "Remaining Tasks for Targetability";
        }
    }
}