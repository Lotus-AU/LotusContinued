using System.Collections.Generic;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Options.Interface;
using Lotus.Utilities;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;

namespace Lotus.Options.Roles;

[Localized(ModConstants.Options)]
public class MadmateOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    public bool MadmatesTakeImpostorSlots;
    public int MinimumMadmates;
    public int MaximumMadmates;

    string GColor(string input) => TranslationUtil.Colorize(input, ModConstants.Palette.MadmateColor);

    public MadmateOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder().Title($"★ {FactionInstances.Madmates.Name()} ★")
            .Color(ModConstants.Palette.MadmateColor)
            // .Tab(DefaultTabs.ImpostorsTab)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean()
            .Builder("Madmates Take Impostor Slots")
            .Name(GColor(Translations.MadmatesTakeImpostorSlots))
            .IsHeader(true)
            .BindBool(b => MadmatesTakeImpostorSlots = b)
            // .Tab(DefaultTabs.ImpostorsTab)
            .ShowSubOptionPredicate(b => !(bool)b)
            .SubOption(sub2 => sub2.KeyName("Minimum Madmates", GColor(Translations.MinimumMadmates))
                .AddIntRange(0, ModConstants.MaxPlayers, 1)
                .BindInt(i => MinimumMadmates = i)
                .Build())
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddIntRange(0, ModConstants.MaxPlayers, 1)
            .Builder("Maximum Madmates")
            .Name(GColor(Translations.MaximumMadmates))
            .Tab(DefaultTabs.ImpostorsTab)
            .BindInt(i => MaximumMadmates = i)
            .Build());
        PostInitialize();
    }

    private static GameOptionBuilder Builder(string key) => new GameOptionBuilder().Key(key).Tab(DefaultTabs.ImpostorsTab);

    [Localized("RolesMadmates")]
    private static class Translations
    {
        [Localized(nameof(MadmatesTakeImpostorSlots))]
        public static string MadmatesTakeImpostorSlots = "Madmates::0 Take Impostor Slots";

        public static string MinimumMadmates = "Minimum Madmates::0";

        public static string MaximumMadmates = "Maximum Madmates::0";
    }
}