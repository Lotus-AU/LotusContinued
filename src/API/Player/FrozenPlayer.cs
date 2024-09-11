using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.Extensions;
using Lotus.Managers.History;
using Lotus.Managers.History.Events;
using Lotus.Roles;
using Lotus.Statuses;
using Lotus.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using static NetworkedPlayerInfo;

namespace Lotus.API.Player;

public class FrozenPlayer
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

    private PlayerControl GetPlayer()
    {
        return PlayerControl.AllPlayerControls.ToArray().FirstOrOptional(p => p.FriendCode == FriendCode).OrElseGet(() => Utils.GetPlayerById(PlayerId)!);
    }

}