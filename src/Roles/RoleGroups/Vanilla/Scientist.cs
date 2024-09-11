using AmongUs.GameOptions;
using Lotus.Options;
using Lotus.Roles.Overrides;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;

namespace Lotus.Roles.RoleGroups.Vanilla;

public class Scientist : Crewmate
{
    protected float VitalsCooldown;
    protected float VitalsBatteryCharge;

    protected GameOptionBuilder AddVitalsOptions(GameOptionBuilder builder)
    {
        return builder.SubOption(sub => sub
                .Key("Vital Cooldown")
                .Name(Translations.Options.VitalsCooldown)
                .AddFloatRange(0, 120, 2.5f, 16, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => VitalsCooldown = f)
                .Build())
            .SubOption(sub => sub.Name(Translations.Options.VitalsBatteryCharge)
                .Key("Vitals Battery Charge")
                .Value(1f)
                .AddFloatRange(2, 120, 2.5f, 6, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => VitalsBatteryCharge = f)
                .Build());
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream)
    {
        try
        {
            var callingMethod = Mirror.GetCaller();
            var callingType = callingMethod?.DeclaringType;

            if (callingType == null)
            {
                return base.RegisterOptions(optionStream);
            }
            if (callingType == typeof(AbstractBaseRole)) return AddVitalsOptions(base.RegisterOptions(optionStream));
            else return base.RegisterOptions(optionStream);
        }
        catch
        {
            return base.RegisterOptions(optionStream);
        }
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .VanillaRole(RoleTypes.Scientist)
        .OptionOverride(Override.VitalsCooldown, () => VitalsCooldown)
        .OptionOverride(Override.VitalsBatteryCharge, () => VitalsBatteryCharge);

    [Localized(nameof(Scientist))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(VitalsCooldown))]
            public static string VitalsCooldown = "Vitals Cooldown";

            [Localized(nameof(VitalsBatteryCharge))]
            public static string VitalsBatteryCharge = "Vitals Battery Charge";
        }
    }
}