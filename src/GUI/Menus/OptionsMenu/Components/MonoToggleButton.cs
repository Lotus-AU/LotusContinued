using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VentLib.Utilities.Attributes;
using Action = System.Action;

namespace Lotus.GUI.Menus.OptionsMenu.Components;

[RegisterInIl2Cpp]
public class MonoToggleButton : MonoBehaviour
{
    private PassiveButton enabledButton;
    private SpriteRenderer enabledRender;
    private PassiveButton disabledButton;
    private SpriteRenderer disabledRender;
    private TextMeshPro enabledTextTMP;
    private TextMeshPro disabledTextTMP;

    private string enabledText = "On";
    private string disabledText = "Off";
    private Action toggleOnAction = () => { };
    private Action toggleOffAction = () => { };

    public bool state;
    public bool runActionOnStart = true;

    public MonoToggleButton()
    {
        PassiveButton template = FindObjectsOfType<PassiveButton>(true).First(pb => pb.name == "CensorChatButton");
        enabledButton = Instantiate(template, transform);
        enabledButton.OnClick = new Button.ButtonClickedEvent();
        enabledButton.OnMouseOut = new UnityEngine.Events.UnityEvent();
        enabledButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
        enabledButton.OnClick.AddListener((Action)(() => SetOffState()));

        enabledRender = enabledButton.GetComponentInChildren<SpriteRenderer>();
        enabledRender.color = Color.white;
        enabledRender.sprite = OptionMenuResources.ButtonOnSprite;

        disabledButton = Instantiate(template, transform);
        disabledButton.OnClick = new Button.ButtonClickedEvent();
        disabledButton.OnMouseOut = new UnityEngine.Events.UnityEvent();
        disabledButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
        disabledButton.OnClick.AddListener((Action)(() => SetOnState()));

        disabledRender = disabledButton.GetComponentInChildren<SpriteRenderer>();
        disabledRender.color = Color.white;
        disabledRender.sprite = OptionMenuResources.ButtonOffSprite;

        disabledButton.activeSprites = null;
        disabledButton.OnMouseOut.AddListener((Action)(() => disabledRender.sprite = OptionMenuResources.ButtonOffSprite));
        disabledButton.OnMouseOver.AddListener((Action)(() => disabledRender.sprite = OptionMenuResources.ButtonOnSprite));

        enabledTextTMP = enabledButton.GetComponentInChildren<TextMeshPro>();
        enabledTextTMP.font = CustomOptionContainer.GetGeneralFont();
        enabledTextTMP.color = new Color(0.11f, 0.51f, 0.6f);
        enabledTextTMP.text = enabledText;
        enabledTextTMP.enableWordWrapping = false;
        enabledTextTMP.transform.localScale = new Vector3(1.3f, 0.8f, enabledTextTMP.transform.localScale.z);

        disabledTextTMP = disabledButton.GetComponentInChildren<TextMeshPro>();
        disabledTextTMP.font = CustomOptionContainer.GetGeneralFont();
        disabledTextTMP.color = new Color(0.34f, 0.36f, 0.37f);
        disabledTextTMP.text = disabledText;
        disabledTextTMP.enableWordWrapping = false;
        disabledTextTMP.transform.localScale = new Vector3(1.3f, 0.8f, disabledTextTMP.transform.localScale.z);

        transform.localScale = new Vector3(0.765f, 1.417f, 1f);
    }

    private void Start() => SetState(state, !runActionOnStart);

    public void Toggle() => SetState(state = !state);

    public void SetState(bool isEnabled, bool noAction = false)
    {
        state = isEnabled;
        if (state) SetOnState(noAction);
        else SetOffState(noAction);
    }

    public void SetTransparency(float transparency)
    {
        if (enabledRender != null)  enabledRender.color  = WithAlpha(enabledRender.color,  transparency);
        if (disabledRender != null) disabledRender.color = WithAlpha(disabledRender.color, transparency);

        if (enabledTextTMP != null)  enabledTextTMP.alpha  = transparency;
        if (disabledTextTMP != null) disabledTextTMP.alpha = transparency;
    }

    private Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    private void SetOnState(bool noAction = false)
    {
        state = true;
        enabledButton.gameObject.SetActive(true);
        disabledButton.gameObject.SetActive(false);
        if (!noAction) toggleOnAction();
    }

    private void SetOffState(bool noAction = false)
    {
        state = false;
        enabledButton.gameObject.SetActive(false);
        disabledButton.gameObject.SetActive(true);
        if (!noAction) toggleOffAction();
    }

    public void SetOnText(string onText) => enabledTextTMP.text = onText;

    public void SetOffText(string offText) => disabledTextTMP.text = offText;

    public void SetToggleOnAction(Action action) => toggleOnAction = action;

    public void SetToggleOffAction(Action action) => toggleOffAction = action;

    public void ConfigureAsPressButton(string text, Action action)
    {
        SetOffText(text);
        toggleOffAction = action;
        toggleOnAction = () => SetOffState();
    }
}

