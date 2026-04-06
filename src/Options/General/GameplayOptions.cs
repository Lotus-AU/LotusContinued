using System;
using System.Collections.Generic;
using Lotus.API;
using Lotus.Extensions;
using Lotus.Roles.Overrides;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;

namespace Lotus.Options.General;

[Localized(ModConstants.Options)]
public class GameplayOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    private static Color _optionColor = new(0.81f, 1f, 0.75f);

    public bool OptimizeRoleAssignment;

    public FirstKillCooldown FirstKillCooldownMode;
    private float setCooldown;

    public bool DisableTasks;
    public bool DisableCommonTasks;
    public bool DisableShortTasks;
    public bool DisableLongTasks;
    public DisabledCommonTask DisabledCommonTaskFlag;
    public DisabledShortTask DisabledShortTaskFlag;
    public DisabledLongTask DisabledLongTaskFlag;
    public bool DisableTaskWin;
    public bool GhostsSeeInfo;

    public bool BlockDeadFromSabotaging;
    public bool BlockDoorsWhenAllSabotagersAreDead;

    public bool CrewCanSoloKillers;

    public int LadderDeathChance = -1;
    public bool EnableLadderDeath => LadderDeathChance > 0;

    public ModifierTextMode ModifierTextMode;

    public float GetFirstKillCooldown(PlayerControl player)
    {
        return FirstKillCooldownMode switch
        {
            FirstKillCooldown.SetCooldown => setCooldown,
            FirstKillCooldown.GlobalCooldown => AUSettings.KillCooldown(),
            FirstKillCooldown.RoleCooldown => player.PrimaryRole().GetOverride(Override.KillCooldown)?.GetValue() as float? ?? AUSettings.KillCooldown(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public GameplayOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(GameplayOptionTranslations.GameplayOptionTitle)
            .Color(_optionColor)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean(true)
            .Builder("Optimize Role Counts for Playability", _optionColor)
            .Name(GameplayOptionTranslations.OptimizeRoleAmounts)
            .BindBool(b => OptimizeRoleAssignment = b)
            .IsHeader(true)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Text(GameplayOptionTranslations.GlobalCooldown).Color(ModConstants.Palette.GlobalColor).Value(0).Build())
            .Value(v => v.Text(GameplayOptionTranslations.SetCooldown).Color(Color.green).Value(1).Build())
            .Value(v => v.Text(GameplayOptionTranslations.RoleCooldown).Color(ModConstants.Palette.InfinityColor).Value(2).Build())
            .Builder("First Kill Cooldown", _optionColor)
            .Name(GameplayOptionTranslations.FirstKillCooldown)
            .BindInt(b => FirstKillCooldownMode = (FirstKillCooldown)b)
            .ShowSubOptionPredicate(i => (int)i == 1)
            .SubOption(sub => sub
                .AddFloatRange(0, 120, 2.5f, 4, GeneralOptionTranslations.SecondsSuffix)
                .KeyName("Set Cooldown Value", GameplayOptionTranslations.SetCooldownValue)
                .BindFloat(f => setCooldown = f)
                .Build())
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Disable Tasks", _optionColor)
            .Name(GameplayOptionTranslations.DisableTaskText)
            .AddBoolean(false)
            .BindBool(b => DisableTasks = b)
            .ShowSubOptionPredicate(b => (bool)b)
            .SubOption(sub2 => sub2
                .KeyName("Disable Common Tasks", GameplayOptionTranslations.DisableCommonTasks)
                .AddBoolean(false)
                .BindBool(b => DisableCommonTasks = b)
                .ShowSubOptionPredicate(b => (bool)b)
                .SubOption(sub => sub
                    .KeyName("Disable Card Swipe", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableCardSwipe)
                    .BindBool(FlagSetter(DisabledCommonTask.SwipeCard))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Fix Wiring", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableFixWiring)
                    .AddBoolean(false)
                    .BindBool(FlagSetter(DisabledCommonTask.FixWiring))
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Enter ID Code", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableIdCode)
                    .AddBoolean(false)
                    .BindBool(FlagSetter(DisabledCommonTask.EnterIdCode))
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Scan Boarding Pass", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableScanBoardingPass)
                    .AddBoolean(false)
                    .BindBool(FlagSetter(DisabledCommonTask.ScanBoardingPass))
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Insert Keys", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableInsertKeys)
                    .AddBoolean(false)
                    .BindBool(FlagSetter(DisabledCommonTask.InsertKeys))
                    .Build())
            .Build())
            .SubOption(sub2 => sub2
                .KeyName("Disable Short Tasks", GameplayOptionTranslations.DisableShortTasks)
                .AddBoolean(false)
                .ShowSubOptionPredicate(b => (bool)b)
                .BindBool(b => DisableShortTasks = b)
                .SubOption(sub => sub
                    .KeyName("Disable Align Telescope", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableAlignTelescope)
                    .BindBool(FlagSetter(DisabledShortTask.AlignTelescope))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Assemble Artifact", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableAssembleArtifact)
                    .BindBool(FlagSetter(DisabledShortTask.AssembleArtifact))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Buy Beverage", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableBuyBeverage)
                    .BindBool(FlagSetter(DisabledShortTask.BuyBeverage))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Calibrate Distributor", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableCalibrateDistributor)
                    .BindBool(FlagSetter(DisabledShortTask.CalibrateDistributor))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Chart Course", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableChartCourse)
                    .BindBool(FlagSetter(DisabledShortTask.ChartCourse))
                    .AddBoolean()
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Clean O2 Filter", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableCleanO2Filter)
                    .BindBool(FlagSetter(DisabledShortTask.CleanO2Filter))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Clean Toilet", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableCleanToilet)
                    .BindBool(FlagSetter(DisabledShortTask.CleanToilet))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Clean Vent", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableCleanVent)
                    .BindBool(FlagSetter(DisabledShortTask.CleanVent))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Clear Asteroids", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableClearAsteroids)
                    .BindBool(FlagSetter(DisabledShortTask.ClearAsteroids))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Decontaminate", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableDecontaminate)
                    .BindBool(FlagSetter(DisabledShortTask.Decontaminate))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Divert Power", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableDivertPower)
                    .BindBool(FlagSetter(DisabledShortTask.DivertPower))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Dress Mannequin", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableDressMannequin)
                    .BindBool(FlagSetter(DisabledShortTask.DressMannequin))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Fix Shower", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableFixShower)
                    .BindBool(FlagSetter(DisabledShortTask.FixShower))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Make Burger", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableMakeBurger)
                    .BindBool(FlagSetter(DisabledShortTask.MakeBurger))
                    .AddBoolean()
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Measure Weather", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableMeasureWeather)
                    .BindBool(FlagSetter(DisabledShortTask.MeasureWeather))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Monitor Oxygen", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableMonitorOxygen)
                    .BindBool(FlagSetter(DisabledShortTask.MonitorOxygen))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Pick Up Towels", GameplayOptionTranslations.DisableTasksOptionTranslations.DisablePickUpTowels)
                    .BindBool(FlagSetter(DisabledShortTask.PickUpTowels))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Polish Ruby", GameplayOptionTranslations.DisableTasksOptionTranslations.DisablePolishRuby)
                    .BindBool(FlagSetter(DisabledShortTask.PolishRuby))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Prime Shields", GameplayOptionTranslations.DisableTasksOptionTranslations.DisablePrimeShields)
                    .BindBool(FlagSetter(DisabledShortTask.PrimeShields))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Process Data", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableProcessData)
                    .BindBool(FlagSetter(DisabledShortTask.ProcessData))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Put Away Pistols", GameplayOptionTranslations.DisableTasksOptionTranslations.DisablePutAwayPistols)
                    .BindBool(FlagSetter(DisabledShortTask.PutAwayPistols))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Put Away Rifles", GameplayOptionTranslations.DisableTasksOptionTranslations.DisablePutAwayRifles)
                    .BindBool(FlagSetter(DisabledShortTask.PutAwayRifles))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Record Temperature", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableRecordTemperature)
                    .BindBool(FlagSetter(DisabledShortTask.RecordTemperature))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Repair Drill", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableRepairDrill)
                    .BindBool(FlagSetter(DisabledShortTask.RepairDrill))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Run Diagnostics", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableRunDiagnostics)
                    .BindBool(FlagSetter(DisabledShortTask.RunDiagnostics))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Sort Records", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableSortRecords)
                    .BindBool(FlagSetter(DisabledShortTask.SortRecords))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Sort Samples", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableSortSamples)
                    .BindBool(FlagSetter(DisabledShortTask.SortSamples))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Stabilize Steering", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableStabilizeSteering)
                    .BindBool(FlagSetter(DisabledShortTask.StabilizeSteering))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Store Artifacts", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableStoreArtifacts)
                    .BindBool(FlagSetter(DisabledShortTask.StoreArtifacts))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Unlock Manifolds", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableUnlockManifolds)
                    .BindBool(FlagSetter(DisabledShortTask.UnlockManifolds))
                    .AddBoolean(false)
                    .Build())
            .Build())
            .SubOption(sub2 => sub2
                .KeyName("Disable Long Tasks", GameplayOptionTranslations.DisableLongTasks)
                .AddBoolean(false)
                .ShowSubOptionPredicate(b => (bool)b)
                .BindBool(b => DisableLongTasks = b)
                .SubOption(sub => sub
                    .KeyName("Disable Align Engine Output", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableAlignEngineOutput)
                    .BindBool(FlagSetter(DisabledLongTask.AlignEngineOutput))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Develop Photos", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableDevelopPhotos)
                    .BindBool(FlagSetter(DisabledLongTask.DevelopPhotos))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Empty Garbage", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableEmptyGarbage)
                    .BindBool(FlagSetter(DisabledLongTask.EmptyGarbage))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Fuel Engines", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableFuelEngines)
                    .BindBool(FlagSetter(DisabledLongTask.FuelEngines))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Inspect Sample", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableInspectSample)
                    .BindBool(FlagSetter(DisabledLongTask.InspectSample))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Open Waterways", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableOpenWaterways)
                    .BindBool(FlagSetter(DisabledLongTask.OpenWaterways))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Reboot Wifi", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableRebootWifi)
                    .BindBool(FlagSetter(DisabledLongTask.RebootWifi))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Replace Water Jug", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableReplaceWaterJug)
                    .BindBool(FlagSetter(DisabledLongTask.ReplaceWaterJug))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Reset Breaker", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableResetBreaker)
                    .BindBool(FlagSetter(DisabledLongTask.ResetBreaker))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Rewind Tapes", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableRewindTapes)
                    .BindBool(FlagSetter(DisabledLongTask.RewindTapes))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Start Fans", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableStartFans)
                    .BindBool(FlagSetter(DisabledLongTask.StartFans))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Start Reactor", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableStartReactor)
                    .BindBool(FlagSetter(DisabledLongTask.StartReactor))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Submit Scan", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableSubmitScan)
                    .BindBool(FlagSetter(DisabledLongTask.SubmitScan))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Unlock Safe", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableUnlockSafe)
                    .BindBool(FlagSetter(DisabledLongTask.UnlockSafe))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Upload Data", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableUploadData)
                    .BindBool(FlagSetter(DisabledLongTask.UploadData))
                    .AddBoolean(false)
                    .Build())
                .SubOption(sub => sub
                    .KeyName("Disable Water Plants", GameplayOptionTranslations.DisableTasksOptionTranslations.DisableWaterPlants)
                    .BindBool(FlagSetter(DisabledLongTask.WaterPlants))
                    .AddBoolean(false)
                    .Build())
            .Build())
        .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Disable Task Win", _optionColor)
            .Name(GameplayOptionTranslations.DisableTaskWinText)
            .AddBoolean(false)
            .BindBool(b => DisableTaskWin = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Ghosts See Info", _optionColor)
            .Name(GameplayOptionTranslations.GhostSeeInfo)
            .AddBoolean()
            .BindBool(b => GhostsSeeInfo = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Ladder Death", _optionColor)
            .Name(GameplayOptionTranslations.LadderDeathText)
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(-1).Color(Color.red).Build())
            .AddIntRange(10, 100, 5, suffix: "%")
            .BindInt(i => LadderDeathChance = i)
            .Build());

        AllOptions.Add(Builder("Block Dead From Sabotaging")
            .Name(GameplayOptionTranslations.BlockDeadFromSabotaging)
            .AddBoolean(false)
            .BindBool(b => BlockDeadFromSabotaging = b)
            .ShowSubOptionPredicate(v => (bool)v)
            .SubOption(sub => sub
                .KeyName("Block Closing Doors when All Sabotagers are Dead", GameplayOptionTranslations.BlockDoorsWhenAllSabotagersAreDead)
                .AddBoolean()
                .BindBool(b => BlockDoorsWhenAllSabotagersAreDead = b)
                .Build())
            .Build());

        AllOptions.Add(Builder("Crew Can 1v1 Killers")
            .Name(GameplayOptionTranslations.CrewCanSoloKillers)
            .AddBoolean(false)
            .BindBool(b => CrewCanSoloKillers = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Modifier Text Mode", _optionColor)
            .Name(GameplayOptionTranslations.ModifierTextMode)
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(1).Color(Color.red).Build())
            .Value(v => v.Text(GameplayOptionTranslations.FirstValue).Value(0).Color(ModConstants.Palette.InfinityColor).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(2).Color(Color.green).Build())
            .BindInt(i => ModifierTextMode = (ModifierTextMode)i)
            .Build());

        PostInitialize();
    }

    private Action<bool> FlagSetter<T>(T disabledTask) where T : Enum
    {
        return b =>
        {
            long flagValue = Convert.ToInt64(disabledTask);

            if (disabledTask is DisabledCommonTask)
            {
                long current = Convert.ToInt64(DisabledCommonTaskFlag);
                current = b ? current | flagValue : current & ~flagValue;
                DisabledCommonTaskFlag = (DisabledCommonTask)Enum.ToObject(typeof(DisabledCommonTask), current);
            }
            else if (disabledTask is DisabledShortTask)
            {
                long current = Convert.ToInt64(DisabledShortTaskFlag);
                current = b ? current | flagValue : current & ~flagValue;
                DisabledShortTaskFlag = (DisabledShortTask)Enum.ToObject(typeof(DisabledShortTask), current);
            }
            else if (disabledTask is DisabledLongTask)
            {
                long current = Convert.ToInt64(DisabledLongTaskFlag);
                current = b ? current | flagValue : current & ~flagValue;
                DisabledLongTaskFlag = (DisabledLongTask)Enum.ToObject(typeof(DisabledLongTask), current);
            }
            else
            {
                throw new ArgumentException($"Unsupported flag enum type: {typeof(T).Name}");
            }
        };
    }

    private GameOptionBuilder Builder(string key) => new GameOptionBuilder().Key(key).Color(_optionColor);

    [Localized("Gameplay")]
    private static class GameplayOptionTranslations
    {

        [Localized("SectionTitle")]
        public static string GameplayOptionTitle = "Gameplay Options";

        [Localized(nameof(OptimizeRoleAmounts))]
        public static string OptimizeRoleAmounts = "Optimize Role Counts for Playability";

        [Localized(nameof(FirstKillCooldown))]
        public static string FirstKillCooldown = "First Kill Cooldown";

        [Localized(nameof(SetCooldown))]
        public static string SetCooldown = "Set CD";

        [Localized(nameof(SetCooldownValue))]
        public static string SetCooldownValue = "Set Cooldown Value";

        [Localized(nameof(GlobalCooldown))]
        public static string GlobalCooldown = "Global CD";

        [Localized(nameof(RoleCooldown))]
        public static string RoleCooldown = "Role CD";

        [Localized("DisableTasks")]
        public static string DisableTaskText = "Disable Tasks";

        [Localized(nameof(DisableCommonTasks))]
        public static string DisableCommonTasks = "Disable Common Tasks";

        [Localized(nameof(DisableShortTasks))]
        public static string DisableShortTasks = "Disable Short Tasks";

        [Localized(nameof(DisableLongTasks))]
        public static string DisableLongTasks = "Disable Long Tasks";

        [Localized("DisableTaskWin")]
        public static string DisableTaskWinText = "Disable Task Win";

        [Localized(nameof(GhostSeeInfo))]
        public static string GhostSeeInfo = "Ghosts See Info";

        [Localized("LadderDeath")]
        public static string LadderDeathText = "Ladder Death";

        [Localized(nameof(ModifierTextMode))]
        public static string ModifierTextMode = "Modifier Text Mode";

        [Localized(nameof(FirstValue))]
        public static string FirstValue = "First";

        [Localized(nameof(BlockDeadFromSabotaging))]
        public static string BlockDeadFromSabotaging = "Block Dead From Sabotaging";

        [Localized(nameof(BlockDoorsWhenAllSabotagersAreDead))]
        public static string BlockDoorsWhenAllSabotagersAreDead = "Block Closing Doors When All Sabotagers Are Dead";

        [Localized(nameof(CrewCanSoloKillers))]
        public static string CrewCanSoloKillers = "Killing Crew Can 1v1 Imp/NK";

        [Localized("DisabledTasks")]
        public static class DisableTasksOptionTranslations
        {
            [Localized(nameof(DisableCardSwipe))]
            public static string DisableCardSwipe = "Disable Card Swipe";

            [Localized(nameof(DisableFixWiring))]
            public static string DisableFixWiring = "Disable Fix Wiring";

            [Localized(nameof(DisableIdCode))]
            public static string DisableIdCode = "Disable Enter ID Code";

            [Localized(nameof(DisableScanBoardingPass))]
            public static string DisableScanBoardingPass = "Disable Scan Boarding Pass";

            [Localized(nameof(DisableInsertKeys))]
            public static string DisableInsertKeys = "Disable Insert Keys";

            [Localized(nameof(DisableAlignTelescope))]
            public static string DisableAlignTelescope = "Disable Align Telescope";

            [Localized(nameof(DisableAssembleArtifact))]
            public static string DisableAssembleArtifact = "Disable Assemble Artifact";

            [Localized(nameof(DisableBuyBeverage))]
            public static string DisableBuyBeverage = "Disable Buy Beverage";

            [Localized(nameof(DisableCalibrateDistributor))]
            public static string DisableCalibrateDistributor = "Disable Calibrate Distributor";

            [Localized(nameof(DisableChartCourse))]
            public static string DisableChartCourse = "Disable Chart Course";

            [Localized(nameof(DisableCleanO2Filter))]
            public static string DisableCleanO2Filter = "Disable Clean O2 Filter";

            [Localized(nameof(DisableCleanToilet))]
            public static string DisableCleanToilet = "Disable Clean Toilet";

            [Localized(nameof(DisableCleanVent))]
            public static string DisableCleanVent = "Disable Clean Vent";

            [Localized(nameof(DisableClearAsteroids))]
            public static string DisableClearAsteroids = "Disable Clear Asteroids";

            [Localized(nameof(DisableDecontaminate))]
            public static string DisableDecontaminate = "Disable Decontaminate";

            [Localized(nameof(DisableDivertPower))]
            public static string DisableDivertPower = "Disable Divert Power";

            [Localized(nameof(DisableDressMannequin))]
            public static string DisableDressMannequin = "Disable Dress Mannequin";

            [Localized(nameof(DisableFixShower))]
            public static string DisableFixShower = "Disable Fix Shower";

            [Localized(nameof(DisableMakeBurger))]
            public static string DisableMakeBurger = "Disable Make Burger";

            [Localized(nameof(DisableMeasureWeather))]
            public static string DisableMeasureWeather = "Disable Measure Weather";

            [Localized(nameof(DisableMonitorOxygen))]
            public static string DisableMonitorOxygen = "Disable Monitor Oxygen";

            [Localized(nameof(DisablePickUpTowels))]
            public static string DisablePickUpTowels = "Disable Pick Up Towels";

            [Localized(nameof(DisablePolishRuby))]
            public static string DisablePolishRuby = "Disable Polish Ruby";

            [Localized(nameof(DisablePrimeShields))]
            public static string DisablePrimeShields = "Disable Prime Shields";

            [Localized(nameof(DisableProcessData))]
            public static string DisableProcessData = "Disable Process Data";

            [Localized(nameof(DisablePutAwayPistols))]
            public static string DisablePutAwayPistols = "Disable Put Away Pistols";

            [Localized(nameof(DisablePutAwayRifles))]
            public static string DisablePutAwayRifles = "Disable Put Away Rifles";

            [Localized(nameof(DisableRecordTemperature))]
            public static string DisableRecordTemperature = "Disable Record Temperature";

            [Localized(nameof(DisableRepairDrill))]
            public static string DisableRepairDrill = "Disable Repair Drill";

            [Localized(nameof(DisableRunDiagnostics))]
            public static string DisableRunDiagnostics = "Disable Run Diagnostics";

            [Localized(nameof(DisableSortRecords))]
            public static string DisableSortRecords = "Disable Sort Records";

            [Localized(nameof(DisableSortSamples))]
            public static string DisableSortSamples = "Disable Sort Samples";

            [Localized(nameof(DisableStabilizeSteering))]
            public static string DisableStabilizeSteering = "Disable Stabilize Steering";

            [Localized(nameof(DisableStartReactor))]
            public static string DisableStartReactor = "Disable Start Reactor";

            [Localized(nameof(DisableStoreArtifacts))]
            public static string DisableStoreArtifacts = "Disable Store Artifacts";

            [Localized(nameof(DisableSubmitScan))]
            public static string DisableSubmitScan = "Disable Submit Scan";

            [Localized(nameof(DisableUnlockManifolds))]
            public static string DisableUnlockManifolds = "Disable Unlock Manifolds";

            [Localized(nameof(DisableAlignEngineOutput))]
            public static string DisableAlignEngineOutput = "Disable Align Engine Output";

            [Localized(nameof(DisableDevelopPhotos))]
            public static string DisableDevelopPhotos = "Disable Develop Photos";

            [Localized(nameof(DisableEmptyGarbage))]
            public static string DisableEmptyGarbage = "Disable Empty Garbage";

            [Localized(nameof(DisableFuelEngines))]
            public static string DisableFuelEngines = "Disable Fuel Engines";

            [Localized(nameof(DisableInspectSample))]
            public static string DisableInspectSample = "Disable Inspect Sample";

            [Localized(nameof(DisableRebootWifi))]
            public static string DisableRebootWifi = "Disable Reboot Wifi";

            [Localized(nameof(DisableOpenWaterways))]
            public static string DisableOpenWaterways = "Disable Open Waterways";

            [Localized(nameof(DisableReplaceWaterJug))]
            public static string DisableReplaceWaterJug = "Disable Replace Water Jug";

            [Localized(nameof(DisableResetBreaker))]
            public static string DisableResetBreaker = "Disable Reset Breaker";

            [Localized(nameof(DisableRewindTapes))]
            public static string DisableRewindTapes = "Disable Rewind Tapes";

            [Localized(nameof(DisableStartFans))]
            public static string DisableStartFans = "Disable Start Fans";

            [Localized(nameof(DisableUnlockSafe))]
            public static string DisableUnlockSafe = "Disable Unlock Safe";

            [Localized(nameof(DisableUploadData))]
            public static string DisableUploadData = "Disable Upload Data";

            [Localized(nameof(DisableWaterPlants))]
            public static string DisableWaterPlants = "Disable Water Plants";
        }
    }
}

[Flags]
public enum DisabledCommonTask
{
    SwipeCard = 1,
    FixWiring = 2,
    EnterIdCode = 4,
    ScanBoardingPass = 8,
    InsertKeys = 16
}

[Flags]
public enum DisabledShortTask : long
{
    AlignTelescope = 1L << 0,
    AssembleArtifact = 1L << 1,
    BuyBeverage = 1L << 2,
    CalibrateDistributor = 1L << 3,
    ChartCourse = 1L << 4,
    CleanO2Filter = 1L << 5,
    CleanToilet = 1L << 6,
    CleanVent = 1L << 7,
    ClearAsteroids = 1L << 8,
    Decontaminate = 1L << 9,
    DivertPower = 1L << 10,
    DressMannequin = 1L << 11,
    FixShower = 1L << 12,
    MakeBurger = 1L << 13,
    MeasureWeather = 1L << 14,
    MonitorOxygen = 1L << 15,
    PickUpTowels = 1L << 16,
    PolishRuby = 1L << 17,
    PrimeShields = 1L << 18,
    ProcessData = 1L << 19,
    PutAwayPistols = 1L << 20,
    PutAwayRifles = 1L << 21,
    RecordTemperature = 1L << 22,
    RepairDrill = 1L << 23,
    RunDiagnostics = 1L << 24,
    SortRecords = 1L << 25,
    SortSamples = 1L << 26,
    StabilizeSteering = 1L << 27,
    StoreArtifacts = 1L << 28,
    UnlockManifolds = 1L << 29
}

[Flags]
public enum DisabledLongTask : long
{
    AlignEngineOutput = 1L << 0,
    DevelopPhotos = 1L << 1,
    EmptyGarbage = 1L << 2,
    FuelEngines = 1L << 3,
    InspectSample = 1L << 4,
    OpenWaterways = 1L << 5,
    RebootWifi = 1L << 6,
    ReplaceWaterJug = 1L << 7,
    ResetBreaker = 1L << 8,
    RewindTapes = 1L << 9,
    StartFans = 1L << 10,
    StartReactor = 1L << 11,
    SubmitScan = 1L << 12,
    UnlockSafe = 1L << 13,
    UploadData = 1L << 14,
    WaterPlants = 1L << 15
}

public enum FirstKillCooldown
{
    GlobalCooldown,
    SetCooldown,
    RoleCooldown
}

public enum ModifierTextMode
{
    First,
    Off,
    All
}