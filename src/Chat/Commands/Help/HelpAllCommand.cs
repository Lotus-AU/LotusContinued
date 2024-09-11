using System.Collections.Generic;
using System.Linq;
using Lotus.Managers;
using UnityEngine;
using VentLib.Commands.Attributes;
using VentLib.Localization.Attributes;
using VentLib.Utilities.Extensions;
using static Lotus.Chat.Commands.Help.HelpAllCommand.HelpCommandTranslations;

namespace Lotus.Chat.Commands.Help;

public class HelpAllCommand : CommandTranslations
{
    public static Dictionary<CommandSection, List<CommandHelp>> HelpSetions =
        new()
        {
            { CommandSection.Host, new List<CommandHelp>
            {
                new("/id", IdCommandDescription),
                new($"/say [{MessageParameter}]", SayCommandDescription),
                new($"/kick [{NameParameter} | ID]", KickCommandDescription),
                new($"/ban [{NameParameter} | ID]", BanCommandDescription),
                new("/dump", DumpCommandDescription),
                new("/t help", TemplateHelpCommandDescription)
            } },

            { CommandSection.General, new List<CommandHelp>
            {
                new($"/r [{RoleNameParameter}]", RoleInfoCommandDescription),
                new("/r", RolesCommandDescription),
                new("/n [crew|imp|nk|mods]", NowSpecificCommandDescription),
                new("/n r", NowAllCommandDescription),
                new("/perc", PercCommandDescription)
            } },

            { CommandSection.InGame, new List<CommandHelp>
            {
                new("/m", MyRoleCommandDescription),
                new("/o", RoleOptionCommandDescription),
                new("/desc", DescCommandDescription)
            } },

            { CommandSection.Lobby, new List<CommandHelp>
            {
                new("/last", LastResultCommandDescription),
                new("/winner", WinnerCommandDescription),
                new("/death", DeathCommandDescription),
                new($"/death [{NameParameter}]", DeathOtherCommandDescription)
            } },

            { CommandSection.Other, new List<CommandHelp>
            {
                new($"/name [{NameParameter}]", NameCommandDescription),
                new("/color [ID]", ColorCommandDescription),
                new("/stats", StatsCommandDescription),
                new($"/stats [{NameParameter}]", StatsOtherCommandDescription)
            } },
        };

    [Command("help", "h")]
    public static void HelpCommand(PlayerControl source)
    {
        string result = HelpSetions.Where(s => source.IsHost() || s.Key is not CommandSection.Host || PluginDataManager.ModManager.IsPlayerModded(source)).Select(section =>
        {
            string content = section.Value.Select(v => v.ToString()).Fuse("\n");
            return $"★ {section.Key.SectionName()}\n{content}";
        }).Fuse("\n\n");

        ChatHandler.Of(message: result)
            .Title(t => t.Text(HelpTitle).Color(new Color(0.62f, 1f, 0.27f)).Build())
            .LeftAlign()
            .Send(source);
    }


    [Localized("Help")]
    public static class HelpCommandTranslations
    {
        [Localized(nameof(HelpTitle))]
        public static string HelpTitle = "Help";

        [Localized(nameof(HostSection))]
        public static string HostSection = "Host Commands:";

        [Localized(nameof(GeneralSection))]
        public static string GeneralSection = "General Commands:";

        [Localized(nameof(LobbySection))]
        public static string LobbySection = "Lobby Commands:";

        [Localized(nameof(InGameSection))]
        public static string InGameSection = "In-Game Commands:";

        [Localized(nameof(OtherSection))]
        public static string OtherSection = "Other Commands:";

        [Localized(nameof(MessageParameter))]
        public static string MessageParameter = "Message";

        [Localized(nameof(RoleNameParameter))]
        public static string RoleNameParameter = "Role";

        [Localized(nameof(NameParameter))]
        public static string NameParameter = "Name";

        [Localized(nameof(IdCommandDescription))]
        public static string IdCommandDescription = "Displays all players and their IDs.";

        [Localized(nameof(SayCommandDescription))]
        public static string SayCommandDescription = "Displays the provided message to ALL players.";

        [Localized(nameof(KickCommandDescription))]
        public static string KickCommandDescription = "Kicks the provided player.";

        [Localized(nameof(BanCommandDescription))]
        public static string BanCommandDescription = "Bans the provided player.";

        [Localized(nameof(DumpCommandDescription))]
        public static string DumpCommandDescription = "Creates a log dump.";

        [Localized(nameof(TemplateHelpCommandDescription))]
        public static string TemplateHelpCommandDescription = "Provides info on all template commands.";

        [Localized(nameof(MyRoleCommandDescription))]
        public static string MyRoleCommandDescription = "Displays your current role's description.";

        [Localized(nameof(RoleOptionCommandDescription))]
        public static string RoleOptionCommandDescription = "Displays your current role's options.";

        [Localized(nameof(DescCommandDescription))]
        public static string DescCommandDescription = "Re-displays the text from the first meeting.";

        [Localized(nameof(LastResultCommandDescription))]
        public static string LastResultCommandDescription = "Displays the results of the last game.";

        [Localized(nameof(WinnerCommandDescription))]
        public static string WinnerCommandDescription = "Displays the winner(s) of the last game.";

        [Localized(nameof(DeathCommandDescription))]
        public static string DeathCommandDescription = "Displays your cause of death. (Only usable if dead or in lobby)";

        [Localized(nameof(DeathOtherCommandDescription))]
        public static string DeathOtherCommandDescription = "Displays another player's cause of death. (Only usable if dead or in lobby)";

        [Localized(nameof(RoleInfoCommandDescription))]
        public static string RoleInfoCommandDescription = "Displays the description and options of the provided role.";

        [Localized(nameof(RolesCommandDescription))]
        public static string RolesCommandDescription = "Displays ALL roles in the mod.";

        [Localized(nameof(NowSpecificCommandDescription))]
        public static string NowSpecificCommandDescription = "Displays all enabled roles, and their options for the provided category";

        [Localized(nameof(NowAllCommandDescription))]
        public static string NowAllCommandDescription = "Displays all enabled roles, and their options";

        [Localized(nameof(PercCommandDescription))]
        public static string PercCommandDescription = "Displays all enabled roles and their spawn rates.";

        [Localized(nameof(NameCommandDescription))]
        public static string NameCommandDescription = "Changes the player's name.";

        [Localized(nameof(ColorCommandDescription))]
        public static string ColorCommandDescription = "Changes the player's color.";

        [Localized(nameof(StatsCommandDescription))]
        public static string StatsCommandDescription = "Displays your stats for the lobby.";

        [Localized(nameof(StatsOtherCommandDescription))]
        public static string StatsOtherCommandDescription = "Displays another player's stats for the lobby.";
    }

}

public class CommandHelp
{
    public string Command { get; set; }
    public string Description { get; set; }

    public CommandHelp(string command, string description)
    {
        Command = command;
        Description = description;
    }

    public override string ToString() => $"• {Command} → {Description}";
}

public enum CommandSection
{
    Host,
    InGame,
    Lobby,
    General,
    Other
}

public static class CommandSectionExtension
{
    public static Dictionary<CommandSection, string> CommandSectionNameMapping = new();

    public static string SectionName(this CommandSection cs)
    {
        return cs switch
        {
            CommandSection.Host => HostSection,
            CommandSection.InGame => InGameSection,
            CommandSection.Lobby => LobbySection,
            CommandSection.General => GeneralSection,
            CommandSection.Other => OtherSection,
            _ => CommandSectionNameMapping.GetValueOrDefault(cs, "Unknown Section")
        };
    }
}

