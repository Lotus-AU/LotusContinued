using System;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;
using Lotus.Victory;
using Lotus.Utilities;
using Lotus.API.Player;
using Lotus.Managers.History;
using Lotus.Managers.History.Events;
using Lotus.Roles.Subroles;
using VentLib.Utilities;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Neutral;

public class SchrodingersCat : CustomRole
{
    private PlayerControl? turnedAttacker;
    private Type? turnedType;

    private bool KillerKnowsCat;
    private int numberOfLives;

    [UIComponent(UI.Counter)]
    private string ShowNumberOfLives() => RoleUtils.Counter(numberOfLives);

    [RoleAction(LotusActionType.Interaction)]
    private void SchrodingerCatAttacked(PlayerControl actor, Interaction interaction, ActionHandle handle)
    {
        if (interaction.IsPromised || interaction is IUnblockedInteraction) return;
        if (interaction.Intent is not (IFatalIntent or Unstoppable.UnstoppableIntent)) return;
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
        turnedAttacker = actor;
        turnedType = role.GetType();
        IFaction faction = role.Faction;
        Game.MatchData.GameHistory.AddEvent(new FactionChangeEvent(MyPlayer, Faction, faction));
        Faction = faction;
        RoleColor = role.RoleColor;
        OverridenRoleName = Translations.CatFactionChangeName.Formatted(role.RoleName);

        PlayerControl[] viewers = KillerKnowsCat ? [actor, MyPlayer] : [MyPlayer];
        MyPlayer.NameModel().GetComponentHolder<RoleHolder>().Add(new RoleComponent(new LiveString(OverridenRoleName, RoleColor), Game.InGameStates, ViewMode.Replace, viewers: viewers));
        actor.NameModel().GCH<RoleHolder>().Last().AddViewer(MyPlayer);
        Game.GetWinDelegate().AddSubscriber(AddCatToWinners);
    }

    private void AddCatToWinners(WinDelegate winDelegate)
    {
        if (turnedType == null || turnedAttacker == null)
        {
            winDelegate.RemoveWinner(MyPlayer);
            return;
        }
        bool winnerContainsKiller;
        if (turnedAttacker != null) winnerContainsKiller = winDelegate.GetAllWinners().Any(p => p.PlayerId == turnedAttacker.PlayerId);
        else winnerContainsKiller = winDelegate.GetAllWinners().SelectMany(p => p.GetSubroles().Concat([p.PrimaryRole()])).Any(r => r.GetType() == turnedType);

        if (winnerContainsKiller) winDelegate.AddAdditionalWinner(MyPlayer);
        else winDelegate.RemoveWinner(MyPlayer);
    }

    public override Relation Relationship(CustomRole role) => role.GetType() == turnedType ? Relation.FullAllies : base.Relationship(role);

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Number of Lives", Translations.Options.NumberOfLives)
                .AddIntRange(1, 20, 1, 8)
                .BindInt(i => numberOfLives = i)
                .Build())
            .SubOption(sub => sub.KeyName("Killer Knows Cat", TranslationUtil.Colorize(Translations.Options.KillerKnowsCat, RoleColor, RoleColor))
                .AddOnOffValues()
                .BindBool(b => KillerKnowsCat = b)
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
            [Localized(nameof(KillerKnowsCat))] public static string KillerKnowsCat = "Killer Knows Schrodinger's::0 Cat::1";
            [Localized(nameof(NumberOfLives))] public static string NumberOfLives = "Number of Lives";
        }
    }

    private class FactionChangeEvent(PlayerControl player, IFaction oldFaction, IFaction newFaction): IRoleEvent
    {
        private readonly FrozenPlayer? user = Game.MatchData.GetFrozenPlayer(player);
        private readonly Optional<CustomRole> role = Optional<CustomRole>.Of(player.PrimaryRole());
        private readonly string oldFactionName = oldFaction.Color.Colorize(oldFaction.Name());
        private readonly string newFactionName = newFaction.Color.Colorize(newFaction.Name());
        private readonly Timestamp timestamp = new();

        public bool IsCompletion() => true;
        public FrozenPlayer Player() => user!;
        public Optional<CustomRole> RelatedRole() => role;
        public Timestamp Timestamp() => timestamp;
        public string Message() => $"{Game.GetName(user)} changed Factions from {oldFactionName} to {newFactionName}.";
    }
}