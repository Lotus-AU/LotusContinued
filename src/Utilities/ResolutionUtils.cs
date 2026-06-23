using Lotus.Managers.Hotkeys;
using UnityEngine;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Utilities;

[LoadStatic]
public class ResolutionUtils
{
    public static bool IsFullscreen = Screen.fullScreen;

    private static bool useAllResolutions = true;

    public static (int width, int height)[] AllResolutions
    {
        get => useAllResolutions ? Resolutions : ResolutionsSixteenNine;
    }

    private static (int width, int height)[] Resolutions = {
        (640, 360), (854, 480), (800, 600), (960, 540), (1024, 576), (1024, 768), (1152, 864), (1176, 664),
        (1280, 720), (1280, 800), (1280, 960), (1360, 768), (1366, 768), (1400, 1050), (1440, 900), (1600, 900),
        (1600, 1024), (1600, 1200), (1680, 1050), (1920, 1080), (1920, 1200), (1920, 1440), (2048, 1152), (2560, 1440),
        (5120, 2880)
    };

    private static (int width, int height)[] ResolutionsSixteenNine = {
        (640, 360), (854, 480), (960, 540), (1024, 576), (1280, 720), (1366, 768), (1600, 900), (1920, 1080), (2560, 1440), (5120, 2880)
    };

    public static int ResolutionIndex
    {
        get => Mathf.Clamp(_resolutionIndex != -1
            ? _resolutionIndex
            : AllResolutions.IndexOf(item => item.width == Screen.width && item.height == Screen.height), 0, AllResolutions.Length - 1);
        set => _resolutionIndex = Mathf.Clamp(value, 0, AllResolutions.Length - 1);
    }
    private static int _resolutionIndex = -1;

    static ResolutionUtils()
    {
        HotkeyManager.Bind(KeyCode.LeftControl, KeyCode.Minus).Name("Shrink Screen").Do(ShrinkScreen);
        HotkeyManager.Bind(KeyCode.LeftControl, KeyCode.Equals).Name("Grow Screen").Do(GrowScreen);
    }

    public static void SetResolution(int width, int height, bool fullscreen = false) => ResolutionManager.SetResolution(width, height, fullscreen);

    private static void ShrinkScreen()
    {
        (int width, int height) = AllResolutions[--ResolutionIndex];
        SetResolution(width, height);
    }

    private static void GrowScreen()
    {
        (int width, int height) = AllResolutions[++ResolutionIndex];
        SetResolution(width, height);
    }
}