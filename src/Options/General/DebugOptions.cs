using System.Collections.Generic;
using System.Linq;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;

namespace Lotus.Options.General;

[Localized(ModConstants.Options)]
public class DebugOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    private static Color _optionColor = new(1f, 0.59f, 0.38f);

    public bool NoGameEnd;
    public bool NameBasedRoleAssignment;

    public DebugOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(DebugOptionTranslations.DebugOptionTitle)
            .Color(_optionColor)
            .Build());

        AllOptions.Add(Builder("No Game End")
            .Name(DebugOptionTranslations.NoGameEndText)
            .BindBool(b => NoGameEnd = b)
            .IsHeader(true)
            .Build());

        AllOptions.Add(Builder("Name Based Role Assignment")
            .Name(DebugOptionTranslations.NameBasedRoleAssignmentText)
            .BindBool(b => NameBasedRoleAssignment = b)
            .Build());

        // AllOptions.Add(Builder("Advanced Role Assignment")
        //     .Name(DebugOptionTranslations.AdvancedRoleAssignment)
        //     .BindBool(b => ProjectLotus.AdvancedRoleAssignment = b)
        //     .Build());

        AllOptions.Where(o => !o.Attributes.ContainsKey("Title")).ForEach(o => GeneralOptions.StandardOptionManager.Register(o, VentLib.Options.OptionLoadMode.LoadOrCreate));
        PostInitialize();
    }

    private GameOptionBuilder Builder(string key) => new GameOptionBuilder().AddBoolean(false).Builder(key, _optionColor);

    [Localized("Debug")]
    private static class DebugOptionTranslations
    {
        [Localized("SectionTitle")]
        public static string DebugOptionTitle = "Debug Options";

        [Localized("NoGameEnd")]
        public static string NoGameEndText = "Prevent Game Over";

        [Localized("NameRoleAssignment")]
        public static string NameBasedRoleAssignmentText = "Name-Based Role Assignment";

        [Localized("AdvancedRoleAssignment")]
        public static string AdvancedRoleAssignment = "Advanced Role Assignment";
    }
}