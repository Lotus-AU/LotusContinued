using System;
using System.Linq;
using Lotus.API.Player;
using Lotus.Managers;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;
using VentLib.Localization.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Chat.Commands;

[Localized("Commands.Mods")]
[Command("mod", "unmod", "admin", "start")]
public class ModCommands : ICommandReceiver
{
    [Localized(nameof(SomethingWentWrong))] public static string SomethingWentWrong = "Something went wrong executing that command. Please check your parameters.";
    [Localized(nameof(SuccessfulMod))] public static string SuccessfulMod = "{0} is now a mod! They have access to the /kick and /ban commands now!";
    [Localized(nameof(RemovedMod))] public static string RemovedMod = "{0} was removed. They no longer have access to mod-related commands.";
    [Localized(nameof(AlreadyMod))] public static string AlreadyMod = "{0} is already a mod. Nothing happenend.";

    public void Receive(PlayerControl source, CommandContext context)
    {
        switch (context.Alias)
        {
            case "start":
                StartGame(source, context);
                break;
            case "mod":
                ModPlayer(source, context);
                break;
            case "unmod":
                UnmodPlayer(source, context);
                break;
            case "admin":
                AdminPlayer(source, context);
                break;
            default:
                ChatHandlers.InvalidCmdUsage(SomethingWentWrong).Send(source);
                break;
        }
    }
    private static int CheckPlayer(PlayerControl source, CommandContext context, out int targetPlayerId, bool checkArgs = true, CheckType checkType = CheckType.ModsAndAbove)
    {
        bool permitted = checkType switch
        {
            CheckType.AdminsAndAbove => PluginDataManager.ModManager.GetStatusOfPlayer(source).ModType == "Admin" || source.IsHost(),
            CheckType.ModsAndAbove => PluginDataManager.ModManager.IsPlayerModded(source) || source.IsHost(),
            CheckType.HostOnly => source.IsHost(),
            _ => throw new ArgumentOutOfRangeException($"{checkType} is not a valid parameter for Enum CheckType.")
        };
        if (!permitted)
        {
            ChatHandlers.NotPermitted().Send(source);
            targetPlayerId = -1;
            goto finished;
        }
        if (!checkArgs)
        {
            targetPlayerId = 1;
            goto finished;
        }
        if (context.Args.Length == 0)
        {
            BasicCommands.PlayerIds(source, context);
            ChatHandlers.InvalidCmdUsage(SomethingWentWrong).Send(source);
            targetPlayerId = -1;
            goto finished;
        }
        string playerId = context.Args[0];

        if (!int.TryParse(playerId, out int numberPlayerId))
        {
            targetPlayerId = -1;
            goto finished;
        }

        targetPlayerId = numberPlayerId;
        goto finished;

    finished:
        return targetPlayerId;
    }
    public static void ModPlayer(PlayerControl source, CommandContext context)
    {
        if (CheckPlayer(source, context, out int targetPlayerId, checkType: CheckType.AdminsAndAbove) == -1) return;
        PlayerControl? target = Players.GetAllPlayers().FirstOrDefault(p => p.PlayerId == targetPlayerId);
        if (target == null)
        {
            ChatHandlers.InvalidCmdUsage(SomethingWentWrong).Send(source);
            return;
        }
        if (PluginDataManager.ModManager.IsPlayerModded(target))
        {
            ChatHandlers.InvalidCmdUsage(AlreadyMod.Formatted(target.name)).Send(source);
            return;
        }
        PluginDataManager.ModManager.ModPlayer(target);
        ChatHandler.Of(SuccessfulMod.Formatted(target.name)).Send(source);
        ChatHandler.Of(SuccessfulMod.Formatted(target.name)).Send(target);
    }

    public static void UnmodPlayer(PlayerControl source, CommandContext context)
    {
        if (CheckPlayer(source, context, out int targetPlayerId, checkType: CheckType.AdminsAndAbove) == -1) return;
        PlayerControl? target = Players.GetAllPlayers().FirstOrDefault(p => p.PlayerId == targetPlayerId);
        if (target == null)
        {
            ChatHandlers.InvalidCmdUsage(SomethingWentWrong).Send(source);
            return;
        }
        if (PluginDataManager.ModManager.IsPlayerModded(target) && PluginDataManager.ModManager.GetStatusOfPlayer(source).ModType == "Admin" && !source.IsHost()) // host can unmod admins
        {
            // cant unmod another admin
            ChatHandlers.InvalidCmdUsage(SomethingWentWrong).Send(source);
            return;
        }
        PluginDataManager.ModManager.UnmodPlayer(target);
        ChatHandler.Of(RemovedMod.Formatted(target.name)).Send(source);
        if (source == target) ChatHandler.Of(RemovedMod.Formatted(target.name)).Send(target);
    }

    public static void AdminPlayer(PlayerControl source, CommandContext context)
    {
        if (CheckPlayer(source, context, out int targetPlayerId, checkType: CheckType.HostOnly) == -1) return;
        PlayerControl? target = Players.GetAllPlayers().FirstOrDefault(p => p.PlayerId == targetPlayerId);
        if (target == null)
        {
            ChatHandlers.InvalidCmdUsage(SomethingWentWrong).Send(source);
            return;
        }
        if (PluginDataManager.ModManager.IsPlayerModded(target))
        {
            ChatHandlers.InvalidCmdUsage(AlreadyMod.Formatted(target.name)).Send(source);
            return;
        }
        PluginDataManager.ModManager.AdminPlayer(target);
        ChatHandler.Of(SuccessfulMod.Formatted(target.name)).Send(source);
    }

    public static void StartGame(PlayerControl source, CommandContext context)
    {
        if (CheckPlayer(source, context, out int targetPlayerId, checkArgs: false, checkType: CheckType.AdminsAndAbove) == -1) return;
        if (GameStartManager.Instance == null) return;
        GameStartManager.Instance.BeginGame();
        GameStartManager.Instance.countDownTimer = 0.0001f;
    }

    private enum CheckType
    {
        ModsAndAbove,
        AdminsAndAbove,
        HostOnly,
    }
}