using System.IO;
using Lotus.GUI;
using Lotus.Logging;
using Lotus.Roles;
using Lotus.Utilities;
using TMPro;
using UnityEngine;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Options.Patches;

public class RoleOptionImagePatch
{
    private static UnityOptional<Sprite> _placeholderSprite = UnityOptional<Sprite>.Null();
    public static void TryAddImageToSetting(RoleOptionSetting roleOption, AbstractBaseRole role)
    {
        string roleImagePath = role.GetRoleOutfitPath().ToLower().Replace("roleoutfits", "roleimages");
        roleImagePath = Path.ChangeExtension(roleImagePath, "png");
        if (!LotusAssets.TryLoadSprite(roleImagePath, out Sprite? sprite))
            // Replace sprite with Missing one
            sprite = _placeholderSprite.OrElseSet(() => LotusAssets.LoadSprite("RoleImages/Missing.png"));
        SpriteRenderer spriteRenderer = Object.Instantiate(roleOption.FindChild<SpriteRenderer>("Chance %/MinusButton/ButtonSprite"), roleOption.transform);
        spriteRenderer.transform.localPosition = new Vector3(.45f, -0.3f, -1f);
        spriteRenderer.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        spriteRenderer.gameObject.layer = LayerMask.NameToLayer("UI");
        spriteRenderer.gameObject.name = "RoleImage";
        spriteRenderer.sprite = sprite;
        roleOption.FindChild<TextMeshPro>("Title_TMP").transform.localPosition -= new Vector3(0.2f, 0f, 0f);
    }
}