using System.Diagnostics.CodeAnalysis;
using VentLib.Localization.Attributes;
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global

namespace Lotus.Chat.Commands;

[Localized("Commands")]
[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public class CommandTranslations
{
    [Localized(nameof(InvalidUsage))] protected static string InvalidUsage = "⚠ Invalid Usage ⚠";
    [Localized(nameof(NotPermittedTitle))] protected static string NotPermittedTitle = "⚠ Not Permitted ⚠";
    [Localized(nameof(NotPermittedText))] protected static string NotPermittedText = "You are not permitted to use this command.";
    [Localized("Say.MessageTitle")] protected static string HostMessage = "Host Message";
    [Localized(nameof(PlayerNotFoundText))] protected static string PlayerNotFoundText = "Player \"{0}\" not found.";
    [Localized(nameof(CommandError))] protected static string CommandError = "⚠ Command Error ⚠";

    [Localized(nameof(OldCommandWarning))] protected static string OldCommandWarning = "You are using a deprecated command!\nEvery command has changed to use '/cmd' in front. For example, you typed: '/{0}'. The correct command should be '/cmd {0}'.\nYou have been warned for now, but this deprecated command will stop working sometime in March.";

    [Localized(nameof(NoPreviousGameText))] public static string NoPreviousGameText = "No game played yet!";

    [Localized("HostOptions")]
    protected static class HostOptionTranslations
    {
        [Localized(nameof(RoleInfo))] public static string RoleInfo = "★ Role Info ★";
        [Localized(nameof(RoleCategory))] public static string RoleCategory = "★ {0} Roles";
        [Localized(nameof(CurrentRoles))] public static string CurrentRoles = "Current Roles";
    }
}