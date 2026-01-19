using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Chat;
using Lotus.Chat.Commands;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Factions.Neutrals;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Patches.Actions;
using Lotus.Patches.Systems;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.Utilities;
using Lotus.Victory;
using Lotus.Victory.Conditions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Turncoat: CustomRole, IInfoResender, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Turncoat));

    private static readonly Color TargetColor = new(0, 0, 0);

    // Options
    private TurncoatStateOption canVent;
    private TurncoatStateOption hasImpVision;
    private TurncoatFactionOption revealedWinCon;
    private bool learnsAllies;
    private bool learnsAlliesImmediately;
    private bool mustBeAliveToWin;

    // Variables
    private TurncoatStateOption myState = TurncoatStateOption.BeforeReveal;
    private Remote<NameComponent>? targetComponent;

    private byte targetPlayer = byte.MaxValue;
    private bool isImpostor = true;
    private bool hasRevealed;
    private bool canWin;

    public RoleButton KillButton(IRoleButtonEditor killButton) => killButton
        .SetText(Witch.Translations.CursingButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Neut/turncoat_curse.png", 130, true));

    public void ResendMessages() => CHandler().Message(hasRevealed | targetPlayer == byte.MaxValue ? BasicCommands.NoInfoMessage : Translations.TurncoatHint).Send(MyPlayer);

    [RoleAction(LotusActionType.RoundStart)]
    private void TakeAwayImpostor(bool gameStart)
    {
        if (hasRevealed && isImpostor)
        {
            isImpostor = false;
            VirtualRole = canVent is TurncoatStateOption.Always or TurncoatStateOption.AfterReveal ? RoleTypes.Engineer : RoleTypes.Crewmate;
            if (canVent is TurncoatStateOption.Always or TurncoatStateOption.AfterReveal) BaseCanVent = true;
            DesyncRole = null;
            Assign(true);
        }

        if (gameStart)
        {
            canWin = true;
            myState = TurncoatStateOption.BeforeReveal;
            Game.GetWinDelegate().AddSubscriber(CheckIfTurncoatCanWin);
        }
    }

    [RoleAction(LotusActionType.RoundEnd)]
    private void SendHint()
    {
        if (hasRevealed)
        {
            if (learnsAllies && !learnsAlliesImmediately)
            {
                learnsAlliesImmediately = true;
                DisplayAllies();
            }
            return;
        }
        if (targetPlayer == byte.MaxValue) return;
        CHandler().Message(Translations.TurncoatHint).Send(MyPlayer);
    }

    [RoleAction(LotusActionType.Attack)]
    private void TryKill(PlayerControl target)
    {
        if (target.PlayerId == targetPlayer || hasRevealed) return;
        // Here is my reasoning for using HostileIntent:
        // The player MAY die at one point if we die.
        // Neutral intent should only be used if it won't EVER harm the other player.
        // But, as there is a small chance here, than we use Hostile.
        if (MyPlayer.InteractWith(target, LotusInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt)
            return;
        targetPlayer = target.PlayerId;

        targetComponent?.Delete();
        targetComponent = target.NameModel().GetComponentHolder<NameHolder>().Add(new NameComponent(
            new LiveString(state =>
            {
                byte shapeshiftedId = target.GetShapeshifted();
                if (shapeshiftedId == 255 || state is GameState.InMeeting) return target.name;
                return Players.GetAllPlayers()
                    .FirstOrDefault(p => p.PlayerId == shapeshiftedId, PlayerControl.LocalPlayer)
                    .name; // use host's name as a "scapegoat"
            }, TargetColor), Game.InGameStates, ViewMode.Replace, MyPlayer));
    }

    [RoleAction(LotusActionType.Vote, priority: Priority.High)]
    private void SelfVote(Optional<PlayerControl> voted, ActionHandle handle)
    {
        if (hasRevealed) return;
        if (!voted.Exists()) return;
        if (voted.Get().PlayerId != MyPlayer.PlayerId) return;
        if (targetPlayer == byte.MaxValue)
        {
            CHandler().Message(Translations.NoTarget).Send(MyPlayer);
            handle.Cancel();
            return;
        }

        handle.Cancel();
        hasRevealed = true;
        myState = TurncoatStateOption.AfterReveal;
        CHandler().Message(Translations.BetrayReveal.Formatted(MyPlayer.name, RoleName)).Send();
        MyPlayer.NameModel().GetComponentHolder<RoleHolder>().Components().ForEach(component => component.SetViewerSupplier(() => PlayerControl.AllPlayerControls.ToArray().ToList()));

        if (learnsAllies && learnsAlliesImmediately) DisplayAllies();
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    [RoleAction(LotusActionType.Exiled)]
    private void MyDeath()
    {
        if (mustBeAliveToWin) canWin = false;
        if (hasRevealed)
            Utils.PlayerById(targetPlayer).IfPresent(p => MyPlayer.InteractWith(
                    p, new IndirectInteraction(
                        new FatalIntent(true,
                            () => new CustomDeathEvent(p, MyPlayer, ModConstants.DeathNames.Cursed))
                    )
                )
            );

        targetPlayer = byte.MaxValue;
        targetComponent?.Delete();
    }

    private ChatHandler CHandler() => ChatHandler.Of(null, this.ColoredRoleName());

    private Option SetDefaultIndex(Option option, int defaultIndex)
    {
        option.SetDefaultIndex(defaultIndex);
        return option;
    }

    private void DisplayAllies()
    {
        List<PlayerControl> sidedPlayers = [];
        string factionName = string.Empty;

        switch (revealedWinCon)
        {
            case TurncoatFactionOption.Impostor:
                sidedPlayers = Players.GetPlayers(PlayerFilter.Impostor).ToList();
                factionName = Translations.Options.ImpFaction;
                break;
            case TurncoatFactionOption.NkAndUndead:
                sidedPlayers = Players.GetPlayers(PlayerFilter.Undead | PlayerFilter.NeutralKilling).ToList();
                factionName = Translations.Options.NeutralFaction;
                break;
            case TurncoatFactionOption.NonCrew:
                sidedPlayers = Players.GetAllRoles()
                    .Where(r => r.Faction.Relationship(FactionInstances.Crewmates) is Relation.None)
                    .Select(r => r.MyPlayer)
                    .ToList();
                factionName = Translations.Options.NonCrewFaction;
                break;
            default:
                log.Exception($"Unknown faction: {revealedWinCon}. This is not properly setup to be a team Turncoat can betray for.");
                break;
        }

        if (sidedPlayers.Any()) CHandler().Message(Translations.BetrayedTeamPlayers.Formatted(factionName, string.Join(", ", sidedPlayers.Select(p => p.name)))).Send(MyPlayer);
        else CHandler().Message(Translations.NoPlayersOnBetrayedTeam).Send(MyPlayer);
    }

    private void CheckIfTurncoatCanWin(WinDelegate winDelegate)
    {
        if (!canWin) return;
        IWinCondition? winCondition = winDelegate.WinCondition();
        if (winCondition == null) return;
        if (winCondition is not IFactionWinCondition factionWinCondition)
        {
            if (!hasRevealed) return;
            if (revealedWinCon is TurncoatFactionOption.NonCrew) winDelegate.AddAdditionalWinner(MyPlayer);
            else if (revealedWinCon is not TurncoatFactionOption.NkAndUndead) return;
            if (winDelegate.GetWinners().All(w => w.PrimaryRole().Faction is INeutralFaction)) winDelegate.AddAdditionalWinner(MyPlayer);
            return;
        }
        if (!hasRevealed)
        {
            // This means they still side with the crew.
            if (factionWinCondition.Factions().Contains(FactionInstances.Crewmates)) winDelegate.AddAdditionalWinner(MyPlayer);
            return;
        }
        bool betrayedFactionWon = false;
        switch (revealedWinCon)
        {
            case TurncoatFactionOption.Impostor:
                betrayedFactionWon = factionWinCondition.Factions().Contains(FactionInstances.Impostors);
                break;
            case TurncoatFactionOption.NkAndUndead:
                betrayedFactionWon = factionWinCondition.Factions().Contains(FactionInstances.TheUndead) || factionWinCondition.Factions().All(f => f is INeutralFaction);
                break;
            case TurncoatFactionOption.NonCrew:
                betrayedFactionWon = !factionWinCondition.Factions().Contains(FactionInstances.Crewmates);
                break;
            default:
                log.Exception($"Unknown faction: {revealedWinCon}. This is not properly setup to be a team Turncoat can betray for.");
                break;
        }
        if (betrayedFactionWon) winDelegate.AddAdditionalWinner(MyPlayer);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => base.RegisterOptions(optionStream)
        .SubOption(sub => SetDefaultIndex(sub
            .KeyName("Can Vent", RoleTranslations.CanVent)
            .Bind(v => canVent = (TurncoatStateOption)v)
            .Value(v => v.Text(GeneralOptionTranslations.AlwaysText).Color(Color.green).Value(0).Build())
            .Value(v => v.Text(Translations.Options.AfterReveal).Color(Color.yellow).Value(1).Build())
            .Value(v => v.Text(Translations.Options.BeforeReveal).Color(Color.cyan).Value(2).Build())
            .Value(v => v.Text(GeneralOptionTranslations.NeverText).Color(Color.red).Value(3).Build())
            .Build(), 1))
        .SubOption(sub => SetDefaultIndex(sub
            .KeyName("Impostor Vision", RoleTranslations.ImpostorVision)
            .Bind(v => hasImpVision = (TurncoatStateOption)v)
            .Value(v => v.Text(GeneralOptionTranslations.AlwaysText).Color(Color.green).Value(0).Build())
            .Value(v => v.Text(Translations.Options.AfterReveal).Color(Color.yellow).Value(1).Build())
            .Value(v => v.Text(Translations.Options.BeforeReveal).Color(Color.cyan).Value(2).Build())
            .Value(v => v.Text(GeneralOptionTranslations.NeverText).Color(Color.red).Value(3).Build())
            .Build(), 1))
        .SubOption(sub => SetDefaultIndex(sub
            .KeyName("Revealed Turncoat Wincon", Translations.Options.RevealedTurncoatWincon)
            .Bind(v => revealedWinCon = (TurncoatFactionOption)v)
            .Value(v => v.Text(Translations.Options.ImpFaction).Color(Color.red).Value(0).Build())
            .Value(v => v.Text(Translations.Options.NeutralFaction).Color(ModConstants.Palette.NeutralColor).Value(1).Build())
            .Value(v => v.Text(Translations.Options.NonCrewFaction).Color(Palette.CrewmateBlue).Value(2).Build())
            .Build(), 2))
        .SubOption(sub => sub
            .KeyName("Learns Allies", Translations.Options.RevealedTurncoatAllies)
            .AddBoolean()
            .BindBool(b => learnsAllies = b)
            .SubOption(sub2 => sub2
                .KeyName("Learns Immediately", Translations.Options.TurncoatLearns)
                .Value(v => v.Text(Translations.Options.NextMeeting).Value(false).Build())
                .Value(v => v.Text(Translations.Options.Immediately).Value(true).Build())
                .BindBool(b => learnsAlliesImmediately = b)
                .Build())
            .ShowSubOptionPredicate(v => (bool)v)
            .Build())
        .SubOption(sub => sub
            .KeyName("Must be Alive", Translations.Options.MustBeAlive)
            .BindBool(b => mustBeAliveToWin = b)
            .AddBoolean(false)
            .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier
        .Faction(FactionInstances.Neutral)
        .RoleColor(new Color(1f, .64f, 0f))
        .DesyncRole(RoleTypes.Impostor)
        .IntroSound(RoleTypes.Shapeshifter)
        .RoleFlags(RoleFlag.CannotWinAlone)
        .SpecialType(SpecialType.Neutral)
        .CanVent(canVent is TurncoatStateOption.BeforeReveal or TurncoatStateOption.Always)
        .OptionOverride(Override.EngVentCooldown, () => 0f, () => !isImpostor && canVent is TurncoatStateOption.AfterReveal or TurncoatStateOption.Always)
        .OptionOverride(Override.EngVentDuration, () => 0f, () => !isImpostor && canVent is TurncoatStateOption.AfterReveal or TurncoatStateOption.Always)
        .OptionOverride(Override.ImpostorLightMod, () => AUSettings.CrewLightMod(),
            () => isImpostor && hasImpVision is not TurncoatStateOption.Always and not TurncoatStateOption.BeforeReveal)
        .OptionOverride(Override.ImpostorLightMod, () => AUSettings.CrewLightMod() / 5,
            () => isImpostor && SabotagePatch.CurrentSabotage != null && SabotagePatch.CurrentSabotage.SabotageType() is SabotageType.Lights
                  && hasImpVision is not TurncoatStateOption.Always and not TurncoatStateOption.BeforeReveal)
        .OptionOverride(Override.CrewLightMod, () => AUSettings.CrewLightMod() * 5,
            () => !isImpostor && SabotagePatch.CurrentSabotage != null && SabotagePatch.CurrentSabotage.SabotageType() is SabotageType.Lights
                  && hasImpVision is TurncoatStateOption.AfterReveal or TurncoatStateOption.Always)
        .OptionOverride(Override.KillCooldown, () => 1f);

    private enum TurncoatStateOption
    {
        Always,
        AfterReveal,
        BeforeReveal,
        Never
    }
    private enum TurncoatFactionOption
    {
        Impostor,
        NkAndUndead,
        NonCrew
    }

    [Localized(nameof(Turncoat))]
    public static class Translations
    {
        [Localized(nameof(NoTarget))] public static string NoTarget = "You cannot reveal until you have selected a target with your kill button.";
        [Localized(nameof(BetrayReveal))] public static string BetrayReveal = "{0} is the {1} and they have chosen to betray the crew!\nHowever, the {1} has cursed someone! If the {1} dies, they take the cursed player with them.";
        [Localized(nameof(TurncoatHint))] public static string TurncoatHint = "Vote yourself to betray the crew. But remember, it will reveal your identity to the rest of the crew.";
        [Localized(nameof(BetrayedTeamPlayers))] public static string BetrayedTeamPlayers = "You sided with the '{0}' faction(s). These players are on the same team: {1}";
        [Localized(nameof(NoPlayersOnBetrayedTeam))] public static string NoPlayersOnBetrayedTeam = "There are no players on the team you betrayed for.";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(RevealedTurncoatWincon))] public static string RevealedTurncoatWincon = "Revealed Turncoat wincon Is:";
            [Localized(nameof(RevealedTurncoatAllies))] public static string RevealedTurncoatAllies = "Revealed Turncoat Learns Allies";
            [Localized(nameof(TurncoatLearns))] public static string TurncoatLearns = "Turncoat Learns When";
            [Localized(nameof(MustBeAlive))] public static string MustBeAlive = "Turncoat must be Alive to Win";

            [Localized(nameof(AfterReveal))] public static string AfterReveal = "After Revealing";
            [Localized(nameof(BeforeReveal))] public static string BeforeReveal = "Before Revealing";

            [Localized(nameof(ImpFaction))] public static string ImpFaction = "Impostor";
            [Localized(nameof(NeutralFaction))] public static string NeutralFaction = "NK / Undead";
            [Localized(nameof(NonCrewFaction))] public static string NonCrewFaction = "Non-Crew";

            [Localized(nameof(Immediately))] public static string Immediately = "Immediately";
            [Localized(nameof(NextMeeting))] public static string NextMeeting = "Next Meeting";
        }
    }
}