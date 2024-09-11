#nullable enable
using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Logging;
using Lotus.Managers.History.Events;
using Lotus.Patches.Actions;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.Operations;
using UnityEngine;
using VentLib.Utilities.Extensions;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Optionals;
using Lotus.Roles.Managers.Interfaces;
using System.Linq;
using Lotus.Factions.Impostors;
using VentLib;
using Lotus.RPC;

namespace Lotus.Extensions;

public static class PlayerControlExtensions
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PlayerControlExtensions));

    public static UniquePlayerId UniquePlayerId(this PlayerControl player) => API.Player.UniquePlayerId.From(player);

    public static CustomRole PrimaryRole(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod?.Name ?? "(Null callerMethod Name)";
            string? callerClassName = callerMethod?.DeclaringType?.FullName ?? "(Null callerMethod ClassName)";
            log.Warn(callerClassName + "." + callerMethodName + " Invalid Custom Role", "GetCustomRole");
            return ProjectLotus.GameModeManager.CurrentGameMode.RoleManager.FallbackRole();
        }
        CustomRole? role = Game.MatchData.Roles.MainRoles.GetValueOrDefault(player.PlayerId);
        return role ?? IRoleManager.Current.FallbackRole();
    }

    public static CustomRole? GetCustomRoleSafe(this PlayerControl player)
    {
        return player == null ? null : Game.MatchData.Roles.MainRoles.GetValueOrDefault(player.PlayerId);
    }

    public static CustomRole GetSubrole(this PlayerControl player)
    {
        List<CustomRole>? role = Game.MatchData.Roles.SubRoles.GetValueOrDefault(player.PlayerId);
        if (role == null || role.Count == 0) return null;
        return role[0];
    }
    public static List<CustomRole> GetSubroles(this PlayerControl player)
    {
        List<CustomRole>? roleList = Game.MatchData.Roles.SubRoles.GetValueOrDefault(player.PlayerId);
        if (roleList == null || roleList.Count == 0) return new List<CustomRole>();
        return roleList;
    }

    public static List<CustomRole> SecondaryRoles(this PlayerControl player) => Game.MatchData.Roles.GetSubroles(player.PlayerId);

    public static void SyncAll(this PlayerControl player)
    {
        if (player == null) return;
        if (!AmongUsClient.Instance.AmHost) return;
        RoleOperations.Current.SyncOptions(player);
    }

    public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId)
    {
        if (player == null) return;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            player.StartCoroutine(player.CoSetRole(role, ProjectLotus.AdvancedRoleAssignment));
            return;
        }

        RpcV3.Immediate(player.NetId, RpcCalls.SetRole).Write((ushort)role).Write(ProjectLotus.AdvancedRoleAssignment).Send(clientId);
    }

    public static void RpcMark(this PlayerControl killer, PlayerControl? target = null, int colorId = 0)
    {
        if (target == null) target = killer;
        MurderPatches.Lock(killer.PlayerId);

        // Host
        if (killer.AmOwner)
        {
            // killer.ProtectPlayer(target, colorId);
            killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
        }

        // Other Clients
        if (killer.PlayerId == 0) return;

        RpcV3.Mass()
            // .Start(killer.NetId, RpcCalls.ProtectPlayer).Write(target).Write(colorId).End()
            .Start(killer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.FailedProtected).End()
            .Send(killer.GetClientId());
    }

    public static T? PrimaryRole<T>(this PlayerControl player) where T : CustomRole
    {
        return (player.PrimaryRole() as T) ?? player.GetSubrole<T>();
    }
    public static T? GetSubrole<T>(this PlayerControl player) where T : CustomRole
    {
        return player.GetSubroles().Where(sR => sR is T).FirstOrDefault() as T ?? null;
    }

    public static void SetKillCooldown(this PlayerControl player, float time)
    {
        if (player.AmOwner)
        {
            player.SyncAll();
            DevLogger.Log("Synced all cooldowns");
            player.SetKillTimer(time);
        }
        else if (player.IsModded())
        {
            player.PrimaryRole().SyncOptions();
            Async.Schedule(() => Vents.FindRPC((uint)ModCalls.SetKillCooldown)!.Send([player.GetClientId()], time), NetUtils.DeriveDelay(0.1f));
        }
        else
        {
            // IMPORTANT: This could be a possible "issue" area
            // Maybe implement Consumable Coroutine?
            CustomRole roleDefinition = player.PrimaryRole();
            IRemote remote = Game.CurrentGameMode.AddOverride(player.PlayerId, new GameOptionOverride(Override.KillCooldown, time * 2));
            roleDefinition.SyncOptions();

            float deriveDelay = NetUtils.DeriveDelay(0.5f);
            deriveDelay = time < deriveDelay ? NetUtils.DeriveDelay((float)(time * 0.1)) : deriveDelay;
            Async.Schedule(() => player.RpcMark(), deriveDelay);

            Async.Schedule(() =>
            {
                remote.Delete();
                roleDefinition.SyncOptions();
            }, deriveDelay * 2);
        }
    }

    public static void RpcMurderSpecificPlayer(this PlayerControl killer, PlayerControl? target = null)
    {
        if (target == null) target = killer;
        if (killer.AmOwner)
            killer.MurderPlayer(target, MurderResultFlags.Succeeded);
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, killer.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write((int)MurderResultFlags.Succeeded);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
    }

    public static void RpcDesyncRepairSystem(this PlayerControl target, SystemTypes systemType, int amount)
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
        messageWriter.Write((byte)systemType);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    public static string? GetAllRoleName(this PlayerControl player)
    {
        if (!player) return null;
        var text = player.PrimaryRole().RoleName;
        List<CustomRole> subroles = player.SecondaryRoles();
        if (subroles.Count == 0) return text;

        text += subroles.StrJoin().Replace("[", " (").Replace("]", ")");
        return text;
    }

    public static string GetNameWithRole(this PlayerControl? player)
    {
        if (player == null) return "(GetNameWithRole(player is null))";
        return $"{player.name}" + (Game.State is GameState.Roaming ? $"({player.GetAllRoleName()})" : "");
    }

    public static Color GetRoleColor(this PlayerControl player)
    {
        return player.PrimaryRole().RoleColor;
    }

    public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f, PlayerControl? target = null)
    {
        if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;
        if (ReferenceEquals(target, null)) target = pc;

        var systemtypes = SystemTypes.Reactor;
        if (ProjectLotus.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;

        Async.Schedule(() => pc.RpcDesyncRepairSystem(systemtypes, 128), 0f + delay);
        Async.Schedule(() => pc.RpcMurderSpecificPlayer(target), 0.2f + delay);

        Async.Schedule(() =>
        {
            pc.RpcDesyncRepairSystem(systemtypes, 16);
            if (ProjectLotus.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncRepairSystem(systemtypes, 17);
            if (ProjectLotus.AdvancedRoleAssignment)
            {
                RoleTypes realRole = pc.PrimaryRole().RealRole;
                bool isDead = pc.Data.IsDead;
                RpcV3.Immediate(pc.NetId, RpcCalls.SetRole).Write((ushort)(isDead ? realRole.GhostEquivelant() : realRole)).Write(true).Send(pc.GetClientId());
                // Async.Schedule(() => RpcV3.Immediate(p.NetId, RpcCalls.SetRole).Write((ushort)(isDead ? realRole.GhostEquivelant() : realRole)).Write(true).Send(p.GetClientId()), NetUtils.DeriveDelay(0.3f));
            }
        }, 0.4f + delay);
    }

    public static void RpcExileV2(this PlayerControl player, bool reallyExiled, bool hookDeath = true)
    {
        log.Trace($"Exiled (V2): {player.GetNameWithRole()}");
        player.Exiled();
        RpcV3.Immediate(player.NetId, RpcCalls.Exiled, SendOption.None);

        if (!reallyExiled)
        {
            if (hookDeath) Hooks.PlayerHooks.PlayerDeathHook.Propagate(new PlayerDeathHookEvent(player, new ExiledEvent(player, new List<PlayerControl>(), new List<PlayerControl>())));
            return;
        }

        ActionHandle uselessHandle = ActionHandle.NoInit();
        RoleOperations.Current.TriggerForAll(LotusActionType.Exiled, player, uselessHandle);

        Hooks.PlayerHooks.PlayerExiledHook.Propagate(new PlayerHookEvent(player));
        Hooks.PlayerHooks.PlayerDeathHook.Propagate(new PlayerDeathHookEvent(player, new ExiledEvent(player, new List<PlayerControl>(), new List<PlayerControl>())));
    }

    public static void RpcVaporize(this PlayerControl player, PlayerControl target, IDeathEvent? deathEvent = null)
    {
        if (player == null || target == null) return;
        log.Trace($"(RpcVaporize) {player.name} => {target.name}");
        target.RpcExileV2(false, false);

        MurderPatches.Lock(player.PlayerId);

        deathEvent ??= new DeathEvent(target, player);

        ActionHandle ignored = ActionHandle.NoInit();
        Optional<FrozenPlayer> fp = Optional<FrozenPlayer>.Of(Game.MatchData.GetFrozenPlayer(player));
        RoleOperations.Current.TriggerForAll(LotusActionType.PlayerDeath, target, ignored, player, fp, deathEvent);

        PlayerMurderHookEvent playerMurderHookEvent = new(player, target, deathEvent);
        Hooks.PlayerHooks.PlayerMurderHook.Propagate(playerMurderHookEvent);
        Hooks.PlayerHooks.PlayerDeathHook.Propagate(playerMurderHookEvent);

        Async.Schedule(() =>
        {
            if (target != null) target.SetChatName(target.name);
        }, 0.1f);
    }



    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
    ///</summary>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
    ///</summary>
    ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
    {
        var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
        List<PlayerControl> rangePlayers = new();
        player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
        foreach (var pc in rangePlayersIL)
        {
            if (predicate(pc)) rangePlayers.Add(pc);
        }
        return rangePlayers;
    }

    public static RoleTypes GetVanillaRole(this PlayerControl player) => player.GetTeamInfo().MyRole;
    // public static RoleTypes GetVanillaRole(this PlayerControl player) => player.PrimaryRole().GetType == IRoleManager.Current.FallbackRole().GetType
    //     ? player.GetTeamInfo().MyRole
    //     : player.PrimaryRole().RealRole;

    public static VanillaRoleTracker.TeamInfo GetTeamInfo(this PlayerControl player) => Game.MatchData.VanillaRoleTracker.GetInfo(player.PlayerId);

    public static bool IsAlive(this PlayerControl target)
    {
        return target != null && !target.Data.IsDead && !target.Data.Disconnected;
    }

    public static void RpcResetAbilityCooldown(this PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (PlayerControl.LocalPlayer.PlayerId == target.PlayerId)
        {
            log.Debug($"Resetting Ability Cooldown for Host (Current Player).");
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
        }
        else
        {
            log.Debug($"Resetting Ability Cooldown for Non-Host ({target.name})");
            RpcV3.Immediate(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None).Write(target).Write(0).Send(target.GetClientId());
        }
    }
}