using System.Collections.Generic;
using Hazel;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.Extensions;
using Lotus.Managers;
using Lotus.Managers.History;
using Lotus.Managers.History.Events;
using Lotus.Roles;
using Lotus.Roles.Subroles;
using Lotus.Statuses;
using Lotus.Utilities;
using VentLib.Networking;
using VentLib.Networking.Interfaces;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using static NetworkedPlayerInfo;

namespace Lotus.API.Player;

public class FrozenPlayer: IRpcSendable<FrozenPlayer>
{
    public byte PlayerId;
    public string FriendCode;
    public string Name;
    public string ColorName;
    public CustomRole MainRole;
    public List<CustomRole> Subroles;
    public RemoteList<IStatus> Statuses = new();
    public uint Level;
    public PlayerOutfit Outfit;
    public ulong GameID;
    public IDeathEvent? CauseOfDeath;
    public PlayerStatus Status = PlayerStatus.Alive;

    public PlayerControl MyPlayer => NullablePlayer == null ? NullablePlayer ??= GetPlayer() : NullablePlayer;

    public PlayerControl NullablePlayer;

    internal FrozenPlayer()
    {

    }

    public FrozenPlayer(PlayerControl player, bool changeStatus = true)
    {
        Name = player.name;
        ColorName = player.Data.ColoredName();
        Level = player.Data.PlayerLevel;
        FriendCode = player.FriendCode;
        PlayerId = player.PlayerId;
        Outfit = player.Data.DefaultOutfit.DeepCopy();
        MainRole = player.PrimaryRole();
        Subroles = player.SecondaryRoles();
        GameID = player.GetGameID();
        Game.MatchData.GameHistory.GetCauseOfDeath(PlayerId).IfPresent(cod => CauseOfDeath = cod);

        if (changeStatus)
            try
            {
                if (player.Data.Disconnected) Status = PlayerStatus.Disconnected;
                else if (player.IsAlive()) Status = PlayerStatus.Alive;
                else Status = Game.MatchData.GameHistory.Events
                    .FirstOrOptional(ev => ev is ExiledEvent exiledEvent && exiledEvent.Player().PlayerId == PlayerId)
                    .Map(_ => PlayerStatus.Exiled)
                    .OrElse(PlayerStatus.Dead);
            }
            catch
            {
                Status = PlayerStatus.Disconnected;
            }
        NullablePlayer = player;
    }

    public PlayerStatus RegenerateStatus()
    {
        try
        {
            if (NullablePlayer.Data.Disconnected) Status = PlayerStatus.Disconnected;
            else if (NullablePlayer.IsAlive()) Status = PlayerStatus.Alive;
            else Status = Game.MatchData.GameHistory.Events
                .FirstOrOptional(ev => ev is ExiledEvent exiledEvent && exiledEvent.Player().PlayerId == PlayerId)
                .Map(_ => PlayerStatus.Exiled)
                .OrElse(PlayerStatus.Dead);
        }
        catch
        {
            Status = PlayerStatus.Disconnected;
        }
        return Status;
    }

    public FrozenPlayer Read(MessageReader reader)
    {
        bool hasPlayer = reader.ReadBoolean();
        if (hasPlayer)
            NullablePlayer = reader.ReadNetObject<PlayerControl>();

        Name = reader.ReadString();
        ColorName =  reader.ReadString();
        Level = reader.ReadUInt32();
        FriendCode = reader.ReadString();
        Outfit = new PlayerOutfit();
        Outfit.Deserialize(reader);
        MainRole = GlobalRoleManager.Instance.GetRoleFromId(reader.ReadInt32());
        Subroles = reader.ReadList<CustomRole>();
        GameID = reader.ReadUInt64();
        if (reader.ReadBoolean()) CauseOfDeath = IDeathEvent.ReadDeathEvent(reader);

        Status = (PlayerStatus)reader.ReadByte();
        return this;
    }

    public void Write(MessageWriter writer)
    {
        PlayerControl thisPlayer = MyPlayer;
        if (thisPlayer)
        {
            writer.Write(true);
            writer.WriteNetObject(thisPlayer);
        } else writer.Write(false);

        writer.Write(Name);
        writer.Write(ColorName);
        writer.Write(Level);
        writer.Write(FriendCode);
        Outfit.Serialize(writer);
        writer.Write(GlobalRoleManager.Instance.GetRoleId(MainRole));
        writer.WriteList(Subroles);
        writer.Write(GameID);
        if (CauseOfDeath != null)
        {
            writer.Write(true);
            CauseOfDeath.Write(writer);
        } else writer.Write(false);
        writer.Write((byte)Status);
    }

    private PlayerControl GetPlayer()
    {
        return PlayerControl.AllPlayerControls.ToArray().FirstOrOptional(p => p.FriendCode == FriendCode).OrElseGet(() => Utils.GetPlayerById(PlayerId)!);
    }

}