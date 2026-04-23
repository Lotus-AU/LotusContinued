using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.Roles.Internals;
using Lotus.Extensions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Options;
using VentLib.Utilities;
using VentLib.Utilities.Debug.Profiling;
using VentLib.Utilities.Extensions;
using Lotus.RPC.CustomObjects;
using System.Linq;
using System.Reflection;
using Lotus.Utilities;
using VentLib.Localization;

namespace Lotus.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
static class FixedUpdatePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(FixedUpdatePatch));
    private static readonly ActionHandle FixedUpdateHandle = ActionHandle.NoInit();
    private static FixedUpdateLock _levelKickLock = new(1f); // needed to stop excessively long name if kicking player
    private static void Postfix(PlayerControl __instance)
    {
        if (__instance is {PlayerId: 254, notRealPlayer: true}) return;
        Game.RecursiveCallCheck = 0;
        // DisplayModVersion(__instance);

        if (!AmongUsClient.Instance.AmHost) return;

        if (!__instance.IsHost() && Game.State is GameState.InLobby &&
            __instance.Data.PlayerLevel < GeneralOptions.AdminOptions.KickPlayersUnderLevel &&
            _levelKickLock.AcquireLock())
        {
            AmongUsClient.Instance.KickPlayerWithMessage(__instance, string.Format(Localizer.Translate("ModerationActions.LevelKick", assembly: Assembly.GetExecutingAssembly()), __instance.name));
            log.Trace($"{__instance.name} was kicked because they are below the minimum level set by the host. ({GeneralOptions.AdminOptions.KickPlayersUnderLevel})");
        }

        if (Game.State is not GameState.Roaming) return;
        bool isLocalPlayer = __instance.PlayerId == PlayerControl.LocalPlayer.PlayerId;
        uint id = Profilers.Global.Sampler.Start("Fixed Update Patch");

        if (isLocalPlayer)
        {
            try
            {
                RoleOperations.Current.Trigger(LotusActionType.FixedUpdate, null, FixedUpdateHandle);
                CustomNetObject.FixedUpdate();
            }
            catch (System.Exception ex)
            {
                log.Exception(ex);
            }
        }

        if (__instance.IsAlive() && GeneralOptions.GameplayOptions.EnableLadderDeath) FallFromLadder.FixedUpdate(__instance);
        Profilers.Global.Sampler.Stop(id);
        /*if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) DisableDevice.FixedUpdate();*/
        /*EnterVentPatch.CheckVentSwap(__instance);*/
    }

    private static void DisplayModVersion(PlayerControl player)
    {
        if (Game.State is not GameState.InLobby) return;
        /*if (!TOHPlugin.playerVersion.TryGetValue(player.PlayerId, out var ver)) return;
        /*if (TOHPlugin.ForkId != ver.forkId) // フォークIDが違う場合
            player.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{player?.name}</color>";#1#
        if (TOHPlugin.version.CompareTo(ver.version) == 0)
            player.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{player.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{player?.name}</color>";
        else player.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{player?.name}</color>";*/
    }
}