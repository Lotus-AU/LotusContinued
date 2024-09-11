using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat.Patches;
using Lotus.Factions.Neutrals;
using Lotus.Logging;
using Lotus.Managers;
using Lotus.Managers.Blackscreen;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Managers.Interfaces;
using Lotus.Roles.Subroles;
using UnityEngine;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.Chat.Commands;

[Localized("Commands")]
public class BasicCommands : CommandTranslations
{
    [Localized("Color.NotInRange")] public static string ColorNotInRangeMessage = "{0} is not in range of valid colors.";
    [Localized(nameof(Winners))] public static string Winners = "Winners";
    [Localized("Dump.Success")] public static string DumpSuccess = "Successfully dumped log. Check your logs folder for a \"dump.log!\"";
    [Localized("Ids.PlayerIdMessage")] public static string PlayerIdMessage = "{0}'s player ID is {1}";

    [Command("perc", "percentage", "percentages", "p")]
    public static void Percentage(PlayerControl source)
    {
        string? factionName = null;
        string text = $"{HostOptionTranslations.CurrentRoles}:\n";

        OrderedDictionary<string, List<CustomRole>> defsByFactions = new();

        string FactionName(CustomRole role)
        {
            if (role is Subrole) return "Modifiers";
            if (role.Faction is not Neutral) return role.Faction.Name();

            SpecialType specialType = role.Metadata.GetOrDefault(LotusKeys.AuxiliaryRoleType, SpecialType.None);

            return specialType is SpecialType.NeutralKilling ? "Neutral Killers" : "Neutral";
        }

        IRoleManager.Current.AllCustomRoles().ForEach(r => defsByFactions.GetOrCompute(FactionName(r), () => new List<CustomRole>()).Add(r));

        defsByFactions.GetValues().SelectMany(s => s).ForEach(r =>
        {

            if (r.Count == 0 || r.Chance == 0) return;

            string fName = FactionName(r);
            if (factionName != fName)
            {
                if (factionName == "Modifiers") text += $"\n★ {factionName}\n";
                else text += $"\n{HostOptionTranslations.RoleCategory.Formatted(fName)}\n";
                factionName = fName;
            }


            text += $"{r.RoleName}: {r.Count} × {r.Chance}%";
            if (r.Count > 1) text += $" (+ {r.AdditionalChance}%)\n";
            else text += "\n";
        });

        ChatHandler.Of(text, HostOptionTranslations.RoleInfo).LeftAlign().Send(source);
    }

    [Command(CommandFlag.LobbyOnly, "name")]
    public static void Name(PlayerControl source, CommandContext context)
    {
        string name = string.Join(" ", context.Args).Trim();
        if (name.IsNullOrWhiteSpace()) return;
        int allowedUsers = GeneralOptions.MiscellaneousOptions.ChangeNameUsers;
        bool permitted = allowedUsers switch
        {
            0 => source.IsHost(),
            1 => source.IsHost() || PluginDataManager.FriendManager.IsFriend(source),
            2 => true,
            _ => throw new ArgumentOutOfRangeException($"{allowedUsers} is not a valid integer for MiscellaneousOptions.ChangeNameUsers.")
        };

        if (!permitted)
        {
            ChatHandlers.NotPermitted().Send(source);
            return;
        }

        if (name.Length > 25)
        {
            ChatHandler.Of($"Name too long ({name.Length} > 25).", CommandError).LeftAlign().Send(source);
            return;
        }

        OnChatPatch.EatMessage = true;

        source.RpcSetName(name);
    }

    [Command(CommandFlag.LobbyOnly, "color", "colour")]
    public static void SetColor(PlayerControl source, CommandContext context)
    {
        int allowedUsers = GeneralOptions.MiscellaneousOptions.ChangeColorAndLevelUsers;
        bool permitted = allowedUsers switch
        {
            0 => source.IsHost(),
            1 => source.IsHost() || PluginDataManager.FriendManager.IsFriend(source),
            2 => true,
            _ => throw new ArgumentOutOfRangeException($"{allowedUsers} is not a valid integer for MiscellaneousOptions.ChangeColorAndLevelUsers.")
        };

        if (!permitted)
        {
            ChatHandlers.NotPermitted().Send(source);
            return;
        }

        int color = -1;
        bool hasArg0 = context.Args.Length > 0;

        if (hasArg0)
        {
            if (int.TryParse(context.Args[0], System.Globalization.NumberStyles.Integer, null, out int result))
            {
                color = int.Parse(context.Args[0]);
            }
            else
            {
                string colorName = context.Args[0];
                color = ModConstants.ColorNames.ToList().FindIndex(name => name.ToLower().Contains(colorName.ToLower()));
                if (color == -1)
                {
                    ChatHandler.Of($"{ColorNotInRangeMessage.Formatted(colorName)}", ModConstants.Palette.InvalidUsage.Colorize(InvalidUsage)).LeftAlign().Send(source);
                    return;
                }
            }
        } // assign a random color
        else color = UnityEngine.Random.RandomRangeInt(0, Palette.PlayerColors.Length - 1);

        if (color > Palette.PlayerColors.Length - 1)
        {
            ChatHandler.Of($"{ColorNotInRangeMessage.Formatted(color)} (0-{Palette.PlayerColors.Length - 1})", ModConstants.Palette.InvalidUsage.Colorize(InvalidUsage)).LeftAlign().Send(source);
            return;
        }

        source.RpcSetColor((byte)color);
    }

    [Command(CommandFlag.HostOnly, "dump")]
    public static void Dump(PlayerControl _, CommandContext context)
    {
        RpcSendChatPatch.EatCommand = true;
        LogManager.WriteSessionLog(context.Join());
    }

    [Command(CommandFlag.LobbyOnly, "winner", "w")]
    public static void ListWinners(PlayerControl source)
    {
        if (Game.MatchData.GameHistory.LastWinners == null!) new ChatHandler()
            .Title(t => t.Text(CommandError).Color(ModConstants.Palette.KillingColor).Build())
            .LeftAlign()
            .Message(NoPreviousGameText)
            .Send(source);
        else
        {
            string winnerText = Game.MatchData.GameHistory.LastWinners.Select(w => $"• {w.Name} ({w.MainRole.RoleName})").Fuse("\n");
            ChatHandler.Of(winnerText, ModConstants.Palette.WinnerColor.Colorize(Winners)).LeftAlign().Send(source);
        }

    }

    private static readonly ColorGradient HostGradient = new(new Color(1f, 0.93f, 0.98f), new Color(1f, 0.57f, 0.73f));

    [Command(CommandFlag.HostOnly, "say", "s")]
    public static void Say(PlayerControl _, string message)
    {
        ChatHandler.Of(message).Title(HostGradient.Apply(HostMessage)).Send();
    }

    [Command("id", "ids", "pid", "pids")] // eur mom is 😣
    public static void PlayerIds(PlayerControl source, CommandContext context)
    {
        if (context.Args.Length == 0)
        {
            string playerIds = "★ Player IDs ★\n-=-=-=-=-=-=-=-=-\n";
            playerIds += PlayerControl.AllPlayerControls.ToArray().Select(p => $"{p.PlayerId} - {p.name} ({ModConstants.ColorNames[p.cosmetics.ColorId]})").Fuse("\n");
            ChatHandler.Of(playerIds).LeftAlign().Send(source);
            return;
        }

        string name = context.Join();
        PlayerControl? player = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.name == name);
        ChatHandler.Of(player == null ? PlayerNotFoundText.Formatted(name) : PlayerIdMessage.Formatted(name, player.PlayerId)).LeftAlign().Send(source);
    }

    [Command(CommandFlag.HostOnly, "tload")]
    public static void ReloadTitles(PlayerControl source)
    {
        OnChatPatch.EatMessage = true;
        PluginDataManager.TitleManager.Reload();
        ChatHandler.Of("Successfully reloaded titles.").Send(source);
    }

    [Command(CommandFlag.HostOnly | CommandFlag.InGameOnly, "fix")]
    public static void FixPlayer(PlayerControl source, CommandContext context, byte id)
    {
        if (context.Args.Length == 0)
        {
            PlayerIds(source, context);
            return;
        }

        PlayerControl? player = Players.FindPlayerById(id);
        if (player == null) ChatHandler.Of(PlayerNotFoundText.Formatted(id), CommandError).LeftAlign().Send(source);
        else if (!BlackscreenResolver.PerformForcedReset(player)) ChatHandler.Of("Unable to perform forced Blackscreen Fix. No players have died yet.", CommandError).LeftAlign().Send(source);
        else ChatHandler.Of($"Successfully cleared blackscreen of \"{player.name}\"").LeftAlign().Send(source);
    }
}