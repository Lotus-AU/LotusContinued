using AmongUs.GameOptions;
using HarmonyLib;
using Lotus.Extensions;
using Lotus.Patches.Actions;
using VentLib.Networking.RPC;
using VentLib.Utilities;

namespace Lotus.Patches;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.AssignRoleOnDeath))]
public class AssignRoleOnDeathPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(AssignRoleOnDeathPatch));

    public static bool Prefix(RoleManager __instance, [HarmonyArgument(0)] PlayerControl player)
    {
        return false;
    }

    public static void Postfix(RoleManager __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] bool specialRolesAllowed)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        RpcV3.Immediate(player.NetId, RpcCalls.SetRole)
            .Write((ushort)(player.GetVanillaRole().IsImpostor() ? RoleTypes.ImpostorGhost : RoleTypes.CrewmateGhost))
            .Write(ProjectLotus.AdvancedRoleAssignment)
            .Send(player.GetClientId());
        player.Data.DefaultOutfit.PetId = "pet_EmptyPet";
        log.Debug($"Dead Player {player.name} => {player.Data.Role.Role}");
    }
}