// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using HarmonyLib;
// using Hazel;
// using InnerNet;
// using Lotus.API.Player;
// using Lotus.Extensions;
// using TMPro;
// using UnityEngine;
// using VentLib.Networking.RPC;
// using VentLib.Utilities;
// using VentLib.Utilities.Extensions;
// using Object = UnityEngine.Object;

// // Credit: https://github.com/Rabek009/MoreGamemodes/blob/e054eb498094dfca0a365fc6b6fea8d17f9974d7/Modules/CustomObjects
// // Huge thanks to Rabek009 for this code!

// namespace Lotus.RPC.CustomObjects;

// public class CustomNetObject
// {
//     private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(CustomNetObject));
//     public static readonly List<CustomNetObject> AllObjects = [];
//     private static int MaxId = -1;
//     private readonly HashSet<byte> HiddenList = [];
//     protected int Id;
//     public PlayerControl playerControl = null!;
//     private float PlayerControlTimer;
//     public Vector2 Position;
//     protected byte OwnerId;

//     public string Sprite = null!;

//     public virtual void SetupPlayer(PlayerControl setupPlayer)
//     {
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = "<size=14><br></size>" + this.Sprite;
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = 255;
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = "";
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = "";
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = "";
//         setupPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = "";
//     }

//     public void RpcChangeSprite(string sprite)
//     {
//         log.Info($"Change Custom Net Object {GetType().Name} (ID {Id}) sprite (RpcChangeSprite)");
//         Sprite = sprite;
//         Async.Schedule(() =>
//         {
//             playerControl.RawSetName(sprite);
//             var name = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName;
//             var colorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId;
//             var hatId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId;
//             var skinId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId;
//             var petId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId;
//             var visorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId;
//             MessageWriter writer = MessageWriter.Get();
//             writer.StartMessage(5);
//             writer.Write(AmongUsClient.Instance.GameId);
//             SetupPlayer(PlayerControl.LocalPlayer);
//             writer.StartMessage(1);
//             {
//                 writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
//                 PlayerControl.LocalPlayer.Data.Serialize(writer, false);
//             }
//             writer.EndMessage();
//             writer.StartMessage(2);
//             {
//                 writer.WritePacked(playerControl.NetId);
//                 writer.Write((byte)RpcCalls.Shapeshift);
//                 writer.WriteNetObject(PlayerControl.LocalPlayer);
//                 writer.Write(false);
//             }
//             writer.EndMessage();
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = name;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = colorId;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = hatId;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = skinId;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = petId;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = visorId;
//             writer.StartMessage(1);
//             {
//                 writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
//                 PlayerControl.LocalPlayer.Data.Serialize(writer, false);
//             }
//             writer.EndMessage();
//             writer.EndMessage();
//             AmongUsClient.Instance.SendOrDisconnect(writer);
//             writer.Recycle();
//         }, 0f);
//     }

//     public void TP(Vector2 position)
//     {
//         playerControl.NetTransform.RpcSnapTo(position);
//         Position = position;
//     }

//     public void Despawn()
//     {
//         log.Info($"Despawn Custom Net Object {GetType().Name} (ID {Id}) (Despawn)");
//         playerControl.Despawn();
//         AllObjects.Remove(this);
//     }

//     protected void Hide(PlayerControl player)
//     {
//         log.Info($"Hide Custom Net Object {GetType().Name} (ID {Id}) from {player.GetNameWithRole()} (Hide)");

//         HiddenList.Add(player.PlayerId);
//         if (player.AmOwner)
//         {
//             Async.Schedule(() => playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(false), 0.1f);
//             playerControl.Visible = false;
//             return;
//         }

//         Async.Schedule(() =>
//         {
//             // CustomRpcSender sender = CustomRpcSender.Create("FixModdedClientCNOText", sendOption: SendOption.Reliable);
//             // sender.AutoStartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.FixModdedClientCNO, player.GetClientId())
//             //     .WriteNetObject(playerControl)
//             //     .Write(false)
//             //     .EndRpc();
//             // sender.SendMessage();
//         }, 0.4f);

//         MessageWriter writer = MessageWriter.Get();
//         writer.StartMessage(6);
//         writer.Write(AmongUsClient.Instance.GameId);
//         writer.WritePacked(player.GetClientId());
//         writer.StartMessage(5);
//         writer.WritePacked(playerControl.NetId);
//         writer.EndMessage();
//         writer.EndMessage();
//         AmongUsClient.Instance.SendOrDisconnect(writer);
//         writer.Recycle();
//     }

//     protected virtual void OnFixedUpdate()
//     {
//         PlayerControlTimer += Time.fixedDeltaTime;
//         if (PlayerControlTimer > 20f)
//         {
//             log.Info($"Recreate Custom Net Object {this.GetType().Name} (ID {Id}) (OnFixedUpdate)");
//             PlayerControl oldPlayerControl = playerControl;
//             playerControl = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
//             playerControl.PlayerId = 255;
//             playerControl.isNew = false;
//             playerControl.notRealPlayer = true;
//             AmongUsClient.Instance.NetIdCnt += 1U;
//             MessageWriter msg = MessageWriter.Get();
//             msg.StartMessage(5);
//             msg.Write(AmongUsClient.Instance.GameId);
//             AmongUsClient.Instance.WriteSpawnMessage(playerControl, -2, SpawnFlags.None, msg);
//             msg.EndMessage();
//             msg.StartMessage(6);
//             msg.Write(AmongUsClient.Instance.GameId);
//             msg.WritePacked(int.MaxValue);
//             for (uint i = 1; i <= 3; ++i)
//             {
//                 msg.StartMessage(4);
//                 msg.WritePacked(2U);
//                 msg.WritePacked(-2);
//                 msg.Write((byte)SpawnFlags.None);
//                 msg.WritePacked(1);
//                 msg.WritePacked(AmongUsClient.Instance.NetIdCnt - i);
//                 msg.StartMessage(1);
//                 msg.EndMessage();
//                 msg.EndMessage();
//             }

//             msg.EndMessage();
//             AmongUsClient.Instance.SendOrDisconnect(msg);
//             msg.Recycle();
//             if (PlayerControl.AllPlayerControls.Contains(playerControl))
//                 PlayerControl.AllPlayerControls.Remove(playerControl);
//             Async.Schedule(() =>
//             {
//                 playerControl.NetTransform.RpcSnapTo(Position);
//                 playerControl.RawSetName(Sprite);
//                 var name = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName;
//                 var colorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId;
//                 var hatId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId;
//                 var skinId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId;
//                 var petId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId;
//                 var visorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId;
//                 MessageWriter writer = MessageWriter.Get();
//                 writer.StartMessage(5);
//                 writer.Write(AmongUsClient.Instance.GameId);
//                 SetupPlayer(PlayerControl.LocalPlayer);
//                 writer.StartMessage(1);
//                 {
//                     writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
//                     PlayerControl.LocalPlayer.Data.Serialize(writer, false);
//                 }
//                 writer.EndMessage();
//                 writer.StartMessage(2);
//                 {
//                     writer.WritePacked(playerControl.NetId);
//                     writer.Write((byte)RpcCalls.Shapeshift);
//                     writer.WriteNetObject(PlayerControl.LocalPlayer);
//                     writer.Write(false);
//                 }
//                 writer.EndMessage();
//                 PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = name;
//                 PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = colorId;
//                 PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = hatId;
//                 PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = skinId;
//                 PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = petId;
//                 PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = visorId;
//                 writer.StartMessage(1);
//                 {
//                     writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
//                     PlayerControl.LocalPlayer.Data.Serialize(writer, false);
//                 }
//                 writer.EndMessage();
//                 writer.EndMessage();
//                 AmongUsClient.Instance.SendOrDisconnect(writer);
//                 writer.Recycle();
//             }, 0.2f);
//             Async.Schedule(() => oldPlayerControl.Despawn(), 0.3f);
//             //playerControl.cosmetics.currentBodySprite.BodySprite.color = Color.clear;
//             //playerControl.cosmetics.colorBlindText.color = Color.clear;
//             Players.GetAllPlayers().ForEach(pc =>
//             {
//                 if (pc.AmOwner) return;
//                 Async.Schedule(() =>
//                 {
//                     MessageWriter writer = MessageWriter.Get();
//                     writer.StartMessage(6);
//                     writer.Write(AmongUsClient.Instance.GameId);
//                     writer.WritePacked(pc.GetClientId());
//                     writer.StartMessage(1);
//                     {
//                         writer.WritePacked(playerControl.NetId);
//                         writer.Write(pc.PlayerId);
//                     }
//                     writer.EndMessage();
//                     writer.StartMessage(2);
//                     {
//                         writer.WritePacked(playerControl.NetId);
//                         writer.Write((byte)RpcCalls.MurderPlayer);
//                         writer.Write((int)MurderResultFlags.FailedError);
//                     }
//                     writer.EndMessage();
//                     writer.StartMessage(1);
//                     {
//                         writer.WritePacked(playerControl.NetId);
//                         writer.Write((byte)255);
//                     }
//                     writer.EndMessage();
//                     writer.EndMessage();
//                     AmongUsClient.Instance.SendOrDisconnect(writer);
//                     writer.Recycle();
//                 }, 0.1f);
//             });
//             Players.GetAllPlayers().ForEach(pc =>
//             {
//                 if (HiddenList.Contains(pc.PlayerId)) Hide(pc);
//             });

//             Async.Schedule(() =>
//             {
//                 // Fix for Host
//                 if (!HiddenList.Contains(PlayerControl.LocalPlayer.PlayerId))
//                     playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(true);
//             }, 0.1f);
//             Async.Schedule(() =>
//             {
//                 // Fix for Non-Host Modded
//                 Players.GetAllPlayers().Where(pc => !HiddenList.Contains(pc.PlayerId) && pc.IsModded()).ForEach(pc =>
//                 {
//                     // CustomRpcSender sender = CustomRpcSender.Create("FixModdedClientCNOText", sendOption: SendOption.Reliable);
//                     // sender.AutoStartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.FixModdedClientCNO, visiblePC.GetClientId())
//                     //     .WriteNetObject(playerControl)
//                     //     .Write(true)
//                     //     .EndRpc();
//                     // sender.SendMessage();
//                 });
//             }, 0.4f);
//             PlayerControlTimer = 0f;
//         }
//     }

//     protected void CreateNetObject(string sprite, Vector2 position)
//     {
//         log.Info($"Create Custom Net Object {this.GetType().Name} (ID {Id}) at {position} (CreateNetObject)");
//         playerControl = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
//         playerControl.PlayerId = 255;
//         playerControl.isNew = false;
//         playerControl.notRealPlayer = true;
//         AmongUsClient.Instance.NetIdCnt += 1U;
//         MessageWriter msg = MessageWriter.Get();
//         msg.StartMessage(5);
//         msg.Write(AmongUsClient.Instance.GameId);
//         AmongUsClient.Instance.WriteSpawnMessage(playerControl, -2, SpawnFlags.None, msg);
//         msg.EndMessage();
//         msg.StartMessage(6);
//         msg.Write(AmongUsClient.Instance.GameId);
//         msg.WritePacked(int.MaxValue);
//         for (uint i = 1; i <= 3; ++i)
//         {
//             msg.StartMessage(4);
//             msg.WritePacked(2U);
//             msg.WritePacked(-2);
//             msg.Write((byte)SpawnFlags.None);
//             msg.WritePacked(1);
//             msg.WritePacked(AmongUsClient.Instance.NetIdCnt - i);
//             msg.StartMessage(1);
//             msg.EndMessage();
//             msg.EndMessage();
//         }

//         msg.EndMessage();
//         AmongUsClient.Instance.SendOrDisconnect(msg);
//         msg.Recycle();
//         if (PlayerControl.AllPlayerControls.Contains(playerControl)) PlayerControl.AllPlayerControls.Remove(playerControl);
//         Sprite = sprite;
//         Async.Schedule(() =>
//         {
//             playerControl.NetTransform.RpcSnapTo(position);
//             playerControl.RawSetName(sprite);
//             var name = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName;
//             var colorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId;
//             var hatId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId;
//             var skinId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId;
//             var petId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId;
//             var visorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId;
//             // var startOutfit = PlayerControl.LocalPlayer.Data.DefaultOutfit.DeepCopy();
//             MessageWriter writer = MessageWriter.Get();
//             writer.StartMessage(5);
//             writer.Write(AmongUsClient.Instance.GameId);
//             SetupPlayer(PlayerControl.LocalPlayer);
//             writer.StartMessage(1);
//             {
//                 writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
//                 PlayerControl.LocalPlayer.Data.Serialize(writer, false);
//             }
//             writer.EndMessage();
//             writer.StartMessage(2);
//             {
//                 writer.WritePacked(playerControl.NetId);
//                 writer.Write((byte)RpcCalls.Shapeshift);
//                 writer.WriteNetObject(PlayerControl.LocalPlayer);
//                 writer.Write(false);
//             }
//             writer.EndMessage();
//             // ReverseEngineeredRPC.RpcChangeSkin(PlayerControl.LocalPlayer, startOutfit, sendToClients: false);
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = name;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = colorId;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = hatId;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = skinId;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = petId;
//             PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = visorId;
//             writer.StartMessage(1);
//             {
//                 writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
//                 PlayerControl.LocalPlayer.Data.Serialize(writer, false);
//             }
//             writer.EndMessage();
//             writer.EndMessage();
//             AmongUsClient.Instance.SendOrDisconnect(writer);
//             writer.Recycle();
//         }, 0.2f);
//         Position = position;
//         PlayerControlTimer = 0f;
//         ++MaxId;
//         Id = MaxId;
//         if (MaxId == int.MaxValue) MaxId = int.MinValue;
//         AllObjects.Add(this);
//         Players.GetAllPlayers().ForEach(pc =>
//         {
//             if (pc.AmOwner) return;
//             Async.Schedule(() =>
//             {
//                 MessageWriter writer = MessageWriter.Get();
//                 writer.StartMessage(6);
//                 writer.Write(AmongUsClient.Instance.GameId);
//                 writer.WritePacked(pc.GetClientId());
//                 writer.StartMessage(1);
//                 {
//                     writer.WritePacked(playerControl.NetId);
//                     writer.Write(pc.PlayerId);
//                 }
//                 writer.EndMessage();
//                 writer.StartMessage(2);
//                 {
//                     writer.WritePacked(playerControl.NetId);
//                     writer.Write((byte)RpcCalls.MurderPlayer);
//                     writer.WriteNetObject(playerControl);
//                     writer.Write((int)MurderResultFlags.FailedError);
//                 }
//                 writer.EndMessage();
//                 writer.StartMessage(1);
//                 {
//                     writer.WritePacked(playerControl.NetId);
//                     writer.Write((byte)255);
//                 }
//                 writer.EndMessage();
//                 writer.EndMessage();
//                 AmongUsClient.Instance.SendOrDisconnect(writer);
//                 writer.Recycle();
//             }, 0.1f);
//         });

//         // Async.Schedule(() => playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(true), 0.1f); // Fix for Host
//         // Async.Schedule(() => Utils.SendRPC(CustomRPC.FixModdedClientCNO, playerControl), 0.4f); // Fix for Non-Host Modded
//     }

//     public static void FixedUpdate() => AllObjects.ToArray().Do(x => x.OnFixedUpdate());
//     public static CustomNetObject Get(int id) => AllObjects.FirstOrDefault(x => x.Id == id)!;
//     public static void DespawnOnQuit(byte Playerid) => AllObjects.Where(x => x.OwnerId == Playerid).ToArray().Do(x => x.Despawn());

//     public static void Reset()
//     {
//         try
//         {
//             AllObjects.ToArray().Do(x => x.Despawn());
//             AllObjects.Clear();
//         }
//         catch (Exception e)
//         {
//             log.Exception(e);
//         }
//     }
// }