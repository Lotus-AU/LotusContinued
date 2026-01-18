using HarmonyLib;
using Discord;

namespace Lotus.Discord.Patches;

[HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
public class DiscordPatch
{
    public static string DiscordMessage = "Project Lotus " + (ProjectLotus.DevVersion ? ProjectLotus.DevVersionStr : "v" + ProjectLotus.VisibleVersion);
    public static void Prefix(ref Activity activity)
    {
        if (string.IsNullOrEmpty(activity.Details)) activity.Details = DiscordMessage;
        else activity.Details += $" ({DiscordMessage})";
    }
}