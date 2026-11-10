using Lotus.Factions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Options.UI;

namespace Lotus.Roles.RoleGroups.Madmates.Roles;

public class Parasite : Shapeshifter
{
    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    public override bool CanSabotage() => true;
    protected override string ForceRoleImageDirectory() => "RoleOutfits/Imposter/parasite";

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => AddKillCooldownOptions(base.RegisterOptions(optionStream));

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .SpecialType(SpecialType.Madmate)
            .RoleColor(new Color(0.73f, 0.18f, 0.02f))
            .Faction(FactionInstances.Madmates);
}