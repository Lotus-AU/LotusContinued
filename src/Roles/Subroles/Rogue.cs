extern alias JBAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using JBAnnotations::JetBrains.Annotations;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
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
using Lotus.Factions.Interfaces;
using Lotus.Factions.Neutrals;
using Lotus.GameModes.Normal.Standard;
using Lotus.Managers;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Neutral;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.Options.Roles;
using Lotus.Roles.RoleGroups.Undead.Roles;
using Lotus.Utilities;
using MonoMod.Utils;
using VentLib.Options;

namespace Lotus.Roles.Subroles;

public class Rogue : Subrole
{
    /// <summary>
    /// A list of roles that Rogue is not compatible with. Add your role to this list to make it not be assigned with your role.
    /// </summary>
    [UsedImplicitly] public static readonly List<Type> IncompatibleRoles =
    [
        typeof(Crusader),
        typeof(Observer),
        typeof(Sheriff),
        typeof(Snitch),
        typeof(Speedrunner),
        typeof(Amnesiac),
        typeof(Copycat),
        typeof(Taskrunner),
        typeof(Postman),
        typeof(Terrorist),
        typeof(Necromancer),
        typeof(Altruist),
        typeof(Charmer),
        typeof(Jailor)
    ];

    private static ColorGradient _psychoGradient = new(new Color(0.41f, 0.1f, 0.18f), new Color(0.85f, 0.77f, 0f));
    public static Dictionary<Type, int> FactionMaxDictionary = new();

    private bool restrictedToCompatibleRoles;
    public bool requiresBaseKillMethod;

    public override string Identifier() => "";

    public Rogue()
    {
        NormalStandardRoles.Callbacks.Add(AddFactionSettings);
    }

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
        role.RoleColorGradient = _psychoGradient;
        roleHolder.Add(new RoleComponent(new LiveString(newRoleName), Game.InGameStates, ViewMode.Replace, MyPlayer));
        role.RoleFlags &= ~RoleFlag.CannotWinAlone;
        new RoleModifier(role)
            .SpecialType(SpecialType.NeutralKilling)
            .Faction(FactionInstances.Neutral)
            .RoleAbilityFlags(RoleAbilityFlag.IsAbleToKill);
        if (role.RealRole.IsCrewmate())
        {
            role.DesyncRole = RoleTypes.Impostor;
            MyPlayer.GetTeamInfo().MyRole = RoleTypes.Impostor;
        }
        requiresBaseKillMethod = !role.GetActions(LotusActionType.Attack).Any();
    }

    private int GetAmountOfPeopleOnFaction(Type faction) => Players.GetAlivePlayers().Count(p =>
        p.PrimaryRole().Faction.GetType() == faction && p.GetSubroles().Any(s => s is Rogue));

    public override bool IsAssignableTo(PlayerControl player)
    {
        IFaction playerFaction = player.PrimaryRole().Faction;
        if (playerFaction is INeutralFaction) playerFaction = FactionInstances.Neutral;
        Type myFaction = playerFaction.GetType();

        // Check if their faction already has the max amount of allowed players.
        // If they are maxed out, we don't even call base and just immediately exit.
        if (GetAmountOfPeopleOnFaction(myFaction) >= FactionMaxDictionary.GetValueOrDefault(myFaction, 0)) return false;

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

    private void AddFactionSettings()
    {
        Dictionary<Type, IFaction> allFactions = new() {
            {FactionInstances.Impostors.GetType(), FactionInstances.Impostors},
            {FactionInstances.Crewmates.GetType(), FactionInstances.Crewmates},
            {FactionInstances.Neutral.GetType(), FactionInstances.Neutral},
            {FactionInstances.TheUndead.GetType(), FactionInstances.TheUndead}
        };
        allFactions.AddRange(FactionInstances.AddonFactions);
        allFactions.ForEach(kvp =>
        {
            string keyName = Translations.Options.FactionMaxRogues.Formatted(kvp.Value.Name());
            Option option = new GameOptionBuilder()
                .KeyName(TranslationUtil.Remove(keyName), TranslationUtil.Colorize(keyName, kvp.Value.Color))
                .AddIntRange(0, ModConstants.MaxPlayers, 1, 1)
                .BindInt(i => FactionMaxDictionary[kvp.Key] = i)
                .Build();
            RoleOptions.AddChild(option);
            GlobalRoleManager.RoleOptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        });
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddRestrictToCrew(base.RegisterOptions(optionStream))
            .SubOption(sub => sub.KeyName("Restrict to Compatible Roles", Translations.Options.RestrictToCompatbileRoles)
                .BindBool(b => restrictedToCompatibleRoles = b)
                .AddBoolean()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleColor(new Color(0.41f, 0.1f, 0.18f))
        .RoleAbilityFlags(RoleAbilityFlag.IsAbleToKill);

    [Localized(nameof(Rogue))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        internal static class Options
        {
            [Localized(nameof(RestrictToCompatbileRoles))] public static string RestrictToCompatbileRoles = "Restrict to Compatible Roles";

            [Localized(nameof(FactionMaxRogues))] public static string FactionMaxRogues = "{0}::0 Faction Max Rogues";
        }
    }
}