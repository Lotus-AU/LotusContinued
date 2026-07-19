using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.GUI.Components;
using Lotus.GUI.Menus.OptionsMenu;
using Lotus.GUI.Menus.OptionsMenu.Components;
using Lotus.GUI.Patches;
using Lotus.Utilities;
using TMPro;
using Twitch;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.GUI.Menus;

[Localized("GUI")]
[RegisterInIl2Cpp]
public class ModUpdateMenu : MonoBehaviour
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ModUpdateMenu));

    private static List<UpdateItem> _updateItems = new();

    public SpriteRenderer background;
    public TextMeshPro Header;
    public GameObject ClickBlocker;

    public MonoToggleButton ContinueButton;
    public GameObject AnchorObject;

    private bool opened;


    public ModUpdateMenu(IntPtr intPtr) : base(intPtr)
    {
        AnchorObject = gameObject.CreateChild("ModUpdateMenu");
        AnchorObject.transform.localPosition = new Vector3(0f, 0f, -2f);
        background = AnchorObject.CreateChild("Background").AddComponent<SpriteRenderer>();
        background.sprite = OptionMenuResources.ModUpdaterBackgroundSprite;
        background.transform.localScale = new Vector3(2f, 2f);

        GameObject headerGameObject = AnchorObject.CreateChild("Header", new Vector3(8.7f, -0.6f));
        Header = headerGameObject.AddComponent<TextMeshPro>();
        Header.font = CustomOptionContainer.GetGeneralFont();
        Header.fontSize = 3f;

        GameObject continueObject = AnchorObject.CreateChild("Continue", new Vector3(2.12f, -0.9f));
        ContinueButton = continueObject.AddComponent<MonoToggleButton>();
        ContinueButton.SetOffText(ModUpdateMenuTranslations.CloseText);
        ContinueButton.SetToggleOnAction(ProcessClose);
        ContinueButton.gameObject.SetActive(false);

        if (TwitchManager.InstanceExists) // should always exist on main menu but just in case
        {
            GameObject blockerOriginal = TwitchManager.Instance.TwitchPopup.gameObject.transform.Find("BackroundIgnore").gameObject;

            ClickBlocker = Instantiate(blockerOriginal, AnchorObject.transform);
            ClickBlocker.transform.localPosition = new Vector3(0f, 0f, 1f);
        }

        AnchorObject.SetActive(false);

        log.Trace($"Update ready during Mod Menu Creation: {SplashPatch.UpdateReady}", "ModUpdateMenu");
        if (SplashPatch.UpdateReady) Open();
    }

    private void Start()
    {
        Header.text = ModUpdateMenuTranslations.UpdateAvailableText;
    }

    public void Open()
    {
        AnchorObject.SetActive(opened = true);
    }


    private void Update()
    {
        if (!opened) return;
        bool anyUpdating = false;
        bool anyComplete = false;

        _updateItems.ForEach((i, ii) =>
        {
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            if (!i.Loaded) LoadUpdateItem(i, ii);
            if (anyUpdating && anyComplete) return;
            switch (i.UpdateState)
            {
                case UpdateState.Updating when !anyUpdating:
                    anyUpdating = true;
                    ContinueButton.gameObject.SetActive(false);
                    break;
                case UpdateState.Complete when !anyComplete:
                    anyComplete = true;
                    ContinueButton.SetOffText(ModUpdateMenuTranslations.ExitGameText);
                    break;
            }
        });

        if (!anyUpdating) ContinueButton.gameObject.SetActive(true);
    }

    private void ProcessClose()
    {
        ContinueButton.SetState(false);
        bool anyUpdates = _updateItems.Any(i => i.UpdateState is UpdateState.Complete);
        if (anyUpdates) Environment.Exit(0);
        AnchorObject.SetActive(opened = false);
    }


    private void LoadUpdateItem(UpdateItem item, int offset)
    {
        GameObject updateComponentObject = AnchorObject.CreateChild("Update Component");
        UpdateComponent component = updateComponentObject.AddComponent<UpdateComponent>();
        component.gameObject.SetActive(true);
        component.UpdateText.text = item.Name;
        component.UpdateButton.SetOffText(item.Version ?? ModUpdateMenuTranslations.UpdateText);
        component.UpdateAction = item.UpdateFunction;
        component.transform.localPosition += new Vector3(0.5f, -0.6f * (offset + 1));
        item.Loaded = true;
        item.UpdateComponent = component;
        if (item.AutoStartUpdate) component.UpdateButton.SetState(true);
    }


    public static void AddUpdateItem(string name, string? version, UpdateDelegate updateDelegate, bool autoStartUpdate = false)
    {
        UpdateItem item = new() { Name = name, Version = version, AutoStartUpdate = autoStartUpdate };
        _updateItems.Add(item.BindUpdateFunction(updateDelegate));
    }

    private class UpdateItem
    {
        public string Name = null!;
        public string? Version;
        public Action<Exception> ExceptionCallback;
        public Func<Progress<float>> UpdateFunction = null!;
        public UpdateState UpdateState = UpdateState.None;
        public bool Loaded;
        public bool AutoStartUpdate;
        internal UpdateComponent? UpdateComponent;

        public UpdateItem() => ExceptionCallback = HandleException;

        public UpdateItem BindUpdateFunction(UpdateDelegate updateDelegate)
        {
            UpdateFunction = () =>
            {
                Progress<float> bar = updateDelegate(ExceptionCallback);
                UpdateState = UpdateState.Updating;
                bar.ProgressChanged += (_, f) =>
                {
                    if (f >= 1) UpdateState = UpdateState.Complete;
                };
                return bar;
            };
            return this;
        }

        public void HandleException(Exception exception)
        {
            UpdateState = UpdateState.None;
            if (UpdateComponent == null) return;
            UpdateComponent.UpdateButton.gameObject.SetActive(true);
            UpdateComponent.UpdateProgressBar.ProgressBar.Value = 0;
            UpdateComponent.UpdateProgressBar.gameObject.SetActive(false);
        }
    }

    private enum UpdateState
    {
        None,
        Updating,
        Complete
    }

    [Localized("ModUpdateMenu")]
    private static class ModUpdateMenuTranslations
    {
        [Localized(nameof(ExitGameText))]
        public static string ExitGameText = "Exit Game";

        [Localized(nameof(CloseText))]
        public static string CloseText = "Close";

        [Localized(nameof(UpdateText))]
        public static string UpdateText = "Update";

        [Localized(nameof(UpdateAvailableText))]
        public static string UpdateAvailableText = "Updates Available!";

        [Localized(nameof(UpdatesFoundText))]
        public static string UpdatesFoundText = "Updates Found!";
    }
}

public delegate Progress<float> UpdateDelegate(Action<Exception> exception);