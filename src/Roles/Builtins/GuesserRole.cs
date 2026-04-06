using System;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Managers;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Trackers;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles.Managers.Interfaces;
using System.Linq;
using Lotus.Roles.Subroles;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using System.Collections.Generic;
using System.Text;
using Lotus.API;
using Lotus.Factions.Interfaces;
using Lotus.GameModes.Normal.Standard;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interfaces;
using Lotus.RPC.CustomObjects.Interfaces;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities.Collections;
using static Lotus.Roles.Subroles.Guesser;
using CollectionExtensions = HarmonyLib.CollectionExtensions;

namespace Lotus.Roles.Builtins;

public class GuesserRole : CustomRole, IInfoResender
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(GuesserRole));

    protected int guessesPerMeeting;
    protected bool followGuesserSettings;

    protected byte guessingPlayer = byte.MaxValue;
    protected bool skippedVote;
    protected CustomRole? guessedRole;
    protected int guessesThisMeeting;

    protected bool meetingEnded;
    protected bool debounce;

    protected IFaction lastFaction;

    protected FixedUpdateLock fixedUpdateLock = new(5f);

    [NewOnSetup(true)] protected MeetingPlayerSelector voteSelector = new();
    [NewOnSetup] protected List<CustomRole> guessableRoles = [];

    public void ResendMessages()
    {
        GuesserMessage(Translations.HintMessage.Formatted(guessesThisMeeting)).Send(MyPlayer);
    }

    [RoleAction(LotusActionType.RoundEnd)]
    public void ResetPreppedPlayer()
    {
        voteSelector.Reset();
        guessingPlayer = byte.MaxValue;
        skippedVote = false;
        meetingEnded = false;
        guessedRole = null;
        debounce = false;
        guessesThisMeeting = guessesPerMeeting;
        if (lastFaction != Faction) ResetGuessableRoles();
        ResendMessages();
    }

    [RoleAction(LotusActionType.Vote, priority:Priority.VeryHigh)]
    public void SelectPlayerToGuess(Optional<PlayerControl> player, MeetingDelegate _, ActionHandle handle)
    {
        if (skippedVote || guessesThisMeeting <= 0 || meetingEnded) return;
        handle.Cancel();
        VoteResult result = voteSelector.CastVote(player);
        switch (result.VoteResultType)
        {
            case VoteResultType.None:
                break;
            case VoteResultType.Skipped:
                skippedVote = true;
                GuesserMessage(Translations.SkippedGuessing).Send(MyPlayer);
                break;
            case VoteResultType.Selected:
                PlayerControl? targetPlayer = Players.FindPlayerById(result.Selected);
                if (targetPlayer == null) break;
                CancelGuessReason reason = CanGuessPlayer(targetPlayer);
                if (reason is CancelGuessReason.None)
                {
                    guessingPlayer = result.Selected;
                    GuesserMessage(Translations.PickedPlayerText.Formatted(targetPlayer.name)).Send(MyPlayer);
                }
                else
                    GuesserMessage(reason switch
                    {
                        CancelGuessReason.RoleSpecificReason => Translations.CantGuessBecauseOfRole.Formatted(targetPlayer.name),
                        CancelGuessReason.Teammate => Translations.CantGuessTeammate.Formatted(targetPlayer.name),
                        CancelGuessReason.CanSeeRole => Translations.CantGuessKnownRole.Formatted(targetPlayer.name),
                        _ => throw new ArgumentOutOfRangeException()
                    }).Send(MyPlayer);
                break;
            case VoteResultType.Confirmed:
                if (guessedRole == null)
                {
                    voteSelector.Reset();
                    voteSelector.CastVote(player);
                    SelectPlayerToGuess(player, _, handle);
                    return;
                }

                PlayerControl? guessed = Players.FindPlayerById(guessingPlayer);
                if (guessed == null) return;

                guessesThisMeeting -= 1;
                if (guessesThisMeeting <= 0) GuesserMessage(Translations.NoGuessesLeft).Send(MyPlayer);
                else ResendMessages();

                guessingPlayer = byte.MaxValue;
                voteSelector.Reset();

                bool successfulGuess = guessed.PrimaryRole().GetType() == guessedRole.GetType() ||
                                       guessed.GetSubroles().Any(s => s.GetType() == guessedRole.GetType());

                if (successfulGuess) HandleCorrectGuess(guessed, guessedRole);
                else HandleBadGuess();
                guessedRole = null;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [RoleAction(LotusActionType.Chat)]
    public void DoGuesserVoting(string message, GameState state, bool isAlive)
    {
        if (!isAlive) return;
        if (state is not GameState.InMeeting) return;
        if (guessingPlayer == byte.MaxValue) return;
        if (!message.StartsWith("/cmd")) return;
        string roleName = message.Substring("/cmd".Length).Trim().ToLower();
        if (string.IsNullOrEmpty(roleName))
        {
            ChatHandlers.InvalidCmdUsage().Send(MyPlayer);
            return;
        }

        if (debounce) return;
        debounce = true;

        if (roleName == "available")
        {
            if (fixedUpdateLock.AcquireLock()) GuesserMessage(string.Join(", ", guessableRoles.Select(r => r.RoleName))).Send(MyPlayer);
            else ChatHandlers.InvalidCmdUsage().Send(MyPlayer);
            debounce = false;
            return;
        }

        Optional<CustomRole> role = guessableRoles.FirstOrOptional(r => r.RoleName.ToLower().Contains(roleName))
            .CoalesceEmpty(() => guessableRoles.FirstOrOptional(r => r.EnglishRoleName.Contains(roleName)))
            .CoalesceEmpty(() => guessableRoles.FirstOrOptional(r => r.Aliases.Contains(roleName)));

        if (role.Exists())
        {
            guessedRole = role.Get();
            GuesserMessage(Translations.PickedRoleText.Formatted(Players.FindPlayerById(guessingPlayer)?.name, guessedRole.RoleName)).Send(MyPlayer);
        }
        else GuesserMessage(Translations.UnknownRole.Formatted(roleName)).Send(MyPlayer);

        debounce = false;
    }

    [RoleAction(LotusActionType.MeetingEnd, ActionFlag.WorksAfterDeath)]
    public void CheckRevive()
    {
        meetingEnded = true;
    }

    protected virtual void ResetGuessableRoles()
    {
        lastFaction = Faction;
        guessableRoles = [];

        guessableRoles = NormalStandardRoles.Instance.AllRoles
            .Where(r => HostTurnedRoleOn(r)
                        || r.GetRoleType() is RoleType.DontShow && r.LinkedRoles().Any(HostTurnedRoleOn)
                        || r.RoleFlags.HasFlag(RoleFlag.VariationRole) && r.LinkedRoles().Any(HostTurnedRoleOn)
                        || r.RoleFlags.HasFlag(RoleFlag.TransformationRole) && r.LinkedRoles().Any(HostTurnedRoleOn))
            .Where(r =>
            {
                bool roleAllowsGuess = CanGuessRole(r);
                if (!roleAllowsGuess || !followGuesserSettings) return roleAllowsGuess;
                int setting = -1;
                RoleTypeBuilders.FirstOrOptional(b => b.predicate(r))
                    .IfPresent(rtb => setting = RoleTypeSettings[RoleTypeBuilders.IndexOf(rtb)]);
                return setting == -1 || setting == 2 ? CanGuessDictionary.GetValueOrDefault(r.GetType(), -1) == 1 : setting == 1;
            })
            .ToList();

        bool HostTurnedRoleOn(CustomRole r) => (r.Count > 0 || r.RoleFlags.HasFlag(RoleFlag.RemoveRoleMaximum)) &&
                                               (r.Chance > 0 || r.RoleFlags.HasFlag(RoleFlag.RemoveRolePercent));
    }

    protected virtual void HandleBadGuess()
    {
        GuesserMessage(Translations.GuessDeathAnnouncement.Formatted(MyPlayer.name)).Send();
        MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(
            new FatalIntent(true,
                () => new CustomDeathEvent(MyPlayer, MyPlayer, ModConstants.DeathNames.Guessed))
            , this));
    }

    protected virtual void HandleCorrectGuess(PlayerControl guessedPlayer, CustomRole guessedRole)
    {
        GuesserMessage(Translations.GuessDeathAnnouncement.Formatted(guessedPlayer.name)).Send();
        MyPlayer.InteractWith(guessedPlayer, new UnblockedInteraction(
            new FatalIntent(true,
                () => new CustomDeathEvent(guessedPlayer, MyPlayer, ModConstants.DeathNames.Guessed))
            , this));
    }

    protected virtual CancelGuessReason CanGuessPlayer(PlayerControl targetPlayer)
    {
        bool canSeeRole = false;
        RoleComponent? roleComponent = targetPlayer.NameModel().GetComponentHolder<RoleHolder>().LastOrDefault();
        if (roleComponent != null) canSeeRole = roleComponent.Viewers().Any(p => p == MyPlayer);
        return canSeeRole ? CancelGuessReason.CanSeeRole : CancelGuessReason.None;
    }
    protected virtual bool CanGuessRole(CustomRole role) => true;

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Guesses per Meeting", Guesser.Translations.Options.GuesserPerMeeting)
                .AddIntRange(1, 10, 1, 0)
                .BindInt(i => guessesPerMeeting = i)
                .Build())
            .SubOption(sub => sub.KeyName("Follow Guesser Settings", Guesser.Translations.Options.FollowGuesserSettings)
                .AddBoolean()
                .BindBool(b => followGuesserSettings = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier
        .VanillaRole(RoleTypes.Crewmate);

    protected ChatHandler GuesserMessage(string message) => ChatHandler.Of(message, RoleColor.Colorize(Translations.GuesserTitle)).LeftAlign();
}