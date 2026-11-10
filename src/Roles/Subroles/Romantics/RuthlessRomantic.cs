using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.NeutralKilling;
using UnityEngine;
using VentLib.Options.UI;

namespace Lotus.Roles.Subroles.Romantics;

public class RuthlessRomantic : NeutralKillingBase
{
    [UIComponent(UI.Cooldown)]
    private Cooldown cooldown;
    private bool usesPetToKill;

    protected override void PostSetup()
    {
        usesPetToKill = !MyPlayer.GetVanillaRole().IsImpostor();
        cooldown.SetDuration(GetOverride(Override.KillCooldown)?.GetValue() as float? ?? AUSettings.KillCooldown());
    }

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(LotusActionType.OnPet)]
    public void EnablePetKill()
    {
        if (!usesPetToKill || cooldown.NotReady()) return;
        PlayerControl? target = MyPlayer.GetPlayersInAbilityRangeSorted().FirstOrDefault();
        if (target == null) return;
        cooldown.Start();
        MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this));
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => AddKillCooldownOptions(base.RegisterOptions(optionStream));

    public override RoleType GetRoleType() => RoleType.Variation;

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleFlags(RoleFlag.VariationRole)
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet)
            .RoleColor(new Color(0.23f, 0f, 0.24f, 0.98f));
}