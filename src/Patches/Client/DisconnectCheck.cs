using System.Collections.Generic;
using HarmonyLib;

namespace Lotus.Patches.Client;

[HarmonyPatch(typeof(GameData), nameof(GameData.HandleDisconnect), typeof(PlayerControl), typeof(DisconnectReasons))]
public class DisconnectCheck
{
    public static HashSet<byte> DisconnectPlayers = [];

    public static void Postfix(GameData __instance, PlayerControl player, DisconnectReasons reason)
    {
        if (!player) return;
        NetworkedPlayerInfo playerById = __instance.GetPlayerById(player.PlayerId);
        if (!playerById) return;
        if (!AmongUsClient.Instance.IsGameStarted) return;
        DisconnectPlayers.Add(player.PlayerId);
    }
}