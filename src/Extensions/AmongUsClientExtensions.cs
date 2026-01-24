using VentLib.Networking.RPC;
using VentLib.Utilities;

namespace Lotus.Extensions;

public static class AmongUsClientExtensions
{
    public static void KickPlayerWithMessage(this AmongUsClient client, PlayerControl target, string message, bool banPlayer = false)
    {
        if (client == null) return;
        message += "<size=0>"; // removes the "left the game" text.
        target.SetName(message);
        RpcV3.Immediate(target.NetId, RpcCalls.SetName).Write(target.Data.NetId).Write(message).Send();
        Async.Schedule(() =>
        {
            target.SetName(message);
            client.KickPlayer(target.GetClientId(), banPlayer);
        }, NetUtils.DeriveDelay(0.2f));
    }
}