using System.Diagnostics.CodeAnalysis;
using Lotus.API.Odyssey;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Options;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using static Lotus.Roles.RoleGroups.Crew.Medic.MedicTranslations;
using static Lotus.Roles.RoleGroups.Crew.Medic.MedicTranslations.MedicOptionTranslations;
using static Lotus.Utilities.TranslationUtil;
using Lotus.API.Vanilla.Meetings;
using System.Linq;
using Lotus.Roles.Interfaces;

namespace Lotus.Roles.RoleGroups.Crew;

public class Bodyguard : Crewmate, IInfoResender
{
    private static readonly Color CrossColor = new(0.74f, 0.58f, 0f);
    private bool protectAgainstHelpfulInteraction;
    private bool protectAgainstNeutralInteraction;

    private GuardMode mode;
    private byte guardedPlayer = byte.MaxValue;

    private bool targetLockedIn;
    private bool confirmedVote;

    private Remote<IndicatorComponent>? protectedIndicator;

    private const string StarText = "<size=1.2><b>★</b></size>";

    [RoleAction(LotusActionType.RoundEnd)]
    private void RoundEndMessage()
    {
        confirmedVote = false;
        ResendMessages();
    }

    public void ResendMessages()
    {
        if (guardedPlayer == byte.MaxValue) CHandler(Translations.BodyguardHelpMessage).Send(MyPlayer);
        else if (mode is GuardMode.AnyMeeting) CHandler(ProtectingMessage.Formatted(Players.FindPlayerById(guardedPlayer)?.name)).Send(MyPlayer);
    }

    [SuppressMessage("ReSharper", "AssignmentInConditionalExpression")]
    [RoleAction(LotusActionType.Vote)]
    private void HandleMedicVote(Optional<PlayerControl> votedPlayer, MeetingDelegate _, ActionHandle handle)
    {
        if (confirmedVote) return;
        // If guarded player is selected, and mode is any meeting then skip
        if (targetLockedIn && guardedPlayer != byte.MaxValue && mode is not GuardMode.AnyMeeting) return;

        handle.Cancel();

        if (confirmedVote = !votedPlayer.Exists())
        {
            guardedPlayer = byte.MaxValue;
            CHandler(ReturnToNormalVoting.Formatted(GeneralOptionTranslations.NoOneText)).Send(MyPlayer);
            return;
        }

        PlayerControl voted = votedPlayer.Get();
        byte player = voted.PlayerId;

        if (player == MyPlayer.PlayerId) return;

        if (confirmedVote = guardedPlayer == player)
        {
            targetLockedIn = true;
            CHandler(ReturnToNormalVoting.Formatted(Players.FindPlayerById(guardedPlayer)?.name)).Send(MyPlayer);
            return;
        }

        protectedIndicator?.Delete();
        guardedPlayer = player;
        protectedIndicator = voted.NameModel().GCH<IndicatorHolder>().Add(new SimpleIndicatorComponent("<b>+</b>", CrossColor, Game.InGameStates, MyPlayer));
        Players.GetDeadPlayers().ForEach(p => protectedIndicator?.Get().AddViewer(p));

        CHandler(SelectedPlayerMessage.Formatted(Players.FindPlayerById(guardedPlayer)?.name)).Send(MyPlayer);
    }

    [RoleAction(LotusActionType.Disconnect)]
    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void CheckForDisconnectAndDeath(PlayerControl player, ActionHandle handle)
    {
        if (player.PlayerId != guardedPlayer) return;
        bool resetGuard = handle.ActionType is LotusActionType.Disconnect;
        resetGuard = resetGuard || handle.ActionType is LotusActionType.PlayerDeath or LotusActionType.Exiled && mode is GuardMode.OnDeath;

        protectedIndicator?.Delete();
        if (!resetGuard) return;

        targetLockedIn = false;
        guardedPlayer = byte.MaxValue;
    }

    [RoleAction(LotusActionType.Exiled, ActionFlag.GlobalDetector)]
    private void CheckForExiledPlayer(PlayerControl exiled, ActionHandle handle)
    {
        if (exiled != null) CheckForDisconnectAndDeath(exiled, handle);
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    private void HandleMyDeath()
    {
        protectedIndicator?.Delete();
        guardedPlayer = byte.MaxValue;
    }


    [RoleAction(LotusActionType.Interaction, ActionFlag.GlobalDetector)]
    private void AnyPlayerInteraction(PlayerControl target, PlayerControl interactor, Interaction interaction, ActionHandle handle)
    {
        Intent intent = interaction.Intent;
        if (interactor.PlayerId == MyPlayer.PlayerId) return;
        if (Game.State is not GameState.Roaming) return;
        if (guardedPlayer != target.PlayerId) return;

        switch (intent)
        {
            case IHelpfulIntent when !protectAgainstHelpfulInteraction:
            case INeutralIntent when !protectAgainstNeutralInteraction:
                return;
        }

        if (interaction is IDelayedInteraction or IRangedInteraction or IIndirectInteraction) return;
        handle.Cancel();

        RoleUtils.SwapPositions(target, MyPlayer);
        Game.MatchData.GameHistory.AddEvent(new PlayerSavedEvent(target, MyPlayer, interactor));
        LotusInteraction lotusInteraction = new(new FatalIntent(false, () => new CustomDeathEvent(MyPlayer, interactor, ModConstants.DeathNames.Parried)), this);
        InteractionResult result = MyPlayer.InteractWith(interactor, lotusInteraction);

        if (result is InteractionResult.Proceed) Game.MatchData.GameHistory.AddEvent(new KillEvent(MyPlayer, interactor));

        if (interactor.PrimaryRole().GetActions(LotusActionType.Attack).Any() || interactor.GetSubroles().Any(r => r.GetActions(LotusActionType.Attack).Any()))
        {
            if (interactor.InteractWith(MyPlayer, LotusInteraction.FatalInteraction.Create(this)) is InteractionResult.Proceed)
                Game.MatchData.GameHistory.AddEvent(new KillEvent(interactor, MyPlayer));
        }
    }

    private ChatHandler CHandler(string message) => new ChatHandler()
        .Title(t => t.PrefixSuffix(StarText).Color(RoleColor).Text(RoleName).Build())
        .LeftAlign().Message(message);

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.3f, 0.45f, 0.31f));

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Change Guarded Player", ChangeGuardedPlayer)
                .Value(v => v.Text(OnDeathValue).Value(2).Build())
                .Value(v => v.Text(MeetingsValue).Value(1).Build())
                .Value(v => v.Text(NeverValue).Value(0).Build())
                .BindInt(o => mode = (GuardMode)o)
                .Build())
            .SubOption(sub => sub.KeyName("Protect against Beneficial Interactions", Colorize(Translations.Options.BeneficialInteractionProtection, ModConstants.Palette.PassiveColor))
                .BindBool(b => protectAgainstHelpfulInteraction = b)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub.KeyName("Protect against Neutral Interactions", Colorize(Translations.Options.NeutralInteractionProtection, ModConstants.Palette.NeutralColor))
                .BindBool(b => protectAgainstNeutralInteraction = b)
                .AddOnOffValues()
                .Build());

    protected enum GuardMode
    {
        Never,
        AnyMeeting,
        OnDeath
    }

    [Localized(nameof(Bodyguard))]
    private static class Translations
    {
        [Localized(nameof(BodyguardHelpMessage))]
        public static string BodyguardHelpMessage = "You are a Bodyguard! Your duty: to protect the innocent! Vote a player to protect next round! Alternatively, you can skip here to return to normal voting.";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(BeneficialInteractionProtection))]
            public static string BeneficialInteractionProtection = "Protect against Beneficial::0 Interactions";

            [Localized(nameof(NeutralInteractionProtection))]
            public static string NeutralInteractionProtection = "Protect against Neutral::0 Interactions";
        }
    }
}