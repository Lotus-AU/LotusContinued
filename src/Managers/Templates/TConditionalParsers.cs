using System;
using System.Collections.Generic;
using Lotus.Managers.Templates.Models.Units;
using Lotus.Managers.Templates.Models.Units.Conditionals;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers.Templates;

public class TConditionalParsers
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(TConditionalParsers));

    public static Dictionary<string, Func<object, IConditionalUnit>> units = new()
    {
        { "Statuses", value => new TConditionStatuses(value) },
        { "Status", value => new TConditionStatuses(value) },

        { "Vitals", value => new TConditionalVitals(value) },
        { "Vital", value => new TConditionalVitals(value) },

        { "States", value => new TConditionalState(value) },
        { "State", value => new TConditionalState(value) },

        { "User", value => new TConditionalUser(value) },
        { "Users", value => new TConditionalUser(value) },
        { "Viewer", value => new TConditionalUser(value) },
        { "Viewers", value => new TConditionalUser(value) },

        { "Condition", value => new TRecursiveCondition(value) },
        { "Conditions", value => new TRecursiveCondition(value) },

        { "PlayerFlag", value => new TConditionalPlayerFlag(value) },
        { "PlayerFlags", value => new TConditionalPlayerFlag(value) },

        { "Roles", value => new TConditionalRoles(value) },
        { "NotRoles", value => new TConditionalNotRole(value) },
        { "EnabledRoles", value => new TConditionalEnabledRoles(value) },

        { "Evaluate", value => new TConditionalEvaluate(value) },

        { "Preset", value => new TConditionalPreset(value) }
    };


    public static IConditionalUnit Parse(string yamlKey, object value)
    {
        return units.GetOptional(yamlKey).Map(c => c(value)).OrElseGet(() =>
        {
            log.Warn($"Could not find conditional for key: \"{yamlKey}\", returning default conditional.");
            return new TConditionalDefault();
        });
    }

    public static IConditionalUnit? ParseNull(string yamlKey, object value)
    {
        return units.GetOptional(yamlKey).Map(c => c(value)).OrElse(null!);
    }
}