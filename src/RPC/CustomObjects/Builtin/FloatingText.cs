using Hazel;
using Lotus.API;
using UnityEngine;

namespace Lotus.RPC.CustomObjects.Builtin;

public class FloatingText: CustomNetObject
{
    // code from MoreGamemodes
    // specifically: https://github.com/Rabek009/MoreGamemodes/blob/master/Modules/CustomObjects/CustomNetObject.cs#L295
    private float PlayerControlOffset => (MapNames)AUSettings.MapId() switch
    {
        MapNames.Skeld => 37.5f,
        MapNames.MiraHQ => Position.y > 10f ? 20f : 40f,
        MapNames.Polus => 40f,
        MapNames.Dleks => 37.5f,
        MapNames.Airship => Position.y > 0f ? 25f : 50f,
        MapNames.Fungle => Position.y > 0f ? 25f : 50f,
        _ => 0f,
    };

    public FloatingText(string text, Vector2 position)
    {
        CreateNetObject(text, position);
    }

    public override void SetupOutfit()
    {
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName =  $"<size={14 + PlayerControlOffset * 25.574f}>\n</size>" + Sprite;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = 255;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = "";
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = "";
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = "";
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = "";
    }

    public override void SnapTo(Vector2 position, SendOption sendOption = SendOption.None)
    {
        base.SnapTo(GetEditedPosition(position), sendOption);
        Position = position;
    }

    protected override void CreateNetObject(string sprite, Vector2 position)
    {
        base.CreateNetObject(sprite, GetEditedPosition(position));
        Position = position;
    }

    private Vector2 GetEditedPosition(Vector2 position) => position + Vector2.up * PlayerControlOffset;
}