using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Extensions;

public static class AmongUsClientExtensions
{
    private static string _disconnectMessage = string.Empty;

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

    public static void DisconnectWithMessage(this AmongUsClient client, string message)
    {
        if (client == null) return;
        _disconnectMessage = message;
        client.ExitGame(DisconnectReasons.Destroy);
    }

    [QuickPostfix(typeof(DisconnectPopup), nameof(DisconnectPopup.DoShow))]
    private static void DisconncetPopUp_DoShowPrefix(DisconnectPopup __instance)
    {
        if (_disconnectMessage == string.Empty) return;
        Async.Schedule(() =>
        {
            __instance.ShowCustom(_disconnectMessage);
            _disconnectMessage = string.Empty;
        }, 0.5f);
    }
}