using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Managers;
using Lotus.Extensions;
using Lotus.Roles;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using static MeetingHud;
using Lotus.src.Managers.Blackscreen.Interfaces;

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

    public bool IsTie { get; set; }
    internal IBlackscreenResolver BlackscreenResolver { get; }


    private MeetingHud MeetingHud => MeetingHud.Instance;
    private Dictionary<byte, List<Optional<byte>>> currentVotes = new();
    private bool isForceEnd;

    public MeetingDelegate()
    {
        Instance = this;
        BlackscreenResolver = ProjectLotus.Instance.GetNewBlackscreenResolver(this);
    }

    public void CastVote(PlayerControl player, Optional<PlayerControl> target)
    {
        log.Trace($"{player.GetNameWithRole()} casted vote for {target.Map(p => p.GetNameWithRole()).OrElse("No One")}");
        CastVote(player.PlayerId, target.Map(p => p.PlayerId));
    }

    public void CastVote(byte playerId, Optional<byte> target)
    {
        currentVotes.GetOrCompute(playerId, () => new List<Optional<byte>>()).Add(target);
    }

    public void RemoveVote(PlayerControl player, Optional<PlayerControl> target) => RemoveVote(player.PlayerId, target.Map(p => p.PlayerId));

    public void RemoveVote(byte playerId, Optional<byte> target)
    {
        List<Optional<byte>> votes = currentVotes.GetOrCompute(playerId, () => new List<Optional<byte>>());
        int index = target.Transform(
            tId => votes.FindIndex(opt => opt.Map(b => b == tId).OrElse(false)),
            () => votes.Count - 1);
        if (index == -1) return;
        votes.RemoveAt(index);
    }

    public void ClearAllVotes()
    {
        List<byte> keys = new();
        List<List<Optional<byte>>> votes = new();
        currentVotes.Keys.ForEach(playerId => keys.Insert(0, playerId));
        currentVotes.Values.ForEach(playerVotes => votes.Insert(0, playerVotes.ToList()));
        keys.ForEach((key, index) => votes[index].ForEach(vote => RemoveVote(key, vote)));
    }

    public Dictionary<byte, int> CurrentVoteCount()
    {
        Dictionary<byte, int> counts = new() { { 255, 0 } };
        currentVotes.ForEach(kv =>
            kv.Value.Select(o => o.OrElse(255))
                .ForEach(b => counts[b] = counts.GetValueOrDefault(b, 0) + 1)
            );
        return counts;
    }

    public Dictionary<byte, List<Optional<byte>>> CurrentVotes() => currentVotes;

    public void EndVoting() => isForceEnd = true;

    public void EndVoting(NetworkedPlayerInfo? exiledPlayer, bool isTie = false)
    {
        List<VoterState> voterStates = new();
        CurrentVoteCount().ForEach(t =>
        {
            VoterState voterState = new() { VotedForId = t.Key };
            for (int i = 0; i < t.Value; i++) voterStates.Add(voterState);
        });

        EndVoting(voterStates.ToArray(), exiledPlayer, isTie);
    }

    public void EndVoting(VoterState[] voterStates, NetworkedPlayerInfo? exiledPlayer, bool isTie = false)
    {
        this.isForceEnd = true;
        this.ExiledPlayer = exiledPlayer;
        this.IsTie = isTie;

        if (ExiledPlayer != null)
        {
            List<byte> playerVotes = CurrentVotes().SelectMany(kv => kv.Value.Filter().Where(i => i == ExiledPlayer.PlayerId).Select(i => kv.Key)).ToList();
            CheckAndSetConfirmEjectText(ExiledPlayer.Object);
            Hooks.MeetingHooks.ExiledHook.Propagate(new ExiledHookEvent(ExiledPlayer, playerVotes));
        }

        MeetingHud.RpcVotingComplete(voterStates, exiledPlayer, isTie);
    }

    public bool IsForceEnd() => isForceEnd;

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

    public void CheckAndSetConfirmEjectText(PlayerControl player)
    {
        if (!AUSettings.ConfirmImpostor() || player == null) return;

        int impostors = Players.GetPlayers(PlayerFilter.Impostor | PlayerFilter.Alive).Count();

        CustomRole roleDefinition = player.PrimaryRole();
        string textFormatting = "<size=0><size=2.5>" + RoleRevealText.Formatted(player.name, roleDefinition.RoleColor.Colorize(roleDefinition.RoleName));
        textFormatting += "\n" + (impostors == 0 ? NoImpostorsText : RemainingImpostorsText.Formatted(impostors)) + "</size>";

        player.RpcSetName(textFormatting);
    }
}