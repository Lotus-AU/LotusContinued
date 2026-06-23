using System.Collections.Generic;
using UnityEngine;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Managers.Hotkeys;

public class HotkeyManager
{
    internal static readonly List<Hotkey> Hotkeys = new();

    public static bool HoldingLeftShift;
    public static bool HoldingRightShift;


    [QuickPostfix(typeof(ControllerManager), nameof(ControllerManager.Update))]
    private static void DoHotkeyCheck()
    {
        Hotkeys.ForEach(h => h.Update());
        HoldingLeftShift = Input.GetKey(KeyCode.LeftShift);
        HoldingRightShift = Input.GetKey(KeyCode.RightShift);
    }

    public static Hotkey AddHokey(Hotkey hotkey)
    {
        Hotkeys.Add(hotkey);
        return hotkey;
    }

    public static Hotkey Bind(params KeyCode[] keyCodes)
    {
        return AddHokey(Hotkey.When(keyCodes));
    }
}

