// using Lotus.Extensions;
// using UnityEngine;

// namespace Lotus.RPC.CustomObjects.Builtin;

// public class FakePlayer : CustomNetObject
// {
//     public NetworkedPlayerInfo.PlayerOutfit outfitToCopy;
//     public FakePlayer(NetworkedPlayerInfo.PlayerOutfit outfitToCopy, Vector2 position, byte OwnerId)
//     {
//         this.OwnerId = OwnerId;
//         this.outfitToCopy = outfitToCopy.DeepCopy();
//         CreateNetObject(this.outfitToCopy.PlayerName, position);
//     }
//     public override void SetupPlayer(PlayerControl setupPlayer)
//     {
//         // ReverseEngineeredRPC.RpcChangeSkin(setupPlayer, outfitToCopy, sendToClients: false);
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = outfitToCopy.PlayerName;
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = outfitToCopy.ColorId;
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = outfitToCopy.HatId;
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = outfitToCopy.SkinId;
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = outfitToCopy.PetId;
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = outfitToCopy.VisorId;
//     }
// }