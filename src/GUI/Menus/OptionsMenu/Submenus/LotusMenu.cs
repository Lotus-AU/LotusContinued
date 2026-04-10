using System;
using TMPro;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities.Attributes;
using Lotus.GUI.Menus.OptionsMenu.Components;
using Lotus.Network.PrivacyPolicy.Patches;
using Lotus.Network.PrivacyPolicy;
using VentLib.Utilities;
using Lotus.Options;

namespace Lotus.GUI.Menus.OptionsMenu.Submenus;

[RegisterInIl2Cpp]
public class LotusMenu : MonoBehaviour, IBaseOptionMenuComponent
{
    private TextMeshPro titleHeader;
    private GameObject anchorObject;

    private MonoToggleButton privacyPolicyLink;
    private MonoToggleButton connectWithAPI;
    private MonoToggleButton anonymousBugReports;

    private MonoToggleButton chatDarkMode;
    private MonoToggleButton keybindsGuiToggle;

    public LotusMenu(IntPtr intPtr) : base(intPtr)
    {
        anchorObject = gameObject.CreateChild("Lotus");
        anchorObject.transform.localPosition = new Vector3(2f, 2f, 0f);
        anchorObject.transform.localScale = new Vector3(1f, 1f, 1);

        GameObject textGameObject = anchorObject.CreateChild("LotusTitle", new Vector3(7.17f, -1.85f, -1), new Vector3(1f, 1f, 1f));
        titleHeader = textGameObject.AddComponent<TextMeshPro>();
        titleHeader.font = CustomOptionContainer.GetGeneralFont();
        titleHeader.fontSize = 5.35f;
        titleHeader.gameObject.layer = LayerMask.NameToLayer("UI");
    }

    private void Start()
    {
        titleHeader.text = "Lotus Settings";
    }

    public void PassMenu(OptionsMenuBehaviour menuBehaviour)
    {
        // ==========================================
        //     Privacy Policy
        // ==========================================
        bool openLink = false; // pl auto runs the action when it is made visible. so we keep track of the first visit to stop this.

        GameObject privacyPolicyLinkObject = new("Privacy Policy Link");
        privacyPolicyLinkObject.transform.SetParent(anchorObject.transform);
        privacyPolicyLinkObject.transform.localScale = new Vector3(1f, 1f, 1f);
        privacyPolicyLink = privacyPolicyLinkObject.AddComponent<MonoToggleButton>();
        privacyPolicyLink.ConfigureAsPressButton("View Privacy Policy", () =>
        {
            if (!openLink)
            {
                openLink = true;
                return;
            }
            Application.OpenURL(PrivacyPolicyPatch.PrivacyPolicyLink);
        });
        privacyPolicyLinkObject.transform.localPosition = new Vector3(-1.995f, 0.353f, -1);

        GameObject anonReportObject = new("Anonymous Bug Reports");
        anonReportObject.transform.SetParent(anchorObject.transform);
        anonReportObject.transform.localScale = new Vector3(1f, 1f, 1f);
        anonymousBugReports = anonReportObject.AddComponent<MonoToggleButton>();
        anonymousBugReports.SetOnText("Anonymous Bug Reports: ON");
        anonymousBugReports.SetOffText("Anonymous Bug Reports: OFF");
        anonymousBugReports.SetToggleOnAction(() => PrivacyPolicyPatch.EditPrivacyPolicy(PrivacyPolicyEditType.AnonymousBugReports, true));
        anonymousBugReports.SetToggleOffAction(() => PrivacyPolicyPatch.EditPrivacyPolicy(PrivacyPolicyEditType.AnonymousBugReports, false));
        anonymousBugReports.SetState(PrivacyPolicyInfo.Instance.AnonymousBugReports);
        anonReportObject.transform.localPosition = new Vector3(2.048f, 0.353f, -1f);
        anonReportObject.SetActive(ProjectLotus.DevVersion);

        GameObject connectAPIObject = new("Connect With API");
        connectAPIObject.transform.SetParent(anchorObject.transform);
        connectAPIObject.transform.localScale = new Vector3(1f, 1f, 1f);
        connectWithAPI = connectAPIObject.AddComponent<MonoToggleButton>();
        connectWithAPI.SetOnText("Connect With API: ON");
        connectWithAPI.SetOffText("Connect With API: Off");
        connectWithAPI.SetToggleOnAction(() =>
        {
            PrivacyPolicyPatch.EditPrivacyPolicy(PrivacyPolicyEditType.ConnectWithAPI, true);
            anonReportObject.SetActive(ProjectLotus.DevVersion);
        });
        connectWithAPI.SetToggleOffAction(() =>
        {
            PrivacyPolicyPatch.EditPrivacyPolicy(PrivacyPolicyEditType.ConnectWithAPI, false);
            anonReportObject.SetActive(false);
        });
        connectWithAPI.SetState(PrivacyPolicyInfo.Instance.ConnectWithAPI);
        connectAPIObject.transform.localPosition = new Vector3(0.029f, 0.353f, -1);

        GameObject darkModeObject = new("Chat Dark Mode");
        darkModeObject.transform.SetParent(anchorObject.transform);
        darkModeObject.transform.localScale = Vector3.one;
        chatDarkMode = darkModeObject.AddComponent<MonoToggleButton>();
        chatDarkMode.SetOnText("Dark Mode: ON");
        chatDarkMode.SetOffText("Dark Mode: OFF");
        chatDarkMode.SetToggleOnAction(() => ClientOptions.VideoOptions.ChatDarkMode = true);
        chatDarkMode.SetToggleOffAction(() => ClientOptions.VideoOptions.ChatDarkMode = false);
        chatDarkMode.SetState(ClientOptions.VideoOptions.ChatDarkMode);
        chatDarkMode.transform.localPosition = new Vector3(-1.995f, -0.328f, -1f);


        GameObject keybindsGuiObject = new("Keybinds GUI Button Toggle");
        keybindsGuiObject.transform.SetParent(anchorObject.transform);
        keybindsGuiObject.transform.localScale = Vector3.one;
        keybindsGuiToggle = keybindsGuiObject.AddComponent<MonoToggleButton>();
        keybindsGuiToggle.SetOnText("Keybinds Menu: ON");
        keybindsGuiToggle.SetOffText("Keybinds Menu: OFF");
        keybindsGuiToggle.SetToggleOnAction(() => ClientOptions.AdvancedOptions.KeybindGuiToggleVisible = true);
        keybindsGuiToggle.SetToggleOffAction(() => ClientOptions.AdvancedOptions.KeybindGuiToggleVisible = false);
        keybindsGuiToggle.SetState(ClientOptions.AdvancedOptions.KeybindGuiToggleVisible);
        keybindsGuiToggle.transform.localPosition = new Vector3(0.029f, -0.328f, -1f);

        /// Done
        anchorObject.SetActive(false);
    }

    public virtual void Open()
    {
        anchorObject.SetActive(true);
        Async.Schedule(() => titleHeader.text = "Lotus Settings", .01f);
        if (titleHeader.color != Color.white)
            Async.Schedule(() =>
            {
                if (titleHeader.color == Color.white) return;
                titleHeader.transform.localPosition = new Vector3(-2.7779f, .6433f, 0f);
                titleHeader.transform.localScale = new Vector3(2f, 2f, 2f);
            }, .01f);
    }

    public virtual void Close()
    {
        anchorObject.SetActive(false);
    }

}