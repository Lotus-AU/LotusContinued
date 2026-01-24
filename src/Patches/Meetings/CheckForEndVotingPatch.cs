using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles.Internals;
using Lotus.Utilities;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Managers.History.Events;
using Lotus.Options.General;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Options;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;
using VentLib.Utilities.Optionals;
using static MeetingHud;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
public class CheckForEndVotingPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(CheckForEndVotingPatch));

    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        MeetingDelegate meetingDelegate = MeetingDelegate.Instance;
        log.Debug($"{meetingDelegate.IsForceEnd()} - {meetingDelegate.GetPlayersRemainingToVote()}");
        if (!meetingDelegate.IsForceEnd() && meetingDelegate.GetPlayersRemainingToVote() > 0) return false;
        if (meetingDelegate.AlreadyCompleted)
        {
            log.Exception($"{Mirror.GetCaller()?.FullDescription() ?? "No description."}");
            log.Exception("The above function tried to call 'MeetingHud.CheckForEndVoting' when we have already completed voting.");
            return false;
        }
        meetingDelegate.AlreadyCompleted = true;
        log.Debug("Beginning End Voting", "CheckEndVotingPatch");


        if (GeneralOptions.MeetingOptions.NoVoteMode is not SkipVoteMode.None)
            Players.GetPlayers(PlayerFilter.Alive)
                .Where(p => new Dictionary<byte, List<Optional<byte>>>(meetingDelegate.CurrentVotes()).GetOptional(p.PlayerId).Compare(r => !r.IsEmpty()) == false).ForEach(p =>
                {
                    DevLogger.Log($"Non-Voters: {p.name}");
                    switch (GeneralOptions.MeetingOptions.NoVoteMode)
                    {
                        case SkipVoteMode.Random:
                            meetingDelegate.CastVote(p, new Optional<PlayerControl>(Players.GetPlayers(PlayerFilter.Alive).ToList().GetRandom()));
                            break;
                        case SkipVoteMode.Reverse:
                            meetingDelegate.CastVote(p, new Optional<PlayerControl>(p));
                            break;
                        case SkipVoteMode.Explode:
                            ProtectedRpc.CheckMurder(p, p);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });


        // Calculate the exiled player once so that we can send the voting complete signal
        log.Trace($"End Vote Count: {meetingDelegate.CurrentVoteCount().Select(kv => $"{Utils.GetPlayerById(kv.Key).GetNameWithRole()}: {kv.Value}").Join()}");
        meetingDelegate.CalculateExiledPlayer();

        byte exiledPlayer = meetingDelegate.ExiledPlayer?.PlayerId ?? 255;

        RoleOperations.Current.Trigger(LotusActionType.VotingComplete, null, meetingDelegate);

        // WE DO NOT RECALCULATE THE EXILED PLAYER!
        // This means its up to roles that modify the meeting delegate to properly update the exiled player

        if (GeneralOptions.MeetingOptions.ResolveTieMode is ResolveTieMode.Random)
            if (meetingDelegate.TiedPlayers.Count >= 2)
                meetingDelegate.ExiledPlayer = Players.PlayerById(meetingDelegate.TiedPlayers.ToList().GetRandom()).Map(p => p.Data).OrElse(null!);

        // Generate voter states to reflect voting
        List<VoterState> votingStates = GenerateVoterStates(meetingDelegate);

        List<byte> playerVotes = meetingDelegate.CurrentVotes()
            // Kinda weird logic here, we take the existing List<Optional<>> and filter it to only existing votes
            // Then we filter all votes to only the votes of the exiled player
            // Finally we transform the exiled player votes into the player's playerID
            .SelectMany(kv => kv.Value.Filter().Where(i => i == exiledPlayer).Select(_ => kv.Key)).ToList();



        if (meetingDelegate.ExiledPlayer != null) Hooks.MeetingHooks.ExiledHook.Propagate(new ExiledHookEvent(meetingDelegate.ExiledPlayer, playerVotes));
        __instance.RpcVotingComplete(votingStates.ToArray(), meetingDelegate.ExiledPlayer, meetingDelegate.IsTie);
        DevLogger.GameInfo();
        return false;
    }

    private static List<VoterState> GenerateVoterStates(MeetingDelegate meetingDelegate)
    {
        List<VoterState> votingStates = new();
        meetingDelegate.CurrentVotes().ForEach(kv =>
        {
            byte playerId = kv.Key;
            Optional<PlayerControl> player = Utils.PlayerById(playerId);
            kv.Value.ForEach(voted =>
            {
                string votedName = voted.FlatMap(b => Utils.PlayerById(b)).Map(p => p.GetNameWithRole()).OrElse("No One");
                player.IfPresent(p => log.Log(LogLevel.All, $"{p.GetNameWithRole()} voted for {votedName}"));
                votingStates.Add(new VoterState
                {
                    VoterId = playerId,
                    VotedForId = voted.OrElse(253) // Skip vote byte
                });
            });
        });
        return votingStates;
    }

    [QuickPrefix(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    public static void VotingCompletePatch(MeetingHud __instance, [HarmonyArgument(1)] NetworkedPlayerInfo? playerInfo)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        MeetingDelegate meetingDelegate = MeetingDelegate.Instance;
        meetingDelegate.ExiledPlayer = playerInfo;

        RoleOperations.Current.Trigger(LotusActionType.MeetingEnd, null,
            Optional<NetworkedPlayerInfo>.Of(playerInfo), meetingDelegate.IsTie,
            new Dictionary<byte, int>(meetingDelegate.CurrentVoteCount()),
            new Dictionary<byte, List<Optional<byte>>>(meetingDelegate.CurrentVotes()),
            meetingDelegate.IsForceEnd());
        DevLogger.GameInfo();
    }
}