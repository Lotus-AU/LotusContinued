using Lotus.Utilities;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Options.IO;
using static Lotus.ModConstants.Palette;
using System.Collections.Generic;
using VentLib.Options;

namespace Lotus.Options.Roles;

[Localized(ModConstants.Options)]
public class NeutralOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    public int MinimumNeutralPassiveRoles;
    public int MaximumNeutralPassiveRoles;

    public int MinimumNeutralKillingRoles;
    public int MaximumNeutralKillingRoles;

    public NeutralTeaming NeutralTeamingMode;
    public bool NeutralsKnowAlliedRoles => NeutralTeamingMode is not NeutralTeaming.Disabled && knowAlliedRoles;

    private bool knowAlliedRoles;

    public NeutralOptions()
    {
        string GColor(string input) => TranslationUtil.Colorize(input, NeutralColor, PassiveColor, KillingColor);

        AllOptions.Add(new GameOptionTitleBuilder().Title(GColor(NeutralOptionTranslations.NeutralTeamMode))
            // .Tab(DefaultTabs.NeutralTab)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Text(GeneralOptionTranslations.DisabledText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(NeutralOptionTranslations.SameRoleText).Value(1).Color(GeneralColor2).Build())
            .Value(v => v.Text(GColor(NeutralOptionTranslations.KillerNeutral)).Value(2).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(3).Color(GeneralColor4).Build())
            .Builder("Neutral Teaming Mode")
            .IsHeader(true)
            .Name(GColor(NeutralOptionTranslations.NeutralTeamMode))
            .BindInt(i => NeutralTeamingMode = (NeutralTeaming)i)
            .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
            // .Tab(DefaultTabs.NeutralTab)
            .ShowSubOptionPredicate(v => (int)v >= 1)
            .SubOption(sub => sub
                .AddBoolean(true)
                .KeyName("Team Knows Each Other's Roles", NeutralOptionTranslations.AlliedKnowRoles)
                .BindBool(b => knowAlliedRoles = b)
                .Build())
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddIntRange(0, ModConstants.MaxPlayers)
            .Builder("Minimum Neutral Passive Roles")
            .IsHeader(true)
            .Name(GColor(NeutralOptionTranslations.MinimumNeutralPassiveRoles))
            .BindInt(i => MinimumNeutralPassiveRoles = i)
            // .Tab(DefaultTabs.NeutralTab)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddIntRange(0, ModConstants.MaxPlayers)
            .Builder("Maximum Neutral Passive Roles")
            .Name(GColor(NeutralOptionTranslations.MaximumNeutralPassiveRoles))
            // .Tab(DefaultTabs.NeutralTab)
            .BindInt(i => MaximumNeutralPassiveRoles = i)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddIntRange(0, ModConstants.MaxPlayers)
            // .Tab(DefaultTabs.NeutralTab)
            .BindInt(i => MinimumNeutralKillingRoles = i)
            .Builder("Minimum Neutral Killing Roles")
            .Name(GColor(NeutralOptionTranslations.MinimumNeutralKillingRoles))
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddIntRange(0, ModConstants.MaxPlayers)
            // .Tab(DefaultTabs.NeutralTab)
            .BindInt(i => MaximumNeutralKillingRoles = i)
            .Builder("Maximum Neutral Killing Roles")
            .Name(GColor(NeutralOptionTranslations.MaximumNeutralKillingRoles))
            .Build());
        PostInitialize();
    }

    [Localized("RolesNeutral")]
    public static class NeutralOptionTranslations
    {
        [Localized(nameof(MinimumNeutralPassiveRoles))]
        public static string MinimumNeutralPassiveRoles = "Minimum Neutral::0 Passive::1 Roles";

        [Localized(nameof(MaximumNeutralPassiveRoles))]
        public static string MaximumNeutralPassiveRoles = "Maximum Neutral::0 Passive::1 Roles";

        [Localized(nameof(MinimumNeutralKillingRoles))]
        public static string MinimumNeutralKillingRoles = "Minimum Neutral::0 Killing::2 Roles";

        [Localized(nameof(MaximumNeutralKillingRoles))]
        public static string MaximumNeutralKillingRoles = "Maximum Neutral::0 Killing::2 Roles";

        [Localized(nameof(NeutralTeamMode))]
        public static string NeutralTeamMode = "Neutral::0 Teaming Mode";

        [Localized(nameof(SameRoleText))]
        public static string SameRoleText = "Same Role";

        [Localized("KillingAndPassiveText")]
        public static string KillerNeutral = "Killing::2 And Passive::1";

        [Localized(nameof(AlliedKnowRoles))]
        public static string AlliedKnowRoles = "Team Knows Everyone's Role";
    }
}

public enum NeutralTeaming
{
    Disabled,
    SameRole,
    KillersNeutrals,
    All
}