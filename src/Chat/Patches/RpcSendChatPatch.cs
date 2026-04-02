using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Managers;
using Lotus.Managers.Hotkeys;
using Lotus.Network;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using UnityEngine;
using VentLib.Commands;
using VentLib.Networking.RPC;

namespace Lotus.Chat.Patches;

[HarmonyPriority(Priority.Last)]
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
internal class RpcSendChatPatch
{
    private static readonly List<string> ChatHistory = new();
    private static int _index = -1;

    private const bool RemoveHtmlTags = false;

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

    public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
    {
        if (RemoveHtmlTags) chatText = Regex.Replace(chatText, "<.*?>", string.Empty);
        if (string.IsNullOrWhiteSpace(chatText) || (!__instance.AmOwner && ConnectionManager.IsVanillaServer))
        {
            __result = false;
            return false;
        }
        if (chatText.StartsWith(CommandRunner.Prefix))
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            __result = false;
            if (PluginDataManager.TemplateManager.CheckAndRunCommand(__instance, chatText)) return false;
            if (Game.State is GameState.InLobby) return false;
            ActionHandle handle = ActionHandle.NoInit();
            RoleOperations.Current.TriggerForAll(LotusActionType.Chat, __instance, handle, chatText, Game.State, __instance.IsAlive());
            return false;
        }
        _index = -1;

        if (AmongUsClient.Instance.AmClient && HudManager.InstanceExists)
            HudManager.Instance.Chat.AddChat(__instance, chatText);
        if (chatText.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
            DestroyableSingleton<UnityTelemetry>.Instance.SendWho();

        if (ChatHistory.Count == 0 || ChatHistory[0] != chatText) ChatHistory.Insert(0, chatText.Trim());
        if (ChatHistory.Count >= 100) ChatHistory.RemoveAt(99);

        RpcV3.Immediate(__instance.NetId, RpcCalls.SendChat).Write(chatText).Send();
        __result = true;
        return false;
    }
}