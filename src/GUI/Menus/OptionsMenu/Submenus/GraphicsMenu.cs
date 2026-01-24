using System;
using AmongUs.Data;
using Lotus.GUI.Menus.OptionsMenu.Components;
using Lotus.Options;
using Lotus.Options.Client;
using Lotus.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VentLib.Utilities;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using static Lotus.Utilities.GameObjectUtils;

namespace Lotus.GUI.Menus.OptionsMenu.Submenus;

[RegisterInIl2Cpp]
public class GraphicsMenu : MonoBehaviour, IBaseOptionMenuComponent
{
    private TextMeshPro graphicsTitle;
    private MonoToggleButton fullscreenButton;
    private MonoToggleButton vsyncButton;
    private MonoToggleButton screenshakeButton;
    private MonoToggleButton applyButton;
    private SlideBar resolutionSlider;
    private SlideBar fpsSlider;
    private TextMeshPro fpsText;
    private TextMeshPro resolutionText;

    private OptionsMenuBehaviour optionsMenuBehaviour;
    private TabGroup tab;
    private GameObject anchor;

    private bool opening;
    private int temporaryResolutionIndex;
    private int temporaryFps;

    public GraphicsMenu(IntPtr intPtr) : base(intPtr)
    {
        anchor = CreateGameObject("Graphics", transform);
        anchor.transform.localPosition += new Vector3(2f, 2f);
        anchor.transform.localScale = new Vector3(1f, 1f, 1);

        GameObject textGameObject = anchor.CreateChild("Title", new Vector3(8.6f, -1.8f));
        graphicsTitle = textGameObject.AddComponent<TextMeshPro>();
        graphicsTitle.font = CustomOptionContainer.GetGeneralFont();
        graphicsTitle.fontSize = 5.35f;
        graphicsTitle.transform.localPosition = new Vector3(8.15f, -1.85f, 0);
        graphicsTitle.gameObject.layer = LayerMask.NameToLayer("UI");
    }

    public void PassMenu(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        this.optionsMenuBehaviour = optionsMenuBehaviour;
        tab = optionsMenuBehaviour.Tabs[1];

        ResolutionSlider graphicsContent = tab.Content.GetComponentInChildren<ResolutionSlider>(true);


        GameObject applyGameObject = anchor.CreateChild("Apply Button", new Vector3(2.048f, 0.353f));
        applyButton = applyGameObject.AddComponent<MonoToggleButton>();
        applyButton.ConfigureAsPressButton("Apply", () =>
        {
            ResolutionUtils.ResolutionIndex = temporaryResolutionIndex;
            SetResolution(fullscreenButton.state);

            Application.targetFrameRate = ClientOptions.VideoOptions.TargetFps = temporaryFps;
            SetFpsText();
            applyGameObject.SetActive(false);
        });
        applyGameObject.SetActive(false);


        GameObject fullscreenGameObject = anchor.CreateChild("Fullscreen Button", new Vector3(-1.995f, -1.014f, -1f));
        fullscreenButton = fullscreenGameObject.AddComponent<MonoToggleButton>();
        fullscreenButton.SetOnText("Fullscreen: ON");
        fullscreenButton.SetOffText("Fullscreen: OFF");
        fullscreenButton.SetState(Screen.fullScreen);
        fullscreenButton.SetToggleOnAction(() => applyGameObject.SetActive(!opening));
        fullscreenButton.SetToggleOffAction(() => applyGameObject.SetActive(!opening));
        graphicsContent.Fullscreen.gameObject.SetActive(false);


        resolutionSlider = Instantiate(graphicsContent.slider, anchor.transform);
        resolutionSlider.transform.localPosition = new Vector3(-2.62f, -0.5f, -1);
        resolutionSlider.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        resolutionSlider.GetComponentInChildren<TextMeshPro>().transform.localPosition = new Vector3(-0.4174f, 0.231f, -1);

        resolutionText = Instantiate(graphicsContent.FindChild<TextMeshPro>("ResolutionText_TMP"), anchor.transform);
        resolutionText.transform.localPosition = new Vector3(-1.02f, -0.2f, 0);

        resolutionSlider.OnValueChange = new UnityEvent();
        resolutionSlider.OnValueChange.AddListener((Action)(() =>
        {
            int resolutionCount = ResolutionUtils.AllResolutions.Length - 1;
            temporaryResolutionIndex = Mathf.Clamp(Mathf.FloorToInt(resolutionSlider.Value * resolutionCount), 0, resolutionCount);
            (int width, int height) = ResolutionUtils.AllResolutions[temporaryResolutionIndex];
            resolutionText.text = $"{width} x {height}";
            if (!opening) applyGameObject.SetActive(true);
        }));
        graphicsContent.slider.gameObject.SetActive(false);
        #if ANDROID
        resolutionSlider.gameObject.SetActive(false);
        resolutionText.gameObject.SetActive(false);
        #endif


        fpsSlider = Instantiate(resolutionSlider, anchor.transform);
        fpsSlider.transform.localPosition = new Vector3(-2.62f, -1.12f, 0);
        fpsSlider.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        TextMeshPro fpsTextLabel = fpsSlider.GetComponentInChildren<TextMeshPro>();
        fpsTextLabel.transform.localPosition = new Vector3(-0.4174f, 0.231f, -1);

        fpsSlider.OnValueChange = new UnityEvent();
        fpsSlider.OnValueChange.AddListener((Action)(() =>
        {
            int index = Mathf.RoundToInt(fpsSlider.Value * 7);
            int fps = (int)VideoOptions.FpsLimits[index];
            temporaryFps = fps;
            SetFpsText();
            if (!opening) applyGameObject.SetActive(true);
        }));
        fpsText = Instantiate(resolutionText, anchor.transform);
        fpsText.transform.localPosition = new Vector3(1, -1, 0);
        fpsText.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        GameObject vsyncObject = anchor.CreateChild("VSync Button", new Vector3(.029f, -1.014f, -1f));
        vsyncButton = vsyncObject.AddComponent<MonoToggleButton>();
        vsyncButton.SetOnText("VSync: ON");
        vsyncButton.SetOffText("VSync: OFF");
        vsyncButton.SetToggleOnAction(() =>
        {
            DataManager.Settings.Video.VSync = true;
            QualitySettings.vSyncCount = 1;
        });
        vsyncButton.SetToggleOffAction(() =>
        {
            DataManager.Settings.Video.VSync = false;
            QualitySettings.vSyncCount = 0;
        });
        vsyncButton.SetState(DataManager.Settings.Video.VSync);
        graphicsContent.VSync.gameObject.SetActive(false);



        GameObject screenShake = anchor.CreateChild("Screenshake Button", new Vector3(2.048f, -1.014f, -1f));
        screenshakeButton = screenShake.AddComponent<MonoToggleButton>();
        screenshakeButton.SetOnText("Screenshake: ON");
        screenshakeButton.SetOffText("Screenshake: OFF");
        screenshakeButton.SetToggleOnAction(() =>
        {
            DataManager.Settings.Gameplay.ScreenShake = true;
            DataManager.Settings.Save();
        });
        screenshakeButton.SetToggleOffAction(() =>
        {
            DataManager.Settings.Gameplay.ScreenShake = false;
            DataManager.Settings.Save();
        });
        screenshakeButton.SetState(DataManager.Settings.Gameplay.ScreenShake);
        graphicsContent.Screenshake.gameObject.SetActive(false);


        graphicsContent.FindChild<PassiveButton>("ApplyButton").gameObject.SetActive(false);
        graphicsContent.Fullscreen.gameObject.SetActive(false);
        anchor.gameObject.SetActive(false);
    }

    private void SetResolution(bool fullscreen)
    {
        (int width, int height) = ResolutionUtils.AllResolutions[ResolutionUtils.ResolutionIndex];
        ResolutionManager.SetResolution(width, height, fullscreen);
    }

    private void SetFpsText()
    {
        int targetFps = temporaryFps;
        string fpsString = targetFps != int.MaxValue ? targetFps.ToString() : "Uncapped";
        fpsText.text = $"{fpsString}";
    }

    private void Start()
    {
        fpsSlider.GetComponentInChildren<TextMeshPro>().text = "Max Framerate";
        graphicsTitle.text = "Display";
    }

    public virtual void Open()
    {
        opening = true;

        temporaryResolutionIndex = ResolutionUtils.ResolutionIndex;
        temporaryFps = ClientOptions.VideoOptions.TargetFps;

        tab.Content.gameObject.SetActive(true);
        anchor.gameObject.SetActive(true);
        applyButton.gameObject.SetActive(false);
        resolutionSlider.SetValue(ResolutionUtils.ResolutionIndex / (ResolutionUtils.AllResolutions.Length - 1));
        resolutionSlider.OnValidate();

        int fpsIndex = VideoOptions.FpsLimits.IndexOf(i => ClientOptions.VideoOptions.TargetFps == (int)i);
        fpsSlider.SetValue((fpsIndex != -1 ? fpsIndex : VideoOptions.FpsLimits.Length - 1) / 7f);


        applyButton.gameObject.SetActive(false);

        SetFpsText();
        Async.Schedule(() =>
        {
            fpsSlider.GetComponentInChildren<TextMeshPro>().text = "Max Framerate";
            (int width, int height) = ResolutionUtils.AllResolutions[ResolutionUtils.ResolutionIndex];
            resolutionText.text = $"{width} x {height}";
        }, 0.0001f);
        Async.Schedule(() => opening = false, 0.1f);
    }

    public virtual void Close()
    {
        tab.Content.gameObject.SetActive(false);
        anchor.gameObject.SetActive(false);
    }
}