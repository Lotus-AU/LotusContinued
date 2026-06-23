using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Lotus.Extensions;
using Lotus.GUI.Keybinds;
using Lotus.GUI.Menus.ComboMenu.Areas;
using Lotus.GUI.Menus.HistoryMenu2;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.GUI.Menus.ComboMenu;

[RegisterInIl2Cpp]
public class ComboMenu : MonoBehaviour
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ComboMenu));

    private List<IComboMenuArea> allAreas = [];

    private ComboButton comboButton;
    private GameObject deviceObject;
    private GameObject menuObject;

    private bool animationDebounce;
    private bool isOpen;

    public void Setup(HudManager hudManager, MonoBehaviour behaviourButton, GameObject menuAnchorObject)
    {
        this.comboButton = (ComboButton)behaviourButton;
        menuObject = menuAnchorObject.CreateChild("Menu", new Vector3(0f, 0f, -25f));
        Transform background = Instantiate(DestroyableSingleton<AccountManager>.Instance.transform.Find("InfoTextBox/Fill"), menuObject.transform);
        background.GetComponent<BoxCollider2D>().isTrigger = true;
        background.transform.localPosition = Vector3.zero;
        background.name = "ClickBlocker";
        ShapeshifterRole shapeshifterRole = DestroyableSingleton<RoleManager>.Instance.GetRole(RoleTypes.Shapeshifter)
            .Cast<ShapeshifterRole>();
        ShapeshifterMinigame minigameBackground = Instantiate(shapeshifterRole.ShapeshifterMenu, menuObject.transform);
        minigameBackground.transform.localPosition = new Vector3(1.25f, 0f, -1f);
        minigameBackground.BackButton.GetComponent<CloseButtonConsoleBehaviour>().Destroy();
        minigameBackground.BackButton.GetComponent<PassiveButton>().Modify(CloseMenu);
        minigameBackground.name = "Device";

        deviceObject = minigameBackground.FindChild<Transform>("PhoneUI").gameObject;
        deviceObject.name = "Main";
        deviceObject.GetComponentsInChildren<SpriteRenderer>().ForEach(s => s.gameObject.Destroy());
        deviceObject.QuickComponent<SpriteRenderer>("Background", new Vector3(0, 0, .5f), Vector3.one)
            .sprite = LotusAssets.LoadSprite("ComboMenu/ComboMenuBg.png", 168, true);

        log.Debug("Initializing the different menus.");
        MainMenuArea menuArea = deviceObject.AddComponent<MainMenuArea>();
        menuArea.Setup(hudManager, this);
        ChooseRoleArea chooseArea = deviceObject.AddComponent<ChooseRoleArea>();
        chooseArea.Setup(hudManager, this);
        FinalizeArea finalizeArea = deviceObject.AddComponent<FinalizeArea>();
        finalizeArea.Setup(hudManager, this);
        AskDeleteArea askDeleteArea = deviceObject.AddComponent<AskDeleteArea>();
        askDeleteArea.Setup(hudManager, this);
        ChooseComboTypeArea chooseComboTypeArea = deviceObject.AddComponent<ChooseComboTypeArea>();
        chooseComboTypeArea.Setup(hudManager, this);
        log.Debug("All areas have been initialized & set-up.");

        allAreas = [menuArea, chooseArea, finalizeArea, askDeleteArea, chooseComboTypeArea];
        menuObject.gameObject.SetActive(false);
    }

    public T GetArea<T>() where T : MonoBehaviour, IComboMenuArea
    {
        return (T)allAreas.First(a => a.GetType() == typeof(T));
    }

    public void OpenMenu()
    {
        if (isOpen || !comboButton.IsEnabled)
        {
            if (animationDebounce) return;
            animationDebounce = true;
            StartCoroutine(ShakeButton().WrapToIl2Cpp());
            return;
        }
        isOpen = true;
        comboButton.DisableButton();
        HM2 historyButton = FindObjectOfType<HM2>();
        if (historyButton != null && historyButton.Opened()) historyButton.Close();
        if (KeybindHud.Instance?.KeybindMenu.active == true) KeybindHud.Instance.KeybindButton.GetComponent<PassiveButton>().OnClick.Invoke();
        HudManager.Instance.IsIntroDisplayed = true;
        PlayerControl.LocalPlayer.NetTransform.Halt();

        CloseAllAreas();
        GetArea<MainMenuArea>().Open();
        menuObject.gameObject.SetActive(true);
    }

    public void CloseMenu()
    {
        if (!isOpen) return;
        isOpen = false;
        StopAllCoroutines();
        animationDebounce = false;
        comboButton.EnableButton();
        HudManager.Instance.IsIntroDisplayed = false;
        menuObject.gameObject.SetActive(false);
    }

    public void CloseAllAreas()
    {
        allAreas.ForEach(a => a.Close());
    }

    public bool Opened() => isOpen;

    private IEnumerator ShakeButton()
    {
        ReportButton buttonObject = comboButton.GetButton();
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            buttonObject.graphic.transform.localPosition = buttonObject.position + (Vector3)Random.insideUnitCircle * 0.05f;
            yield return null;
        }
        buttonObject.Start();
        animationDebounce = false;
    }
}