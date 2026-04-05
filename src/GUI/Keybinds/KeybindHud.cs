using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.Extensions;
using Lotus.GUI.Menus.ComboMenu;
using Lotus.GUI.Menus.HistoryMenu2;
using Lotus.GUI.Menus.OptionsMenu;
using Lotus.GUI.Menus.OptionsMenu.Components;
using Lotus.Managers.Hotkeys;
using Lotus.Options;
using Lotus.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VentLib.Localization;
using VentLib.Utilities.Extensions;

namespace Lotus.GUI.Keybinds;

// Code based off of https://github.com/All-Of-Us-Mods/LaunchpadReloaded/blob/cc9ca48676a8549fcc6347ed76f3efd659734195/LaunchpadReloaded/Features/NotepadHud.cs
// Credit: xtracube, angxdd

public class KeybindHud
{
    private const string KeybindHudHookKey = nameof(KeybindHudHookKey);

    private const int MaxKeybindsPerRow = 4;
    private const float KeybindXAdd = 2.1f;
    private const float KeybindYAdd = -0.7f;

    public static KeybindHud? Instance {get; private set;}
    public GameObject KeybindButton { get; private set; } = null!;
    public GameObject KeybindMenu { get; private set; } = null!;

    private bool _initialized;
    private readonly HudManager _hud;
    private AspectPosition _buttonAspectPos = null!;

    private Dictionary<Hotkey, MonoToggleButton> _hotkeyToButton;

    public KeybindHud(HudManager hud)
    {
        _hud = hud;
        Instance = this;

        CreateKeybindMenu();
        CreateKeybindButton();
        UpdateAspectPos();
        SetVisible(ClientOptions.AdvancedOptions.KeybindGuiToggleVisible);
        _initialized = true;

        SetupHooks();
    }

    public void UpdateAspectPos()
    {
        _buttonAspectPos.DistanceFromEdge = MeetingHud.Instance || HudManager.Instance.Chat.chatButton.gameObject.active || PlayerControl.LocalPlayer.Data.IsDead
            ? new Vector3(2.75f, 0.505f, -400f) : new Vector3(2.15f, 0.505f, -400f);

        _buttonAspectPos.AdjustPosition();
    }
    public void Destroy()
    {
        _hotkeyToButton = [];
        KeybindButton.gameObject.DestroyImmediate();
        KeybindMenu.gameObject.DestroyImmediate();

        Instance = null;
    }

    public void SetVisible(bool visible)
    {
        KeybindButton.gameObject.SetActive(visible);
        if (KeybindMenu.gameObject.active && !visible) KeybindMenu.gameObject.SetActive(false);
    }

    public void UpdateButtons()
    {
        if (!_initialized && !KeybindMenu.gameObject.active) return;
        foreach(var kvp in _hotkeyToButton)
            kvp.Value.SetTransparency(kvp.Key.PredicatesPass() ? 1f : 0.5f);
    }

    private void CreateKeybindButton()
    {
        KeybindButton = Object.Instantiate(_hud.SettingsButton.gameObject, _hud.SettingsButton.transform.parent);
        KeybindButton.name = "KeybindButton";

        _buttonAspectPos = KeybindButton.GetComponent<AspectPosition>();
        var activeSprite = KeybindButton.transform.FindChild("Active").GetComponent<SpriteRenderer>();
        var inactiveSprite = KeybindButton.transform.FindChild("Inactive").GetComponent<SpriteRenderer>();
        var passiveButton = KeybindButton.GetComponent<PassiveButton>();

        inactiveSprite.sprite = LotusAssets.LoadSprite("KeybindMenu/keybind_inactive.png");
        activeSprite.sprite = LotusAssets.LoadSprite("KeybindMenu/keybind_highlight.png");

        activeSprite.transform.localPosition = inactiveSprite.transform.localPosition = new Vector3(0.005f, 0.025f, 0f);

        passiveButton.ClickSound = _hud.MapButton.ClickSound;
        passiveButton.Modify(() =>
        {
            if (Minigame.Instance) return;

            KeybindMenu.gameObject.SetActive(!KeybindMenu.gameObject.active);

            if (KeybindMenu.gameObject.active)
            {
                inactiveSprite.sprite = LotusAssets.LoadSprite("KeybindMenu/keybind_active.png");
                activeSprite.sprite = LotusAssets.LoadSprite("KeybindMenu/keybind_active.png");
            }
            else
            {
                inactiveSprite.sprite = LotusAssets.LoadSprite("KeybindMenu/keybind_inactive.png");
                activeSprite.sprite = LotusAssets.LoadSprite("KeybindMenu/keybind_highlight.png");
            }

            if (MapBehaviour.Instance) MapBehaviour.Instance.Close();
            if (Game.State is GameState.InLobby) _hud.GetComponent<ComboMenuHandler>().ComboMenu.CloseMenu();
            var historyButton = Object.FindObjectOfType<HM2>();
            if (historyButton != null && historyButton.Opened()) historyButton.Close();

            PlayerControl.LocalPlayer.MyPhysics.SetNormalizedVelocity(Vector2.zero);
        });
    }

    private void CreateKeybindMenu()
    {
        KeybindMenu = _hud.gameObject.CreateChild("KeybindMenu", new Vector3(0, 0, -25f));

        SpriteRenderer backgroundSprite =
            KeybindMenu.QuickComponent<SpriteRenderer>("Background", new Vector3(0, 0, .5f), Vector3.one);
        backgroundSprite.sprite = LotusAssets.LoadSprite("ComboMenu/ComboMenuBg.png", 168, true);

        CreateText("Title_TMP",
                Localizer.Translate("KeybindHud.TitleText", "Keybinds Menu"),
                new Vector3(0f, 2f, 0f),
                4f,
                backgroundSprite.gameObject
                ).alignment = TextAlignmentOptions.Center;

        float curX = -2.2f;
        float curY = 2f;
        int currentPerRow = 0;

        _hotkeyToButton = [];

        HotkeyManager.Hotkeys
            .Where(h => !h.Text.IsNullOrWhiteSpace())
            .Sorted(h => h.Text)
            .ForEach(h =>
            {
                GameObject button = backgroundSprite.gameObject.CreateChild("PressButton", new Vector3(curX, curY, -1f));
                var buttonComponent = button.AddComponent<MonoToggleButton>();
                buttonComponent.ConfigureAsPressButton(h.Text, () =>
                {
                    if (!h.PredicatesPass()) return;
                    h.Call();
                });
                buttonComponent.runActionOnStart = false;
                _hotkeyToButton[h] = buttonComponent;

                currentPerRow++;
                if (currentPerRow == MaxKeybindsPerRow)
                {
                    curX = -2.2f;
                    curY += KeybindYAdd;
                    currentPerRow = 0;
                }
                else curX += KeybindXAdd;
            });

        UpdateButtons();

        KeybindMenu.GetChildren(true).ForEach(go => go.layer = LayerMask.NameToLayer("UI"));
        KeybindMenu.gameObject.SetActive(false);
    }

    private void SetupHooks()
    {
        Hooks.UnbindAll(KeybindHudHookKey);

        Hooks.PlayerHooks.PlayerDeathHook.Bind(KeybindHudHookKey, _ => UpdateAspectPos());
        Hooks.PlayerHooks.PlayerExiledHook.Bind(KeybindHudHookKey, _ => UpdateAspectPos());
        Hooks.PlayerHooks.PlayerRevivedHook.Bind(KeybindHudHookKey, _ => UpdateAspectPos());
        Hooks.GameStateHooks.RoundStartHook.Bind(KeybindHudHookKey, _ => UpdateAspectPos());

        Hooks.GameStateHooks.GameStartHook.Bind(KeybindHudHookKey, _ =>
        {
            if (!KeybindMenu.gameObject.active) return;
            KeybindButton.GetComponent<PassiveButton>().OnClick.Invoke();
        });
    }


    private static TextMeshPro CreateText(string objectName, string text, Vector3 position, float fontSize, GameObject targetObject)
    {
        TextMeshPro outputText = targetObject.QuickComponent<TextMeshPro>(objectName, position);
        outputText.fontSize = outputText.fontSizeMax = outputText.fontSizeMin = fontSize;
        outputText.font = CustomOptionContainer.GetGeneralFont();
        outputText.color = Color.white;
        outputText.text = text;
        return outputText;
    }
}