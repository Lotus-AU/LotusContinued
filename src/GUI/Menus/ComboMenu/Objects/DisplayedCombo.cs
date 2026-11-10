using System;
using System.Collections;
using System.IO;
using System.Linq;
using Lotus.Extensions;
using Lotus.GUI.Menus.ComboMenu.Areas;
using Lotus.GUI.Menus.OptionsMenu;
using Lotus.Logging;
using Lotus.Managers;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Utilities;
using TMPro;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.GUI.Menus.ComboMenu.Objects;

[RegisterInIl2Cpp]
public class DisplayedCombo: MonoBehaviour
{
    private PassiveButton removeButton;
    private RoleComboInfo comboInfo;

    private ComboType comboType;
    private CustomRole role1;
    private CustomRole role2;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("UI");
        SpriteRenderer background = gameObject.AddComponent<SpriteRenderer>();
        background.sprite = LotusAssets.LoadSprite("ComboMenu/ComboBgShort.png", 150, true);
    }

    public void Setup(RoleComboInfo info, bool showTrashIcon = true)
    {
        comboInfo = info;

        comboType = (ComboType)info.ComboType;
        role1 = GlobalRoleManager.Instance.AllCustomRoles()
            .FirstOrDefault(r => r.EnglishRoleName == info.Role1EnglishName, EmptyRole.Instance);
        role2 = GlobalRoleManager.Instance.AllCustomRoles()
            .FirstOrDefault(r => r.EnglishRoleName == info.Role2EnglishName, EmptyRole.Instance);
        AddRoleToObject(true);
        AddRoleToObject(false);

        bool isBanned = comboType is ComboType.Banned;
        TextMeshPro typeText = CreateText("ComboTypeText", isBanned ? "Banned" : "Forced", new Vector3(-8.9f, 0f, 0f), 3f,
            gameObject);
        typeText.color = isBanned ? Color.red : Color.cyan;
        typeText.alignment = TextAlignmentOptions.Right;

        if (!showTrashIcon) return;

        PassiveButton dummyButton = FindObjectOfType<ShapeshifterMinigame>(true).BackButton.GetComponent<PassiveButton>();
        removeButton = Instantiate(dummyButton, gameObject.transform);
        removeButton.name = "RemoveButton";
        removeButton.Modify(RemoveCombo);
        removeButton.transform.localScale = new Vector3(.5f, .5f, 1f);
        removeButton.transform.localPosition = new Vector3(1.5f, 0f, -1f);
        removeButton.GetComponent<SpriteRenderer>().sprite = LotusAssets.LoadSprite("Presets/Trash.png", 200f);
    }

    public void RemoveCombo()
    {
        var comboMenu = FindObjectOfType<ComboMenu>();
        comboMenu.GetArea<MainMenuArea>().Close();
        comboMenu.GetArea<AskDeleteArea>().OpenWithCombo(this);
    }

    public void ToggleDeleteButton(bool active) => removeButton.gameObject.SetActive(active);

    private void AddRoleToObject(bool isTop)
    {
        CustomRole targetRole = isTop ? role1 : role2;
        string roleImagePath = targetRole.GetRoleOutfitPath().ToLower().Replace("roleoutfits", "roleimages");
        roleImagePath = Path.ChangeExtension(roleImagePath, "png");
        if (!LotusAssets.TryLoadSprite(roleImagePath, out Sprite? sprite))
            // Replace sprite with Missing one
            sprite = LotusAssets.LoadSprite("RoleImages/Missing.png");

        GameObject roleAnchorObject = gameObject.CreateChild(isTop ? "TopRole" : "BottomRole", isTop ? new Vector3(0f, 0.15f, -1f) : new Vector3(0, -.15f, -1f));
        SpriteRenderer roleImage = roleAnchorObject.QuickComponent<SpriteRenderer>("RoleImage", new Vector3(-1.7f, 0f, -1f), new Vector3(0.3f, .3f));
        roleImage.sprite = sprite;

        TextMeshPro roleNameText = CreateText("RoleNameText", targetRole.RoleName.Trim(), new Vector3(8.475f, 0, -1f), 2f,
            roleAnchorObject);
        roleNameText.color = targetRole.RoleColor;
        roleNameText.alignment = TextAlignmentOptions.Left;
    }

    private static TextMeshPro CreateText(string objectName, string text, Vector3 position, float fontSize, GameObject targetObject)
    {
        // we have to copy previous title because FOR SOME REASON, it will error in this class if we set the font normally. IDK WHY
        ComboMenu comboMenu = FindObjectOfType<ComboMenu>();
        Transform anchorObject = comboMenu.GetArea<MainMenuArea>().FindChild<Transform>("MainMenu", true);
        TextMeshPro dummyText = anchorObject.FindChild("Title_TMP").GetComponent<TextMeshPro>();
        TextMeshPro outputText = Instantiate(dummyText, targetObject.transform);
        outputText.fontSize = outputText.fontSizeMax = outputText.fontSizeMin = fontSize;
        outputText.transform.localPosition = position;
        outputText.name = objectName;
        outputText.text = text;
        return outputText;
    }
}