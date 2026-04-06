using System;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.Subroles;
using UnityEngine;
using Lotus.Extensions;
using VentLib.Utilities;
using VentLib.Utilities.Collections;

namespace Lotus.Roles.RoleGroups.Extra.April;

public class Lagger: Subrole, IAprilFoolsRole
{
    private System.Random randomObject;
    private Remote<GameOptionOverride> lagOverride;

    private bool hasStarted;

    public override string Identifier() => "☹";

    public override string GetRoleOutfitPath() => "RoleOutfits/Neutral/Jester".ToLower();

    [RoleAction(LotusActionType.RoundStart)]
    private void StartLagLoop()
    {
        if (hasStarted || !MyPlayer.IsAlive()) return;
        hasStarted = true;
        randomObject = new System.Random(DateTime.Now.Millisecond);
        Async.Schedule(SendLagEvent, randomObject.Next(5f, 30f));
    }

    [RoleAction(LotusActionType.RoundEnd)]
    [RoleAction(LotusActionType.PlayerDeath)]
    private void EndLagLoop()
    {
        if (!hasStarted) return;
        hasStarted = false;
        if (lagOverride != null!)
            lagOverride.Delete();

    }

    private void SendLagEvent()
    {
        if (!hasStarted) return;
        lagOverride = AddOverride(new MultiplicativeOverride(Override.PlayerSpeedMod, 0.01f));
        SyncOptions();
        Async.Schedule(EndLagEvent, randomObject.Next(0.05f, 4f));
    }

    private void EndLagEvent()
    {
        if (!hasStarted) return;
        lagOverride.Delete();
        SyncOptions();
        Async.Schedule(SendLagEvent, randomObject.Next(0.1f, 20f));
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleFlags(((IAprilFoolsRole)this).HideIfNotAprilFools(RoleFlags))
            .RoleColor(new Color(0.16f, 0.2f, 0f));
}