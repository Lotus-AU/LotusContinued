using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API;
using Lotus.API.Stats;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Roles.Internals.Enums;
using Lotus.Extensions;
using Lotus.Factions.Impostors;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Patches.Systems;
using Lotus.Roles.Events;
using Lotus.Roles.Overrides;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.GameModes.Standard;

namespace Lotus.Roles.RoleGroups.Crew;

public class Sheriff : Crewmate
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Sheriff));
    private static IAccumulativeStatistic<int> _misfires = Statistic<int>.CreateAccumulative($"Roles.{nameof(Sheriff)}.Misfires", () => Translations.MisfireStat);
    public static readonly List<Statistic> SheriffStatistics = new() { VanillaStatistics.Kills, _misfires };
    public override List<Statistic> Statistics() => SheriffStatistics;

    public static Dictionary<Type, int> RoleKillerDictionary = new();

    public static List<(Func<CustomRole, bool> predicate, GameOptionBuilder builder, bool allKillable)> RoleTypeBuilders = new()
    {
        (r => r.SpecialType is SpecialType.NeutralKilling, new GameOptionBuilder()
            .KeyName("Neutral Killing Settings", TranslationUtil.Colorize(Translations.NeutralKillingSetting, ModConstants.Palette.NeutralColor, ModConstants.Palette.KillingColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2), false),
        (r => r.SpecialType is SpecialType.Neutral, new GameOptionBuilder()
            .KeyName("Neutral Passive Settings", TranslationUtil.Colorize(Translations.NeutralPassiveSetting, ModConstants.Palette.NeutralColor, ModConstants.Palette.PassiveColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2), false),
        (r => r.Faction is Factions.Impostors.Madmates, new GameOptionBuilder()
            .KeyName("Madmates Settings", TranslationUtil.Colorize(Translations.MadmateSetting, ModConstants.Palette.MadmateColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2), false)
    };


    private int totalShots;
    private bool oneShotPerRound;
    private bool canKillCrewmates;
    private bool isSheriffDesync;

    private bool shotThisRound;
    private int shotsRemaining;

    [UIComponent(UI.Cooldown)]
    private Cooldown shootCooldown;

    public Sheriff()
    {
        StandardRoles.Callbacks.Add(PopulateSheriffOptions);
    }

    protected override void Setup(PlayerControl player)
    {
        if (!isSheriffDesync) base.Setup(player);
        shotsRemaining = totalShots;
    }

    public override bool HasTasks() => !isSheriffDesync;

    private bool HasShots() => !(oneShotPerRound && shotThisRound) && shotsRemaining >= 0;

    [UIComponent(UI.Counter, ViewMode.Additive, GameState.Roaming, GameState.InMeeting)]
    public string RemainingShotCounter() => RoleUtils.Counter(shotsRemaining, totalShots);

    [RoleAction(LotusActionType.RoundStart)]
    public bool RefreshShotThisRound() => shotThisRound = false;

    [RoleAction(LotusActionType.OnPet)]
    public bool TryKillWithPet(ActionHandle handle)
    {
        log.Trace("Sheriff Shoot Ability (Pet)", "SheriffAbility");
        handle.Cancel();
        if (isSheriffDesync || !shootCooldown.IsReady() || !HasShots()) return false;
        List<PlayerControl> closestPlayers = MyPlayer.GetPlayersInAbilityRangeSorted();
        if (closestPlayers.Count == 0) return false;
        PlayerControl target = closestPlayers[0];
        return TryKill(target, handle);
    }

    [RoleAction(LotusActionType.Attack)]
    public bool TryKill(PlayerControl target, ActionHandle handle)
    {
        handle.Cancel();
        if (!shootCooldown.IsReady() || !HasShots()) return false;
        shotsRemaining--;
        if (!isSheriffDesync) shootCooldown.Start();


        CustomRole role = target.PrimaryRole();
        int setting = RoleTypeBuilders.FirstOrOptional(rtb => rtb.predicate(role)).Map(rtb => rtb.allKillable ? 1 : 0).OrElse(0);
        if (setting == 0)
        {
            setting = RoleKillerDictionary.GetValueOrDefault(role.GetType(), 0);
            if (setting == 0) setting = role.Faction.GetType() == typeof(ImpostorFaction) ? 1 : 2;
        }

        if (setting == 2) return Suicide(target);
        return MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this)) is InteractionResult.Proceed;
    }

    private bool Suicide(PlayerControl target)
    {
        if (canKillCrewmates)
        {
            bool killed = MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this)) is InteractionResult.Proceed;
            Game.MatchData.GameHistory.AddEvent(new KillEvent(MyPlayer, target, killed));
        }

        DeathEvent deathEvent = new MisfiredEvent(MyPlayer);
        UnblockedInteraction lotusInteraction = new(new FatalIntent(false, () => deathEvent), this);
        MyPlayer.InteractWith(MyPlayer, lotusInteraction);
        return true;
    }
    // OPTIONS

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Color(RoleColor)
            .SubOption(sub => sub
                .KeyName("Kill On Misfire", Translations.Options.KillOnMisfire)
                .Bind(v => canKillCrewmates = (bool)v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .KeyName("Kill Cooldown", Translations.Options.KillCooldown)
                .BindFloat(this.shootCooldown.SetDuration)
                .AddFloatRange(0, 120, 2.5f, 12, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Total Shots", Translations.Options.TotalShots)
                .Bind(v => this.totalShots = (int)v)
                .AddIntRange(1, 60, 1, 4)
                .Build())
            .SubOption(sub => sub
                .KeyName("One Shot Per Round", Translations.Options.OneShotPerRound)
                .Bind(v => this.oneShotPerRound = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .KeyName("Sheriff Action Button", Translations.Options.SheriffActionButton)
                .Bind(v => isSheriffDesync = (bool)v)
                .Value(v => v.Text("Kill Button (legacy)").Value(true).Color(Color.green).Build())
                .Value(v => v.Text("Pet Button").Value(false).Color(Color.cyan).Build())
                .Build());

    // Sheriff is not longer a desync role for simplicity sake && so that they can do tasks
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .DesyncRole(isSheriffDesync ? RoleTypes.Impostor : RoleTypes.Crewmate)
            .OptionOverride(Override.ImpostorLightMod, () => AUSettings.CrewLightMod(), () => isSheriffDesync)
            .OptionOverride(Override.ImpostorLightMod, () => AUSettings.CrewLightMod() / 5, () => isSheriffDesync && SabotagePatch.CurrentSabotage != null && SabotagePatch.CurrentSabotage.SabotageType() is SabotageType.Lights)
            .OptionOverride(Override.KillCooldown, () => shootCooldown.Duration)
            .RoleAbilityFlags(RoleAbilityFlag.CannotVent | RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.IsAbleToKill | RoleAbilityFlag.UsesPet)
            .RoleColor(new Color(0.97f, 0.8f, 0.27f))
            .IntroSound(RoleTypes.Crewmate);

    private void PopulateSheriffOptions()
    {
        StandardGameMode.Instance.RoleManager.RoleHolder.AllRoles.OrderBy(r => r.EnglishRoleName).ForEach(r =>
        {
            RoleTypeBuilders.FirstOrOptional(b => b.predicate(r)).Map(i => i.builder)
                .IfPresent(builder =>
                {
                    builder.SubOption(sub => sub.KeyName(r.EnglishRoleName, r.RoleColor.Colorize(r.RoleName))
                        .AddOnOffValues(r.SpecialType is not SpecialType.Neutral)
                        .BindBool(b =>
                        {
                            if (b) RoleKillerDictionary[r.GetType()] = 1;
                            else RoleKillerDictionary[r.GetType()] = 2;
                        })
                        .Build());
                });
        });
        RoleTypeBuilders.ForEach(rtb =>
        {
            rtb.builder.BindInt(i => rtb.allKillable = i == 1);
            Option option = rtb.builder.Build();
            RoleOptions.AddChild(option);
            GlobalRoleManager.RoleOptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        });
    }

    [Localized(nameof(Sheriff))]
    public static class Translations
    {
        [Localized(nameof(MisfireStat))]
        public static string MisfireStat = "Misfires";

        [Localized(nameof(NeutralKillingSetting))]
        public static string NeutralKillingSetting = "Can Kill Neutral::0 Killing::1";

        [Localized(nameof(NeutralPassiveSetting))]
        public static string NeutralPassiveSetting = "Can Kill Neutral::0 Passive::1";

        [Localized(nameof(MadmateSetting))]
        public static string MadmateSetting = "Can Kill Madmates::0 Settings";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(SheriffActionButton))] public static string SheriffActionButton = "Sheriff Action Button";
            [Localized(nameof(OneShotPerRound))] public static string OneShotPerRound = "One Shot Per Round";
            [Localized(nameof(KillOnMisfire))] public static string KillOnMisfire = "Kill Target On Misfire";
            [Localized(nameof(KillCooldown))] public static string KillCooldown = "Shoot Cooldown";
            [Localized(nameof(TotalShots))] public static string TotalShots = "Total Shots";
        }
    }
}