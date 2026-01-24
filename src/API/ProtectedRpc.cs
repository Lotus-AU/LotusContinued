using Lotus.Extensions;
using Lotus.Patches.Actions;
using Lotus.RPC.CustomObjects;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Optionals;

namespace Lotus.API;

public class ProtectedRpc
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ProtectedRpc));

    public static void CheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost) return;
        log.Trace($"Protected Check Murder ({killer.name ?? "null"} => {target?.name ?? "null"})");
        if (target == null) return;
        NetworkedPlayerInfo data = target.Data;
        if (data == null)
        {
            log.Trace("Kill was canceled because target's data is null.");
            return;
        }

        if (target is {PlayerId: 254, notRealPlayer: true})
        {
            Optional<CustomNetObject> netObject = CustomNetObject.ObjectFromPlayer(target);
            if (netObject.Exists())
            {
                log.Trace($"Kill was canceled because they are trying to kill a CustomNetObject.");
                return;
            }
        }

        if (MeetingHud.Instance != null)
        {
            killer.RpcVaporize(target);
            RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.Succeeded).Send(target.GetClientId());
            target.Data.IsDead = true;
            return;
        }

        if (AmongUsClient.Instance.AmHost)
        {
            target.SetPet("pet_EmptyPet");
            killer.MurderPlayer(target, MurderResultFlags.Succeeded);
        }

        RpcV3.Immediate(target.NetId, RpcCalls.SetPetStr).Write("").Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr)).Send();
        RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.Succeeded).Send();
        target.Data.IsDead = true;
    }
}