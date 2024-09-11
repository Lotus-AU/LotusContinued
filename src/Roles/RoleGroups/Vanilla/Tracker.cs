using System.Diagnostics;
using AmongUs.GameOptions;
using Lotus.Extensions;
using Lotus.Options;
using Lotus.Roles.Overrides;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;


namespace Lotus.Roles.RoleGroups.Vanilla;

public class Tracker : Crewmate
{
    // Vanilla Settings
    protected float TrackerCooldown;
    protected float TrackerDuration;
    protected float TrackerDelay;

    protected GameOptionBuilder AddTrackerOptions(GameOptionBuilder builder)
    {
        return builder.SubOption(sub => sub
                .KeyName("Tracker Cooldown", TrackerTranslations.Options.TrackerCooldown)
                .AddFloatRange(0, 120, 2.5f, 12, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => TrackerCooldown = f)
                .Build())
            .SubOption(sub => sub
                .KeyName("Tracker Duration", TrackerTranslations.Options.TrackerDuration)
                .AddFloatRange(0f, 120, 2.5f, 7, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => TrackerDuration = f)
                .Build())
            .SubOption(sub => sub
                .KeyName("Tracker Update Delay", TrackerTranslations.Options.TrackerDelay)
                .AddFloatRange(0, 120, .5f, 6, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => TrackerDelay = f)
                .Build());
    }

    // protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream)
    // {
    //     try
    //     {
    //         var callingMethod = Mirror.GetCaller();
    //         var callingType = callingMethod?.DeclaringType;

    //         if (callingType == null)
    //         {
    //             return base.RegisterOptions(optionStream);
    //         }
    //         if (callingType == typeof(AbstractBaseRole)) return AddTrackerOptions(base.RegisterOptions(optionStream));
    //         else return base.RegisterOptions(optionStream);
    //     }
    //     catch
    //     {
    //         return base.RegisterOptions(optionStream);
    //     }
    // }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .VanillaRole(RoleTypes.Tracker)
            .OptionOverride(Override.TrackerCooldown, () => TrackerCooldown)
            .OptionOverride(Override.TrackerDuration, () => TrackerDuration)
            .OptionOverride(Override.TrackerDelay, () => TrackerDelay);

    [Localized(nameof(Tracker))]
    public static class TrackerTranslations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(TrackerCooldown))]
            public static string TrackerCooldown = "Tracker Cooldown";

            [Localized(nameof(TrackerDuration))]
            public static string TrackerDuration = "Tracker Duration";

            [Localized(nameof(TrackerDelay))]
            public static string TrackerDelay = "Tracker Update Delay";
        }
    }
}