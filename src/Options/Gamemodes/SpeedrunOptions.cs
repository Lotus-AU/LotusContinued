using Lotus.Extensions;
using Lotus.Options.General;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Vanilla;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;

namespace Lotus.Options.Gamemodes;

public class SpeedrunOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.SpeedrunOptionManager;

    public float KillCooldown;

    public bool TaskersCanVent;
    public float VentCooldown;
    public float VentDuration;

    public bool KillingTaskersCanVent;

    public SpeedrunOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(Translations.SpeedrunTitle)
            .IsHeader(false)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Kill Cooldown", RoleTranslations.KillCooldown)
            .Value(v => v.Text(GameplayOptions.GameplayOptionTranslations.GlobalCooldown).Color(ModConstants.Palette.GlobalColor).Value(0).Build())
            .AddFloatRange(.5f, 30, .5f, 17, GeneralOptionTranslations.SecondsSuffix)
            .BindFloat(f => KillCooldown = f)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Taskers Can Vent", Translations.TaskersCanVent)
            .AddBoolean()
            .BindBool(b => TaskersCanVent = b)
            .ShowSubOptionPredicate(v => (bool)v)
            .SubOption(sub => sub
                .KeyName("Vent Cooldown", Engineer.EngineerTranslations.Options.VentCooldown)
                .AddFloatRange(0, 120, 2.5f, 16, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => VentCooldown = f)
                .Build())
            .SubOption(sub => sub
                .KeyName("Vent Duration", Engineer.EngineerTranslations.Options.VentDuration)
                .Value(1f)
                .AddFloatRange(2.5f, 120, 2.5f, 6, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => VentDuration = f)
                .Build())
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .KeyName("Killing Taskers Can Vent", Translations.KillingTaskersCanVent)
            .AddBoolean()
            .BindBool(b => KillingTaskersCanVent = b)
            .Build());
    }

    [Localized("Speedrun")]
    public class Translations
    {
        [Localized("SectionTitle")] public static string SpeedrunTitle = "Speedrun Settings";

        [Localized("TaskersCanVent")] public static string TaskersCanVent = "Taskers Can Vent";
        [Localized("KillingTaskersCanVent")] public static string KillingTaskersCanVent = "Killing Taskers Can Vent";
    }
}