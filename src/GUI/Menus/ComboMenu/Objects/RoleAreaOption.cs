using System.IO;
using BepInEx;
using Lotus.Extensions;
using Lotus.GUI.Menus.ComboMenu.Areas;
using Lotus.GUI.Menus.OptionsMenu;
using Lotus.Roles;
using Lotus.Utilities;
using TMPro;
using UnityEngine;
using VentLib.Utilities.Attributes;

namespace Lotus.GUI.Menus.ComboMenu.Objects;

[RegisterInIl2Cpp]
public class RoleAreaOption: MonoBehaviour
{
    private ChooseRoleArea roleArea;
    private PassiveButton inputButton;
    private CustomRole myRole;
    private bool isSelected;

    private SpriteRenderer buttonRenderer;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("UI");
        SpriteRenderer background = gameObject.AddComponent<SpriteRenderer>();
        background.sprite = LotusAssets.LoadSprite("ComboMenu/ComboBg.png", 150, true);
    }

    public void Setup(CustomRole searchedRole, bool selected, MonoBehaviour monoArea)
    {
        this.roleArea = (ChooseRoleArea)monoArea;
        this.isSelected = selected;
        this.myRole = searchedRole;

        string roleImagePath = myRole.GetRoleOutfitPath().ToLower().Replace("roleoutfits", "roleimages");
        roleImagePath = Path.ChangeExtension(roleImagePath, "png");
        if (!LotusAssets.TryLoadSprite(roleImagePath, out Sprite? sprite))
            // Replace sprite with Missing one
            sprite = LotusAssets.LoadSprite("RoleImages/Missing.png");

        SpriteRenderer roleImage = gameObject.QuickComponent<SpriteRenderer>("RoleImage", new Vector3(-1.8f, 0f, -1f), new Vector3(0.5f, .5f));
        roleImage.sprite = sprite;

        TextMeshPro roleNameText = CreateText("RoleNameText", myRole.RoleName.Trim(), new Vector3(8.55f, 0, -1f), 3f, gameObject);
        roleNameText.color = myRole.RoleColor;
        roleNameText.alignment = TextAlignmentOptions.Left;

        TextMeshPro factionText = CreateText("RoleFactionText", myRole.Faction.Name(), new Vector3(-8.3f, 0f, -1f), 1.5f, gameObject);
        factionText.color = myRole.Faction.Color;
        factionText.alignment = TextAlignmentOptions.Right;


        PassiveButton dummyButton = FindObjectOfType<ShapeshifterMinigame>(true).BackButton.GetComponent<PassiveButton>();
        inputButton = Instantiate(dummyButton, gameObject.transform);
        inputButton.name = "InputButton";
        inputButton.transform.localScale = new Vector3(.5f, .5f, 1f);
        inputButton.transform.localPosition = new Vector3(1.9f, 0f, -1f);
        buttonRenderer = inputButton.GetComponent<SpriteRenderer>();
        if (isSelected)
        {
            inputButton.Modify(TryRemoveRole);
            buttonRenderer.sprite = LotusAssets.LoadSprite("Buttons/lotus_x.png", 200f);
        }
        else
        {
            inputButton.Modify(TryAddRole);
            buttonRenderer.sprite = LotusAssets.LoadSprite("Buttons/lotus_checkmark.png", 200f);
        }
    }

    private void TryAddRole()
    {
        isSelected = true;
        inputButton.Modify(TryRemoveRole);
        buttonRenderer.sprite = LotusAssets.LoadSprite("Buttons/lotus_x.png", 200f);
        roleArea.AddRole(myRole);
    }

    private void TryRemoveRole()
    {
        isSelected = false;
        inputButton.Modify(TryAddRole);
        buttonRenderer.sprite = LotusAssets.LoadSprite("Buttons/lotus_checkmark.png", 200f);
        roleArea.RemoveRole(myRole);
    }

    private static TextMeshPro CreateText(string objectName, string text, Vector3 position, float fontSize, GameObject targetObject)
    {
        TextMeshPro outputText = targetObject.QuickComponent<TextMeshPro>(objectName, position);
        outputText.fontSize = outputText.fontSizeMax = outputText.fontSizeMin = fontSize;
        outputText.font = CustomOptionContainer.GetGeneralFont();
        outputText.color = Color.white;
        outputText.text = text;
        return outputText;
    }
}