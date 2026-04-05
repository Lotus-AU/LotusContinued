using System;
using Lotus.GUI.Keybinds;
using Lotus.Server.Patches;
using VentLib.Options;
using VentLib.Options.UI;
using VentLib.Options.IO;

namespace Lotus.Options.Client;

public class AdvancedOptions
{
    public bool KeybindGuiToggleVisible
    {
        get => _showKeybindGuiToggle;
        set
        {
            _showKeybindGuiToggle = value;
            guiToggleOption.SetHardValue(value);

            KeybindHud.Instance?.SetVisible(value);
        }
    }

    private bool _showKeybindGuiToggle;

    private GameOption guiToggleOption;

    public AdvancedOptions()
    {
        OptionManager defaultManager = OptionManager.GetManager(file: "advanced.txt", managerFlags: OptionManagerFlags.IgnorePreset);

        guiToggleOption = new GameOptionBuilder()
            .Key("Keybinds GUI Toggle")
            .AddBoolean(!OperatingSystem.IsWindows())
            .BindBool(b =>
            {
                _showKeybindGuiToggle = b;
                defaultManager.DelaySave(0);
            })
            .IOSettings(s => s.UnknownValueAction = ADEAnswer.UseDefault)
            .BuildAndRegister(defaultManager);
    }
}