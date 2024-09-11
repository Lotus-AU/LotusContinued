using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Reactive.Actions;
using Lotus.Managers;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Victory;
using Lotus.Roles.Managers.Interfaces;
using VentLib.Options.UI.Tabs;
using VentLib.Utilities;
using VentLib.Networking.RPC;
using Lotus.API.Player;
using VentLib.Utilities.Extensions;
using Lotus.Extensions;
using AmongUs.GameOptions;
using Lotus.Factions;
using Lotus.Factions.Impostors;
using Hazel;

namespace Lotus.GameModes;

public abstract class GameMode : IGameMode
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(GameMode));

    protected Dictionary<LotusActionType, List<LotusAction>> LotusActions = new();

    public GameMode()
    {
        SetupRoleActions();
    }

    public abstract string Name { get; set; }

    public CoroutineManager CoroutineManager { get; } = new();
    public abstract MatchData MatchData { get; set; }
    public abstract RoleOperations RoleOperations { get; }
    public abstract Roles.Managers.RoleManager RoleManager { get; }
    public abstract IEnumerable<GameOptionTab> EnabledTabs();
    public abstract MainSettingTab MainTab();

    public virtual void Activate() { }

    public virtual void Deactivate() { }

    public virtual void FixedUpdate() { }

    public abstract void Setup();

    public abstract void Assign(PlayerControl player, CustomRole role, bool addAsMainRole = true, bool sendToClient = false);

    public virtual void AssignRoles(List<PlayerControl> players)
    {
        if (!ProjectLotus.AdvancedRoleAssignment) return;
        Async.Schedule(() =>
        {
            Dictionary<byte, bool> Disconnected = new();
            players.ForEach(pc =>
            {
                Disconnected[pc.PlayerId] = pc.Data.Disconnected;
                pc.Data.Disconnected = true;
            });
            log.Trace("Sending Disconncted Data.");
            GeneralRPC.SendGameData();
            players.ForEach(pc => pc.Data.Disconnected = Disconnected[pc.PlayerId]);
        }, NetUtils.DeriveDelay(0.5f));
        Async.Schedule(() =>
        {
            // CustomRole currentRole = PlayerControl.LocalPlayer.PrimaryRole();
            // PlayerControl[] alliedPlayers = Players.GetPlayers().Where(p => currentRole.Relationship(p) is Relation.FullAllies).ToArray();
            // int[] alliedPlayerClientIds = alliedPlayers.Where(currentRole.Faction.CanSeeRole).Select(p => p.GetClientId()).ToArray();
            // RoleTypes currentRole = PlayerControl.LocalPlayer.Data.Role.Role;
            RoleTypes currentRole = RoleTypes.Crewmate;
            players.Where(pc => !pc.IsHost()).ForEach(pc =>
            {
                RpcV3.Immediate(PlayerControl.LocalPlayer.NetId, RpcCalls.SetRole).Write((ushort)currentRole).Write(true).Send(pc.GetClientId());
                // if (currentRole.RealRole.IsCrewmate())
                // {
                //     RpcV3.Immediate(PlayerControl.LocalPlayer.NetId, RpcCalls.SetRole).Write((ushort)currentRole.RealRole).Write(true).Send(pc.GetClientId());
                //     return;
                // }
                // players.ForEach(pc =>
                // {
                //     bool shouldSeeThemAsImpostor = alliedPlayerClientIds.Contains(pc.GetClientId()); // if they are allies. e.g. Neutral Teaming/Undead/Impostor
                //     if (!shouldSeeThemAsImpostor)
                //     {   // If their team is impostor and they are crew. Force impostor.
                //         if (currentRole.Faction is ImpostorFaction && pc.PrimaryRole().RealRole.IsCrewmate()) shouldSeeThemAsImpostor = true;
                //     }
                //     RoleTypes finalRole = shouldSeeThemAsImpostor ? currentRole.RealRole : RoleTypes.Crewmate;
                //     RpcV3.Immediate(PlayerControl.LocalPlayer.NetId, RpcCalls.SetRole).Write((ushort)finalRole).Write(true).Send(pc.GetClientId());
                // });
            });
            // PlayerControl.LocalPlayer.StartCoroutine(PlayerControl.LocalPlayer.CoSetRole(currentRole, true));
            log.Trace("Sent! Cleaning up in a second...");
        }, NetUtils.DeriveDelay(1f));
        Async.Schedule(() =>
        {
            players.ForEach(pc => PlayerNameColor.Set(pc));
            DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
            DestroyableSingleton<HudManager>.Instance.HideGameLoader();
        }, NetUtils.DeriveDelay(1.2f));
        Async.Schedule(() =>
        {
            GeneralRPC.SendGameData();
            log.Trace("Cleaned up and sent old disconnect info.");
        }, NetUtils.DeriveDelay(1.5f));
    }

    public virtual void OnShowRole()
    {
        // Players.GetAllPlayers().Sorted(p => p.IsHost() ? 0 : 1).ForEach(p => p.PrimaryRole().Assign(true));
    }

    public abstract void SetupWinConditions(WinDelegate winDelegate);

    public void Trigger(LotusActionType action, ActionHandle handle, params object[] arguments)
    {
        List<LotusAction>? actions = LotusActions.GetValueOrDefault(action);
        if (actions == null) return;

        arguments = arguments.AddToArray(handle);

        foreach (LotusAction lotusAction in actions)
        {
            if (handle.Cancellation is not (ActionHandle.CancelType.None or ActionHandle.CancelType.Soft)) return;
            lotusAction.Execute(arguments);
        }
    }

    private void SetupRoleActions()
    {
        Enum.GetValues<LotusActionType>().Do(action => this.LotusActions.Add(action, new List<LotusAction>()));
        this.GetType().GetMethods(AccessFlags.InstanceAccessFlags)
            .SelectMany(method => method.GetCustomAttributes<LotusActionAttribute>().Select(a => (a, method)))
            .Where(t => t.a.Subclassing || t.method.DeclaringType == this.GetType())
            .Select(t => new LotusAction(t.Item1, t.method))
            .Do(AddLotusAction);
    }

    private void AddLotusAction(LotusAction action)
    {
        List<LotusAction> currentActions = this.LotusActions.GetValueOrDefault(action.ActionType, new List<LotusAction>());

        log.Log(LogLevel.All, $"Registering Action {action.ActionType} => {action.Method.Name} (from: \"{action.Method.DeclaringType}\")", "RegisterAction");
        if (action.ActionType is LotusActionType.FixedUpdate &&
            currentActions.Count > 0)
            throw new ConstraintException("LotusActionType.FixedUpdate is limited to one per class. If you're inheriting a class that uses FixedUpdate you can add Override=METHOD_NAME to your annotation to override its Update method.");

        if (action.Attribute.Subclassing || action.Method.DeclaringType == this.GetType())
            currentActions.Add(action);

        this.LotusActions[action.ActionType] = currentActions;
    }
}