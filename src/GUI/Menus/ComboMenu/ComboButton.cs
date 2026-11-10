extern alias JBAnnotations;
using System;
using JBAnnotations::JetBrains.Annotations;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Attributes;

namespace Lotus.GUI.Menus.ComboMenu;

[RegisterInIl2Cpp]
[Localized("GUI.ComboMenu")]
public class ComboButton: MonoBehaviour
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ComboButton));
    [Localized("ButtonText")] private static string _buttonText = "Edit Combo";

    public bool IsEnabled { get; private set; }

    private ComboMenuHandler menuHandler;
    private ReportButton comboButton;

    public ReportButton GetButton() => comboButton;
    public void AddButton(HudManager hudManager)
    {
        menuHandler = hudManager.GetComponent<ComboMenuHandler>();
        hudManager.ReportButton.gameObject.SetActive(true);
        comboButton = Instantiate(hudManager.ReportButton, hudManager.transform);
        comboButton.graphic.sprite = LotusAssets.LoadSprite("ComboMenu/Button.png", 130, true);
        comboButton.transform.localPosition = comboButton.position = new Vector3(-4.7f, -2.285f, -1f);
        comboButton.GetComponentInChildren<PassiveButton>().Modify(menuHandler.OpenMenu);
        comboButton.name = "ComboButton";
        comboButton.ToggleVisible(true);
        comboButton.SetEnabled();
        // if (AmongUsClient.Instance.AmHost) EnableButton();
        // else DisableButton();
        EnableButton();
        Async.Schedule(() => comboButton.buttonLabelText.text = _buttonText, 0.05f);
    }

    public void HideButton()
    {
        if (comboButton != null) comboButton.Hide();
    }

    public void DisableButton()
    {
        comboButton.SetDisabled();
        IsEnabled = false;
    }

    public void EnableButton()
    {
        comboButton.SetEnabled();
        IsEnabled = true;
    }
}