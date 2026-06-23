using System;
using AmongUs.Data;
using Lotus.GUI.Menus.OptionsMenu.Components;
using Lotus.Options;
using TMPro;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using VentLib.Utilities;
using System.Globalization;

namespace Lotus.GUI.Menus.OptionsMenu.Submenus;

[RegisterInIl2Cpp]
public class GeneralMenu : MonoBehaviour, IBaseOptionMenuComponent
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(GeneralMenu));

    private TextMeshPro title;
    private TextMeshPro soundHeader;
    private TextMeshPro controlsText;

    private MonoToggleButton censorChatButton;
    private MonoToggleButton friendInviteButton;
    private MonoToggleButton colorblindTextButton;
    private MonoToggleButton streamerModeButton;

    private MonoToggleButton mouseMovementButton;
    private MonoToggleButton changeKeyBindingButton;
    private MonoToggleButton languageButton;
    private SlideBar mobileJoystickSize;
    private TextMeshPro joystickText;

    private TiledToggleButton controlScheme;
    private LanguageSetter languageSetter;

    // SOUND STUFF
    private SlideBar sfxSlider;
    private SlideBar musicSlider;

    private MonoToggleButton lobbyMusicButton;

    private TextMeshPro musicText;
    private TextMeshPro sfxText;

    private GameObject anchorObject;
    private bool languageSetterExists;

    private FloatRange joystickSizes;

    public GeneralMenu(IntPtr intPtr) : base(intPtr)
    {
        anchorObject = new GameObject();
        anchorObject.transform.SetParent(transform);
        anchorObject.transform.localPosition = new Vector3(2f, 2f, 0f);
        anchorObject.transform.localScale = new Vector3(1f, 1f, 1);
        anchorObject.name = "General";
    }

    private void Start()
    {
        title.text = "General";
        title.gameObject.SetActive(false);
        controlsText.text = "Controls";

        soundHeader.text = "Sounds";
    }

    private void Update()
    {
        if (!languageSetterExists) return;
        if (languageButton != null) languageButton.SetOffText(DataManager.Settings.language.language);
    }

    public void PassMenu(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        GameObject textGameObject = gameObject.CreateChild("GeneralTitle", new Vector3(8, -1.85f, 0));
        title = textGameObject.AddComponent<TextMeshPro>();
        title.font = CustomOptionContainer.GetGeneralFont();
        title.fontSize = 5.35f;
        title.gameObject.layer = LayerMask.NameToLayer("UI");
        soundHeader = Instantiate(title, anchorObject.transform);
        soundHeader.font = CustomOptionContainer.GetGeneralFont();
        soundHeader.transform.localPosition = new Vector3(8.07f, -3.95f, -1);
        soundHeader.transform.localScale = new Vector3(1f, 1f, 1f);
        soundHeader.name = "SoundsTitle";

        languageSetterExists = false;
        GameObject censorGameObject = new("Censor Button");
        censorGameObject.transform.SetParent(anchorObject.transform);
        censorGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        censorChatButton = censorGameObject.AddComponent<MonoToggleButton>();
        censorChatButton.SetOnText("Censor Chat: ON");
        censorChatButton.SetOffText("Censor Chat: OFF");
        censorChatButton.SetToggleOnAction(() => DataManager.Settings.Multiplayer.CensorChat = true);
        censorChatButton.SetToggleOffAction(() => DataManager.Settings.Multiplayer.CensorChat = false);
        censorChatButton.SetState(DataManager.Settings.Multiplayer.CensorChat);
        censorGameObject.transform.localPosition = new Vector3(-1.995f, -3.066f, -1f);

        GameObject fIGameObject = new("Friend & Invite Button");
        fIGameObject.transform.SetParent(anchorObject.transform);
        fIGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        friendInviteButton = fIGameObject.AddComponent<MonoToggleButton>();
        friendInviteButton.SetOnText("Friend & Lobby Invites: ON");
        friendInviteButton.SetOffText("Friend & Lobby Invites: OFF");
        friendInviteButton.SetToggleOnAction(() => DataManager.Settings.Multiplayer.AllowFriendInvites = true);
        friendInviteButton.SetToggleOffAction(() => DataManager.Settings.Multiplayer.AllowFriendInvites = false);
        friendInviteButton.SetState(DataManager.Settings.Multiplayer.AllowFriendInvites);
        fIGameObject.transform.localPosition = new Vector3(2.048f, -3.066f, -1f);

        optionsMenuBehaviour.EnableFriendInvitesButton.gameObject.SetActive(false);

        GameObject colorblindGameObject = new("Colorblind Button");
        colorblindGameObject.transform.SetParent(anchorObject.transform);
        colorblindGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        colorblindTextButton = colorblindGameObject.AddComponent<MonoToggleButton>();
        colorblindTextButton.SetOnText("Colorblind Mode: ON");
        colorblindTextButton.SetOffText("Colorblind Mode: OFF");
        colorblindTextButton.SetToggleOnAction(() => DataManager.Settings.Accessibility.ColorBlindMode = true);
        colorblindTextButton.SetToggleOffAction(() => DataManager.Settings.Accessibility.ColorBlindMode = false);
        colorblindTextButton.SetState(DataManager.Settings.Accessibility.ColorBlindMode);
        colorblindGameObject.transform.localPosition = new Vector3(-1.995f, -3.748f, -1f);
        optionsMenuBehaviour.ColorBlindButton.gameObject.SetActive(false);

        GameObject streamerGameObject = new("Streamer Mode Button");
        streamerGameObject.transform.SetParent(anchorObject.transform);
        streamerGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        streamerModeButton = streamerGameObject.AddComponent<MonoToggleButton>();
        streamerModeButton.SetOnText("Streamer Mode: ON");
        streamerModeButton.SetOffText("Streamer Mode: OFF");
        streamerModeButton.SetToggleOnAction(() => DataManager.Settings.Gameplay.StreamerMode = true);
        streamerModeButton.SetToggleOffAction(() => DataManager.Settings.Gameplay.StreamerMode = false);
        streamerModeButton.SetState(DataManager.Settings.Gameplay.StreamerMode);
        streamerGameObject.transform.localPosition = new Vector3(2.048f, -3.748f, -1f);
        optionsMenuBehaviour.StreamerModeButton.gameObject.SetActive(false);

        GameObject controlGameObject = new("Control Scheme Button");
        controlsText = Instantiate(title, anchorObject.transform);
        // controlsText.transform.localPosition = new Vector3(0, 0, 0);

        controlGameObject.transform.SetParent(anchorObject.transform);
        controlGameObject.transform.localScale = new Vector3(1f, 1f, 1f);

        controlScheme = controlGameObject.AddComponent<TiledToggleButton>();
        controlScheme.SetLeftButtonText(OperatingSystem.IsWindows() ? "Mouse" : "Joystick");
        controlScheme.SetRightButtonText(OperatingSystem.IsWindows() ? "Mouse & Keyboard" : "Tap to Move");
        controlScheme.SetState(DataManager.Settings.input.inputMode is ControlTypes.Keyboard);

        PassiveButton joystickModeButton = optionsMenuBehaviour.gameObject.transform.Find("GeneralTab/ControlGroup/JoystickModeButton").GetComponent<PassiveButton>();
        PassiveButton touchModeButton = optionsMenuBehaviour.gameObject.transform.Find("GeneralTab/ControlGroup/TouchModeButton").GetComponent<PassiveButton>();

        controlScheme.SetToggleOffAction(() => joystickModeButton.ReceiveClickDown());
        controlScheme.SetToggleOnAction(() => touchModeButton.ReceiveClickDown());
        controlGameObject.transform.localPosition = new Vector3(0, 0, 0);

        optionsMenuBehaviour.MouseAndKeyboardOptions.gameObject.SetActive(false);
        optionsMenuBehaviour.MouseAndKeyboardOptions.gameObject.GetComponentsInChildren<Component>(true).ForEach(c => c.gameObject.SetActive(false));


        // ==========================================
        //     Mouse Movement Button
        // ==========================================
        GameObject mouseMovementObject = new("Mouse Movement Button");
        mouseMovementObject.transform.SetParent(anchorObject.transform);
        mouseMovementObject.transform.localScale = new Vector3(1f, 1f, 1f);
        mouseMovementButton = mouseMovementObject.AddComponent<MonoToggleButton>();
        mouseMovementButton.SetOnText("Mouse Movement: ON");
        mouseMovementButton.SetOffText("Mouse Movement: OFF");
        mouseMovementButton.SetToggleOnAction(() => optionsMenuBehaviour.DisableMouseMovement.UpdateText(true));
        mouseMovementButton.SetToggleOffAction(() => optionsMenuBehaviour.DisableMouseMovement.UpdateText(false));
        mouseMovementButton.SetState(optionsMenuBehaviour.DisableMouseMovement.onState);
        mouseMovementObject.transform.localPosition = new Vector3(-0.983f, -0.328f, -1f);
        mouseMovementObject.SetActive(OperatingSystem.IsWindows());

        optionsMenuBehaviour.DisableMouseMovement.gameObject.SetActive(false);

        // ==========================================
        //     Keybinding Button
        // ==========================================
        GameObject keybindingObject = new("Keybinding Button");
        keybindingObject.transform.SetParent(anchorObject.transform);
        keybindingObject.transform.localScale = new Vector3(1f, 1f, 1f);
        changeKeyBindingButton = keybindingObject.AddComponent<MonoToggleButton>();
        changeKeyBindingButton.runActionOnStart = false;
        changeKeyBindingButton.ConfigureAsPressButton("Change Keybindings", () =>
        {
            optionsMenuBehaviour.KeyboardOptions.GetComponentInChildren<PassiveButton>(true).ReceiveClickDown();
        });
        keybindingObject.SetActive(OperatingSystem.IsWindows());
        keybindingObject.transform.localPosition = new Vector3(1.04f, -0.328f, -1f);

        optionsMenuBehaviour.KeyboardOptions.SetActive(false);
        optionsMenuBehaviour.MouseAndKeyboardOptions.GetComponentsInChildren<Component>().ForEach(c => c.gameObject.SetActive(false));

        // ==========================================
        //     Joystick Button
        // ==========================================

        mobileJoystickSize = Instantiate(optionsMenuBehaviour.JoystickSizeSlider, anchorObject.transform);
        mobileJoystickSize.transform.localPosition = new Vector3(-3.85f, -1.2f, 1f);
        mobileJoystickSize.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        mobileJoystickSize.gameObject.GetChildren(true).ForEach(go => go.SetActive(true));

        var joystickTitleText = mobileJoystickSize.GetComponentInChildren<TextMeshPro>();
        joystickTitleText.transform.localPosition += new Vector3(0.7f, 0.25f);

        joystickText = Instantiate(joystickTitleText, mobileJoystickSize.transform);
        joystickText.transform.localPosition += new Vector3(4f, 0.25f);
        mobileJoystickSize.OnValueChange.AddListener((Action)(() =>
        {
            joystickText.text = CalculateJoystickSizePercent(mobileJoystickSize.Value);
            optionsMenuBehaviour.JoystickSizeSlider.SetValue(mobileJoystickSize.Value);
            optionsMenuBehaviour.JoystickSizeSlider.OnValidate();
        }));
        if (OperatingSystem.IsAndroid())
        {
            mobileJoystickSize.enabled = true;
            mobileJoystickSize.SetEnabledColors();
            mobileJoystickSize.Value = optionsMenuBehaviour.JoystickSizes.ReverseLerp(DataManager.Settings.Input.TouchJoystickSize);

            joystickText.gameObject.SetActive(true);
            mobileJoystickSize.gameObject.SetActive(true);
        }
        else
        {
            joystickText.gameObject.SetActive(false);
            mobileJoystickSize.gameObject.SetActive(false);
        }

        joystickSizes = optionsMenuBehaviour.JoystickSizes;

        // ==========================================
        //              SOUND STUFF
        // ==========================================

        musicSlider = Instantiate(optionsMenuBehaviour.MusicSlider, anchorObject.transform);
        musicSlider.transform.localPosition = new Vector3(-3.85f, -2.42f, 1f);
        musicSlider.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        var musicTitleText = musicSlider.GetComponentInChildren<TextMeshPro>();
        musicTitleText.transform.localPosition += new Vector3(0.7f, 0.25f);

        musicText = Instantiate(musicTitleText, musicSlider.transform);
        musicText.transform.localPosition += new Vector3(4f, 0.25f);
        musicSlider.OnValueChange.AddListener((Action)(() =>
        {
            musicText.text = CalculateVolPercent(musicSlider.Value);
            optionsMenuBehaviour.MusicSlider.SetValue(musicSlider.Value);
            optionsMenuBehaviour.MusicSlider.OnValidate();
        }));

        sfxSlider = Instantiate(optionsMenuBehaviour.SoundSlider, anchorObject.transform);
        sfxSlider.transform.localPosition = new Vector3(-3.85f, -3.1f, 1f);
        sfxSlider.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        var sfxTitleText = sfxSlider.GetComponentInChildren<TextMeshPro>();
        sfxTitleText.transform.localPosition += new Vector3(0.7f, 0.25f);

        sfxText = Instantiate(sfxTitleText, sfxSlider.transform);
        sfxText.transform.localPosition += new Vector3(4f, 0.25f);
        sfxSlider.OnValueChange.AddListener((Action)(() =>
        {
            sfxText.text = CalculateVolPercent(sfxSlider.Value);
            optionsMenuBehaviour.SoundSlider.SetValue(sfxSlider.Value);
            optionsMenuBehaviour.SoundSlider.OnValidate();
        }));

        // ==========================================
        //              LOBBY MUSIC
        // ==========================================

        GameObject lobbyGameObject = new("Lobby Music");
        lobbyGameObject.transform.SetParent(anchorObject.transform);
        lobbyGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        lobbyMusicButton = lobbyGameObject.AddComponent<MonoToggleButton>();
        lobbyMusicButton.SetOnText("Lobby Music: DEFAULT");
        lobbyMusicButton.SetOffText("Lobby Music: OFF");
        lobbyMusicButton.SetToggleOnAction(() =>
        {
            ClientOptions.SoundOptions.CurrentSoundType = Options.Client.SoundOptions.SoundTypes.Default;
            if (LobbyBehaviour.Instance == null) return;
            else SoundManager.Instance.CrossFadeSound("MapTheme", LobbyBehaviour.Instance.MapTheme, 0.5f, 1.5f);
        });
        lobbyMusicButton.SetToggleOffAction(() =>
        {
            ClientOptions.SoundOptions.CurrentSoundType = Options.Client.SoundOptions.SoundTypes.Off;
            if (LobbyBehaviour.Instance == null) return;
            else SoundManager.Instance.StopNamedSound("MapTheme");
        });
        lobbyMusicButton.SetState(ClientOptions.SoundOptions.CurrentSoundType == Options.Client.SoundOptions.SoundTypes.Default);
        lobbyGameObject.transform.localPosition = new Vector3(0.029f, -3.066f, -1f);

        // ==========================================
        //               Language Button
        // ==========================================
        LanguageSetter languageSetterPrefab = FindObjectOfType<LanguageSetter>(true);
        if (languageSetterPrefab == null) return;

        languageSetter = Instantiate(languageSetterPrefab, anchorObject.transform);
        languageSetter.transform.localPosition -= new Vector3(2.5f, 1.88f);
        languageSetter.FindChild<Transform>("Backdrop").localScale = new Vector3(20, 10, 0);

        GameObject languageButtonObject = anchorObject.CreateChild("Language Button", new Vector3(0.029f, -3.748f, -1));
        languageButton = languageButtonObject.AddComponent<MonoToggleButton>();
        languageButton.SetOffText(DataManager.Settings.language.language);
        languageButton.SetToggleOnAction(() =>
        {
            languageButton.SetState(false);
            languageSetter.Open();
        });
        languageSetterExists = true;

    }


    public void Open()
    {
        // log.Fatal("Opening!!");
        anchorObject.SetActive(true);
        Async.Schedule(() =>
        {
            joystickText.text = CalculateJoystickSizePercent(mobileJoystickSize.Value);
            musicText.text = CalculateVolPercent(musicSlider.Value);
            sfxText.text = CalculateVolPercent(sfxSlider.Value);
        }, 0.000001f);
        if (soundHeader.color != Color.white)
            Async.Schedule(() =>
            {
                if (soundHeader.color == Color.white) return;
                soundHeader.transform.localPosition = new Vector3(-2f, -1.4f, 0f);
                soundHeader.transform.localScale = new Vector3(2f, 2f, 2f);
            }, .01f);
    }

    public void Close()
    {
        anchorObject.SetActive(false);
    }
    private static string CalculateVolPercent(float value)
    {
        return Math.Round(value * 100, 0).ToString(CultureInfo.InvariantCulture) + "%";
    }
    private string CalculateJoystickSizePercent(float value)
    {
        return Math.Round(joystickSizes.Lerp(value) * 100, 0).ToString(CultureInfo.InvariantCulture) + "%";
    }
}