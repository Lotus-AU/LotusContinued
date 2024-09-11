using Lotus.Extensions;
using Lotus.Patches.Actions;
using VentLib.Networking.RPC;
using VentLib.Utilities;

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

        if (MeetingHud.Instance != null)
        {
            killer.RpcVaporize(target);
            RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.Succeeded).Send(target.GetClientId());
            return;
        }

        if (AmongUsClient.Instance.AmHost) killer.MurderPlayer(target, MurderResultFlags.Succeeded);

        RpcV3.Immediate(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.Succeeded).Send();
        target.Data.IsDead = true;
    }
}