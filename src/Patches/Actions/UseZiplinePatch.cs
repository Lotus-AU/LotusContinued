using HarmonyLib;
using Lotus.Roles.Internals;
using Lotus.Roles.Operations;
using UnityEngine;
using Lotus.Roles.Internals.Enums;
using VentLib.Utilities.Extensions;

namespace Lotus.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
public static class UseZiplinePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(UseZiplinePatch));
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument("ziplineBehaviour")] ZiplineBehaviour ziplineBehaviour, [HarmonyArgument("fromTop")] bool fromTop)
    {
        log.Debug("Checking if {0} can use zipline".Formatted(__instance.name));
        if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost) return false;

        if (!__instance)
        {
            log.Warn("Invalid zipline use, player is null");
            return false;
        }
        NetworkedPlayerInfo data = __instance.Data;
        if (!data|| data.IsDead || __instance.inMovingPlat)
        {
            log.Warn("Invlaid zipline use from {0}".Formatted(__instance.name));
            return false;
        }
        if (MeetingHud.Instance)
        {
            log.Warn("Tried to zipline while a meeting was in progress.");
            return false;
        }
        Vector3 vector = ziplineBehaviour.GetHandlePos(fromTop) - __instance.transform.position;
        if (vector.magnitude > 3f)
        {
            log.Info("{0} was denied the zipline: distance={1}".Formatted(__instance, vector.magnitude));
            return false;
        }

        ActionHandle handle = ActionHandle.NoInit();
        RoleOperations.Current.Trigger(LotusActionType.Zipline, __instance, handle, ziplineBehaviour, fromTop);
        if (handle.IsCanceled) return false;

        __instance.RpcUseZipline(__instance, ziplineBehaviour, fromTop);
        return false;
    }
}