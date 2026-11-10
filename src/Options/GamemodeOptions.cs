using System.Collections.Generic;
using Lotus.Options.General;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Attributes;
using VentLib.Options;
using Lotus.Options.Gamemodes;
using System.Linq;
using VentLib.Utilities.Extensions;

namespace Lotus.Options;

[LoadStatic]
public static class ExtraGamemodeOptions
{
    public static ColorwarsOptions ColorwarsOptions;
    public static CaptureOptions CaptureOptions;
    public static SpeedrunOptions SpeedrunOptions;

    public static EscapeOptions EscapeOptions;

    static ExtraGamemodeOptions()
    {
        ColorwarsOptions = new ColorwarsOptions();
        ColorwarsOptions.AllOptions.ForEach(o =>
        {
            // DefaultTabs.ColorwarsTab.AddOption(o);
            if (!o.Attributes.ContainsKey("Title")) GeneralOptions.ColorwarsOptionManager.Register(o, OptionLoadMode.LoadOrCreate);
        });
        ColorwarsOptions.AddTabListener(DefaultTabs.ColorwarsTab, 2);

        CaptureOptions = new CaptureOptions();
        CaptureOptions.AllOptions.ForEach(o =>
        {
            // DefaultTabs.CaptureTab.AddOption(o);
            if (!o.Attributes.ContainsKey("Title")) GeneralOptions.CaptureOptionManager.Register(o, OptionLoadMode.LoadOrCreate);
        });
        CaptureOptions.AddTabListener(DefaultTabs.CaptureTab, 2);

        SpeedrunOptions = new SpeedrunOptions();
        SpeedrunOptions.AllOptions.ForEach(o =>
        {
            // DefaultTabs.SpeedrunTab.AddOption(o);
            if (!o.Attributes.ContainsKey("Title")) GeneralOptions.SpeedrunOptionManager.Register(o, OptionLoadMode.LoadOrCreate);
        });
        SpeedrunOptions.AddTabListener(DefaultTabs.SpeedrunTab, 2);

        //// ---- Hide & Seek Gamemodes

        EscapeOptions = new EscapeOptions();
        EscapeOptions.AllOptions.ForEach(o =>
        {
            // DefaultTabs.EscapeOptions.AddOption(o);
            if (!o.Attributes.ContainsKey("Title")) GeneralOptions.EscapeOptionManager.Register(o, OptionLoadMode.LoadOrCreate);
        });
        EscapeOptions.AddTabListener(DefaultTabs.EscapeTab, 2);
    }
}