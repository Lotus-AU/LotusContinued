using System.Collections.Generic;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.Roles;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Options.IO;
using System;
using Lotus.Managers.Blackscreen;
using System.Linq;
using Lotus.API.Odyssey;
using VentLib.Options;
using VentLib.Utilities.Extensions;
using static Lotus.Options.General.MayhemOptions;

namespace Lotus.Options.General;

[Localized(ModConstants.Options)]
public class MiscellaneousOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    private static readonly Color _optionColor = new(1f, 0.75f, 0.81f);
    private static Dictionary<string, Action> BlackscreenResolvers = new()
    {
        {"Legacy", () => ProjectLotus.Instance.SetBlackscreenResolver(md => new LegacyResolver(md))},
        {"New", () => ProjectLotus.Instance.SetBlackscreenResolver(md => new BlackscreenResolver(md))},
    };

    public AuMap RandomMaps;

    public string AssignedPet = null!;
    public int ChangeNameUsers;
    public int AllowTeleportInLobby;
    public int ChangeColorAndLevelUsers;
    public bool AutoDisplayLastResults;
    public bool AutoDisplayCOD;
    public int SuffixMode;
    public bool ColoredNameMode;
    public string CurrentResolver;
    public int EventLogType;

    public bool UseRandomMap => randomMapOn && RandomMaps != 0;
    private bool randomMapOn;

    public GameOption BlackscreenOption;

    public MiscellaneousOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(MiscOptionTranslations.MiscOptionTitle)
            .Color(_optionColor)
            .Build());

        GameOptionBuilder AddPets(GameOptionBuilder b)
        {
            foreach ((string? key, string? value) in ModConstants.Pets) b = b.Value(v => v.Text(key).Value(value).Build());
            return b;
        }

        AllOptions.Add(AddPets(new GameOptionBuilder())
            .Builder("Assigned Pet", _optionColor)
            .Name(MiscOptionTranslations.AssignedPetText)
            .IsHeader(true)
            .BindString(s => AssignedPet = s)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Value(0).Text(GeneralOptionTranslations.OffText).Color(Color.red).Build())
            .Value(v => v.Value(1).Text(GeneralOptionTranslations.FriendsText).Color(new Color(0.85f, 0.66f, 1f)).Build())
            .Value(v => v.Value(2).Text(GeneralOptionTranslations.EveryoneText).Color(Color.green).Build())
            .Builder("Allow /name", _optionColor)
            .Name(MiscOptionTranslations.AllowNameCommand)
            .BindInt(b => ChangeNameUsers = b)
            .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Value(0).Text(GeneralOptionTranslations.OffText).Color(Color.red).Build())
            .Value(v => v.Value(1).Text(GeneralOptionTranslations.FriendsText).Color(new Color(0.85f, 0.66f, 1f)).Build())
            .Value(v => v.Value(2).Text(GeneralOptionTranslations.EveryoneText).Color(Color.green).Build())
            .Builder("Allow /color and /level", _optionColor)
            .Name(MiscOptionTranslations.AllowColorAndLevelCommand)
            .BindInt(b => ChangeColorAndLevelUsers = b)
            .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Value(0).Text(GeneralOptionTranslations.OffText).Color(Color.red).Build())
            .Value(v => v.Value(1).Text(GeneralOptionTranslations.FriendsText).Color(new Color(0.85f, 0.66f, 1f)).Build())
            .Value(v => v.Value(2).Text(GeneralOptionTranslations.EveryoneText).Color(Color.green).Build())
            .Builder("Allow /tp in and /tpout", _optionColor)
            .Name(MiscOptionTranslations.AllowTeleportCommand)
            .BindInt(b => AllowTeleportInLobby = b)
            .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean()
            .Builder("Auto Display Results", _optionColor)
            .Name(MiscOptionTranslations.AutoDisplayResultsText)
            .BindBool(b => AutoDisplayLastResults = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean()
            .Builder("Auto Display Cause of Death", _optionColor)
            .Name(MiscOptionTranslations.AutoDisplayCauseOfDeath)
            .BindBool(b => AutoDisplayCOD = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Enable Random Map", _optionColor)
            .Name(Translations.RandomMapModeText)
            .AddBoolean(false)
            .BindBool(b => randomMapOn = b)
            .ShowSubOptionPredicate(b => (bool)b)
            .SubOption(sub => sub
                .KeyName("Skeld", Translations.MapNameSkeld)
                .AddBoolean()
                .BindBool(FlagSetter(AuMap.Skeld))
                .Build())
            .SubOption(sub => sub
                .KeyName("Mira", Translations.MapNameMira)
                .AddBoolean()
                .BindBool(FlagSetter(AuMap.Mira))
                .Build())
            .SubOption(sub => sub
                .KeyName("Polus", Translations.MapNamePolus)
                .AddBoolean()
                .BindBool(FlagSetter(AuMap.Polus))
                .Build())
            .SubOption(sub => sub
                .KeyName("Airship", Translations.MapNameAirship)
                .AddBoolean()
                .BindBool(FlagSetter(AuMap.Airship))
                .Build())
            .IsHeader(true)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean(false)
            .Builder("Color Names", _optionColor)
            .Name(MiscOptionTranslations.ColorNames)
            .BindBool(b => ColoredNameMode = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Blackscreen Resolver", _optionColor)
            .Name(MiscOptionTranslations.BlackscreenResolver)
            .Values(BlackscreenResolvers.Keys)
            .BindString(s =>
            {
                CurrentResolver = s;
                if (BlackscreenResolvers.TryGetValue(s, out Action? onSelect)) onSelect();
            })
            .Build());
        BlackscreenOption = AllOptions.Last();

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Event Log", _optionColor)
            .Value(v => v.Value(0).Text(GeneralOptionTranslations.AllText).Color(Color.green).Build())
            .Value(v => v.Value(1).Text(MiscOptionTranslations.KillsOnly).Color(Color.cyan).Build())
            .Value(v => v.Value(2).Text(GeneralOptionTranslations.NoneText).Color(Color.red).Build())
            .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
            .Name(MiscOptionTranslations.EventLog)
            .BindInt(i => EventLogType = i)
            .Build());

        AllOptions.Where(o => !o.Attributes.ContainsKey("Title")).ForEach(o => GeneralOptions.StandardOptionManager.Register(o, VentLib.Options.OptionLoadMode.LoadOrCreate));
        PostInitialize();
    }

    /// <summary>
    /// Adds your custom blackscreen resolver to the option selecter.
    /// </summary>
    /// <param name="name">The name of the specified blackscreen resolver.</param>
    /// <param name="onSelect">The action that runs when option is chosen.</param>
    public static void AddBlackscreenResolver(string name, Action onSelect) => BlackscreenResolvers.Add(name, onSelect);

    private Action<bool> FlagSetter(AuMap map)
    {
        return b =>
        {
            if (b) RandomMaps |= map;
            else RandomMaps &= ~map;
        };
    }

    [Localized("Miscellaneous")]
    private static class MiscOptionTranslations
    {
        [Localized("SectionTitle")]
        public static string MiscOptionTitle = "Miscellaneous Options";

        [Localized("AssignedPet")]
        public static string AssignedPetText = "Assigned Pet";

        [Localized(nameof(AllowNameCommand))]
        public static string AllowNameCommand = "Allow /name";

        [Localized(nameof(AllowColorAndLevelCommand))]
        public static string AllowColorAndLevelCommand = "Allow /color and /level";

        [Localized(nameof(AllowTeleportCommand))]
        public static string AllowTeleportCommand = "Allow /tpin and /tpout";

        [Localized("AutoDisplayResults")]
        public static string AutoDisplayResultsText = "Auto Display Results";

        [Localized(nameof(AutoDisplayCauseOfDeath))]
        public static string AutoDisplayCauseOfDeath = "Auto Display Cause of Death";

        [Localized("SuffixMode")]
        public static string SuffixModeText = "Suffix Mode";

        [Localized(nameof(ColorNames))]
        public static string ColorNames = "Color Names";

        [Localized(nameof(BlackscreenResolver))]
        public static string BlackscreenResolver = "Blackscreen Resolver";

        [Localized(nameof(EventLog))]
        public static string EventLog = "Log Events";

        [Localized(nameof(KillsOnly))]
        public static string KillsOnly = "Only Kills";
    }

}

[Flags]
public enum AuMap
{
    Skeld = 1,
    Mira = 2,
    Polus = 4,
    Airship = 8
}