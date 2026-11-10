using System.Collections.Generic;
using Lotus.GameModes;
using Lotus.GUI;
using Lotus.Utilities;
using VentLib.Options.UI;
using VentLib.Options.UI.Controllers;
using VentLib.Options.UI.Tabs;
using VentLib.Utilities.Attributes;

namespace Lotus.Options;

[LoadStatic]
public class DefaultTabs
{
    public static readonly MainSettingTab NormalStandardTab = new(GamemodeTranslations.Standard.ButtonText, GamemodeTranslations.Standard.Description);
    public static readonly MainSettingTab CaptureTab = new(GamemodeTranslations.CaptureTheFlag.ButtonText, GamemodeTranslations.CaptureTheFlag.Description);
    public static readonly MainSettingTab ColorwarsTab = new(GamemodeTranslations.Colorwars.ButtonText, GamemodeTranslations.Colorwars.Description);
    public static readonly MainSettingTab DraftTab = new(GamemodeTranslations.Draft.ButtonText, GamemodeTranslations.Draft.Description);
    public static readonly MainSettingTab SpeedrunTab = new(GamemodeTranslations.Speedrun.ButtonText, GamemodeTranslations.Speedrun.Description);

    public static readonly MainSettingTab HNSStandardTab = new(GamemodeTranslations.Standard.ButtonText, GamemodeTranslations.Standard.Description);
    public static readonly MainSettingTab EscapeTab = new(GamemodeTranslations.EscapeTheFacility.ButtonText, GamemodeTranslations.EscapeTheFacility.Description);

    // Standard Tabs
    public static readonly GameOptionTab ImpostorsTab = new(GamemodeTranslations.Standard.ImpostorTab,
        () => LotusAssets.LoadSprite("TabIcons/ImpostorsIconNew.png", 270));
    public static readonly GameOptionTab CrewmateTab = new(GamemodeTranslations.Standard.CrewmateTab,
        () => LotusAssets.LoadSprite("TabIcons/CrewmatesIconNew.png", 270));
    public static readonly GameOptionTab NeutralTab = new(GamemodeTranslations.Standard.NeutralTab,
        () => LotusAssets.LoadSprite("TabIcons/NeutralsIconNew.png", 270));
    public static readonly GameOptionTab MiscTab = new(GamemodeTranslations.Standard.MiscTab,
        () => LotusAssets.LoadSprite("TabIcons/MiscIconNew.png", 270));

    // Escape The Facility Tabs
    public static readonly GameOptionTab WardenTab = new(GamemodeTranslations.EscapeTheFacility.WardenTab,
        () => LotusAssets.LoadSprite("TabIcons/ImpostorsIconNew.png", 270));

    // Extra Tabs
    public static readonly GameOptionTab HiddenTab = new(GamemodeTranslations.Standard.HiddenTab,
        () => LotusAssets.LoadSprite("TabIcons/GeneralIcon.png", 300));

    // Tab Groups
    public static List<GameOptionTab> NormalStandardTabs = [ImpostorsTab, CrewmateTab, NeutralTab, MiscTab];

    public static List<GameOptionTab> HNSStandardTabs = [];
    public static List<GameOptionTab> ETFTabs = [WardenTab];

    // Extra
    public static List<GameOptionTab> EmptyTabs = [];

    static DefaultTabs()
    {
        // GameOptionController.AddTab(GeneralTab);
        // GameOptionController.AddTab(ImpostorsTab);
        // GameOptionController.AddTab(CrewmateTab);
        // GameOptionController.AddTab(NeutralTab);
        // GameOptionController.AddTab(MiscTab);
        // SettingsOptionController.SetMainTab(NormalStandardTab);
    }
}