using System;
using Lotus.Extensions;

namespace Lotus.Managers.Templates.Models.Units.Conditionals;

public class TConditionalPreset: StringListConditionalUnit
{
    public TConditionalPreset(object input) : base(input)
    {
    }

    public override bool Evaluate(object? _) => Values.Contains(TemplateUnit.GetPresetName());
}