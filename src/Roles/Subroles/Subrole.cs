using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Extensions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Interfaces;
using VentLib.Options.UI;
using VentLib.Utilities;
using Lotus.Roles.Builtins;

namespace Lotus.Roles.Subroles;

public abstract class Subrole : CustomRole, ISubrole
{
    public readonly HashSet<IFaction> FactionRestrictions = new();
    public readonly HashSet<Type> RoleRestrictions = new();

    public abstract string Identifier();

    /// <summary>
    /// Returns the <see cref="CompatabilityMode"/> handling for set of types returned by <see cref="RestrictedRoles"/>
    /// </summary>
    public virtual CompatabilityMode RoleCompatabilityMode => RoleRestrictions.Count > 0 ? CompatabilityMode.Whitelisted : CompatabilityMode.Blacklisted;

    /// <summary>
    /// Returns the <see cref="CompatabilityMode"/> handling for set of factions returned by <see cref="RegulatedFactions"/>
    /// </summary>
    public virtual CompatabilityMode FactionCompatabilityMode => FactionRestrictions.Count > 0 ? CompatabilityMode.Whitelisted : CompatabilityMode.Blacklisted;

    /// <summary>
    /// A set of types of roles that this role is either restricted to or against determined by <see cref="RoleCompatabilityMode"/>
    /// </summary>
    /// <returns>Set of role types</returns>
    public virtual HashSet<Type>? RestrictedRoles() => RoleRestrictions;


    /// <summary>
    /// A set of factions that this role is either restricted to or against determined by <see cref="FactionCompatabilityMode"/>
    /// </summary>
    /// <returns>Set of factions</returns>
    public virtual HashSet<IFaction>? RegulatedFactions() => FactionRestrictions;

    public virtual bool IsAssignableTo(PlayerControl player)
    {
        CustomRole role = player.PrimaryRole();
        if (role is GameMaster) return false;
        if (role.BannedModifiers().Contains(this.GetType())) return false;

        Type factionType = role.Faction.GetType();
        if (RegulatedFactions() != null)
        {
            bool anyMatchFactions = RegulatedFactions()!.Any(f => factionType.IsAssignableTo(f.GetType()));
            if (anyMatchFactions && FactionCompatabilityMode is CompatabilityMode.Blacklisted) return false;
            if (!anyMatchFactions && FactionCompatabilityMode is CompatabilityMode.Whitelisted) return false;
        }
        List<CustomRole> subroles = player.GetSubroles();
        if (subroles.Any(cr =>
            {
                if (cr is not Subrole sub) return false;
                var restrictedRoles = sub.RestrictedRoles();
                if (restrictedRoles == null || restrictedRoles.Count == 0) return true;

                bool anyMatchRoles = restrictedRoles.Contains(this.GetType());
                if (anyMatchRoles && sub.RoleCompatabilityMode is CompatabilityMode.Blacklisted) return false;
                return anyMatchRoles || sub.FactionCompatabilityMode is not CompatabilityMode.Whitelisted;
            })) return false;

        var restrictedRoles = RestrictedRoles();
        if (restrictedRoles == null || restrictedRoles.Count == 0) return true;

        bool anyMatchRoles = restrictedRoles.Any(r => r == role.GetType()) |
                             subroles.Any(r => restrictedRoles.Contains(r.GetType()));
        if (anyMatchRoles && RoleCompatabilityMode is CompatabilityMode.Blacklisted) return false;
        return anyMatchRoles || FactionCompatabilityMode is not CompatabilityMode.Whitelisted;
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier
        .RoleFlags(RoleFlag.IsSubrole)
        .Faction(FactionInstances.Modifiers);

    protected GameOptionBuilder AddRestrictToCrew(GameOptionBuilder builder, bool defaultOn = false)
    {
        return builder.SubOption(sub => sub.Name($"Restricted to {FactionInstances.Crewmates.Color.Colorize(FactionInstances.Crewmates.Name())}")
            .Key("Restricted to Crew")
            .AddBoolean(defaultOn)
            .BindBool(b =>
            {
                if (b) FactionRestrictions.Add(FactionInstances.Crewmates);
                else FactionRestrictions.Remove(FactionInstances.Crewmates);
            })
            .Build());
    }
}

/// <summary>
/// Specifies the compatability of a list of objects for Subrole assignment
/// </summary>
public enum CompatabilityMode
{
    /// <summary>
    /// Excludes items from assignment
    /// </summary>
    Blacklisted,
    /// <summary>
    /// Exclusively includes items for assignment
    /// </summary>
    Whitelisted,
}
