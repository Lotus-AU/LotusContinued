using System;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API.Vanilla.Meetings;
using Lotus.Extensions;
using Lotus.Options;
using Lotus.Roles.Internals;
using Lotus.RPC;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Optionals;
using Lotus.API.Player;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Camouflager : Shapeshifter
{
    private bool canVent;
    private DateTime lastShapeshift;
    private DateTime lastUnshapeshift;
    private bool camouflaged;

    [RoleAction(LotusActionType.Attack)]
    public new bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(LotusActionType.Shapeshift)]
    private void CamouflagerShapeshift(PlayerControl target)
    {
        if (camouflaged) return;
        Players.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId && p.PlayerId != target.PlayerId).Do(p => p.CRpcShapeshift(target, true));
        camouflaged = true;
    }

    [RoleAction(LotusActionType.Unshapeshift)]
    private void CamouflagerUnshapeshift(ActionHandle handle)
    {
        if (!camouflaged) return;
        camouflaged = false;
        Players.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).Do(p => p.CRpcRevertShapeshift(true));
    }

    // [RoleAction(LotusActionType.ReportBody, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath, priority: API.Priority.VeryLow)]
    // private void HandleMeetingCall(PlayerControl reporter, Optional<NetworkedPlayerInfo> reported, ActionHandle handle)
    // {
    //     if (!camouflaged) return;
    //     camouflaged = false;
    //     Players.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).Do(p => p.CRpcRevertShapeshift(true));
    //     handle.Cancel();
    //     Async.Schedule(() => MeetingPrep.PrepMeeting(reporter, reported.OrElse(null!)), NetUtils.DeriveDelay(0.5f));
    // }
    [RoleAction(LotusActionType.RoundEnd, ActionFlag.WorksAfterDeath, priority: API.Priority.High)]
    private void HandleMeetingCall()
    {
        if (!camouflaged) return;
        camouflaged = false;
        Players.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).Do(p => p.CRpcRevertShapeshift(true));
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    private void HandlePlayerDeath()
    {
        if (!camouflaged) return;
        camouflaged = false;
        Players.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).Do(p => p.CRpcRevertShapeshift(true));
    }


    [RoleAction(LotusActionType.Shapeshift, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath, priority: API.Priority.VeryLow)]
    private void StopShapeshift(PlayerControl player, ActionHandle handle)
    {
        if (!camouflaged) return;
        if (player.PlayerId != MyPlayer.PlayerId) handle.Cancel();
    }
    [RoleAction(LotusActionType.Unshapeshift, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath, priority: API.Priority.VeryLow)]
    private void StopUnShapeshift(PlayerControl player, ActionHandle handle)
    {
        if (!camouflaged) return;
        if (player.PlayerId != MyPlayer.PlayerId) handle.Cancel();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Camouflage Cooldown")
                .Bind(v => ShapeshiftCooldown = (float)v)
                .AddFloatRange(5, 120, 2.5f, 5, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .Name("Camouflage Duration")
                .Bind(v => ShapeshiftDuration = (float)v)
                .AddFloatRange(5, 60, 2.5f, 5, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .Name("Can Vent")
                .Bind(v => canVent = (bool)v)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).CanVent(canVent);
}