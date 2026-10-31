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
        // Every client runs ExitAllVents for every vented player when a meeting starts. Only the host may send this RPC:
        // a non-host client sending it for a player it doesn't own is kicked by the server's anticheat for hacking.
        if (!AmongUsClient.Instance.AmHost) return;
        if (!__instance.myPlayer.inVent) return;
        int ventId = Object.FindObjectsOfType<Vent>().FirstOrDefault()?.Id ?? 1;

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