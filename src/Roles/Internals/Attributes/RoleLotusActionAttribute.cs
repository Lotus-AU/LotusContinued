extern alias JBAnnotations;
using System;
using System.Collections.Generic;
using JBAnnotations::JetBrains.Annotations;
using Lotus.API;
using Lotus.API.Reactive.Actions;
using Lotus.Roles.Internals.Enums;

// ReSharper disable InvalidXmlDocComment

namespace Lotus.Roles.Internals.Attributes;

[UsedImplicitly]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)] // Inherited = false because inheritance is managed through Subclassing, DO NOT WORRY!
public class RoleActionAttribute : LotusActionAttribute
{
    public ActionFlag ActionFlags { get; }
    public bool WorksAfterDeath { get; }
    public bool Blockable { set; get; }
    /// <summary>
    /// If provided, overrides any methods of the same action with the same name from any parent classes
    /// </summary>
    public String? Override;

    public RoleActionAttribute(LotusActionType actionType, ActionFlag actionFlags = ActionFlag.None, Priority priority = Priority.Normal) : base(actionType, priority)
    {
        this.ActionFlags = actionFlags;
        this.WorksAfterDeath = actionFlags.HasFlag(ActionFlag.WorksAfterDeath) || actionType is LotusActionType.PlayerDeath or LotusActionType.Exiled;
        this.Blockable = !actionFlags.HasFlag(ActionFlag.Unblockable) && actionType is not LotusActionType.Vote;
    }

    public override string ToString() => $"RoleAction(type={ActionType}, Priority={Priority}, Blockable={Blockable}, Subclassing={Subclassing}, Override={Override})";
}

public static class LotusActionTypeMethods
{
    // ReSharper disable once CollectionNeverUpdated.Global
    public static readonly HashSet<LotusActionType> PlayerActions = [];

    public static bool IsPlayerAction(this LotusActionType actionType)
    {
        return actionType switch
        {
            LotusActionType.None => false,
            LotusActionType.PlayerAction => false,
            LotusActionType.OnPet => true,
            LotusActionType.VentEntered => true,
            LotusActionType.VentExit => true,
            LotusActionType.SuccessfulAngelProtect => false,
            LotusActionType.SabotageStarted => true,
            LotusActionType.SabotagePartialFix => true,
            LotusActionType.SabotageFixed => true,
            LotusActionType.Shapeshift => true,
            LotusActionType.Unshapeshift => true,
            LotusActionType.Attack => true,
            LotusActionType.Vanish => true,
            LotusActionType.Appear => true,
            LotusActionType.Zipline => true,
            LotusActionType.PlayerDeath => false,
            LotusActionType.Exiled => false,
            LotusActionType.RoundStart => false,
            LotusActionType.RoundEnd => false,
            LotusActionType.ReportBody => true,
            LotusActionType.TaskComplete => false,
            LotusActionType.FixedUpdate => false,
            LotusActionType.Vote => true,
            LotusActionType.Interaction => false,
            LotusActionType.OnHoldPet => false,
            LotusActionType.OnPetRelease => false,
            LotusActionType.Chat => false,
            LotusActionType.Disconnect => false,
            LotusActionType.VotingComplete => false,
            _ => PlayerActions.Contains(actionType)
        };
    }
}