using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API;
using Lotus.API.Stats;
using Lotus.Extensions;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.NeutralKilling;

public class CrewPostor : Engineer
{
    private bool refreshTasks;
    private bool warpToTarget;
    private bool canKillAllied;

    protected override void OnTaskComplete(Optional<NormalPlayerTask> _)
    {
        if (HasAllTasksComplete && refreshTasks) Tasks.AssignAdditionalTasks(this);

        if (MyPlayer.Data.IsDead) return;
        List<PlayerControl> inRangePlayers = RoleUtils.GetPlayersWithinDistance(MyPlayer, 999, true).Where(p => canKillAllied || p.Relationship(MyPlayer) is Relation.None).ToList();
        if (inRangePlayers.Count == 0) return;
        PlayerControl target = inRangePlayers.GetRandom();
        var interaction = new RangedInteraction(new FatalIntent(!warpToTarget, () => new TaskDeathEvent(target, MyPlayer)), 0, this);

        bool death = MyPlayer.InteractWith(target, interaction) is InteractionResult.Proceed;
        Game.MatchData.GameHistory.AddEvent(new TaskKillEvent(MyPlayer, target, death));
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddTaskOverrideOptions(base.RegisterOptions(optionStream)
            .IsHeader(true)
            .Tab(DefaultTabs.ImpostorsTab)
            .SubOption(sub => sub.KeyName("Warp to Target", Translations.Options.WarpToTarget)
                .AddOnOffValues()
                .BindBool(b => warpToTarget = b)
                .Build())
            .SubOption(sub => sub.KeyName("Can Kill Allies", Translations.Options.CanKillAllies)
                .AddOnOffValues(false)
                .BindBool(b => canKillAllied = b)
                .Build())
            .SubOption(sub => sub.KeyName("Refresh Tasks When All Complete", Translations.Options.RefreshTasks)
                .AddOnOffValues()
                .BindBool(b => refreshTasks = b)
                .Build()));

    protected override string ForceRoleImageDirectory() => "RoleOutfits/Imposter/crewpostor.yaml";

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleAbilityFlags(RoleAbilityFlag.IsAbleToKill)
            .RoleColor(ModConstants.Palette.MadmateColor)
            .SpecialType(SpecialType.Madmate)
            .Faction(FactionInstances.Madmates);

    public override List<Statistic> Statistics() => new() { VanillaStatistics.Kills };

    class TaskKillEvent : KillEvent, IRoleEvent
    {
        public TaskKillEvent(PlayerControl killer, PlayerControl victim, bool successful = true) : base(killer, victim, successful)
        {
        }

        public override string Message() => $"{Game.GetName(Player())} viciously completed his task and killed {Game.GetName(Target())}.";
    }

    class TaskDeathEvent : DeathEvent
    {
        TaskDeathEvent()
        {

        }

        public TaskDeathEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
        {
        }
    }

    [Localized(nameof(CrewPostor))]
    internal static class Translations
    {
        [Localized(ModConstants.Options)]
        internal static class Options
        {
            [Localized(nameof(CanKillAllies))]
            public static string CanKillAllies = "Can Kill Allies";

            [Localized(nameof(WarpToTarget))]
            public static string WarpToTarget = "Warp To Target";

            [Localized(nameof(RefreshTasks))]
            public static string RefreshTasks = "Refresh Tasks When All Complete";
        }
    }
}