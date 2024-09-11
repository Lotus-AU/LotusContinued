using System;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;
using Lotus.GameModes.Standard;

namespace Lotus.Roles.RoleGroups.Neutral;

public class SchrodingersCat : CustomRole
{
    private Type? turnedType;
    private int numberOfLives;

    [UIComponent(UI.Counter)]
    private string ShowNumberOfLives() => RoleUtils.Counter(numberOfLives);

    [RoleAction(LotusActionType.Interaction)]
    private void SchrodingerCatAttacked(PlayerControl actor, Interaction interaction, ActionHandle handle)
    {
        if (interaction.Intent is not IFatalIntent) return;
        if (numberOfLives <= 0) return;
        numberOfLives--;
        AssignFaction(actor);
        handle.Cancel();
    }

    private void AssignFaction(PlayerControl actor)
    {
        CustomRole? role;
        if (actor.PrimaryRole().GetActions(LotusActionType.Attack).Any()) role = actor.PrimaryRole();
        else role = actor.GetSubroles().FirstOrDefault(sub => sub.GetActions(LotusActionType.Attack).Any()) ?? actor.PrimaryRole();
        turnedType = role.GetType();
        IFaction faction = role.Faction;
        Faction = faction;
        RoleColor = role.RoleColor;
        OverridenRoleName = Translations.CatFactionChangeName.Formatted(role.RoleName);

        PlayerControl[] viewers = (StandardGameMode.Instance.RoleManager.RoleHolder as StandardRoles).Static.Copycat.KillerKnowsCopycat ? new[] { actor, MyPlayer } : new[] { MyPlayer };
        MyPlayer.NameModel().GetComponentHolder<RoleHolder>().Add(new RoleComponent(new LiveString(OverridenRoleName, RoleColor), Game.InGameStates, ViewMode.Replace, viewers: viewers));
        actor.NameModel().GCH<RoleHolder>().Last().AddViewer(MyPlayer);
    }

    public override Relation Relationship(CustomRole role) => role.GetType() == turnedType ? Relation.FullAllies : base.Relationship(role);

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Number of Lives", Translations.Options.NumberOfLives)
                .AddIntRange(1, 20, 1, 8)
                .BindInt(i => numberOfLives = i)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleColor(new Color(0.41f, 0.41f, 0.41f))
            .VanillaRole(RoleTypes.Crewmate)
            .Faction(FactionInstances.Neutral)
            .RoleFlags(RoleFlag.CannotWinAlone)
            .SpecialType(SpecialType.Neutral);

    [Localized(nameof(SchrodingersCat))]
    private static class Translations
    {
        [Localized(nameof(CatFactionChangeName))]
        public static string CatFactionChangeName = "{0}cat";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(NumberOfLives))]
            public static string NumberOfLives = "Number of Lives";
        }
    }
}