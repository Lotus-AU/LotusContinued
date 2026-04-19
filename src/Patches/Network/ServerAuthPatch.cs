using System.Collections.Generic;
using Lotus.Extensions;
using Lotus.Server;
using Lotus.Server.Interfaces;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Patches.Network;

public class ServerAuthPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ServerAuthPatch));

    private static Queue<byte> _ignoreBroadcastQueue = new();

    public static bool IsLocal;

    [QuickPostfix(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
    public static void ConstantVersionPatch(ref int __result)
    {
        if (_ignoreBroadcastQueue.TryDequeue(out byte lobbyType))
        {
            IsLocal = lobbyType == 0;
            log.Debug($"Updating to match new queued lobby type.");
        }
        if (IsLocal) return;
        var revision = __result % 50;
        if (revision < Constants.MODDED_REVISION_MODIFIER_VALUE)
        {
            __result += Constants.MODDED_REVISION_MODIFIER_VALUE;
        }
    }

    [QuickPostfix(typeof(Constants), nameof(Constants.IsVersionModded))]
    public static void ConstantVersionPatch(ref bool __result)
    {
        if (_ignoreBroadcastQueue.TryDequeue(out byte lobbyType))
        {
            IsLocal = lobbyType == 0;
            log.Debug($"Updating to match new queued lobby type.");
        }
        __result = !IsLocal;
    }


    [QuickPostfix(typeof(HostLocalGameButton), nameof(HostLocalGameButton.OnClick))]
    public static void OverrideLocalVersion(HostLocalGameButton __instance)
    {
        _ignoreBroadcastQueue.Enqueue(0);
        log.Debug("Queued Local Lobby (0) to broadcast.");
    }

    [QuickPostfix(typeof(ConfirmCreatePopUp), nameof(ConfirmCreatePopUp.SetupInfo))]
    public static void OverrideOnlineVersion(ConfirmCreatePopUp __instance)
    {
        PassiveButton button = __instance.FindChild<PassiveButton>("CreateGame");
        if (button.OnClick.m_Calls.m_RuntimeCalls.Count == 0)
            button.OnClick.AddListener((System.Action)(() =>
            {
                _ignoreBroadcastQueue.Enqueue(1);
                log.Debug("Queued Online Lobby (1) to broadcast.");
            }));
    }

}