using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Lotus.API;
using Lotus.API.Player;
using Lotus.Roles.Overrides;
using Lotus.Extensions;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using InnerNet;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.Network;
using Array = Il2CppSystem.Array;
using Buffer = Il2CppSystem.Buffer;

namespace Lotus.Options;

public static class DesyncOptions
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(DesyncOptions));

    public static void SyncToAll(IGameOptions options) => Players.GetPlayers().Do(p => SyncToPlayer(options, p));

    public static void SyncToPlayer(IGameOptions options, PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player == null) return;
        if (!player.AmOwner)
        {
            try
            {
                SyncToClient(options, player.GetClientId());
            }
            catch (Exception exception)
            {
                log.Exception("Error syncing game options to client.", exception);
            }
            return;
        }

        if (GameManager.Instance?.LogicComponents != null)
        {
            try
            {
                foreach (GameLogicComponent com in GameManager.Instance.LogicComponents)
                    if (com.TryCast(out LogicOptions lo))
                    {
                        lo.SetGameOptions(options);
                    }
            }
            catch (Exception ex)
            {
                log.Exception(ex);
            }
        }
        GameOptionsManager.Instance.CurrentGameOptions = options;
    }

    public static void SyncToClient(IGameOptions options, int clientId)
    {
        MessageWriter writer = MessageWriter.Get(SendOption.None);
        writer.Write(options.Version);
        writer.StartMessage(0);
        writer.Write((byte)options.GameMode);

        if (options.TryCast(out NormalGameOptionsV12? normalOpt))
            NormalGameOptionsV12.Serialize(writer, normalOpt);
        else if (options.TryCast(out HideNSeekGameOptionsV12? hnsOpt))
            HideNSeekGameOptionsV12.Serialize(writer, hnsOpt);
        else
        {
            writer.Recycle();
            log.Fatal("Option cast failed.");
        }
        writer.EndMessage();

        // Array & Send
        var byteArray = new Il2CppStructArray<byte>(writer.Length - 1);
        // MessageWriter.ToByteArray
        Buffer.BlockCopy(writer.Buffer.CastFast<Array>(), 1, byteArray.CastFast<Array>(), 0, writer.Length - 1);

        try
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                Il2CppSystem.Object logicComponent = GameManager.Instance.LogicComponents[(Index)i];
                if (!logicComponent.TryCast<LogicOptions>(out _)) continue;
                MessageWriter sentWriter = MessageWriter.Get(SendOption.Reliable);

                sentWriter.StartMessage(clientId == -1 ? Tags.GameData : Tags.GameDataTo);

                {
                    sentWriter.Write(AmongUsClient.Instance.GameId);
                    if (clientId != -1) sentWriter.WritePacked(clientId);

                    sentWriter.StartMessage(1);

                    {
                        sentWriter.WritePacked(GameManager.Instance.NetId);
                        sentWriter.StartMessage(i);

                        {
                            sentWriter.WriteBytesAndSize(byteArray);
                        }

                        sentWriter.EndMessage();
                    }

                    sentWriter.EndMessage();
                }

                sentWriter.EndMessage();

                AmongUsClient.Instance.SendOrDisconnect(sentWriter);
                sentWriter.Recycle();
            }
        }
        catch (Exception ex) { log.Fatal(ex.ToString()); }
        writer.Recycle();
    }

    public static int GetTargetedClientId(string name)
    {
        int clientId = -1;
        var allClients = AmongUsClient.Instance.allObjects.allObjectsFast;
        var allClientIds = allClients.Keys;

        foreach (uint id in allClientIds)
            if (clientId == -1 && allClients[id].name.Contains(name))
                clientId = (int)id;
        return clientId;
    }

    // This method is used to find the "GameManager" client which is now needed for synchronizing options
    public static int GetManagerClientId() => GetTargetedClientId("Manager");

    public static IGameOptions GetModifiedOptions(IEnumerable<GameOptionOverride> overrides)
    {
        IGameOptions clonedOptions = AUSettings.StaticOptions.DeepCopy();
        overrides.Where(o => o != null).ForEach(optionOverride => optionOverride.ApplyTo(clonedOptions));
        return clonedOptions;
    }

    public static void SendModifiedOptions(IEnumerable<GameOptionOverride> overrides, PlayerControl player)
    {
        SyncToPlayer(GetModifiedOptions(overrides), player);
    }
}