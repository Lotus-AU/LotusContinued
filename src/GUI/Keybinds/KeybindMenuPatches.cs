using HarmonyLib;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.GUI.Keybinds;

[HarmonyPatch]
public static class KeybindMenuPatches
{
    [QuickPostfix(typeof(HudManager), nameof(HudManager.Start))]
    public static void HudManager_StartPostfix(HudManager __instance)
    {
        if (KeybindHud.Instance != null) KeybindHud.Instance.Destroy();
        _ = new KeybindHud(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
    public static void PlayerControl_CanMovePatch(PlayerControl __instance, ref bool __result)
    {
        if (KeybindHud.Instance?.KeybindMenu.active == true) __result = false;
    }
}