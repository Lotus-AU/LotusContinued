using System;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Extensions;
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
using Lotus.GameModes.Standard;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles.Managers.Interfaces;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Lotus.API;
using Lotus.Utilities;
using Lotus.Options;
using VentLib.Options;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using MonoMod.Utils;
using Lotus.Factions.Impostors;
using Lotus.Factions.Crew;
using Lotus.Factions.Neutrals;
using Lotus.Factions.Undead;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Impl;
using Lotus.Logging;
using Lotus.Managers.History.Events;
using Lotus.Roles.Builtins;
using Lotus.Roles.Interfaces;
using Lotus.RPC.CustomObjects.Interfaces;
using RewiredConsts;
using VentLib.Networking.RPC;
using VentLib.Utilities.Collections;
using CollectionExtensions = HarmonyLib.CollectionExtensions;

namespace Lotus.Roles.Subroles;

public class Guesser : Subrole, IInfoResender
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Guesser));

    public override string Identifier() => "⌘";

    // ctrl+c, ctrl+ --- BOOM! we have options :)
    public static Dictionary<Type, int> CanGuessDictionary = new();
    public static Dictionary<Type, int> FactionMaxDictionary = new();

    public static List<(Func<CustomRole, bool> predicate, GameOptionBuilder builder)> RoleTypeBuilders = [
        (r => r.Faction.GetType() == typeof(ImpostorFaction), new GameOptionBuilder()
            .KeyName("Impostor Settings", TranslationUtil.Colorize(Translations.Options.ImpostorSetting, Color.red))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2)),
        (r => r.Faction is Madmates, new GameOptionBuilder()
            .KeyName("Madmates Settings", TranslationUtil.Colorize(Translations.Options.MadmateSetting, ModConstants.Palette.MadmateColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2)),
        (r => r.Faction is Crewmates, new GameOptionBuilder()
            .KeyName("Crewmate Settings", TranslationUtil.Colorize(Translations.Options.CrewmateSetting, ModConstants.Palette.CrewmateColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2)),
        (r => r.SpecialType is SpecialType.NeutralKilling or SpecialType.Undead, new GameOptionBuilder()
            .KeyName("Neutral Killing Settings", TranslationUtil.Colorize(Translations.Options.NeutralKillingSetting, ModConstants.Palette.NeutralColor, ModConstants.Palette.KillingColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2)),
        (r => r.SpecialType is SpecialType.Neutral, new GameOptionBuilder()
            .KeyName("Neutral Passive Settings", TranslationUtil.Colorize(Translations.Options.NeutralPassiveSetting, ModConstants.Palette.NeutralColor, ModConstants.Palette.PassiveColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2)),
        (r => r.RoleFlags.HasFlag(RoleFlag.IsSubrole), new GameOptionBuilder()
            .KeyName("Modifier Settings", TranslationUtil.Colorize(Translations.Options.SubroleSetting, ModConstants.Palette.ModifierColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2))
    ];
    public static readonly List<int> RoleTypeSettings = [0, 0, 0, 0, 0, 0];

    private bool restrictToNonVotingRoles;
    private bool canGuessTeammates;
    private int guessesPerMeeting;

    private byte guessingPlayer = byte.MaxValue;
    private bool skippedVote;
    private CustomRole? guessedRole;
    private int guessesThisMeeting;

    private bool meetingEnded;
    private bool debounce;

    private IFaction lastFaction;

    private FixedUpdateLock fixedUpdateLock = new(5f);

    [NewOnSetup(true)] private MeetingPlayerSelector voteSelector = new();
    [NewOnSetup] private List<CustomRole> guessableRoles = [];

    public Guesser()
    {
        StandardRoles.Callbacks.Add(PopulateGuesserSettings);
    }

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

    private void ResetGuessableRoles()
    {
        lastFaction = Faction;
        guessableRoles = [];

        guessableRoles = StandardRoles.Instance.AllRoles
            .Where(r => HostTurnedRoleOn(r)
                        || r.GetRoleType() is RoleType.DontShow && r.LinkedRoles().Any(HostTurnedRoleOn)
                        || r.RoleFlags.HasFlag(RoleFlag.VariationRole) && r.LinkedRoles().Any(HostTurnedRoleOn)
                        || r.RoleFlags.HasFlag(RoleFlag.TransformationRole) && r.LinkedRoles().Any(HostTurnedRoleOn))
            .Where(r =>
            {
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
        if (targetPlayer.PlayerId == MyPlayer.PlayerId) return CancelGuessReason.Teammate;
        if (targetPlayer.PrimaryRole().Faction.GetType() == MyPlayer.PrimaryRole().Faction.GetType())
        {
            if (MyPlayer.PrimaryRole().Faction.CanSeeRole(MyPlayer)) return canGuessTeammates ? CancelGuessReason.None : CancelGuessReason.Teammate;
            return CancelGuessReason.None;
        }
        bool canSeeRole = false;
        RoleComponent? roleComponent = targetPlayer.NameModel().GetComponentHolder<RoleHolder>().LastOrDefault();
        if (roleComponent != null) canSeeRole = roleComponent.Viewers().Any(p => p.PlayerId == MyPlayer.PlayerId);
        return canSeeRole ? CancelGuessReason.CanSeeRole : CancelGuessReason.None;
    }
    protected virtual bool CanGuessRole(CustomRole role) => true;

    public override CompatabilityMode RoleCompatabilityMode => CompatabilityMode.Blacklisted;
    public override HashSet<Type>? RestrictedRoles()
    {
        HashSet<Type>? restrictedRoles = base.RestrictedRoles();
        if (!restrictToNonVotingRoles) return restrictedRoles;
        // last resort is also a vote related subrole, so just use what they have
        LastResort.IncompatibleRoles.ForEach(r => restrictedRoles?.Add(r));
        return restrictedRoles;
    }
    // If the faction is neutral, use neutral type attribute.
    private Type GetFactionType(IFaction playerFaction) => playerFaction is INeutralFaction
        ? FactionInstances.Neutral.GetType()
        : playerFaction.GetType();

    private int GetAmountOfPeopleOnFaction(Type faction) => Players.GetAlivePlayers().Count(p =>
        GetFactionType(p.PrimaryRole().Faction) == faction && p.GetSubroles().Any(s => s is Guesser));

    public override bool IsAssignableTo(PlayerControl player)
    {
        Type myFaction = GetFactionType(player.PrimaryRole().Faction);

        // Check if their faction already has the max amount of allowed players.
        // If they are maxed out, we don't even call base and just immediately exit.

        if (GetAmountOfPeopleOnFaction(myFaction) >= FactionMaxDictionary.GetValueOrDefault(myFaction, 0))
            return false;

        // Return base as that's the only check.
        // Base checks restricted roles.
        return player.PrimaryRole() is not GuesserRole && base.IsAssignableTo(player);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Restricted to Non Vote-Related Roles", Translations.Options.RestrictedToNonVote)
                .AddBoolean()
                .BindBool(b => restrictToNonVotingRoles = b)
                .Build())
            .SubOption(sub => sub
                .KeyName("Guesses per Meeting", Translations.Options.GuesserPerMeeting)
                .AddIntRange(1, ModConstants.MaxPlayers, 1, 0)
                .BindInt(i => guessesPerMeeting = i)
                .Build())
            .SubOption(sub => sub
                .KeyName("Can Guess Teammates", Translations.Options.CanGuessTeammates)
                .AddBoolean(false)
                .BindBool(b => canGuessTeammates = b)
                .Build());

    private void PopulateGuesserSettings()
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
            string keyName = Translations.Options.FactionMaxGuessers.Formatted(kvp.Value.Name());
            Option option = new GameOptionBuilder()
                .KeyName(TranslationUtil.Remove(keyName), TranslationUtil.Colorize(keyName, kvp.Value.Color))
                .AddIntRange(0, ModConstants.MaxPlayers, 1, 1)
                .BindInt(i => FactionMaxDictionary[kvp.Key] = i)
                .Build();
            RoleOptions.AddChild(option);
            GlobalRoleManager.RoleOptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        });
        StandardRoles.Instance.AllRoles.OrderBy(r => r.EnglishRoleName).ForEach(r =>
        {
            RoleTypeBuilders.FirstOrOptional(b => b.predicate(r)).Map(i => i.builder)
                .IfPresent(builder =>
                {
                    builder.SubOption(sub => sub.KeyName(r.EnglishRoleName, r.RoleColor.Colorize(r.RoleName))
                        .AddBoolean()
                        .BindBool(b =>
                        {
                            if (b) CanGuessDictionary[r.GetType()] = 1;
                            else CanGuessDictionary[r.GetType()] = 2;
                        })
                        .Build());
                });
        });
        RoleTypeBuilders.ForEach((rtb, index) =>
        {
            rtb.builder.BindInt(i => RoleTypeSettings[index] = i);
            Option option = rtb.builder.Build();
            RoleOptions.AddChild(option);
            GlobalRoleManager.RoleOptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        });
    }
    protected ChatHandler GuesserMessage(string message) => ChatHandler.Of(message, RoleColor.Colorize(Translations.GuesserTitle)).LeftAlign();

    [Localized(nameof(Guesser))]
    public static class Translations
    {
        [Localized(nameof(GuesserTitle))] public static string GuesserTitle = "Guesser";

        [Localized(nameof(HintMessage))] public static string HintMessage = "You have {0} guess(es) left. Vote the player you want to try to guess.";
        [Localized(nameof(NoGuessesLeft))] public static string NoGuessesLeft = "You have ran out of guesses. Your next vote now acts as normal.";
        [Localized(nameof(SkippedGuessing))] public static string SkippedGuessing = "You have exited guessing mode. Your next vote now acts as normal.";

        [Localized(nameof(GuessDeathAnnouncement))] public static string GuessDeathAnnouncement = "A guesser has made a guess.\n{0} has died.";

        [Localized(nameof(CantGuessBecauseOfRole))] public static string CantGuessBecauseOfRole = "Your role prevented you from guessing {0}! Try a different player.";
        [Localized(nameof(CantGuessKnownRole))] public static string CantGuessKnownRole = "You can see {0}'s role! You can't guess that player.";
        [Localized(nameof(CantGuessTeammate))] public static string CantGuessTeammate = "{0} is your teammate! You can't guess that player.";
        [Localized(nameof(UnknownRole))] public static string UnknownRole = "Unknown role {0}. You can type '/cmd available' to view all roles your are able to guess.";

        [Localized(nameof(PickedRoleText))] public static string PickedRoleText = "You are attempting to guess {0} as {1}. To confirm this guess, vote that player again.\nOtherwise, vote another player to reset the process.";
        [Localized(nameof(PickedPlayerText))] public static string PickedPlayerText = "You have selected to guess {0}. To select a role, type `/cmd [rolename]`. This command will be hidden from other players.";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(RestrictedToNonVote))] public static string RestrictedToNonVote = "Restricted to Non Vote-Related Roles";
            [Localized(nameof(GuesserPerMeeting))] public static string GuesserPerMeeting = "Guesses per Meeting";
            [Localized(nameof(CanGuessTeammates))] public static string CanGuessTeammates = "Can Guess Teammates";
            [Localized(nameof(FollowGuesserSettings))] public static string FollowGuesserSettings = "Follow Guesser Settings";

            [Localized(nameof(FactionMaxGuessers))] public static string FactionMaxGuessers = "{0}::0 Faction Max Guessers";

            [Localized(nameof(NeutralKillingSetting))] public static string NeutralKillingSetting = "Can Guess Neutral::0 Killing::1";
            [Localized(nameof(NeutralPassiveSetting))] public static string NeutralPassiveSetting = "Can Guess Neutral::0 Passive::1";
            [Localized(nameof(ImpostorSetting))] public static string ImpostorSetting = "Can Guess Impostors::0";
            [Localized(nameof(CrewmateSetting))] public static string CrewmateSetting = "Can Guess Crewmates::0";
            [Localized(nameof(MadmateSetting))] public static string MadmateSetting = "Can Guess Madmates::0";
            [Localized(nameof(SubroleSetting))] public static string SubroleSetting = "Can Guess Subroles::0";
        }
    }


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleFlags(RoleFlag.RemoveRoleMaximum)
            .RoleColor(new Color(.84f, .74f, 0f));


}

public enum CancelGuessReason
{
    None,
    Teammate,
    CanSeeRole,
    RoleSpecificReason
}