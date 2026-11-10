using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Extra.Speedrun;

public class Tasker: Crewmate
{
    private bool isKiller;

    [RoleAction(LotusActionType.Attack)]
    private void TryKill(PlayerControl target)
    {
        if (!isKiller) return;
        InteractionResult result = MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this));
        Game.MatchData.GameHistory.AddEvent(new KillEvent(MyPlayer, target, result is InteractionResult.Proceed));
    }

    protected override void OnTaskComplete(Optional<NormalPlayerTask> _)
    {
        int remainingTasks = TotalTasks - TasksComplete;
        if (remainingTasks != 0) return;
        if (!MyPlayer.IsAlive()) return;
        ActivateKillerMode();
    }

    private void ActivateKillerMode()
    {
        if (isKiller) return;
        isKiller = true;

        BaseCanVent = ExtraGamemodeOptions.SpeedrunOptions.KillingTaskersCanVent;
        VirtualRole = RoleTypes.Impostor;
        RoleColor = Color.red;
        Assign(true);
        SyncOptions();
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    private void RemoveTasksOnDeath()
    {
        MyPlayer.Data.RpcSetTasks(new Il2CppStructArray<byte>(0));
    }

    public override RoleType GetRoleType() => RoleType.DontShow;

    public override string GetRoleOutfitPath() => $"RoleOutfits/Extra/{EnglishRoleName}".ToLower();

    protected override RoleModifier Modify(RoleModifier roleModifier) => base
        .Modify(roleModifier)
        .RoleFlags(RoleFlag.DontRegisterOptions | RoleFlag.Hidden)
        .CanVent(ExtraGamemodeOptions.SpeedrunOptions.TaskersCanVent)
        .VanillaRole(ExtraGamemodeOptions.SpeedrunOptions.TaskersCanVent ? RoleTypes.Engineer : RoleTypes.Crewmate)
        .OptionOverride(Override.KillCooldown, () => ExtraGamemodeOptions.SpeedrunOptions.KillCooldown,
            () => ExtraGamemodeOptions.SpeedrunOptions.KillCooldown > 0)
        .OptionOverride(Override.EngVentCooldown, ExtraGamemodeOptions.SpeedrunOptions.VentCooldown)
        .OptionOverride(Override.EngVentDuration, ExtraGamemodeOptions.SpeedrunOptions.VentDuration)
        .RoleColor(new Color(0.17f, 0.41f, 0.86f));
}