using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Roles.Overrides;
using VentLib.Networking.RPC;
using VentLib.Utilities;

namespace Lotus.GameModes.Normal.Standard;

public class NormalStandardRoleOperations : RoleOperations
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(NormalStandardRoleOperations));
    public GameMode ParentGameMode { get; }

    public NormalStandardRoleOperations(GameMode parentGamemode)
    {
        ParentGameMode = parentGamemode;
    }

    public void Assign(CustomRole role, PlayerControl player, bool addAsMainRole = true, bool sendToClient = false)
    {
        if (addAsMainRole) Game.AssignRole(player, role, sendToClient);
        else Game.AssignSubRole(player, role, sendToClient);
        // if (player.AmOwner) role.GUIProvider.Start();
    }

    public Relation Relationship(CustomRole source, CustomRole comparison) => source.Relationship(comparison);

    public Relation Relationship(CustomRole source, IFaction comparison) => comparison.Relationship(source.Faction);

    public void SyncOptions(PlayerControl target, IEnumerable<CustomRole> definitions, IEnumerable<GameOptionOverride>? overrides = null, bool deepSet = false)
    {
        if (target == null || !AmongUsClient.Instance.AmHost) return;

        overrides = CalculateOverrides(target, definitions, overrides);

        IGameOptions modifiedOptions = DesyncOptions.GetModifiedOptions(overrides);
        if (deepSet) RpcV3.Immediate(PlayerControl.LocalPlayer.NetId, RpcCalls.SyncSettings).Write(modifiedOptions).Send(target.GetClientId());
        DesyncOptions.SyncToPlayer(modifiedOptions, target);
    }

    public ActionHandle Trigger(LotusActionType action, PlayerControl? source, ActionHandle handle, params object[] parameters)
    {
        // if (action is not LotusActionType.FixedUpdate)
        // {
        //     Game.CurrentGameMode.Trigger(action, handle, parameters);
        //     if (handle.Cancellation is not (ActionHandle.CancelType.Soft or ActionHandle.CancelType.None)) return handle;
        // }

        return TriggerFor(Players.GetPlayers().SelectMany(p => p.GetAllRoleDefinitions()), action, source, handle, parameters);
    }

    public ActionHandle TriggerFor(IEnumerable<CustomRole> recipients, LotusActionType action, PlayerControl? source, ActionHandle handle, params object[] parameters)
    {
        if (action == LotusActionType.FixedUpdate)
        {
            foreach (CustomRole role in recipients)
            {
                if (role.RoleActions.TryGetValue(action, out List<RoleAction>? actions) && actions.Count > 0)
                {
                    if (!actions[0].CanExecute(role.MyPlayer, null)) continue;
                    actions[0].SetExecuter(role);
                    actions[0].ExecuteFixed();
                }
            }
            return handle;
        }

        // Ensure handle is included in parameters
        List<object> parameterList = new(parameters) { handle };
        parameters = parameterList.ToArray();
        object[] globalActionParameters = parameters;
        if (source != null)
        {
            globalActionParameters = new object[parameters.Length + 1];
            Array.Copy(parameters, 0, globalActionParameters, 1, parameters.Length);
            globalActionParameters[0] = source;
        }

        IEnumerable<(RoleAction, AbstractBaseRole)> actionsAndDefinitions = recipients.SelectMany(cR => cR.GetActions(action)).OrderBy(t => t.Item1.Attribute.Priority);
        foreach ((RoleAction roleAction, AbstractBaseRole roleDefinition) in actionsAndDefinitions)
        {
            PlayerControl myPlayer = roleDefinition.MyPlayer;

            if (myPlayer == null)
            {
                log.Exception($"MyPlayer is null for role {roleDefinition.GetType().Name} in action {roleAction.Attribute.ActionType}");
                continue;
            }

            if (handle.Cancellation is not (ActionHandle.CancelType.None or ActionHandle.CancelType.Soft)) continue;
            if (!roleAction.CanExecute(myPlayer, source)) continue;

            if (roleAction.Attribute.ActionType.IsPlayerAction() && source == myPlayer)
            {
                Hooks.PlayerHooks.PlayerActionHook.Propagate(new PlayerActionHookEvent(myPlayer, roleAction, parameters));
                Trigger(LotusActionType.PlayerAction, myPlayer, handle, roleAction, parameters);
            }

            handle.ActionType = action;

            if (handle.Cancellation is not (ActionHandle.CancelType.None or ActionHandle.CancelType.Soft)) continue;

            roleAction.SetExecuter(roleDefinition); // Ensure correct executer is set

            var sentParams = roleAction.Flags.HasFlag(ActionFlag.GlobalDetector) ? globalActionParameters : parameters;
            var methodParameters = roleAction.Method.GetParameters();
            var invokeParameters = new object[methodParameters.Length];
            var sentParamsList = sentParams.ToList();
            var usedIndices = new HashSet<int>();

            for (int i = 0; i < methodParameters.Length; i++)
            {
                var methodParam = methodParameters[i];
                bool parameterAssigned = false;

                // Try to find a matching sent parameter by type and order
                for (int j = 0; j < sentParamsList.Count; j++)
                {
                    // if (!usedIndices.Contains(j) && sentParamsList[j].GetType() == methodParam.ParameterType)
                    if (!usedIndices.Contains(j) && methodParam.ParameterType.IsAssignableFrom(sentParamsList[j].GetType()))
                    {
                        invokeParameters[i] = sentParamsList[j];
                        usedIndices.Add(j);
                        parameterAssigned = true;
                        break;
                    }
                }

                if (!parameterAssigned)
                {
                    if (methodParam.HasDefaultValue)
                    {
                        invokeParameters[i] = methodParam.DefaultValue;
                    }
                    else
                    {
                        throw new ArgumentException($"Missing required parameter: {methodParam.Name}:{methodParam.ParameterType}, {action}, {roleAction.Method}");
                        // invokeParameters[i] = Type.Missing;
                    }
                }
            }

            roleAction.Execute(invokeParameters.ToArray());
        }

        return handle;
    }

    protected IEnumerable<GameOptionOverride> CalculateOverrides(PlayerControl player, IEnumerable<CustomRole> definitions, IEnumerable<GameOptionOverride>? overrides)
    {
        IEnumerable<GameOptionOverride> definitionOverrides = definitions.SelectMany(d => d.GetRoleOverrides());
        if (overrides != null) definitionOverrides = definitionOverrides.Concat(overrides);
        return definitionOverrides;
    }

    public IRoleComponent Instantiate(GameMode setupHelper, PlayerControl player) => this;
}