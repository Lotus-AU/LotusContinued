using AmongUs.GameOptions;
using Lotus.Extensions;
using Lotus.Options;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;

namespace Lotus.Roles.RoleGroups.Vanilla;

public class Judge : Crewmate
{
    protected float TaskRequirementPercentage;

    protected GameOptionBuilder AddJudgeOptions(GameOptionBuilder builder) => builder
        .SubOption(sub => sub
            .KeyName("Task Requirement Percentage", Translations.Options.TaskRequirementPercentage)
            .AddFloatRange(0, 100, 5, 5, "%")
            .BindFloat(i => TaskRequirementPercentage = i)
            .Build());

    [RoleAction(LotusActionType.RoundStart, Subclassing = false)]
    protected virtual void FixTaskRequirement(bool gameStart)
    {
        if (!gameStart) return;
        // Reassign Judge role to itself so it will update the cache of the task requirement percentage (since it will be pointed to the vanilla value)
        // If your role wants to change tasks remaining for whatever reason, make sure to reassign Judge role.
        // Just make sure they haven't used judge ability yet, so they don't get ANOTHER judge.
        if (MyPlayer.IsAlive()) MyPlayer.RpcSetRoleDesync(RoleTypes.Judge, MyPlayer);
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
            if (callingType == typeof(AbstractBaseRole)) return AddJudgeOptions(base.RegisterOptions(optionStream));
            else return base.RegisterOptions(optionStream);
        }
        catch
        {
            return base.RegisterOptions(optionStream);
        }
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .VanillaRole(RoleTypes.Judge)
            .OptionOverride(Override.JudgeTaskRequirementPercentage, () => TaskRequirementPercentage);

    [Localized(nameof(Judge))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(TaskRequirementPercentage))]
            public static string TaskRequirementPercentage = "Tasks Completed to use Ability";
        }
    }
}