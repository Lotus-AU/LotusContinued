using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Utilities;
using Lotus.Extensions;
using VentLib.Options.UI;
using VentLib.Utilities;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Options;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Ninja : Vanilla.Impostor
{
    private List<PlayerControl> playerList;
    private bool playerTeleportsToNinja;
    public NinjaMode Mode = NinjaMode.Killing;
    private ActivationType activationType;


    private float ShapeshiftCooldown;
    private float ShapeshiftDuration;

    [UIComponent(UI.Text)]
    private string CurrentMode() => RoleColor.Colorize(Mode == NinjaMode.Hunting ? "(Hunting)" : "(Killing)");

    protected override void Setup(PlayerControl player) => playerList = new List<PlayerControl>();

    [RoleAction(LotusActionType.Attack)]
    public new bool TryKill(PlayerControl target)
    {
        SyncOptions();
        if (Mode is NinjaMode.Killing) return base.TryKill(target);
        if (MyPlayer.InteractWith(target, LotusInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt) return false;

        playerList.Add(target);
        MyPlayer.RpcMark(target);
        return true;
    }

    [RoleAction(LotusActionType.Shapeshift)]
    private void NinjaTargetCheck()
    {
        if (activationType is not ActivationType.Shapeshift) return;
        Mode = NinjaMode.Hunting;
    }

    [RoleAction(LotusActionType.Unshapeshift)]
    private void NinjaUnShapeShift()
    {
        if (activationType is not ActivationType.Shapeshift) return;
        NinjaHuntAbility();
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void EnterKillMode() => Mode = NinjaMode.Killing;

    [RoleAction(LotusActionType.RoundEnd)]
    private void NinjaClearTarget() => playerList.Clear();

    [RoleAction(LotusActionType.OnPet)]
    public void SwitchMode()
    {
        if (activationType is not ActivationType.PetButton) return;

        if (Mode is NinjaMode.Hunting) NinjaHuntAbility();

        Mode = Mode is NinjaMode.Killing ? NinjaMode.Hunting : NinjaMode.Killing;
    }

    private void NinjaHuntAbility()
    {
        if (playerList.Count == 0) return;
        foreach (var target in playerList.Where(target => target.IsAlive()))
        {
            if (!playerTeleportsToNinja)
                MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this));
            else
            {
                Utils.Teleport(target.NetTransform, MyPlayer.transform.position);
                Async.Schedule(() => MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this)), 0.25f);
            }
        }

        playerList.Clear();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .SubOption(sub => sub
            .Name("Players Teleport to Ninja")
            .BindBool(v => playerTeleportsToNinja = v)
            .AddOnOffValues(false)
            .Build())
        .SubOption(sub => sub
            .Name("Ninja Ability Activation")
            .BindInt(v => activationType = (ActivationType)v)
            .Value(v => v.Text("Pet Button").Value(0).Build())
            .Value(v => v.Text("Shapeshift Button").Value(1).Build())
            .ShowSubOptionPredicate(v => (int)v == 1)
            .SubOption(sub => sub.KeyName("Shapeshift Cooldown", Shapeshifter.Translations.Options.ShapeshiftCooldown)
                .AddFloatRange(0, 120, 2.5f, 12, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => ShapeshiftCooldown = f)
                .Build())
            .SubOption(sub => sub.KeyName("Shapeshift Duration", Shapeshifter.Translations.Options.ShapeshiftDuration)
                .Value(1f)
                .AddFloatRange(2.5f, 120, 2.5f, 6, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => ShapeshiftDuration = f)
                .Build())
            .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .VanillaRole(activationType is ActivationType.Shapeshift ? RoleTypes.Shapeshifter : RoleTypes.Impostor)
            .OptionOverride(new IndirectKillCooldown(KillCooldown, () => Mode is NinjaMode.Hunting))
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet)
            .OptionOverride(Override.ShapeshiftCooldown, () => ShapeshiftCooldown)
            .OptionOverride(Override.ShapeshiftDuration, () => ShapeshiftDuration);

    public enum NinjaMode
    {
        Killing,
        Hunting
    }
}