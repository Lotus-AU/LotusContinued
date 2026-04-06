using Lotus.Extensions;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using Lotus.Roles.Internals.Enums;
using System.Collections.Generic;
using Lotus.Chat;
using VentLib.Utilities;
using Lotus.Roles.Internals;
using VentLib.Utilities.Optionals;
using Lotus.Roles.Internals.Trackers;
using Lotus.API.Player;
using System;
using System.Linq;
using Lotus.GameModes.Normal.Standard;
using VentLib.Utilities.Extensions;
using Lotus.Roles.Interfaces;
using VentLib.Options;
using Lotus.Managers;
using Lotus.Roles.Subroles;
using Lotus.Logging;

namespace Lotus.Roles.RoleGroups.Crew;

// There are probably a million ways to program this role better.
// But no one ever said I was good at programming.

public class Genie : Crewmate, IInfoResender
{
    /// <summary>
    /// A list of subroles the Genie is able to grant.
    /// </summary>
    public static List<Type> GoodSubroles = new()
    {
        typeof(Bait),
        typeof(Bewilder),
        typeof(Diseased),
        typeof(Flash),
        typeof(LastResort),
        typeof(Sleuth),
        typeof(TieBreaker),
        typeof(Torch),
        typeof(Watcher)
    };
    public static Dictionary<string, bool> SubroleAllowedDictionary = new();

    private int successChance;
    private int totalWishes;

    [NewOnSetup] private HashSet<byte> wishedPlayers = null!;
    private bool selectedPlayer;
    private int remainingWishes;

    [NewOnSetup(true)] private MeetingPlayerSelector voteSelector = new();

    public Genie()
    {
        NormalStandardRoles.Callbacks.Add(PopulateGenieOptions);
    }

    protected override void PostSetup() => remainingWishes = totalWishes;

    public void ResendMessages() => ChatHandler.Of(Translations.MeetingMessage, RoleColor.Colorize(RoleName)).Send(MyPlayer);

    [RoleAction(LotusActionType.RoundEnd)]
    private void MeetingMessage()
    {
        if (remainingWishes <= 0) return;
        ResendMessages();
        selectedPlayer = false;
        voteSelector.Reset();
    }

    [RoleAction(LotusActionType.Vote)]
    private void TargetSelect(Optional<PlayerControl> voted, ActionHandle handle)
    {
        if (selectedPlayer) return;
        handle.Cancel();
        VoteResult result = voteSelector.CastVote(voted);
        switch (result.VoteResultType)
        {
            case VoteResultType.None:
                break;
            // case VoteResultType.Unselected:
            case VoteResultType.Selected:
                if (wishedPlayers.Contains(result.Selected))
                {
                    voteSelector.Reset();
                    ChatHandler.Of(Translations.AlreadyWished.Formatted(voted.Get().name), RoleColor.Colorize(RoleName)).Send(MyPlayer);
                    return;
                }
                result.Send(MyPlayer);
                break;
            case VoteResultType.Skipped:
                result.Send(MyPlayer);
                selectedPlayer = true;
                break;
            case VoteResultType.Confirmed:
                TryGrantWish(Players.FindPlayerById(result.Selected));
                if (!selectedPlayer) voteSelector.Reset();
                break;
        }
    }

    private void TryGrantWish(PlayerControl? target)
    {
        if (target == null) return;

        IEnumerable<string> allowedSubRoleNames = SubroleAllowedDictionary.Where(kvp => kvp.Value).Select(kvp => kvp.Key);
        if (!allowedSubRoleNames.Any()) return;

        IEnumerable<ISubrole> allowedSubRoles = allowedSubRoleNames.Select(n => (ISubrole)NormalStandardRoles.Instance.AllRoles.First(r => r.EnglishRoleName == n)).Where(r => r.IsAssignableTo(target));
        if (!allowedSubRoles.Any()) return;

        allowedSubRoles = allowedSubRoles.Where(r => target.GetSubroles().All(s => s.GetType() != r.GetType()));
        if (!allowedSubRoles.Any()) return;

        CustomRole chosenSubRole = (CustomRole)allowedSubRoles.ToList().GetRandom();

        selectedPlayer = true;
        remainingWishes -= 1;

        bool grantedWish = new System.Random().Next(100 + 1) <= successChance;

        if (grantedWish)
        {
            ChatHandler.Of(Translations.WishGranted.Formatted(target.name, chosenSubRole.RoleName), RoleColor.Colorize(RoleName)).Send(MyPlayer);
            NormalStandardGameMode.Instance.Assign(target, chosenSubRole, false);
        }
        else ChatHandler.Of(Translations.WishFailed.Formatted(target.name, chosenSubRole.RoleName), RoleColor.Colorize(RoleName)).Send(MyPlayer);

        wishedPlayers.Add(target.PlayerId);
        if (remainingWishes == 0) ChatHandler.Of(Translations.OutOfWishes, RoleColor.Colorize(RoleName)).Send(MyPlayer);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => base.RegisterOptions(optionStream)
        .SubOption(sub => sub
            .KeyName("Max Wishes", Translations.Options.MaxGifts)
            .AddIntRange(1, Mathf.CeilToInt(ModConstants.MaxPlayers / 3), 1, 2)
            .BindInt(i => totalWishes = i)
            .Build())
        .SubOption(sub => sub
            .KeyName("Success Chance", Translations.Options.SuccessChance)
            .AddIntRange(10, 100, 10, 4, "%")
            .BindInt(i => successChance = i)
            .Build());

    private void PopulateGenieOptions()
    {
        NormalStandardRoles.Instance.AllRoles.OrderBy(r => r.EnglishRoleName).ForEach(r =>
        {
            if (r is not ISubrole) return;
            if (!GoodSubroles.Contains(r.GetType())) return;
            Option option = new GameOptionBuilder()
                .KeyName(r.EnglishRoleName, r.RoleColor.Colorize(r.RoleName))
                .AddBoolean()
                .BindBool(b => SubroleAllowedDictionary[r.EnglishRoleName] = b)
                .Build();
            RoleOptions.AddChild(option);
            GlobalRoleManager.RoleOptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        });
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleColor(new Color(0f, 0.2f, 0.8f));

    [Localized(nameof(Genie))]
    public static class Translations
    {
        [Localized(nameof(AlreadyWished))] public static string AlreadyWished = "You've already wished {0} a modifier. Try another player!";
        [Localized(nameof(WishFailed))] public static string WishFailed = "You tried to wish {0} the {1} modifier, but it failed.";
        [Localized(nameof(WishGranted))] public static string WishGranted = "You wished {0} the {1} modifier, and suceeded! They were granted the modifier!";
        [Localized(nameof(MeetingMessage))] public static string MeetingMessage = "Vote a player twice to wish them a gift! Or skip to go to regular voting.";
        [Localized(nameof(OutOfWishes))] public static string OutOfWishes = "You have ran out of wishes. You will no longer be given the option to vote for someone at the start of the meeting.";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(SuccessChance))] public static string SuccessChance = "Success Chance";
            [Localized(nameof(MaxGifts))] public static string MaxGifts = "Max Wishes";
        }
    }
}