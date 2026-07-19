using VentLib.Localization.Attributes;

namespace Lotus.GenTranslations;

[Localized("ModerationActions")]
public class ModerationActionTranslations
{
    [Localized("BannedByBanList")] public static string BannedByBanList = "{0} was banned because they are on the host's banlist.";
    [Localized("BlockedPlayer")] public static string BlockedPlayer = "{0} was banned because they are blocked by the host.";
    [Localized("NoFriendCode")] public static string NoFriendCode = "{0} was kicked because they do not have a friendcode.";
    [Localized("MobileDevice")] public static string MobileDevice = "{0} was kicked because they are on a mobile platform.";
    [Localized("LevelKick")] public static string LevelKick = "{0} was kicked because they do not meet the minimum level requirement.";
    [Localized("BannedName")] public static string BannedName = "{0} was kicked because they have a banned name.";
}