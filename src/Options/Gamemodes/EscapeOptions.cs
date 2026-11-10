using Lotus.Extensions;
using Lotus.Options.General;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Vanilla;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;

namespace Lotus.Options.Gamemodes;

public class EscapeOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.EscapeOptionManager;

    public float GameLength;
    public int WardenCount;
    public float WardenSpeedMultiplier;
    public float DownedDuration;
    public float DownedSpeedMultiplier;
    public float CapturedDuration;
    public float TerminalHackTime;
    public float RescuedPlayerInvisDuration;

    public int MaxPlayersPerTerminal;
    public bool ForceWardenFlashlight;

    public EscapeOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(Translations.ETFTitle)
            .IsHeader(false)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Game Length", CaptureOptions.Translations.GameLength)
            .AddFloatRange(60, 1200, 15, 56, GeneralOptionTranslations.SecondsSuffix)
            .IsHeader(true)
            .BindFloat(f => GameLength = f)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Warden Count", Translations.WardenCount)
            .AddIntRange(1, ModConstants.MaxPlayers)
            .BindInt(i => WardenCount = i)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Warden Speed Multiplier", Translations.WardenSpeedMultiplier)
            .AddFloatRange(0.05f, 1.5f,0.05f, 19, "x")
            .BindFloat(f => WardenSpeedMultiplier = f)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Down Duration", Translations.DownedDuration)
            .AddFloatRange(10, 60, 2.5f, 7, GeneralOptionTranslations.SecondsSuffix)
            .BindFloat(f => DownedDuration = f)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Downed Speed Multiplier", Translations.DownedSpeedMultiplier)
            .AddFloatRange(0.05f, 1f,0.05f, 3, "x")
            .BindFloat(f => DownedSpeedMultiplier = f)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Captured Duration", Translations.CapturedDuration)
            .AddFloatRange(10, 60, 2.5f, 16, GeneralOptionTranslations.SecondsSuffix)
            .BindFloat(f => CapturedDuration = f)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Rescued Invis Duration", Translations.RescuedInvisDuration)
            .AddFloatRange(0, 10, .5f, 10, GeneralOptionTranslations.SecondsSuffix)
            .BindFloat(f => RescuedPlayerInvisDuration = f)
            .Build());


        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Terminal Hack Duration", Translations.TerminalHackTime)
            .AddFloatRange(10, 120, 2.5f, 24, GeneralOptionTranslations.SecondsSuffix)
            .BindFloat(f => TerminalHackTime = f)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Max Players per Terminal", Translations.MaxPlayersPerTerminal)
            .AddIntRange(1, ModConstants.MaxPlayers, 2)
            .BindInt(i => MaxPlayersPerTerminal = i)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Force Warden Flashlight", Translations.ForceWardenFlashlight)
            .AddBoolean()
            .BindBool(b => ForceWardenFlashlight = b)
            .Build());
    }

    [Localized("Speedrun")]
    public class Translations
    {
        [Localized("SectionTitle")] public static string ETFTitle = "Escape The Facility Settings";

        [Localized(nameof(GameLength))] public static string GameLength = "Game Duration";
        [Localized(nameof(WardenCount))] public static string WardenCount = "# Of Wardens";
        [Localized(nameof(WardenSpeedMultiplier))] public static string WardenSpeedMultiplier = "Warden Speed Multiplier";
        [Localized(nameof(DownedDuration))] public static string DownedDuration = "Downed Duration";
        [Localized(nameof(DownedSpeedMultiplier))] public static string DownedSpeedMultiplier = "Downed Speed Multiplier";
        [Localized(nameof(CapturedDuration))] public static string CapturedDuration = "Captured Duration";
        [Localized(nameof(RescuedInvisDuration))] public static string RescuedInvisDuration = "Rescued Player Invisible Duration";
        [Localized(nameof(TerminalHackTime))] public static string TerminalHackTime = "Terminal Hack Time";
        [Localized(nameof(MaxPlayersPerTerminal))] public static string MaxPlayersPerTerminal = "Maximum Players per Terminal";
        [Localized(nameof(ForceWardenFlashlight))] public static string ForceWardenFlashlight = "Force Warden Flashlight";
    }
}