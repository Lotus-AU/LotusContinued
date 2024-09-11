using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Reactive;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.GUI.Menus.HistoryMenu2;

[RegisterInIl2Cpp]
public class HM2 : MonoBehaviour
{
    private GameObject anchorObject;
    private GameObject buttonObject;

    private ReportButton historyButton;
    private SpriteRenderer background;

    public ResultsMenu ResultsMenu;

    private List<(PassiveButton passiveButton, IHistoryMenuChild menuChild)> menuTabs = new();

    private bool opened;

    public HM2(IntPtr intPtr) : base(intPtr)
    {
        anchorObject = gameObject.CreateChild("Anchor", new Vector3(-1.44f, -0.1f, -1.5f));
        buttonObject = gameObject.CreateChild("HistoryButton", new Vector3(.15f, -0.175f), new Vector3(0.95f, 0.95f));
        Hooks.GameStateHooks.GameStartHook.Bind(nameof(HM2), _ =>
        {
            if (anchorObject != null) anchorObject.SetActive(false);
            if (buttonObject != null) buttonObject.SetActive(false);
        }, true);
    }

    public void PassHudManager(HudManager hudManager)
    {
        ChatController chatController = hudManager.Chat;

        // =====================
        // Set up History Button
        // =====================
        DevLogger.Log("Setting up history button");
        hudManager.ReportButton.gameObject.SetActive(true);
        historyButton = Instantiate(hudManager.ReportButton, buttonObject.transform);
        historyButton.graphic.sprite = AssetLoader.LoadSprite("Lotus.assets.History.png", 800);
        historyButton.GetComponentInChildren<PassiveButton>().Modify(ToggleMenu);
        historyButton.SetActive(true);
        Async.Schedule(() => historyButton.buttonLabelText.text = "History", 0.05f);

        // ===================
        // Set up Parent Menu
        // ====================
        background = Instantiate(hudManager.Chat.backgroundImage, anchorObject.transform);
        background.flipX = background.flipY = true;
        background.transform.localScale += new Vector3(0.3f, 0f);

        PassiveButton parentButton = chatController.openKeyboardButton.GetComponentInChildren<PassiveButton>();

        ResultsMenu = anchorObject.AddComponent<ResultsMenu>();
        ResultsMenu.PassHudManager(hudManager);
        menuTabs.Add((ResultsMenu.CreateTabButton(parentButton), ResultsMenu));

        CreateTabBehaviours();
        menuTabs[0].menuChild.Open();
        anchorObject.SetActive(false);
    }


    public void ToggleMenu()
    {
        if (opened) Close();
        else Open();
    }

    public void Open()
    {
        HudManager.Instance.SetHudActive(false);
        if (AmongUsClient.Instance.AmHost) GameStartManager.Instance.StartButton.gameObject.SetActive(false);
        else GameStartManager.Instance.StartButtonClient.gameObject.SetActive(false);

        // HudManager.Instance.gameObject.transform.Find("LobbyInfoPane/AspectSize").gameObject.SetActive(false);
        historyButton.SetDisabled();

        HudManager.Instance.IsIntroDisplayed = true;

        opened = true;
        anchorObject.SetActive(true);
    }

    public void Close()
    {
        HudManager.Instance.SetHudActive(true);
        if (AmongUsClient.Instance.AmHost) GameStartManager.Instance.StartButton.gameObject.SetActive(true);
        else GameStartManager.Instance.StartButtonClient.gameObject.SetActive(true);

        HudManager.Instance.gameObject.transform.Find("LobbyInfoPane/AspectSize").gameObject.SetActive(true);
        historyButton.SetEnabled();

        HudManager.Instance.IsIntroDisplayed = false;

        opened = false;
        anchorObject.SetActive(false);
    }

    private void CreateTabBehaviours()
    {
        menuTabs.ForEach((t, i) =>
        {
            t.passiveButton.Modify(() => menuTabs[i].menuChild.Open());
            menuTabs.Where((_, i2) => i2 != i).ForEach(t2 => t2.menuChild.Close());
        });
    }

    [QuickPostfix(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    public static void CreateButton(LobbyBehaviour __instance)
    {
        HudManager Instance = DestroyableSingleton<HudManager>.Instance;
        HM2 historyMenu = Instance.gameObject.AddComponent<HM2>();
        historyMenu.PassHudManager(Instance);
    }
}