using HarmonyLib;
using UnityEngine;

namespace Lotus.Chat.Patches;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatControllerUpdatePatch
{
    public static void Postfix(ChatController __instance)
    {
        if (!__instance.freeChatField.textArea.hasFocus) return;
        // __instance.freeChatField.textArea.AllowPaste = true;
        __instance.freeChatField.textArea.AllowSymbols = true;
        __instance.freeChatField.textArea.AllowEmail = true;

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + GUIUtility.systemCopyBuffer.Trim());

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
            __instance.freeChatField.textArea.SetText("");
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendFreeChat))]
public static class SendFreeChatPatch
{
    // patch to avoid the link & other detections
    public static bool Prefix(ChatController __instance)
    {
        string text = __instance.freeChatField.Text;
        ChatController.Logger.Debug("SendFreeChat () :: Sending message: '" + text + "'", null);
        PlayerControl.LocalPlayer.RpcSendChat(text);
        return false;
    }
}
