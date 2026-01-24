extern alias JBAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using JBAnnotations::JetBrains.Annotations;
using Lotus.Logging;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Extra.Colorwars;

namespace Lotus.GameModes.Colorwars;

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

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class StaticRoles
    {
        public Painter Painter = new();
    }
}