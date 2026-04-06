using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.Options;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization;


namespace Lotus.Patches.Network;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
class PingTrackerPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PingTrackerPatch));
    public static float deltaTime;
    private static bool dipped;

    static void Postfix(PingTracker __instance)
    {
        __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;

        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = Mathf.Ceil(1.0f / deltaTime);
        if (fps < 30 && !dipped && Game.State is GameState.Roaming)
        {
            log.High($"FPS Dipped Below 30 => {fps}");
            dipped = true;
        }
        else dipped = false;

        __instance.text.text += " " + fps + " fps";
        __instance.text.sortingOrder = -1;

        __instance.text.text += ProjectLotus.CredentialsText;
        if (GeneralOptions.DebugOptions.NoGameEnd) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, Localizer.Translate("StaticOptions.NoGameEnd"));
        __instance.text.text += $"\r\n" + Game.CurrentGameMode.Name;


        __instance.aspectPosition.DistanceFromEdge = GetPingPosition();

        // var offsetX = 1.2f; //右端からのオフセット
        // if (HudManager.InstanceExists && HudManager._instance.Chat.chatButton.enabled) offsetX += 0.8f;
        // if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offsetX += 0.8f;
        // __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offsetX, 0f, 0f);
    }

    // THANKS TOHE. YOU GUYS ARE REALLY HELPFUL
    private static Vector3 GetPingPosition()
    {
        var settingButtonTransformPosition = DestroyableSingleton<HudManager>.Instance.SettingsButton.transform.localPosition;
        var offset_x = settingButtonTransformPosition.x - 1.58f;
        var offset_y = settingButtonTransformPosition.y + 3.2f;
        Vector3 position;
        if (ClientOptions.AdvancedOptions.KeybindGuiToggleVisible) offset_y -= 0.55f;
        if (AmongUsClient.Instance.IsGameStarted)
        {
            if (DestroyableSingleton<HudManager>.Instance && !HudManager.Instance.Chat.isActiveAndEnabled)
            {
                offset_x += 0.7f; // Additional offsets for chat button if present
            }
            else
            {
                offset_x += 0.1f;
            }

            position = new Vector3(offset_x, offset_y, 0f);
        }
        else
        {
            position = new Vector3(offset_x, offset_y, 0f);
        }

        return position;
    }
}