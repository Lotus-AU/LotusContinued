using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Managers.History.Events;
using Lotus.Roles;
using Lotus.Extensions;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers.History;

public class PlayerHistory
{
    public byte PlayerId;
    public UniquePlayerId UniquePlayerId;
    public string Name;
    public string ColorName;
    public CustomRole MainRole;
    public List<CustomRole> Subroles;
    public uint Level;
    public NetworkedPlayerInfo.PlayerOutfit Outfit;
    public ulong GameID;
    public IDeathEvent? CauseOfDeath;
    public PlayerStatus Status;

    public PlayerHistory(FrozenPlayer frozenPlayer)
    {
        PlayerId = frozenPlayer.PlayerId;
        Name = frozenPlayer.Name;
        ColorName = frozenPlayer.ColorName;
        MainRole = frozenPlayer.MainRole;
        Subroles = frozenPlayer.Subroles;
        UniquePlayerId = UniquePlayerId.FromFriendCode(frozenPlayer.FriendCode);
        Level = frozenPlayer.Level;
        Outfit = frozenPlayer.Outfit;
        GameID = frozenPlayer.GameID;
        CauseOfDeath = frozenPlayer.CauseOfDeath;
        Status = frozenPlayer.RegenerateStatus();
    }
}

public enum PlayerStatus
{
    Alive,
    Exiled,
    Dead,
    Disconnected
}