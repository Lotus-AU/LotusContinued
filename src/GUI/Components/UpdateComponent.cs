using System;
using Lotus.GUI.Menus.OptionsMenu;
using Lotus.GUI.Menus.OptionsMenu.Components;
using Lotus.Utilities;
using TMPro;
using UnityEngine;
using VentLib.Utilities.Attributes;

namespace Lotus.GUI.Components;

[RegisterInIl2Cpp]
public class UpdateComponent : MonoBehaviour
{
    public TextMeshPro UpdateText;
    public LazyProgressBar UpdateProgressBar;
    public MonoToggleButton UpdateButton;

    public Func<Progress<float>> UpdateAction;

    public UpdateComponent(IntPtr intPtr) : base(intPtr)
    {
        GameObject contentGameObject = gameObject.CreateChild("Content", new Vector3(7.7f, -0.85f));
        UpdateText = contentGameObject.AddComponent<TextMeshPro>();
        UpdateText.font = CustomOptionContainer.GetGeneralFont();
        UpdateText.fontSize = 1.6f;

        GameObject progressBarObject = gameObject.CreateChild("ProgressBar", new Vector3(0.6f, 1.55f));
        UpdateProgressBar = progressBarObject.AddComponent<LazyProgressBar>();
        UpdateProgressBar.transform.localScale -= new Vector3(0.4f, 0.3f);
        UpdateProgressBar.gameObject.SetActive(false);

        GameObject updateButtonObject = gameObject.CreateChild("Update Button", new Vector3(1.1f, 1.9f));
        UpdateButton = updateButtonObject.AddComponent<MonoToggleButton>();
        UpdateButton.transform.localScale -= new Vector3(0.4f, 0.7f);
        UpdateButton.SetOffText("V1.30.218");
        UpdateButton.SetToggleOnAction(DoUpdate);
    }

    public void DoUpdate()
    {
        UpdateButton.SetState(false);
        UpdateButton.gameObject.SetActive(false);
        UpdateProgressBar.gameObject.SetActive(true);
        Progress<float> progress = UpdateAction();
        progress.ProgressChanged += (_, f) => UpdateProgressBar.ProgressBar.Value = f;
    }
}