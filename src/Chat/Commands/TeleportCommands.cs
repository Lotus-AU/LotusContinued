using System;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.GameModes.Standard;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Utilities;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Utilities.Extensions;
using Vector2 = UnityEngine.Vector2;

namespace Lotus.Chat.Commands;

public class TeleportCommands
{
    public static bool IsAllowedToTeleport(PlayerControl source)
    {
        int allowedUsers = GeneralOptions.MiscellaneousOptions.AllowTeleportInLobby;
        bool permitted = allowedUsers switch
        {
            0 => source.IsHost(),
            1 => source.IsHost() || PluginDataManager.FriendManager.IsFriend(source),
            2 => true,
            _ => throw new ArgumentOutOfRangeException($"{allowedUsers} is not a valid integer for MiscellaneousOptions.AllowTeleportInLobby.")
        };

        if (!permitted) ChatHandlers.NotPermitted().Send(source);

        return permitted;
    }
    [Command(CommandFlag.None, "tpout")]
    public static void TeleportOutOfLobby(PlayerControl source)
    {

        switch (Game.State)
        {
            case GameState.Roaming:
                if (Game.CurrentGameMode is not StandardGameMode || source.IsAlive())
                {
                    ChatHandlers.NotPermitted().Send(source);
                    break;
                }
                Vector2 position = source.GetTruePosition();
                position = new Vector2(position.x + 150, position.y);
                Utils.Teleport(source.NetTransform, position);
                break;
            case GameState.InLobby:
                if (!IsAllowedToTeleport(source)) return;
                Utils.Teleport(source.NetTransform, new Vector2(0.1f, 3.8f));
                break;
            default:
                ChatHandlers.InvalidCmdUsage().Send(source);
                break;
        }
    }

    [Command(CommandFlag.None, "tpin")]
    public static void TeleportIntoLobby(PlayerControl source)
    {

        switch (Game.State)
        {
            case GameState.Roaming:
                if (Game.CurrentGameMode is not StandardGameMode || source.IsAlive())
                {
                    ChatHandlers.NotPermitted().Send(source);
                    break;
                }
                if (ShipStatus.Instance is AirshipStatus) Utils.Teleport(source.NetTransform, RandomSpawn.AirshipLocations["MainHall"]);
                else
                    switch (ShipStatus.Instance.Type)
                    {
                        case ShipStatus.MapType.Ship:
                            Utils.Teleport(source.NetTransform, RandomSpawn.SkeldLocations["Cafeteria"]);
                            break;
                        case ShipStatus.MapType.Hq:
                            Utils.Teleport(source.NetTransform, RandomSpawn.MiraLocations["Cafeteria"]);
                            break;
                        case ShipStatus.MapType.Pb:
                            Utils.Teleport(source.NetTransform, RandomSpawn.PolusLocations["Office1"]);
                            break;
                    }

                break;
            case GameState.InLobby:
                if (!IsAllowedToTeleport(source)) return;
                Utils.Teleport(source.NetTransform, new Vector2(-0.2f, 1.3f));
                break;
            default:
                ChatHandlers.InvalidCmdUsage().Send(source);
                break;
        }
    }
}