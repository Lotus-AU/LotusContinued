using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Extensions;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Neutral;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.Options.Roles;

namespace Lotus.Roles.Subroles;

public class Bloodlust : Subrole
{
    /// <summary>
    /// A list of roles that Bloodlust is not compatiable with. Add your role to this list to make it not be assigned with your role.
    /// </summary>
    public static readonly List<Type> IncompatibleRoles = new()
    {
        typeof(Crusader),
        typeof(Observer),
        typeof(Sheriff),
        typeof(Snitch),
        typeof(Speedrunner),
        typeof(Amnesiac),
        typeof(Copycat),
        typeof(Taskrunner),
        typeof(Postman),
    };

    private bool restrictedToCompatibleRoles;
    private static ColorGradient _psychoGradient = new(new Color(0.41f, 0.1f, 0.18f), new Color(0.85f, 0.77f, 0f));
    public bool requiresBaseKillMethod;


    public override string Identifier() => "";

    [RoleAction(LotusActionType.Attack)]
    private bool TryKill(PlayerControl target)
    {
        if (!requiresBaseKillMethod) return false;
        InteractionResult result = MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this));
        Game.MatchData.GameHistory.AddEvent(new KillEvent(MyPlayer, target, result is InteractionResult.Proceed));
        return result is InteractionResult.Proceed;
    }

    protected override void PostSetup()
    {
        CustomRole role = MyPlayer.PrimaryRole();
        RoleHolder roleHolder = MyPlayer.NameModel().GetComponentHolder<RoleHolder>();
        string newRoleName = _psychoGradient.Apply(role.RoleName);
        roleHolder.Add(new RoleComponent(new LiveString(newRoleName), Game.InGameStates, ViewMode.Replace, MyPlayer));
        role.Faction = FactionInstances.Neutral;
        if (role.RealRole.IsCrewmate())
        {
            role.DesyncRole = RoleTypes.Impostor;
            MyPlayer.GetTeamInfo().MyRole = role.DesyncRole.Value;
        }
        requiresBaseKillMethod = !role.GetActions(LotusActionType.Attack).Any();
    }

    public override bool IsAssignableTo(PlayerControl player)
    {
        // If the role is NOT a neutral killing role, then we immediately pass and it's legal
        if (player.PrimaryRole().SpecialType is not SpecialType.NeutralKilling) return base.IsAssignableTo(player);
        NeutralTeaming teaming = Options.RoleOptions.NeutralOptions.NeutralTeamingMode;

        // If neutral teaming is disabled we return false, because solo NKs should never get bloodlust
        // if neutral teaming is NP + NK or ALL then we pass as chances are, there'll be teaming there
        if (teaming is not NeutralTeaming.SameRole) return teaming is not NeutralTeaming.Disabled && base.IsAssignableTo(player);

        // This means neutral teaming is Same Role, so now we check if Max is 1. If max is 1, there'll never be a team so bloodlust is useless, return false
        return player.PrimaryRole().Count > 1 && base.IsAssignableTo(player);
    }

    public override HashSet<Type>? RestrictedRoles()
    {
        HashSet<Type>? restrictedRoles = base.RestrictedRoles();
        if (!restrictedToCompatibleRoles) return restrictedRoles;
        IncompatibleRoles.ForEach(r => restrictedRoles?.Add(r));
        return restrictedRoles;
    }

    public override CompatabilityMode RoleCompatabilityMode => CompatabilityMode.Blacklisted;

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddRestrictToCrew(base.RegisterOptions(optionStream))
            .SubOption(sub => sub.KeyName("Restrict to Compatible Roles", Translations.Options.RestrictToCompatbileRoles)
                .BindBool(b => restrictedToCompatibleRoles = b)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier).RoleColor(new Color(0.41f, 0.1f, 0.18f));

    [Localized(nameof(Bloodlust))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        internal static class Options
        {
            [Localized(nameof(RestrictToCompatbileRoles))]
            public static string RestrictToCompatbileRoles = "Restrict to Compatible Roles";
        }
    }
}