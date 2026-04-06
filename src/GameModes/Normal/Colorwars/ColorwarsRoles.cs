extern alias JBAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.Logging;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Extra.Colorwars;
using ImplicitUseTargetFlags = JBAnnotations::JetBrains.Annotations.ImplicitUseTargetFlags;

namespace Lotus.GameModes.Normal.Colorwars;

public class ColorwarsRoles : RoleHolder
{
    public override List<Action> FinishedCallbacks() => Callbacks;

    public static List<Action> Callbacks { get; set; } = new List<Action>();

    public static ColorwarsRoles Instance = null!;

    public StaticRoles Static;

    public ColorwarsRoles()
    {
        Instance = this;

        AllRoles = new List<CustomRole>();
        Static = new StaticRoles();

        AllRoles.AddRange(Static.GetType()
            .GetFields()
            .Select(f => (CustomRole)f.GetValue(Static)!)
            .ToList());


        // solidify every role to finish them off
        AllRoles.ForEach(r => r.Solidify());
    }

    public override void AddAddonRole(CustomRole addonRole)
    {
        if (AddonRoles.Contains(addonRole)) return;
        DevLogger.Log($"adding {addonRole.EnglishRoleName} to Colorwars.");
        AddonRoles.Add(addonRole);
        AllRoles.Add(addonRole);
        addonRole.Solidify();
        ColorwarsGamemode.Instance.RoleManager.RegisterRole(addonRole);
    }

    [JBAnnotations::JetBrains.Annotations.UsedImplicitlyAttribute(ImplicitUseTargetFlags.WithMembers)]
    public class StaticRoles
    {
        public Painter Painter = new();
    }
}