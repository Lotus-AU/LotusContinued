using System.Collections.Generic;
using Lotus.Options.General;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Attributes;
using VentLib.Options;
using System.Linq;
using VentLib.Utilities.Extensions;

namespace Lotus.Options;

[Localized(ModConstants.Options)]
[LoadStatic]
public static class GeneralOptions
{
    public static readonly OptionManager StandardOptionManager = OptionManager.GetManager(file: "standard.txt", managerFlags: OptionManagerFlags.SyncOverRpc);
    public static readonly OptionManager CaptureOptionManager = OptionManager.GetManager(file: "ctf.txt", managerFlags: OptionManagerFlags.SyncOverRpc);
    public static readonly OptionManager ColorwarsOptionManager = OptionManager.GetManager(file: "colorwars.txt", managerFlags: OptionManagerFlags.SyncOverRpc);

    public static readonly AdminOptions AdminOptions;
    public static readonly DebugOptions DebugOptions;
    public static readonly GameplayOptions GameplayOptions;
    public static readonly MayhemOptions MayhemOptions;
    public static readonly MeetingOptions MeetingOptions;
    public static readonly MiscellaneousOptions MiscellaneousOptions;
    public static readonly SabotageOptions SabotageOptions;

    public static readonly List<GameOption> StandardOptions = new();

    static GeneralOptions()
    {
        AdminOptions = new AdminOptions();
        // StandardOptions.AddRange(AdminOptions.AllOptions);

        GameplayOptions = new GameplayOptions();
        GameplayOptions.AddTabListener(DefaultTabs.NormalStandardTab, () => 2 + AdminOptions.AllOptions.Count);

        SabotageOptions = new SabotageOptions();
        SabotageOptions.AddTabListener(DefaultTabs.NormalStandardTab, () => 2 + AdminOptions.AllOptions.Count + SabotageOptions.AllOptions.Count);

        MeetingOptions = new MeetingOptions();
        MeetingOptions.AddTabListener(DefaultTabs.NormalStandardTab,
            () => 2 + AdminOptions.AllOptions.Count + SabotageOptions.AllOptions.Count + SabotageOptions.AllOptions.Count);

        MayhemOptions = new MayhemOptions();
        MayhemOptions.AddTabListener(DefaultTabs.NormalStandardTab,
            () => 2 + AdminOptions.AllOptions.Count + SabotageOptions.AllOptions.Count + SabotageOptions.AllOptions.Count);
        MiscellaneousOptions = new MiscellaneousOptions();
        // MiscellaneousOptions.AddTabListener(DefaultTabs.StandardTab);

        DebugOptions = new DebugOptions();
        // DebugOptions.AddTabListener(DefaultTabs.StandardTab);

        RoleOptions.LoadMadmateOptions().AddTabListener(DefaultTabs.NormalStandardTab,
            () => 2 + AdminOptions.AllOptions.Count + SabotageOptions.AllOptions.Count + SabotageOptions.AllOptions.Count + MayhemOptions.AllOptions.Count );
        RoleOptions.LoadNeutralOptions().AddTabListener(DefaultTabs.NormalStandardTab,
            () => 2 + AdminOptions.AllOptions.Count + SabotageOptions.AllOptions.Count + SabotageOptions.AllOptions.Count + MayhemOptions.AllOptions.Count
                  + RoleOptions.LoadMadmateOptions().AllOptions.Count);
        RoleOptions.LoadSubroleOptions().AddTabListener(DefaultTabs.NormalStandardTab,
            () => 2 + AdminOptions.AllOptions.Count + SabotageOptions.AllOptions.Count + SabotageOptions.AllOptions.Count + MayhemOptions.AllOptions.Count
                  + RoleOptions.LoadMadmateOptions().AllOptions.Count + RoleOptions.LoadNeutralOptions().AllOptions.Count);
    }
}