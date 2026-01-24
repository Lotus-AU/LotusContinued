using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmongUs.Data;
using InnerNet;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Chat.Commands;
using Lotus.Extensions;
using Lotus.Factions.Interfaces;
using Lotus.GUI.Name;
using Lotus.Managers.Templates.Models.Backing;
using Lotus.Managers.Templates.Models.Units.Actions;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using VentLib.Options;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Random = UnityEngine.Random;

namespace Lotus.Managers.Templates.Models.Units;

// ReSharper disable once CollectionNeverUpdated.Global
public class TemplateUnit
{
    public static byte Triggerer = byte.MaxValue;
    protected static string? MetaVariable;
    public static string[] Arguments = Array.Empty<string>();

    private static readonly Regex Regex = new(@"\${(?>[^{}]+|(?<Open>{)|(?<Close-Open>}))*}");
    public string? Text { get; set; }
    public string? Color { get; set; }
    public List<TCondition> Conditions { get; set; } = new();
    public List<TCondition> ConditionsAny { get; set; } = new();

    protected bool Evaluate(object? data) => Conditions.All(c => c.Evaluate(data)) && (ConditionsAny.IsEmpty() || ConditionsAny.Any(c => c.Evaluate(data)));

    public string Format(object? obj = null) => Text == null ? "" : Format(Text, obj);

    public string Format(string text, object? obj = null) => Evaluate(obj) ? Colorize(FormatStatic(text, obj), Color) : (Conditions.FirstOrDefault(c => c.Fallback != null)?.Fallback ?? "");

    public static string FormatStatic(string text, object? obj = null)
    {
        return Regex.Replace(text, match =>
        {
            string value = match.Value[2..^1];
            var split = value.Split(".");
            if (split.Length > 1)
                return VariableValues.TryGetValue(split[0], out var dynSupplier)
                    ? dynSupplier(obj!, split[1..].Fuse("."))
                    : match.Value;
            return TemplateValues.TryGetValue(value, out var funcSupplier)
                ? funcSupplier(obj!)
                : PluginDataManager.TemplateManager.FormatVariable(value, obj) ?? match.Value;
        });
    }

    public static string Colorize(string text, string? color)
    {
        if (string.IsNullOrEmpty(color)) return text;
        color = color.ToUpper();

        if (!color.Contains(','))
        {
            UnityEngine.Color textColor = color.ToColor() ?? UnityEngine.Color.white;
            return textColor.Colorize(text);
        }

        return TextUtils.ApplyGradient(text, color
            .Split(',')
            .Select(c => c.ToUpper())
            .Select(c => c.StartsWith('#') ? c : "#" + c)
            .Select(c => c.ToColor() ?? UnityEngine.Color.white).ToArray());
    }

    public static readonly Dictionary<string, Func<object, String>> TemplateValues = new()
    {
        { "RoomCode", _ => GameCode.IntToGameName(AmongUsClient.Instance.GameId) },
        { "Host", _ => DataManager.Player.Customization.name },
        { "AUVersion", _ => UnityEngine.Application.version },
        { "ModName" , _ => ProjectLotus.ModName },
        { "ModVersion", _ => ProjectLotus.DevVersion ? ProjectLotus.DevVersionStr : ProjectLotus.PluginVersion },
        { "Map", _ => Constants.MapNames[GameOptionsManager.Instance.CurrentGameOptions.MapId] },
        { "GameMode", _ => Game.CurrentGameMode.Name },
        { "Date", _ => DateTime.Now.ToShortDateString() },
        { "Time", _ => DateTime.Now.ToShortTimeString() },
        { "Players", _ => PlayerControl.AllPlayerControls.ToArray().Select(p => p.name).Fuse() },
        { "PlayerCount", _ => PlayerControl.AllPlayerControls.Count.ToString() },
        { "AllRoles", _ => TUAllRoles.GetAllRoles(RoleSearchType.OnlyRoles) },
        { "AllModifiers", _ => TUAllRoles.GetAllRoles(RoleSearchType.OnlyModifiers).Replace("Modifiers\n", "") },

        { "AlivePlayers", _ => Players.GetAlivePlayers().Select(p => p.name).Fuse() },
        { "AlivePlayerCount", _ => Players.GetAlivePlayers().Count().ToString() },
        { "DeadPlayers", _ => Players.GetPlayers(PlayerFilter.Dead).Select(p => p.name).Fuse() },
        { "DeadPlayerCount", _ => Players.GetPlayers(PlayerFilter.Dead).Count().ToString() },

        { "Impostors", _ => Players.GetPlayers(PlayerFilter.Impostor).Select(p => p.name).Fuse() },
        { "ImpostorsCount", _ => Players.GetPlayers(PlayerFilter.Impostor).Count().ToString() },
        { "AliveImpostorsCount", _ => Players.GetPlayers(PlayerFilter.Alive | PlayerFilter.Impostor).Count().ToString() },

        { "Crewmates", _ => Players.GetPlayers(PlayerFilter.Crewmate).Select(p => p.name).Fuse().ToString() },
        { "CrewmatesCount", _ => Players.GetPlayers(PlayerFilter.Crewmate).Count().ToString() },
        { "AliveCrewmatesCount", _ => Players.GetPlayers(PlayerFilter.Alive | PlayerFilter.Crewmate).Count().ToString() },

        { "Neutrals", _ => Players.GetPlayers(PlayerFilter.Neutral).Select(p => p.name).Fuse().ToString() },
        { "NeutralsCount", _ => Players.GetPlayers(PlayerFilter.Neutral).Count().ToString() },
        { "AliveNeutralsCount", _ => Players.GetPlayers(PlayerFilter.Alive | PlayerFilter.Neutral).Count().ToString() },

        { "NeutralKillers", _ => Players.GetPlayers(PlayerFilter.NeutralKilling).Select(p => p.name).Fuse().ToString() },
        { "NeutralKillersCount", _ => Players.GetPlayers(PlayerFilter.NeutralKilling).Count().ToString() },
        { "AliveNeutralKillersCount", _ => Players.GetPlayers(PlayerFilter.Alive | PlayerFilter.NeutralKilling).Count().ToString() },

        { "Name", player => ((PlayerControl) player).name },
        { "Level", player => ((PlayerControl) player).Data.PlayerLevel.ToString() },
        { "Color", player => ModConstants.ColorNames[((PlayerControl) player).cosmetics.bodyMatProperties.ColorId] },
        { "Role", player =>((PlayerControl)player).PrimaryRole().ColoredRoleName() },
        { "Blurb", player => ((PlayerControl) player).PrimaryRole().Blurb },
        { "Description", player => ((PlayerControl) player).PrimaryRole().Description },
        { "Status", player => Optional<FrozenPlayer>.Of(Game.MatchData.GetFrozenPlayer((PlayerControl)player)).Map(fp => StatusCommand.GetPlayerStatus((PlayerControl)player, fp)).OrElse("")},
        { "Death", player => Game.MatchData.GameHistory.GetCauseOfDeath(((PlayerControl)player).PlayerId).Map(c => c.SimpleName()).OrElse("Unknown") },
        { "Killer", player => Game.MatchData.GameHistory.GetCauseOfDeath(((PlayerControl)player).PlayerId).FlatMap(c => c.Instigator()).Map(p => p.Name).OrElse("Unknown") },
        { "Options", player => OptionUtils.OptionText(((PlayerControl) player).PrimaryRole().RoleOptions) },
        { "Faction", player =>
            {
                IFaction faction = ((PlayerControl)player).PrimaryRole().Faction;
                return faction.Color.Colorize(faction.Name());
            }
        },
        { "Modifiers", ShowModifiers },
        { "Mods", ShowModifiers },
        { "ModsDescriptive", ModifierText },
        { "ModsShort", ModifierBlurb },
        { "MyRole", player => MyRoleCommand.GenerateMyRoleText(((PlayerControl)player).PrimaryRole()) },
        { "TasksComplete", QW(p => (GetTaskHolderRole(p)?.CompleteTasks ?? 0).ToString() )},
        { "TotalTasks", QW(p => (GetTaskHolderRole(p)?.TotalTasks ?? 0).ToString() )},
        { "TasksRemaining", QW(p => (GetTaskHolderRole(p)?.TotalTasks - GetTaskHolderRole(p)?.CompleteTasks ?? 0).ToString())},

        { "Role_Name", role => ((CustomRole) role).RoleName },
        { "Role_Description", role => ((CustomRole) role).Description },
        { "Role_Blurb", role => ((CustomRole) role).Blurb },
        { "Role_Options", role => OptionUtils.OptionText(((CustomRole) role).RoleOptions) },
        { "Role_Faction", role => ((CustomRole) role).Faction.Name() },
        { "Role_Basis", role => ((CustomRole) role).RealRole.ToString() },
        { "Role_Color", role => ((CustomRole) role).RoleColor.ToHex() },

        {"ActionMeta", _ => TAction.MetaVariable},
        {"TriggerMeta", _ => MetaVariable ?? "" },
        {"Triggerer", _ => Players.FindPlayerById(Triggerer)?.name ?? "Unknown"},
        {"Arguments", _ => Arguments.Fuse(" ")}
    };

    private static ITaskHolderRole? GetTaskHolderRole(PlayerControl player) => player.PrimaryRole() is ITaskHolderRole tr ? tr : null;

    private static string ShowModifiers(object obj)
    {
        return ((PlayerControl)obj).GetSubroles().OrderBy(r => r.RoleName).Select(r => r.ColoredRoleName()).Fuse();
    }

    public static readonly Dictionary<string, TFormat> VariableValues = new()
    {
        { "Option", (_, qualifier) => OptionManager.GetManager().GetOption(qualifier)?.GetValue().ToString() ?? $"Unknown Option \"{qualifier}\"" },
        { "Optionf", (_, qualifier) => OptionManager.GetManager().GetOption(qualifier)?.GetValueText() ?? $"Unknown Option \"{qualifier}\"" },
        { "OptionName", (_, qualifier) =>
            {
                Option? option = OptionManager.GetManager().GetOption(qualifier);
                return option is GameOption go ? go.Name() : option?.Name() ?? $"Unknown Option \"{qualifier}\"";
            }
        },
        { "Template", (obj, template) => PluginDataManager.TemplateManager.TryFormat(obj, FormatStatic(template, obj), out string text) ? text : "" },
        { "TemplateTitle", (obj, template) =>
            {
                return PluginDataManager.TemplateManager.GetTemplates(FormatStatic(template, obj))?.FirstOrOptional()
                    .FlatMap(t => new Optional<string>(t.Title))
                    .Map(t => FormatStatic(t, obj))
                    .OrElse(null!) ?? "";
            }
        },
        { "Random", (obj, input) => RangeUtils.TryParseRange(FormatStatic(input, obj), out (int min, int max) range) ? Random.Range(range.min, range.max + 1).ToString() : "" },
        { "Stored", (obj, input) =>
            {
                input = FormatStatic(input, obj);
                return TActionStore.StoredVariables.GetValueOrDefault(input, $"No stored entry for \"{input}\"");
            }
        },
        { "Argument", (_, index) => int.TryParse(index, out int v) ? Arguments.Length > v ? Arguments[v] : "" : ""}
    };


    private static string ModifierText(object obj)
    {
        PlayerControl player = (PlayerControl)obj;
        IEnumerable<CustomRole> subroles = player.SecondaryRoles().OrderBy(r => r.RoleName);
        if (PluginDataManager.TemplateManager.HasTemplate("modifier-info"))
            return subroles.Select(sr => !PluginDataManager.TemplateManager.TryFormat(sr, "modifier-info", out string text) ? "" : text).Fuse("\n\n");

        return subroles.Select(sr =>
        {
            string symbol = sr is ISubrole subrole ? sr.RoleColor.Colorize(subrole.Identifier() ?? "") : "";
            return $"{symbol}{sr.RoleColor.Colorize(sr.RoleName)}\n{sr.Description}";
        }).Fuse("\n\n");
    }

    private static string ModifierBlurb(object obj)
    {
        PlayerControl player = (PlayerControl)obj;
        IEnumerable<CustomRole> subroles = player.SecondaryRoles().OrderBy(r => r.RoleName);
        if (PluginDataManager.TemplateManager.HasTemplate("modifier-blurb"))
            return subroles.Select(sr => !PluginDataManager.TemplateManager.TryFormat(sr, "modifier-blurb", out string text) ? "" : text).Fuse("\n\n");
        return subroles.Select(sr =>
        {
            string symbol = sr is ISubrole subrole ? sr.RoleColor.Colorize(subrole.Identifier() ?? "") : "";
            return $"{symbol}{sr.RoleColor.Colorize(sr.RoleName)}\n{sr.Blurb}";
        }).Fuse("\n\n");
    }

    private static Func<object, string> QW(Func<PlayerControl, string> playerToString) => obj => playerToString((PlayerControl)obj);

    // ReSharper disable once InconsistentNaming
    public delegate string TFormat(object? obj, string context);


}