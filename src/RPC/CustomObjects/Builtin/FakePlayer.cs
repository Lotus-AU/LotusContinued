using Lotus.Extensions;
using UnityEngine;

namespace Lotus.RPC.CustomObjects.Builtin;

public sealed class FakePlayer : CustomNetObject
{
    public NetworkedPlayerInfo.PlayerOutfit OutfitToCopy;
    public byte OwnerId;

    public FakePlayer(NetworkedPlayerInfo.PlayerOutfit outfitToCopy, Vector2 position, byte ownerId)
    {
        this.OwnerId = ownerId;
        this.OutfitToCopy = outfitToCopy.DeepCopy();
        CreateNetObject(this.OutfitToCopy.PlayerName, position);
    }
    public override void SetupOutfit()
    {
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = OutfitToCopy.PlayerName;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = OutfitToCopy.ColorId;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = OutfitToCopy.HatId;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = OutfitToCopy.SkinId;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = OutfitToCopy.PetId;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = OutfitToCopy.VisorId;
        this.playerControl.RawSetColor(OutfitToCopy.ColorId);
    }
    public override bool CanTarget() => true;
}