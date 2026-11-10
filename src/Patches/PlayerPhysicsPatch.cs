using System.Linq;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Networking.RPC;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Patches;

public class PlayerPhysicsPatch
{
    [QuickPrefix(typeof(PlayerPhysics), nameof(PlayerPhysics.ExitAllVents))]
    public static void ExitAllVentsPrefix(PlayerPhysics __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!__instance.myPlayer.inVent) return;
        int ventId = ShipStatus.Instance.AllVents.First().Id;

        if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var ventilationType))
        {
            if (ventilationType.TryCast<VentilationSystem>(out var ventilationSystem))
            {
                if (ventilationSystem.PlayersInsideVents.TryGetValue(__instance.myPlayer.PlayerId, out byte currentVent)) ventId = (int)currentVent;
            }
        }

        RpcV3.Immediate(__instance.NetId, RpcCalls.BootFromVent).WritePacked(ventId).Send();
    }
}