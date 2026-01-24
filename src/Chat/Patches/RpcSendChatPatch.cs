using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Lotus.Logging;
using Lotus.Managers.Hotkeys;
using UnityEngine;
using VentLib.Networking.RPC;

namespace Lotus.Chat.Patches;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
internal class RpcSendChatPatch
{
    private static readonly List<string> ChatHistory = new();
    private static int _index = -1;

    static RpcSendChatPatch()
    {
        HotkeyManager.Bind(KeyCode.UpArrow)
            .If(b => b.Predicate(() =>
            {
                if (ChatHistory.Count == 0 || !HudManager.InstanceExists || HudManager.Instance.Chat == null) return false;
                return HudManager.Instance.Chat.freeChatField.textArea.hasFocus;
            })).Do(BackInChatHistory);
        HotkeyManager.Bind(KeyCode.DownArrow)
            .If(b => b.Predicate(() =>
            {
                if (ChatHistory.Count == 0 || !HudManager.InstanceExists || HudManager.Instance.Chat == null) return false;
                return HudManager.Instance.Chat.freeChatField.textArea.hasFocus;
            })).Do(ForwardInChatHistory);
    }

     private static void BackInChatHistory()
    {
        string text = ChatHistory[_index = Mathf.Clamp(_index + 1, 0, ChatHistory.Count - 1)];
        DevLogger.Log($"current index: {_index} | {ChatHistory.Count - 1} | {text}");
        HudManager.Instance.Chat.freeChatField.textArea.SetText(text);
    }

    private static void ForwardInChatHistory()
    {
        string text = ChatHistory[_index = Mathf.Clamp(_index - 1, 0, ChatHistory.Count - 1)];
        DevLogger.Log($"current index: {_index} | {ChatHistory.Count - 1} | {text}");
        HudManager.Instance.Chat.freeChatField.textArea.SetText(text);
    }

    internal static bool EatCommand;
    public static bool Prefix(PlayerControl __instance, string chatText)
    {
        if (string.IsNullOrWhiteSpace(chatText)) return false;
        _index = -1;

        if (!EatCommand) RpcV3.Immediate(__instance.NetId, RpcCalls.SendChat, SendOption.None).Write(chatText).Send();

        if (AmongUsClient.Instance.AmClient && HudManager.InstanceExists)
            HudManager.Instance.Chat.AddChat(__instance, chatText);

        EatCommand = false;

        if (ChatHistory.Count == 0 || ChatHistory[0] != chatText) ChatHistory.Insert(0, chatText.Trim());
        if (ChatHistory.Count >= 100) ChatHistory.RemoveAt(99);

        return false;
    }
}