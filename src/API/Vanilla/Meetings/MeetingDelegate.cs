using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using InnerNet;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Managers;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Impostors;
using Lotus.Roles;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using static MeetingHud;
using Lotus.Managers.Blackscreen.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;

namespace Lotus.API.Vanilla.Meetings;

[Localized("Meetings")]
public class MeetingDelegate
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(MeetingDelegate));

    [Localized(nameof(RoleRevealText))]
    public static string RoleRevealText = "{0} was the {1}.";

    [Localized(nameof(RemainingImpostorsText))]
    public static string RemainingImpostorsText = "{0} impostors remaining.";

    [Localized(nameof(NoImpostorsText))]
    public static string NoImpostorsText = "No impostors remaining.";

    public static MeetingDelegate Instance = null!;
    public NetworkedPlayerInfo? ExiledPlayer { get; set; }
    public HashSet<byte> TiedPlayers = new();
    public bool AlreadyCompleted;

    public bool IsTie { get; set; }
    internal IBlackscreenResolver BlackscreenResolver { get; }


    private MeetingHud MeetingHud => MeetingHud.Instance;
    private Dictionary<byte, List<Optional<byte>>> currentVotes = new();
    private List<JudgeOverrule> currentOverrules = new();
    private bool isForceEnd;

    public MeetingDelegate()
    {
        Instance = this;
        BlackscreenResolver = ProjectLotus.Instance.GetNewBlackscreenResolver(this);
    }

    public void CastVote(PlayerControl player, Optional<PlayerControl> target)
    {
        log.Trace($"{player.GetNameWithRole()} casted vote for {target.Map(p => p.GetNameWithRole()).OrElse("No One")}.");
        CastVote(player.PlayerId, target.Map(p => p.PlayerId));
    }

    public void CastVote(PlayerControl player, PlayerId target)
    {
        log.Trace($"{player.GetNameWithRole()} casted vote for {Utils.PlayerById(target.Value).Transform(p => p.GetNameWithRole(), () => "No One")}.");
        CastVote(player.PlayerId, Optional<byte>.NonNull(target.Value));
    }

    public void CastVote(byte playerId, Optional<byte> target)
    {
        currentVotes.GetOrCompute(playerId, () => new List<Optional<byte>>()).Add(target);
    }

    public void AddOverrule(PlayerControl player, PlayerControl target, ushort overruleNonce)
    {
        log.Trace($"{player.GetNameWithRole()} casted overrule vote for {target.GetNameWithRole()}.");
        AddOverrule(player.PlayerId, target.PlayerId, overruleNonce);
        CastVote(player.PlayerId, Optional<byte>.NonNull(254));
    }

    public void AddOverrule(byte playerId, byte targetId, ushort overruleNonce)
    {
        JudgeOverrule? existingOverrule = currentOverrules.FirstOrDefault(o => o.JudgePlayerId.Value == playerId);
        if (existingOverrule != null)
        {
            existingOverrule.OverruledPlayerId = new PlayerId(targetId);
            existingOverrule.OverruleNonce = overruleNonce;
            return;
        }
        currentOverrules.Add(new JudgeOverrule(new PlayerId(targetId), new PlayerId(playerId), overruleNonce));
    }

    public void RemoveVote(PlayerControl player, Optional<PlayerControl> target) => RemoveVote(player.PlayerId, target.Map(p => p.PlayerId));

    public void RemoveVote(byte playerId, Optional<byte> target)
    {
        RemoveOverrule(playerId);
        List<Optional<byte>> votes = currentVotes.GetOrCompute(playerId, () => new List<Optional<byte>>());
        int index = target.Transform(
            tId => votes.FindIndex(opt => opt.Map(b => b == tId).OrElse(false)),
            () => votes.Count - 1);
        if (index == -1) return;
        votes.RemoveAt(index);
    }

    public void RemoveOverrule(PlayerControl player) => RemoveOverrule(player.PlayerId);

    public void RemoveOverrule(byte playerId) => currentOverrules.RemoveAll(o => o.JudgePlayerId.Value == playerId);

    public void ClearAllVotes()
    {
        List<byte> keys = new();
        List<List<Optional<byte>>> votes = new();
        currentOverrules.Clear();
        currentVotes.Keys.ForEach(playerId => keys.Insert(0, playerId));
        currentVotes.Values.ForEach(playerVotes => votes.Insert(0, playerVotes.ToList()));
        keys.ForEach((key, index) => votes[index].ForEach(vote => RemoveVote(key, vote)));
    }

    public Dictionary<byte, int> CurrentVoteCount()
    {
        Dictionary<byte, int> counts = new() { { 253, 0 } };
        currentVotes.ForEach(kv =>
            kv.Value.Select(o => o.OrElse(255))
                .ForEach(b => counts[b] = counts.GetValueOrDefault(b, 0) + 1)
            );
        counts.Remove(255); // 255 is no vote. we remove it.
        return counts;
    }

    public int GetPlayersRemainingToVote() =>
        Players.GetPlayers(PlayerFilter.Alive)
            .Count(p => !currentVotes
                .GetOrCompute(p.PlayerId, () => [])
                .Any());

    public Dictionary<byte, List<Optional<byte>>> CurrentVotes() => currentVotes;

    public List<JudgeOverrule> CurrentOverrules() => currentOverrules;

    public void EndVoting() => isForceEnd = true;

    public void EndVoting(NetworkedPlayerInfo? exiledPlayer, bool isTie = false, bool checkOverrule = true)
    {
        List<VoterState> voterStates = new();
        CurrentVoteCount().ForEach(t =>
        {
            VoterState voterState = new() { VotedForId = t.Key };
            for (int i = 0; i < t.Value; i++) voterStates.Add(voterState);
        });

        EndVoting(voterStates.ToArray(), exiledPlayer, isTie, checkOverrule);
    }

    public void EndVoting(VoterState[] voterStates, NetworkedPlayerInfo? exiledPlayer, bool isTie = false, bool checkOverrule = true)
    {
        this.isForceEnd = true;
        this.IsTie = isTie;

        bool wasOverruled = false;
        ushort overruleNonce = 0;
        if (checkOverrule && TryGetWinningOverrule(out JudgeOverrule? winningOverrule, out PlayerControl? judge, out PlayerControl? target))
        {
            wasOverruled = true;
            overruleNonce = winningOverrule.OverruleNonce;
            exiledPlayer = judge.PrimaryRole().Relationship(target.PrimaryRole()) is Relation.None
                ? target.Data
                : judge.Data;
        }

        this.ExiledPlayer = exiledPlayer;

        if (ExiledPlayer != null)
        {
            List<byte> playerVotes = CurrentVotes().SelectMany(kv => kv.Value.Filter().Where(i => i == ExiledPlayer.PlayerId).Select(i => kv.Key)).ToList();
            CheckAndSetConfirmEjectText(ExiledPlayer.Object);
            Hooks.MeetingHooks.ExiledHook.Propagate(new ExiledHookEvent(ExiledPlayer, playerVotes));
        }

        MeetingHud.RpcVotingComplete(voterStates, exiledPlayer, isTie, wasOverruled, overruleNonce);
    }

    public bool IsForceEnd() => isForceEnd;

    // This function does NOT check for overrules.
    // You have to handle that in your own code.
    public void CalculateExiledPlayer()
    {
        List<KeyValuePair<byte, int>> sortedVotes = this.CurrentVoteCount().Sorted(kvp => kvp.Value).Reverse().ToList();
        bool isTie = false;
        byte exiledPlayer = byte.MaxValue;
        int mostVotes = 0;
        switch (sortedVotes.Count)
        {
            case 0: break;
            case 1:
                mostVotes = sortedVotes[0].Value;
                exiledPlayer = sortedVotes[0].Key;
                break;
            case >= 2:
                mostVotes = sortedVotes[0].Value;
                isTie = sortedVotes[0].Value == sortedVotes[1].Value;
                exiledPlayer = sortedVotes[0].Key;
                break;
        }

        TiedPlayers = sortedVotes.Where(sv => sv.Value == mostVotes).Select(sv => sv.Key).ToHashSet();

        this.ExiledPlayer = Players.PlayerById(exiledPlayer).Map(p => p.Data).OrElse(null!);
        this.IsTie = isTie;

        string mostVotedPlayer = this.ExiledPlayer?.Object != null ? this.ExiledPlayer.Object.name : "Unknown";
        log.Trace($"Calculated player votes. Player with most votes = {mostVotedPlayer}, isTie = {isTie}");

        if (IsTie) this.ExiledPlayer = null;
    }

    public bool TryGetWinningOverrule([MaybeNullWhen(false)] out JudgeOverrule winningOverrule, [MaybeNullWhen(false)] out PlayerControl judge,
        [MaybeNullWhen(false)] out PlayerControl target)
    {
        winningOverrule = null;
        judge = null;
        target = null;
        var winning = CurrentOverrules().FirstOrDefault(o =>
        {
            PlayerControl? tempJudge = Utils.GetPlayerById(o.JudgePlayerId);
            PlayerControl? tempTarget = Utils.GetPlayerById(o.OverruledPlayerId);
            return (tempJudge && tempJudge!.Data && !tempJudge.Data.Disconnected && tempTarget && tempTarget!.Data &&
                    !tempTarget.Data.Disconnected);
        });
        if (winning == null)
            return false;

        winningOverrule = winning;
        judge = Utils.GetPlayerById(winning.JudgePlayerId)!;
        target = Utils.GetPlayerById(winning.OverruledPlayerId)!;
        return true;
    }

    public void CheckAndSetConfirmEjectText(PlayerControl player)
    {
        if (!AUSettings.ConfirmImpostor() || !player) return;

        int impostorCount = Players.GetPlayers(PlayerFilter.Impostor | PlayerFilter.Alive).Count() - 1;

        CustomRole roleDefinition = player.PrimaryRole();
        string textFormatting = "<size=0><size=2.5>" + RoleRevealText.Formatted(player.name, roleDefinition.RoleColor.Colorize(roleDefinition.RoleName));
        textFormatting += "\n" + (impostorCount == 0 ? NoImpostorsText : RemainingImpostorsText.Formatted(impostorCount)) + "</size>";

        player.RpcSetName(textFormatting);
    }
}