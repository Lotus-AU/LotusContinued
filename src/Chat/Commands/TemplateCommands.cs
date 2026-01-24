using System.Linq;
using Lotus.API.Player;
using Lotus.Chat.Patches;
using Lotus.Managers;
using Lotus.Managers.Hotkeys;
using Lotus.Managers.Templates;
using Lotus.Managers.Templates.Models;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Chat.Commands;

[Command("template", "t", "templates")]
public class TemplateCommands: CommandTranslations, ICommandReceiver
{
    public static string TemplateTitle = ModConstants.Palette.GeneralColor4.Colorize("Templates");


    [Command(CommandFlag.HostOnly, "list", "l")]
    public static void ListTemplates(PlayerControl source)
    {
        string templates = Templates.ListTemplates.Select((t, i) => TemplateText(i + 1, t)).Fuse("\n\n");
        SuccessMsg(templates).Send(source);
    }

    [Command(CommandFlag.HostOnly, "tags")]
    public static void ListTags(PlayerControl source)
    {
        string tags = Templates.AllTags().Select((t, i) => $"{i + 1}. {t.tag}: {t.description}").Fuse("\n\n");
        SuccessMsg(tags).Send(source);
    }

    /*[Command(CommandFlag.HostOnly, "variables", "v")]
    public static void ListVariables(PlayerControl source)
    {
        string variables = Template.TemplateVariables.Select(t => $"<b>{t.Key}</b>: {t.Value}").Fuse("\n");
        SuccessMsg(variables).Send(source);
    }*/

    [Command(CommandFlag.HostOnly, "reload")]
    public static void Reload(PlayerControl source)
    {
        string? exception = PluginDataManager.TemplateManager.LoadTemplates();
        if (exception != null) ErrorMsg(exception).Send(source);
        else SuccessMsg(TemplateCommandTranslations.ReloadTemplatesText).Send(source);
    }

    [Command(CommandFlag.HostOnly, "help", "h")]
    public static void Help(PlayerControl source)
    {
        const string help = @"/template [tag] → Shows the template with the given tag to all players.
/template variables → Shows all built-in template variables.
/template tags → Shows all built-in template tags.
/template list → Shows all loaded templates
/template reload → Reloads all templates from file.
";
        SuccessMsg(help).Send(source);
    }

    private static string TemplateText(int i, Template t)
    {
        string tagText = t.Tag != null ? $"({t.Tag}) " : "";
        return $"{i})<indent=10%>{tagText}{t.Text}</indent>";
    }

    private static ChatHandler SuccessMsg(string message) => ChatHandler.Of(message, title: TemplateTitle).LeftAlign();
    private static ChatHandler ErrorMsg(string message) => ChatHandler.Of(message, title: CommandError).LeftAlign();

    private static TemplateManager Templates => PluginDataManager.TemplateManager;

    [Localized("Template")]
    private static class TemplateCommandTranslations
    {
        [Localized(nameof(CreatedTemplateText))]
        public static string CreatedTemplateText = "Successfully created template (ID: {0}).";

        [Localized(nameof(ErrorRemovingTemplateText))]
        public static string ErrorRemovingTemplateText = "Error removing template.";

        [Localized(nameof(SuccessRemovingTemplateText))]
        public static string SuccessRemovingTemplateText = "Successfully removed template (ID: {0}).";

        [Localized(nameof(SuccessTaggingTemplateText))]
        public static string SuccessTaggingTemplateText = "Successfully added tag \"{0}\" to template (ID: {1}).";

        [Localized(nameof(ErrorTaggingTemplateText))]
        public static string ErrorTaggingTemplateText = "Error adding tag \"{0}\" to template (ID: {1}).";

        [Localized(nameof(SuccessRemovingTagText))]
        public static string SuccessRemovingTagText = "Successfully removed tags from template (ID: {0}).";

        [Localized(nameof(ErrorRemovingTagText))]
        public static string ErrorRemovingTagText = "Error removing tags from template (ID: {0}).";

        [Localized(nameof(ErrorPreviewingTemplateText))]
        public static string ErrorPreviewingTemplateText = "Error previewing template (ID: {0}).";

        [Localized(nameof(ErrorShowingTemplateText))]
        public static string ErrorShowingTemplateText = "Error showing template to {0}.";

        [Localized(nameof(SuccessEditingTemplateText))]
        public static string SuccessEditingTemplateText = "Successfully edited template (ID: {0}).";

        [Localized(nameof(ErrorEditingTemplateText))]
        public static string ErrorEditingTemplateText = "Error editing template (ID: {0}).";

        [Localized(nameof(ReloadTemplatesText))]
        public static string ReloadTemplatesText = "Successfully reloaded templates.";
    }

    public void Receive(PlayerControl source, CommandContext context)
    {
        if (context.Args.Length == 0) return;
        if (source.IsHost()) RpcSendChatPatch.EatCommand = true;

        string tag = context.Join();
        if (source.IsHost() && HotkeyManager.HoldingRightShift) Templates.ShowAll(tag, source, Players.GetPlayers(PlayerFilter.Dead));
        else if (source.IsHost()) Templates.ShowAll(tag, source);
        else Templates.GetTemplates(tag)?.ForEach(t =>
        {
            if (!t.AliasOnly) t.SendMessage(PlayerControl.LocalPlayer, source);
        });
    }
}