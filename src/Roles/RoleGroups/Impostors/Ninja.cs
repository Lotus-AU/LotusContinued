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
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Localization.Attributes;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Ninja : Vanilla.Impostor, IRoleUI
{
    private List<PlayerControl> playerList;
    private bool playerTeleportsToNinja;
    public NinjaMode Mode = NinjaMode.Killing;
    private ActivationType activationType;

    private float ShapeshiftCooldown;
    private float ShapeshiftDuration;

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        playerList = new List<PlayerControl>();
    }

    public RoleButton AbilityButton(IRoleButtonEditor abilityButton) => activationType is ActivationType.Shapeshift
        ? abilityButton
            .SetText(RoleTranslations.Switch)
            .SetSprite(() => LotusAssets.LoadSprite(Mode == NinjaMode.Hunting ? "Buttons/Imp/ninja_kill.png" : "Buttons/generic_switch_ability.png", 130, true))
        :  abilityButton.Default(true);


    public RoleButton PetButton(IRoleButtonEditor abilityButton) => activationType is ActivationType.PetButton
        ? abilityButton
            .SetText(RoleTranslations.Switch)
            .SetSprite(() => LotusAssets.LoadSprite(Mode == NinjaMode.Hunting ? "Buttons/Imp/ninja_kill.png" : "Buttons/generic_switch_ability.png", 130, true))
        : abilityButton.Default(true);

    public RoleButton KillButton(IRoleButtonEditor killButton) => killButton.Default(false);

    [UIComponent(UI.Text)]
    private string CurrentMode() => RoleColor.Colorize(Mode == NinjaMode.Hunting ? "(Hunting)" : "(Killing)");

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
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
        if (MyPlayer.AmOwner) UpdateAllButtons();
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateNinja)?.Send([MyPlayer.OwnerId], (int)Mode);
    }

    [RoleAction(LotusActionType.Unshapeshift)]
    private void NinjaUnShapeShift()
    {
        if (activationType is not ActivationType.Shapeshift) return;
        Async.Schedule(NinjaHuntAbility, .5f);
        Mode = NinjaMode.Killing;
        if (MyPlayer.AmOwner) UpdateAllButtons();
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateNinja)?.Send([MyPlayer.OwnerId], (int)Mode);
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void EnterKillMode()
    {
        Mode = NinjaMode.Killing;
        if (MyPlayer.AmOwner) UpdateAllButtons();
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateNinja)?.Send([MyPlayer.OwnerId], (int)Mode);
    }

    [RoleAction(LotusActionType.RoundEnd)]
    private void NinjaClearTarget() => playerList.Clear();

    [RoleAction(LotusActionType.OnPet)]
    public void SwitchMode()
    {
        if (activationType is not ActivationType.PetButton) return;

        if (Mode is NinjaMode.Hunting) Async.Schedule(NinjaHuntAbility, .5f);

        Mode = Mode is NinjaMode.Killing ? NinjaMode.Hunting : NinjaMode.Killing;
        if (MyPlayer.AmOwner) UpdateAllButtons();
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateNinja)?.Send([MyPlayer.OwnerId], (int)Mode);
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
                Utils.Teleport(target.NetTransform, MyPlayer.transform.position, this);
                Async.Schedule(() => MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this)), 0.25f);
            }
        }

        playerList.Clear();
    }

    private void UpdateAllButtons()
    {
        UpdateKillButton();
        if (activationType is ActivationType.Shapeshift) AbilityButton(UIManager.AbilityButton);
        else PetButton(UIManager.PetButton);
    }

    [ModRPC((uint)ModCalls.UpdateNinja, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateNinja(int modeEnum)
    {
        Ninja? ninja = PlayerControl.LocalPlayer.PrimaryRole<Ninja>();
        if (ninja is null) return;
        ninja.Mode = (NinjaMode)modeEnum;
        ninja.UpdateAllButtons();
    }

    private RoleButton UpdateKillButton() => Mode is NinjaMode.Killing
        ? UIManager.KillButton.RevertSprite().SetText(Witch.Translations.KillButtonText)
        : UIManager.KillButton.SetSprite(() => LotusAssets.LoadSprite("Buttons/Imp/ninja_hunt.png", 130, true)).SetText(Translations.HuntButtonText);

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .SubOption(sub => sub
            .KeyName("Players Teleport to Ninja", Translations.Options.TeleportToNinja)
            .BindBool(v => playerTeleportsToNinja = v)
            .AddBoolean(false)
            .Build())
        .SubOption(sub => sub
            .KeyName("Ninja Ability Activation", Translations.Options.AbilityActiviation)
            .BindInt(v => activationType = (ActivationType)v)
            .Value(v => v.Text("Pet Button").Value(0).Build())
            .Value(v => v.Text("Shapeshift Button").Value(1).Build())
            .ShowSubOptionPredicate(v => (int)v == 1)
            .SubOption(sub2 => sub2
                .KeyName("Shapeshift Cooldown", Shapeshifter.Translations.Options.ShapeshiftCooldown)
                .AddFloatRange(0, 120, 2.5f, 12, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => ShapeshiftCooldown = f)
                .Build())
            .SubOption(sub2 => sub2
                .KeyName("Shapeshift Duration", Shapeshifter.Translations.Options.ShapeshiftDuration)
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

    [Localized(nameof(Ninja))]
    public static class Translations
    {
        [Localized(nameof(HuntButtonText))]
        public static string HuntButtonText = "Hunt";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(TeleportToNinja))]
            public static string TeleportToNinja = "Players Teleport to Ninja";

            [Localized(nameof(AbilityActiviation))]
            public static string AbilityActiviation = "Ninja Ability Activation";
        }
    }
}