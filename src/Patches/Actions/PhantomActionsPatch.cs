using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Impostors;
using Lotus.Roles;
using Lotus.Utilities;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Patches.Actions;

// phantom is very broken right now...
// its not very clear how to reject an invisble attempt
// and the og code doesnt even check for modded host.
// so phantom is pretty much useless unfortunately
// pls fix innersloth

public static class PhantomActionsPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PhantomActionsPatch));
    [QuickPostfix(typeof(PlayerControl), nameof(PlayerControl.SetRoleInvisibility))]
    public static void Postifx(PlayerControl __instance, bool isActive)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        log.Debug($"{__instance.name} {(isActive ? "is going invisible as Phantom." : "is appearing as Phantom.")}");
        IEnumerable<byte> alliedPlayerIds = Players.GetPlayers().Where(p => __instance.Relationship(p) is Relation.FullAllies).Where(__instance.PrimaryRole().Faction.CanSeeRole).Select(p => p.PlayerId);
        Vent farthestVent = Utils.GetFurthestVentFromPlayers();
        bool isImpFaction = __instance.PrimaryRole().Faction is ImpostorFaction;
        Players.GetAllPlayers().ForEach(p =>
        {
            if (__instance == p) return; // skip player going invis
            CustomRole role = p.PrimaryRole();
            if (role.RealRole.IsCrewmate() && isImpFaction) return; // they are crewmate and we are imp so we are phantom for them
            if (alliedPlayerIds.Contains(p.PlayerId)) return; // if we are allied than continue
            if (isActive)
            {
                if (p.AmOwner)
                {
                    __instance.MyPhysics.StopAllCoroutines();
                    __instance.NetTransform.SnapTo(farthestVent.transform.position);
                    __instance.MyPhysics.StartCoroutine(__instance.MyPhysics.CoEnterVent(farthestVent.Id));
                }
                else
                {
                    Utils.TeleportDeferred(__instance.NetTransform, farthestVent.transform.position).Send(p.GetClientId());
                    RpcV3.Immediate(__instance.MyPhysics.NetId, RpcCalls.EnterVent).WritePacked(farthestVent.Id).Send(p.GetClientId());
                }
            }
            else
            {
                if (p.AmOwner)
                {
                    var pos = __instance.GetTruePosition();
                    __instance.MyPhysics.BootFromVent(farthestVent.Id);
                    __instance.NetTransform.SnapTo(pos);
                }
                else
                {
                    var pos = __instance.GetTruePosition();
                    RpcV3.Immediate(__instance.MyPhysics.NetId, RpcCalls.ExitVent).WritePacked(farthestVent.Id).Send(p.GetClientId());
                    Utils.TeleportDeferred(__instance.NetTransform, pos).Send(p.GetClientId());
                }
            }
        });
    }
}