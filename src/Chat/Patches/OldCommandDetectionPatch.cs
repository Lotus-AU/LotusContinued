using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Lotus.Chat.Commands;
using VentLib;
using VentLib.Commands;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Chat.Patches;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public class OldCommandDetectionPatch: CommandTranslations
{
    private static Dictionary<string, List<Command>> Commands =
        (typeof(CommandRunner).GetField("Commands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(Vents.CommandRunner) as Dictionary<string, List<Command>>)!;
    private static Dictionary<string, List<Command>> CaseSensitiveCommands =
        (typeof(CommandRunner).GetField("CaseSensitiveCommands",  BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(Vents.CommandRunner) as Dictionary<string, List<Command>>)!;

    internal static DateTime SunsetDate = DateTime.Parse("03/14/2026", CultureInfo.GetCultureInfo("en-US"));

    internal static void Postfix(ChatController __instance, PlayerControl sourcePlayer, string chatText, bool censor)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (DateTime.Now > SunsetDate) return; // past due date.
        if (!chatText.StartsWith("/") || chatText.StartsWith("/cmd")) return;

        chatText = chatText.Trim();

        StaticLogger.Debug($"{Commands} - {CaseSensitiveCommands}");

        string command = chatText[1..];
        string alias = command.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        bool isLegitCommand = Commands.Any(c => String.Equals(c.Key, alias, StringComparison.CurrentCultureIgnoreCase))
                              || CaseSensitiveCommands.ContainsKey(alias);

        if (!isLegitCommand) return;

        Async.Schedule(() => AnnounceDeprecatedCommand(sourcePlayer, chatText), 2f);
        if (sourcePlayer.IsHost()) sourcePlayer.RpcSendChat("/cmd" + command);
        else HudManager.Instance.Chat.AddChat(sourcePlayer, "/cmd" + command, censor);
    }

    public static void AnnounceDeprecatedCommand(PlayerControl sourcePlayer, string chatText)
    {
        string command;
        if (chatText.StartsWith("/")) command = chatText.Substring(1);
        else command = chatText;
        ChatHandlers.InvalidCmdUsage(OldCommandWarning.Formatted(command)).Send(sourcePlayer);
    }
}