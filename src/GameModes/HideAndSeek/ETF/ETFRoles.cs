extern alias JBAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.Logging;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Extra.HNS;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using ImplicitUseTargetFlags = JBAnnotations::JetBrains.Annotations.ImplicitUseTargetFlags;
namespace Lotus.GameModes.HideAndSeek.ETF;

// ReSharper disable once InconsistentNaming
public sealed class ETFRoles : RoleHolder
{
    public override List<Action> FinishedCallbacks() => Callbacks;

    public static List<Action> Callbacks { get; set; } = new List<Action>();

    public StaticRoles Static;
    public Modifiers Mods;

    public static ETFRoles Instance = null!;

    public ETFRoles()
    {
        Instance = this;
        AllRoles = new List<CustomRole>();
        Static = new StaticRoles();
        Mods = new Modifiers();

        MainRoles = Static.GetType()
            .GetFields()
            .Select(f => (CustomRole)f.GetValue(Static)!)
            .ToList();

        ModifierRoles = Mods.GetType()
            .GetFields()
            .Select(f => (CustomRole)f.GetValue(Mods)!)
            .ToList();

        // add all roles
        AllRoles.AddRange(MainRoles);
        AllRoles.AddRange(ModifierRoles);
        AllRoles.AddRange(AddonRoles);

        // avoid `Collection was modified` error
        var newRoles = new List<CustomRole>();
        AllRoles.ForEach(r => newRoles.AddRange(r.LinkedRoles()));
        AllRoles.AddRange(newRoles);

        // solidify every role to finish them off
        AllRoles.ForEach(r =>
        {
            if (!r.RoleFlags.HasFlag(RoleFlag.TransformationRole) &&
                !r.RoleFlags.HasFlag(RoleFlag.VariationRole)) r.Solidify();
        });
    }

    [JBAnnotations::JetBrains.Annotations.UsedImplicitlyAttribute]
    [Obsolete("This function no longer needs to be called by mods. PL will now auto add your roles to the game-mode you export them with.", true)]
    public static void AddRole(CustomRole role)
    {
        if (role.Addon == null || AddonRoles.Contains(role)) return;
        DevLogger.Log($"adding {role.EnglishRoleName} to Standard.");
        AddonRoles.Add(role);
        Instance.AllRoles.Add(role);
        role.Solidify();
        ETFGameMode.Instance.RoleManager.RegisterRole(role);
    }

    public override void AddAddonRole(CustomRole addonRole)
    {
        if (addonRole.Addon == null || AddonRoles.Contains(addonRole)) return;
        DevLogger.Log($"adding {addonRole.EnglishRoleName} to Standard.");
        AddonRoles.Add(addonRole);
        AllRoles.Add(addonRole);
        addonRole.Solidify();
        ETFGameMode.Instance.RoleManager.RegisterRole(addonRole);
    }

    [JBAnnotations::JetBrains.Annotations.UsedImplicitlyAttribute(ImplicitUseTargetFlags.WithMembers)]
    public class StaticRoles
    {
        /// Impostors
        // Base
        public Warden Warden = new();

        // Roles
        public Runner Runner = new();

        /// Fugitives
        // Base
        public Fugitive Fugitive = new();

        // Roles
    }

    [JBAnnotations::JetBrains.Annotations.UsedImplicitlyAttribute(ImplicitUseTargetFlags.WithMembers)]
    public class Modifiers
    {

    }
}

