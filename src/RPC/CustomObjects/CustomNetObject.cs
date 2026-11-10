using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.InnerNet.GameDataMessages;
using HarmonyLib;
using Hazel;
using InnerNet;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Network;
using TMPro;
using UnityEngine;
using VentLib;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Object = UnityEngine.Object;

// Code from: https://github.com/Gurge44/EndlessHostRoles/blob/main/Modules/CustomNetObject.cs
// It is sort-of modified, however.

// Sidenote:
// I HATE working with CustomRpcSender.
// It's just so weird.
// And it sucks because this is not a TOH-based mod so I have to convert everything over.

namespace Lotus.RPC.CustomObjects;

public class CustomNetObject
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(CustomNetObject));
    private static int MaxId = -1;

    public static readonly List<CustomNetObject> AllObjects = [];

    public PlayerControl playerControl;
    public Vector2 Position;
    public string Sprite;

    protected int Id = -1;

    private float playerControlTimer;

    private readonly HashSet<byte> HiddenList = [];

    public virtual bool CanTarget() => false;

    public virtual void SetupOutfit()
    {
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = "<size=14><br></size>" + Sprite;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = 255;
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = "";
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = "";
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = "";
        PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = "";
    }

    protected void RpcChangeSprite(string sprite)
    {
        log.Info($" Change Custom Net Object {GetType().Name} (ID {Id}) sprite");

        Sprite = sprite;

        Async.Execute(() =>
        {
            string name = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName;
            int colorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId;
            string hatId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId;
            string skinId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId;
            string petId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId;
            string visorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId;
            MessageWriter writer = MessageWriter.Get(SendOption.Reliable); // Create Code

            // Start Message
            writer.StartMessage(5);
            writer.Write(AmongUsClient.Instance.GameId);

            SetupOutfit();
            playerControl.RawSetName(PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName);
            writer.StartMessage(1);
            {
                writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                PlayerControl.LocalPlayer.Data.Serialize(writer, false);
            }

            writer.EndMessage();

            // Start RPC
            writer.StartMessage(2);
            writer.WritePacked(playerControl.NetId);
            writer.Write((byte)RpcCalls.Shapeshift);

            writer.WriteNetObject(PlayerControl.LocalPlayer);
            writer.Write(false);

            writer.EndMessage();

            ReverseEngineeredRPC.RpcChangeSkin(playerControl, PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default], sendToClients: false);
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = name;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = colorId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = hatId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = skinId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = petId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = visorId;
            writer.StartMessage(1);

            {
                writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                PlayerControl.LocalPlayer.Data.Serialize(writer, false);
            }

            writer.EndMessage();

            // 2nd endmessage
            writer.EndMessage();

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        });
    }

    public virtual void SnapTo(Vector2 position, SendOption sendOption = SendOption.None)
    {
        if (AmongUsClient.Instance.AmClient)
            playerControl.NetTransform.SnapTo(position, (ushort)(playerControl.NetTransform.lastSequenceId + 1U));

        ushort num = (ushort)(playerControl.NetTransform.lastSequenceId + 2U);
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(playerControl.NetTransform.NetId, 21, sendOption);
        NetHelpers.WriteVector2(position, messageWriter);
        messageWriter.Write(num);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);

        Position = position;
    }

    public void Despawn()
    {
        log.Info($" Despawn Custom Net Object {GetType().Name} (ID {Id})");

        try
        {
            playerControl.Despawn();
            AllObjects.Remove(this);
            MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
            writer.StartMessage(5);
            writer.Write(AmongUsClient.Instance.GameId);
            writer.StartMessage(5);
            writer.WritePacked(playerControl.NetId);
            writer.EndMessage();
            writer.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();

            AmongUsClient.Instance.RemoveNetObject(playerControl);
            Object.Destroy(playerControl.gameObject);
        }
        catch (Exception e)
        {
            log.Exception(e);
        }

        OnDestroy();
    }

    protected void Hide(PlayerControl player)
    {
        log.Info($" Hide Custom Net Object {GetType().Name} (ID {Id}) from {player.GetNameWithRole()}");

        HiddenList.Add(player.PlayerId);
        if (playerControl == null) return;

        if (player.AmOwner)
        {
            Async.Schedule(() => playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(false), 0.1f);
            playerControl.Visible = false;
            return;
        }

        // if (player.IsModded())
        //     Async.Schedule(() =>
        //     {
        //         Vents.FindRPC((uint)ModCalls.FixModdedClientCNO).Send([player.GetClientId()], playerControl, false);
        //     }, 0.4f);

        MessageWriter writer = MessageWriter.Get();
        writer.StartMessage(6);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(player.GetClientId());
        writer.StartMessage(5);
        writer.WritePacked(playerControl.NetId);
        writer.EndMessage();
        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }

    protected virtual void OnFixedUpdate()
    {
        // need to respawn every 20 seconds because server will disconnect everyone on the 30th second.
        playerControlTimer += Time.fixedDeltaTime;
        if (playerControlTimer > 20f)
        {
            playerControlTimer = 0f;
            Async.Execute(RecreateNetObject);
        }
    }

    protected virtual void CreateNetObject(string sprite, Vector2 position)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        log.Info($"Create Custom Net Object {GetType().Name} (ID {MaxId + 1}) at {position}");
        playerControl = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
        playerControl.PlayerId = 254;
        playerControl.isNew = false;
        playerControl.notRealPlayer = true;
        AmongUsClient.Instance.NetIdCnt += 1U;

        MessageWriter msg = MessageWriter.Get(SendOption.Reliable);
        msg.StartMessage(5);
        msg.Write(AmongUsClient.Instance.GameId);
        msg.StartMessage(4);
        msg.WritePacked(playerControl.SpawnId);
        msg.WritePacked(-2);
        msg.Write((byte)SpawnFlags.None);
        InnerNetObject[] componentsInChildren = playerControl.GetComponentsInChildren<InnerNetObject>();
        msg.WritePacked(componentsInChildren.Length);

        for (int index = 0; index < componentsInChildren.Length; ++index)
        {
            InnerNetObject innerNetObject = componentsInChildren[index];
            innerNetObject.OwnerId = -2;
            innerNetObject.SpawnFlags = SpawnFlags.None;

            if (innerNetObject.NetId == 0U)
            {
                innerNetObject.NetId = AmongUsClient.Instance.NetIdCnt++;
                InnerNetObjectCollection allObjects = AmongUsClient.Instance.allObjects;
                allObjects.allObjects.Add(innerNetObject);
                allObjects.allObjectsFast.Add(innerNetObject.NetId, innerNetObject);
            }

            msg.WritePacked(innerNetObject.NetId);
            msg.StartMessage(1);
            innerNetObject.Serialize(msg, true);
            msg.EndMessage();
        }

        msg.EndMessage();

        if (ConnectionManager.IsVanillaServer)
            for (uint i = 1; i <= 3; ++i)
            {
                msg.StartMessage(4);
                msg.WritePacked(2U);
                msg.WritePacked(-2);
                msg.Write((byte)SpawnFlags.None);
                msg.WritePacked(1);
                msg.WritePacked(AmongUsClient.Instance.NetIdCnt - i);
                msg.StartMessage(1);
                msg.EndMessage();
                msg.EndMessage();
            }


        msg.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(msg);
        msg.Recycle();
        if (PlayerControl.AllPlayerControls.Contains(playerControl)) PlayerControl.AllPlayerControls.Remove(playerControl);

        Async.Schedule(() =>
        {
            playerControl.NetTransform.RpcSnapTo(position);
            string name = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName;
            int colorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId;
            string hatId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId;
            string skinId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId;
            string petId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId;
            string visorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId;
            MessageWriter writer = MessageWriter.Get(SendOption.Reliable); // Create Code

            // Start Message
            writer.StartMessage(5);
            writer.Write(AmongUsClient.Instance.GameId);

            SetupOutfit();
            playerControl.RawSetName(PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName);
            writer.StartMessage(1);

            {
                writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                PlayerControl.LocalPlayer.Data.Serialize(writer, false);
            }

            writer.EndMessage();

            writer.StartMessage(2);
            writer.WritePacked(playerControl.NetId);
            writer.Write((byte)RpcCalls.Shapeshift);

            writer.WriteNetObject(PlayerControl.LocalPlayer);
            writer.Write(false);

            writer.EndMessage();

            ReverseEngineeredRPC.RpcChangeSkin(playerControl, PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default], sendToClients: false);
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = name;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = colorId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = hatId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = skinId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = petId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = visorId;
            writer.StartMessage(1);

            {
                writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                PlayerControl.LocalPlayer.Data.Serialize(writer, false);
            }

            writer.EndMessage();
            // 2nd endmessage
            writer.EndMessage();

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }, 0.2f);

        Position = position;
        Sprite = sprite;

        if (Id == -1)
        {
            if (MaxId != int.MaxValue) ++MaxId;
            Id = MaxId;

            Async.Schedule(() => AllObjects.Add(this), 0.05f, false, true);
        }

        foreach (PlayerControl pc in Players.GetAllPlayers())
        {
            if (pc.AmOwner) continue;

            Async.Schedule(() =>
            {
                MessageWriter writer = MessageWriter.Get(SendOption.Reliable); // Create Code

                // StartMessage
                writer.StartMessage(6);
                writer.Write(AmongUsClient.Instance.GameId);
                writer.WritePacked(pc.GetClientId());

                writer.StartMessage(1);

                {
                    writer.WritePacked(playerControl.NetId);
                    writer.Write(pc.PlayerId);
                }

                writer.EndMessage();

                writer.StartMessage(2);
                writer.WritePacked(playerControl.NetId);
                writer.Write((byte)RpcCalls.MurderPlayer);

                writer.WriteNetObject(playerControl);
                writer.Write((int)MurderResultFlags.FailedError);

                writer.EndMessage();

                writer.StartMessage(1);

                {
                    writer.WritePacked(playerControl.NetId);
                    writer.Write((byte)254);
                }

                writer.EndMessage();
                writer.EndMessage();

                AmongUsClient.Instance.SendOrDisconnect(writer);
                writer.Recycle();
            }, 0.1f);
        }

        foreach (PlayerControl pc in Players.GetAllPlayers())
            if (HiddenList.Contains(pc.PlayerId))
                Hide(pc);


        Async.Schedule(() => playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(true), 0.1f); // Fix for Host
        // Async.Schedule(() => Vents.FindRPC((uint)ModCalls.FixModdedClientCNO).Send([player.GetClientId()], playerControl), 0.4f); // Fix for Non-Host Modded
        Async.Schedule(() => RpcChangeSprite(sprite), .6f);
    }

    protected virtual void OnDestroy() {}

    private void RecreateNetObject()
    {
        PlayerControl oldPlayerControl = playerControl;
        log.Trace("Recreating old net object.");
        Async.Schedule(() => oldPlayerControl.Despawn(), .3f);
        CreateNetObject(Sprite, Position);
    }

    public static void FixedUpdate()
    {
        foreach (CustomNetObject cno in AllObjects) cno?.OnFixedUpdate();
    }

    public static Optional<CustomNetObject> Get(int id)
    {
        return AllObjects.FirstOrOptional(x => x.Id == id);
    }

    public static Optional<CustomNetObject> ObjectFromPlayer(PlayerControl control) => AllObjects.FirstOrOptional(x => x.playerControl == control);

    public static void Reset()
    {
        try
        {
            AllObjects.ToArray().Do(x => x.Despawn());
            AllObjects.Clear();
        }
        catch (Exception e)
        {
            log.Exception(e);
        }
    }
}