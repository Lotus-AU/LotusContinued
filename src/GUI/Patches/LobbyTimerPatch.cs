using System;
using HarmonyLib;
using UnityEngine;
using VentLib.Utilities.Attributes;
using VentLib.Utilities;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using VentLib.Networking.RPC;
using Lotus.Network;

namespace Lotus.GUI.Patches;

[LoadStatic]
public static class SendTimerToOtherPlayers
{
    static SendTimerToOtherPlayers()
    {
        // this code currently kicks host
        // const string sendTimerHookKey = nameof(sendTimerHookKey);
        // Hooks.PlayerHooks.PlayerJoinHook.Bind(sendTimerHookKey, SendTimer);
    }

    public static void SendTimer(PlayerHookEvent hookEvent)
    {
        if (!AmongUsClient.Instance.AmHost || !GameData.Instance || !ConnectionManager.IsVanillaServer || LobbyBehaviour.Instance == null) return;
        TimeSpan elapsed = DateTime.Now - LobbyTimerPatch.lobbyStart;
        double remainingTime = Math.Clamp(600 - elapsed.TotalSeconds, 0, 600);
        RpcV3.Immediate(LobbyBehaviour.Instance.NetId, RpcCalls.LobbyTimeExpiring).WritePacked((int)remainingTime).Send(hookEvent.Player.GetClientId());
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
public class LobbyTimerPatch
{
    public static DateTime lobbyStart;
    public static void Postfix(GameStartManager __instance)
    {
        if (!GameData.Instance || !ConnectionManager.IsVanillaServer) return; // Not host or no instance or LocalGame
        lobbyStart = DateTime.Now;
        if (AmongUsClient.Instance.AmHost) HudManager.Instance.ShowLobbyTimer(600);
        HudManager.Instance.LobbyTimerExtensionUI.timerText.transform.parent.transform.Find("LabelBackground").gameObject.SetActive(false);
        HudManager.Instance.LobbyTimerExtensionUI.timerText.transform.parent.transform.Find("Icon").gameObject.SetActive(false);
    }
}

[HarmonyPatch(typeof(TimerTextTMP), nameof(TimerTextTMP.UpdateText))]
public class TimerTextStringPatch
{
    static bool Prefix(TimerTextTMP __instance)
    {
        if (__instance.name != "WarningText") return true;
        int timer = __instance.GetSecondsRemaining();
        int minutes = timer / 60;
        int seconds = timer % 60;
        string suffix = $" {minutes:00}:{seconds:00}";
        if (timer <= 60) suffix = Color.red.Colorize(suffix);
        __instance.text.text = string.Format(__instance.format, suffix);
        return false;
    }
}