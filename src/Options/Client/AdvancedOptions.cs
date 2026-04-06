using System;
using Lotus.Extensions;
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
            .AddBoolean(OperatingSystem.IsAndroid())
            .KeyName("Keybinds GUI Toggle", "Keybinds GUI Toggle")
            .Description("Whether to show keybinds button in-game.")
            .BindBool(b =>
            {
                _showKeybindGuiToggle = b;
                defaultManager.DelaySave(0);
            })
            .IOSettings(s => s.UnknownValueAction = ADEAnswer.Allow)
            .BuildAndRegister(defaultManager);
    }
}