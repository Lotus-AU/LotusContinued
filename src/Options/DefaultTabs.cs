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
    public static MainSettingTab StandardTab = new(GamemodeTranslations.Standard.ButtonText, GamemodeTranslations.Standard.Description);
    public static MainSettingTab CaptureTab = new(GamemodeTranslations.CaptureTheFlag.ButtonText, GamemodeTranslations.CaptureTheFlag.Description);
    public static MainSettingTab ColorwarsTab = new(GamemodeTranslations.Colorwars.ButtonText, GamemodeTranslations.Colorwars.Description);
    public static MainSettingTab DraftTab = new(GamemodeTranslations.Draft.ButtonText, GamemodeTranslations.Draft.Description);

    public static GameOptionTab ImpostorsTab = new(GamemodeTranslations.Standard.ImpostorTab, () => LotusAssets.LoadSprite("TabIcons/ImpostorsIconNew.png", 270));

    public static GameOptionTab CrewmateTab = new(GamemodeTranslations.Standard.CrewmateTab, () => LotusAssets.LoadSprite("TabIcons/CrewmatesIconNew.png", 270));

    public static GameOptionTab NeutralTab = new(GamemodeTranslations.Standard.NeutralTab, () => LotusAssets.LoadSprite("TabIcons/NeutralsIconNew.png", 270));

    public static GameOptionTab MiscTab = new(GamemodeTranslations.Standard.MiscTab, () => LotusAssets.LoadSprite("TabIcons/MiscIconNew.png", 270));

    public static GameOptionTab HiddenTab = new(GamemodeTranslations.Standard.HiddenTab, () => LotusAssets.LoadSprite("TabIcons/GeneralIcon.png", 300));

    public static List<GameOptionTab> StandardTabs = new() { ImpostorsTab, CrewmateTab, NeutralTab, MiscTab };
    public static List<GameOptionTab> CaptureTabs = new();
    public static List<GameOptionTab> ColorwarsTabs = new();

    static DefaultTabs()
    {
        // GameOptionController.AddTab(GeneralTab);
        // GameOptionController.AddTab(ImpostorsTab);
        // GameOptionController.AddTab(CrewmateTab);
        // GameOptionController.AddTab(NeutralTab);
        // GameOptionController.AddTab(MiscTab);
        SettingsOptionController.SetMainTab(StandardTab);
    }
}