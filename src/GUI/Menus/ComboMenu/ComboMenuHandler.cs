using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.GUI.Menus.ComboMenu;

[RegisterInIl2Cpp]
public class ComboMenuHandler: MonoBehaviour
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ComboMenuHandler));

    public ComboMenu ComboMenu => comboMenu;

    private ComboButton comboButton;
    private ComboMenu comboMenu;

    private GameObject anchorObject;

    private void Awake()
    {
        anchorObject = gameObject.CreateChild("Anchor", new Vector3(-1.44f, -0.1f, -1.5f));
        Hooks.GameStateHooks.GameStartHook.Bind(nameof(ComboMenu), _ =>
        {
            if (anchorObject != null) anchorObject.SetActive(false);
            comboMenu.CloseMenu();
            comboButton.HideButton();
        }, true);
    }

    public void Setup(HudManager hudManager)
    {
        log.Debug("Initializing Combo Button.");
        comboButton = anchorObject.AddComponent<ComboButton>();
        comboButton.AddButton(hudManager);
        log.Debug("Initializing Combo Menu.");
        comboMenu = anchorObject.AddComponent<ComboMenu>();
        comboMenu.Setup(hudManager, comboButton, anchorObject);
        anchorObject.GetChildren(true).ForEach(go => go.layer = LayerMask.NameToLayer("UI"));
    }

    public void OpenMenu() => comboMenu.OpenMenu();

    [QuickPostfix(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    public static void CreateButton(LobbyBehaviour __instance)
    {
        if (Game.State is GameState.Roaming or GameState.InMeeting) return; // fixes this patch running when a CNO spawns.
        HudManager instance = HudManager.Instance;
        instance.gameObject.AddComponent<ComboMenuHandler>().Setup(instance);
    }
}