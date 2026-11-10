using System;
using System.Reflection;
using Lotus.API.Reactive.Actions;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Roles.Internals.Attributes;

namespace Lotus.Roles.Internals;

public class RoleAction : LotusAction
{
    public ActionFlag Flags { get; }
    public bool TriggerWhenDead { get; }
    public bool Blockable;
    public RoleAction(RoleActionAttribute attribute, MethodInfo method, object executer) : base(attribute, method)
    {
        this.Executer = executer ?? throw new ArgumentNullException(nameof(executer));
        this.TriggerWhenDead = attribute.WorksAfterDeath;
        this.Blockable = attribute.Blockable;
        this.Flags = attribute.ActionFlags;
    }

    public bool CanExecute(PlayerControl executer, PlayerControl? source)
    {
        if (!executer.IsAlive() && !TriggerWhenDead) return false;
        if (!source) return true;
        return Flags.HasFlag(ActionFlag.GlobalDetector) || executer.PlayerId == source!.PlayerId;
    }

    public new RoleAction Clone()
    {
        return (RoleAction)this.MemberwiseClone();
    }
    public override string ToString() => $"RoleAction(type={ActionType}, executer={Executer}, priority={Priority}, method={Method}))";
}