using HarmonyLib;

namespace Lotus.Patches;

// This patch allows host to have bigger range when setting options
[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.CreateSettings))]
public class GameOptionsMenuPatch
{
    public static void Postfix(GameOptionsMenu __instance)
    {
        foreach (var ob in __instance.Children)
        {
            switch (ob.Title)
            {
                case StringNames.GameVotingTime:
                    ob.Cast<NumberOption>().ValidRange = new(0, 600);
                    break;
                case StringNames.GameShortTasks:
                case StringNames.GameLongTasks:
                case StringNames.GameCommonTasks:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                    break;
                case StringNames.GameKillCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    ob.Cast<NumberOption>().Increment = 0.5f;
                    break;
                case StringNames.GamePlayerSpeed:
                case StringNames.GameCrewLight:
                case StringNames.GameImpostorLight:
                    ob.Cast<NumberOption>().Increment = 0.125f;
                    break;
                case StringNames.GameNumImpostors when ProjectLotus.DevVersion:
                    ob.Cast<NumberOption>().ValidRange.min = 0;
                    break;
            }
        }
    }
}