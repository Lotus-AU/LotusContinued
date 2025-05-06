using System.Collections.Generic;
using System.Linq;
using System.Text;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Chat.Commands;

[Localized("Commands.Poll")]
[Command(CommandFlag.LobbyOnly, "gmpoll", "gamemodepoll")]
public class GamemodePollCommand : ICommandReceiver
{
    private bool isPollActive;
    private int pollDuration = 60;

    private Dictionary<int, int> gamemodeVotes = [];
    private Dictionary<int, int> playerVotes = [];

    [Localized("PollStarted")] private static string _pollStarted = "A poll to change the current gamemode has started!\n\nPlease vote for your desired game mode using the command /gmpoll vote (id)\n\nThis poll will expire in {0}s";
    [Localized("PollEnded")] private static string _pollEnded = "The poll has ended! The winning gamemode is {0} with {1} votes.";
    [Localized("VoteMessage")] private static string _voteMessage = "You have casted your vote for {0}!";
    [Localized("NoVotes")] private static string _noVotes = "The poll is over, No votes were cast.";

    [Command("start")]
    private void PollStartCommand(PlayerControl source, CommandContext context)
    {
        if (isPollActive)
        {
            ChatHandlers.NotPermitted("There is already an active poll.").Send(source);
            return;
        }

        if (context.Args.Length > 0 && int.TryParse(context.Args[0], out int duration))
        {
            pollDuration = duration;
        }
        isPollActive = true;

        ChatHandler.Of(_pollStarted.Formatted(pollDuration)).Send();
        StartPoll();
    }

    [Command("vote")]
    private void PollVoteCommand(PlayerControl source, CommandContext context)
    {
        if (!isPollActive)
        {
            ChatHandlers.NotPermitted("No poll is currently in progress.").Send(source);
            return;
        }

        if (context.Args.Length == 0 || !int.TryParse(context.Args[0], out int gamemode))
        {
            ChatHandlers.InvalidCmdUsage("Please provide the ID of the Gamemode you wish to vote.").Send(source);
            return;
        }

        if (gamemode < 0 || gamemode >= ProjectLotus.GameModeManager.GetGameModes().Count())
        {
            ChatHandlers.InvalidCmdUsage("Invalid gamemode ID.").Send(source);
            return;
        }

        if (playerVotes.ContainsKey(source.PlayerId))
        {
            gamemodeVotes[playerVotes[source.PlayerId]]--;
        }

        playerVotes[source.PlayerId] = gamemode;
        gamemodeVotes[gamemode] = gamemodeVotes.ContainsKey(gamemode) ? gamemodeVotes[gamemode] + 1 : 1;
        ChatHandler.Of(_voteMessage.Formatted(ProjectLotus.GameModeManager.GetGameMode(gamemode).Name)).Send(source);
    }

    private void StartPoll()
    {
        ChatHandler.Of(GetGamemodeOptions(), "Available Gamemodes").Send();
        Async.Schedule(() => EndPoll(), pollDuration);
    }

    private void EndPoll()
    {
        if (gamemodeVotes.Count == 0)
        {
            ChatHandler.Of(_noVotes).Send();
            ResetPoll();
            return;
        }

        int winningGamemodeId = gamemodeVotes
            .OrderByDescending(g => g.Value)
            .First().Key;

        int highestVotes = gamemodeVotes[winningGamemodeId];

        ProjectLotus.GameModeManager.SetGameMode(winningGamemodeId);
        ChatHandler.Of(_pollEnded.Formatted(ProjectLotus.GameModeManager.GetGameMode(winningGamemodeId).Name, highestVotes)).Send();

        ResetPoll();
    }

    private string GetGamemodeOptions()
    {
        var _gamemodeManager = ProjectLotus.GameModeManager;
        StringBuilder gamemodeOptions = new();
        for (int i = 0; i < _gamemodeManager.GetGameModes().Count(); i++)
        {
            var gamemode = _gamemodeManager.GetGameMode(i);
            gamemodeOptions.AppendLine($"{i}: {gamemode.Name}");
        }

        return gamemodeOptions.ToString();
    }

    private void ResetPoll()
    {
        gamemodeVotes.Clear();
        playerVotes.Clear();
        pollDuration = 60;
        isPollActive = false;
    }

    public void Receive(PlayerControl source, CommandContext context)
    {
        if (isPollActive)
        {
            ChatHandler.Send(source, "There is currently an active poll, you can vote using /gmpoll vote");
            ChatHandler.Send(source, GetGamemodeOptions(), "Available Gamemodes");
            return;
        }
    }
}