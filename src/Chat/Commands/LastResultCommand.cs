using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.Extensions;
using Lotus.Managers.History;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Victory;
using Lotus.Victory.Conditions;
using Lotus.Options;
using UnityEngine;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using static Lotus.ModConstants.Palette;

namespace Lotus.Chat.Commands;

[LoadStatic]
public class LastResultCommand : CommandTranslations
{
    static LastResultCommand()
    {
        Hooks.NetworkHooks.ReceiveVersionHook.Bind(nameof(LastResultCommand), versionEvent =>
        {
            if (!GeneralOptions.MiscellaneousOptions.AutoDisplayLastResults) return;
            if (versionEvent.Player == null) return;
            if (PlayerHistories == null) return;
            if (AmongUsClient.Instance.NetworkMode is not NetworkModes.LocalGame && PlayerHistories.All(p => p.UniquePlayerId.ToFriendcode() != versionEvent.Player.FriendCode)) return;
            Async.Schedule(() => GeneralResults(versionEvent.Player), 0.5f);
        });
        Hooks.NetworkHooks.GameJoinHook.Bind(nameof(LastResultCommand), gameJoinEvent =>
        {
            if (!GeneralOptions.MiscellaneousOptions.AutoDisplayLastResults) return;
            if (gameJoinEvent.IsNewLobby) return;
            Async.WaitUntil(() => PlayerControl.LocalPlayer, p => p != null, GeneralResults, 0.1f, 30);
        });
    }

    [Command(CommandFlag.LobbyOnly, "last", "l", "lastresult")]
    public static void LastGame(PlayerControl source, CommandContext context)
    {
        if (PlayerHistories == null) ErrorHandler(source).Message(NoPreviousGameText).Send();
        else if (context.Args.Length == 0) GeneralResults(source);
        else PlayerResults(source, context.Join());
    }

    public static void PlayerResults(PlayerControl source, string name)
    {
        HashSet<byte> winners = Game.MatchData.GameHistory.LastWinners.Select(p => p.MyPlayer.PlayerId).ToHashSet();
        PlayerHistory? foundPlayer = PlayerHistories!.FirstOrDefault(p => p.Name == name);
        if (foundPlayer == null)
        {
            ErrorHandler(source).Message(PlayerNotFoundText.Formatted(name)).Send();
            return;
        }

        string coloredName = foundPlayer.ColorName;

        string statusText = foundPlayer.Status is PlayerStatus.Dead ? foundPlayer.CauseOfDeath?.SimpleName() ?? foundPlayer.Status.ToString() : foundPlayer.Status.ToString();
        string playerStatus = StatusColor(foundPlayer.Status).Colorize(statusText);

        string winnerText = winners.Contains(foundPlayer.PlayerId) ? WinnerColor.Colorize(" ★ " + LRTranslations.WinnerText + " ★") : "";
        string titleText = $"{name} {coloredName}{winnerText}\n";
        titleText += $"{LRTranslations.RoleText.Formatted(foundPlayer.MainRole.RoleColor.Colorize(foundPlayer.MainRole.RoleName))} ({playerStatus})\n";
        if (foundPlayer.Subroles.Count > 0) titleText += LRTranslations.ModifierText.Formatted(foundPlayer.Subroles.Select(sr => sr.RoleColor.Colorize(sr.RoleName)).Fuse());

        string text = "";
        text += "\n\n";

        text += foundPlayer.MainRole.Statistics().Select(s => $"{s.Name()}: {s.GetGenericValue(foundPlayer.UniquePlayerId)}").Fuse("\n") + "\n.";

        ChatHandler.Of(text, titleText).LeftAlign().Send(source);
    }

    public static void GeneralResults(PlayerControl source)
    {
        if (PlayerHistories == null) return;

        HashSet<byte> winners = Game.MatchData.GameHistory.LastWinners.Select(p => p.MyPlayer.PlayerId).ToHashSet();

        string text = PlayerHistories
            .Where(ph => ph.MainRole is not GameMaster)
            .OrderBy(StatusOrder)
            .Select(p => CreateSmallPlayerResult(p, winners.Contains(p.PlayerId)))
            .Fuse("<line-height=4>\n</line-height>");

        WinDelegate winDelegate = Game.GetWinDelegate();
        string winResult = new Optional<IWinCondition>(winDelegate.WinCondition()).Map(wc =>
        {
            string t;
            if (wc is IFactionWinCondition factionWin)
            {
                t = factionWin.Factions().Select(f => f.Color.Colorize(f.Name())).Distinct().Fuse();
                List<FrozenPlayer> additionalWinners = Game.MatchData.GameHistory.AdditionalWinners;
                if (additionalWinners.Count > 0)
                {
                    string awText = additionalWinners.Select(fp => new List<CustomRole> { fp.MainRole }.Concat(fp.Subroles).MaxBy(r => r.DisplayOrder)).Fuse();
                    t += $" + {awText}";
                }
            }
            else t = Game.MatchData.GameHistory.LastWinners.Select(lw => lw.MainRole.ColoredRoleName()).Fuse();

            string? wcText = wc.GetWinReason().ReasonText;
            string reasonText = wcText == null ? "" : $"\n<size=1.45>{LRTranslations.WinReasonText.Formatted(wcText)}</size>";
            return $"\n\n<size=1.6>{LRTranslations.WinResultText.Formatted(t)}{reasonText}</size>";
        }).OrElse("");


        string content = $"<size=1.7>{text + winResult}</size>";

        ChatHandler.Of("\n".Repeat(PlayerHistories.Count - 1), content).LeftAlign().Send(source);
        return;

        float DeathTimeOrder(PlayerHistory ph) => ph.CauseOfDeath == null ? 0f : (7200f - (float)ph.CauseOfDeath.Timestamp().TimeSpan().TotalSeconds) / 7200f;
        float StatusOrder(PlayerHistory ph) => winners.Contains(ph.PlayerId) ? (float)ph.Status + DeathTimeOrder(ph) : (float)ph.Status + 99 + DeathTimeOrder(ph);
    }
    private static string CreateSmallPlayerResult(PlayerHistory history, bool isWinner)
    {
        const string indent = "<indent=5.5%>{0}</indent>";
        string winnerPrefix = isWinner ? WinnerColor.Colorize("★ ") + "{0}" : indent;

        string statusText = history.Status is PlayerStatus.Dead ? history.CauseOfDeath?.SimpleName() ?? history.Status.ToString() : history.Status.ToString();
        string playerStatus = StatusColor(history.Status).Colorize(statusText);

        string statText = history.MainRole.Statistics().FirstOrOptional().Map(t => $" <size=1.5>[{t.Name()}: {t.GetGenericValue(history.UniquePlayerId)}]</size>").OrElse("");

        int colorId = history.Outfit.ColorId;
        string coloredName = ((Color)Palette.PlayerColors[colorId]).Colorize(ModConstants.ColorNames[colorId]);
        string modifiers = history.Subroles.Count == 0 ? "" : $"\n<size=1.3>{history.Subroles.Select(sr => sr.ColoredRoleName()).Fuse()}</size>";

        string totalText = winnerPrefix.Formatted($"{history.Name} : {coloredName} <size=1.5>({playerStatus})</size>\n{indent.Formatted($"<size=1.9>{history.MainRole.ColoredRoleName()}</size> {statText}{modifiers}")}");
        return $"<line-height=2.2>{totalText}</line-height>";
    }

    private static ChatHandler ErrorHandler(PlayerControl source) => new ChatHandler().Title(t => t.Text(CommandError).Color(KillingColor).Build()).Player(source).LeftAlign();

    private static Color StatusColor(PlayerStatus status)
    {
        return status switch
        {
            PlayerStatus.Alive => Color.green,
            PlayerStatus.Exiled => new Color(0.73f, 0.54f, 0.45f),
            PlayerStatus.Disconnected => Color.grey,
            PlayerStatus.Dead => Color.red,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    private static List<PlayerHistory>? PlayerHistories => Game.MatchData.GameHistory.PlayerHistory;

    [Localized("LastResults")]
    public static class LRTranslations
    {
        [Localized(nameof(RoleText))]
        public static string RoleText = "<b>Role:</b> {0}";

        [Localized(nameof(ModifierText))]
        public static string ModifierText = "<b>Mods:</b> {0}";

        [Localized(nameof(WinnerText))]
        public static string WinnerText = "Winner";

        [Localized(nameof(ResultsText))]
        public static string ResultsText = "Results";

        [Localized(nameof(WinResultText))]
        public static string WinResultText = "Winners: {0}";

        [Localized(nameof(WinReasonText))]
        public static string WinReasonText = "Reason: {0}";
    }
}