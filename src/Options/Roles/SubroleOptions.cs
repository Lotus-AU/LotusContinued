using Lotus.Utilities;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using System.Collections.Generic;
using VentLib.Options;

namespace Lotus.Options.Roles;

[Localized(ModConstants.Options)]
public class SubroleOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    public static Color ModifierColor = new(0.43f, 0.89f, 0.61f);

    public int ModifierLimits;
    public bool EvenlyDistributeModifiers;
    public bool UncappedModifiers => ModifierLimits == -1;

    public SubroleOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(SubroleOptionTranslations.SubroleOptions)
            // .Tab(DefaultTabs.NeutralTab)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Text(SubroleOptionTranslations.NoModifiersText).Color(Color.red).Value(0).Build())
            .Value(v => v.Text(SubroleOptionTranslations.UncappedText).Color(new Color(0.19f, 0.8f, 0f)).Value(-1).Build())
            .AddIntRange(0, 10, 1)
            .Builder("Maximum Modifiers per Player")
            .Name(TranslationUtil.Colorize(SubroleOptionTranslations.ModifierMaximumText, ModifierColor))
            .IsHeader(true)
            .BindInt(i => ModifierLimits = i)
            // .Tab(DefaultTabs.MiscTab)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean()
            .Builder("Evenly Distribute Modifiers")
            .Name(TranslationUtil.Colorize(SubroleOptionTranslations.EvenlyDistributeModifierText, ModifierColor))
            .BindBool(b => EvenlyDistributeModifiers = b)
            // .Tab(DefaultTabs.MiscTab)
            .Build());
        PostInitialize();
    }

    private GameOptionBuilder Builder(string key) => new GameOptionBuilder().Key(key).Tab(DefaultTabs.MiscTab);

    [Localized("Subroles")]
    public static class SubroleOptionTranslations
    {
        [Localized(nameof(SubroleOptions))]
        public static string SubroleOptions = "Subrole Options";
        [Localized(nameof(UncappedText))]
        public static string UncappedText = "Uncapped";

        [Localized(nameof(NoModifiersText))]
        public static string NoModifiersText = "No Modifiers";

        [Localized(nameof(ModifierMaximumText))]
        public static string ModifierMaximumText = "Maximum Modifiers::0 per Player";

        [Localized(nameof(EvenlyDistributeModifierText))]
        public static string EvenlyDistributeModifierText = "Evenly Distribute Modifiers::0";
    }
}