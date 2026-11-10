using HarmonyLib;
using Hazel;
using Il2CppSystem.Collections.Generic;
using InnerNet;
using Lotus.Logging;
using Lotus.Managers;
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable InconsistentNaming

namespace Lotus.Patches.Client;

[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
class BanMenuSetVisiblePatch
{
    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;
        __instance.BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
        __instance.KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
        __instance.MenuButton.gameObject.SetActive(show);
        /*__instance.hotkeyGlyph.SetActive(show);*/
        return false;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
class InnerNetClientCanBanPatch
{
    public static bool Prefix(InnerNetClient __instance, ref bool __result)
    {
        __result = __instance.AmHost;
        return false;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
class KickPlayerPatch
{
    public static void Prefix(InnerNetClient __instance, int clientId, bool ban)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!ban)  return;
        var client = AmongUsClient.Instance.GetClient(clientId);
        if (PluginDataManager.BanManager == null!)
        {
            LogManager.SendInGame($"Couldn't ban {client.Character?.name ?? client.PlayerName} because BannedPlayers.yaml is corrupted.");
            return;
        }
        PluginDataManager.BanManager.AddBanPlayer(client);
    }
}