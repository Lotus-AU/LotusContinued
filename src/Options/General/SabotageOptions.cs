using System;
using System.Collections.Generic;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;

namespace Lotus.Options.General;

[Localized(ModConstants.Options)]
public class SabotageOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    private static Color _optionColor = new(1f, 0.94f, 0.63f);

    public int SkeldReactorCountdown;
    public bool CustomSkeldReactorCountdown => SkeldReactorCountdown != -1;

    public int SkeldOxygenCountdown;
    public bool CustomSkeldOxygenCountdown => SkeldOxygenCountdown != -1;

    public int MiraReactorCountdown;
    public bool CustomMiraReactorCountdown => MiraReactorCountdown != -1;

    public int MiraOxygenCountdown;
    public bool CustomMiraOxygenCountdown => MiraOxygenCountdown != -1;

    public int PolusReactorCountdown;
    public bool CustomPolusReactorCountdown => PolusReactorCountdown != -1;

    public int AirshipReactorCountdown;
    public bool CustomAirshipReactorCountdown => AirshipReactorCountdown != -1;

    public SabotageType DisabledSabotages => disableSabotages ? 0 : disabledSabotageTypes;
    private bool disableSabotages;
    private SabotageType disabledSabotageTypes;

    public SabotageOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(SabotageOptionTranslations.SabotageOptionTitle)
            .Color(_optionColor)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean(false)
            .IsHeader(true)
            .Name(SabotageOptionTranslations.DisableSabotagesText)
            .Key("Disable Sabotages")
            .Color(_optionColor)
            .ShowSubOptionPredicate(b => (bool)b)
            .BindBool(b => this.disableSabotages = b)
            .SubOption(sub => sub
                .AddBoolean(false)
                .Key("Disable Reactor")
                .Name(SabotageOptionTranslations.DisableReactor)
                .BindBool(FlagSetter(SabotageType.Reactor))
                .Build())
            .SubOption(sub => sub
                .AddBoolean(false)
                .Key("Disable Oxygen")
                .Name(SabotageOptionTranslations.DisableOxygen)
                .BindBool(FlagSetter(SabotageType.Oxygen))
                .Build())
            .SubOption(sub => sub
                .AddBoolean(false)
                .Key("Disable Lights")
                .Name(SabotageOptionTranslations.DisableLights)
                .BindBool(FlagSetter(SabotageType.Lights))
                .Build())
            .SubOption(sub => sub
                .AddBoolean(false)
                .Key("Disable Communications")
                .Name(SabotageOptionTranslations.DisableCommunications)
                .BindBool(FlagSetter(SabotageType.Communications))
                .Build())
            .SubOption(sub => sub
                .AddBoolean(false)
                .Key("Disable Doors")
                .Name(SabotageOptionTranslations.DisableDoors)
                .BindBool(FlagSetter(SabotageType.Door))
                .Build())
            .SubOption(sub => sub
                .AddBoolean(false)
                .Key("Disable Helicopters")
                .Name(SabotageOptionTranslations.DisableHelicopters)
                .BindBool(FlagSetter(SabotageType.Helicopter))
                .Build())
            .Build());

        AllOptions.Add(Builder("Skeld Reactor Countdown")
            .Name(AuMap.Skeld + " " + SabotageOptionTranslations.ReactorCountdown)
            .BindInt(i => SkeldReactorCountdown = i)
            .Build());

        AllOptions.Add(Builder("Skeld Oxygen Countdown")
            .Name(AuMap.Skeld + " " + SabotageOptionTranslations.OxygenCountdown)
            .BindInt(i => SkeldOxygenCountdown = i)
            .Build());

        AllOptions.Add(Builder("Mira Reactor Countdown")
            .Name(AuMap.Mira + " " + SabotageOptionTranslations.ReactorCountdown)
            .BindInt(i => MiraReactorCountdown = i)
            .Build());

        AllOptions.Add(Builder("Mira Oxygen Countdown")
            .Name(AuMap.Mira + " " + SabotageOptionTranslations.OxygenCountdown)
            .BindInt(i => MiraOxygenCountdown = i)
            .Build());

        AllOptions.Add(Builder("Polus Reactor Countdown")
            .Name(AuMap.Polus + " " + SabotageOptionTranslations.ReactorCountdown)
            .BindInt(i => PolusReactorCountdown = i)
            .Build());

        AllOptions.Add(Builder("Airship Crash Course Countdown")
            .Name(AuMap.Airship + " " + SabotageOptionTranslations.CrashCourseCountdown)
            .BindInt(i => AirshipReactorCountdown = i)
            .Build());

        PostInitialize();
    }

    private Action<bool> FlagSetter(SabotageType map)
    {
        return b =>
        {
            if (b) disabledSabotageTypes |= map;
            else disabledSabotageTypes &= ~map;
        };
    }

    private GameOptionBuilder Builder(string key) => new GameOptionBuilder()
        .Value(v => v.Text(SabotageOptionTranslations.DefaultValue).Value(-1).Color(Color.cyan).Build())
        .AddIntRange(5, 120, 5, 0, GeneralOptionTranslations.SecondsSuffix)
        .Key(key)
        .Color(_optionColor);

    [Localized("Sabotage")]
    private static class SabotageOptionTranslations
    {
        [Localized("SectionTitle")]
        public static string SabotageOptionTitle = "Sabotage Options";

        [Localized("DefaultValue")]
        public static string DefaultValue = "Default";

        [Localized("DisableSabotages")]
        public static string DisableSabotagesText = "Disable Sabotages";

        [Localized(nameof(ReactorCountdown))]
        public static string ReactorCountdown = "Reactor Countdown";

        [Localized(nameof(OxygenCountdown))]
        public static string OxygenCountdown = "Oxygen Countdown";

        [Localized(nameof(CrashCourseCountdown))]
        public static string CrashCourseCountdown = "Crash Course Countdown";

        [Localized(nameof(DisableReactor))]
        public static string DisableReactor = "Disable Reactor";

        [Localized(nameof(DisableOxygen))]
        public static string DisableOxygen = "Disable Oxygen";

        [Localized(nameof(DisableLights))]
        public static string DisableLights = "Disable Lights";

        [Localized(nameof(DisableCommunications))]
        public static string DisableCommunications = "Disable Communications";

        [Localized(nameof(DisableDoors))]
        public static string DisableDoors = "Disable Doors";

        [Localized(nameof(DisableHelicopters))]
        public static string DisableHelicopters = "Disable Crash Course";
    }
}