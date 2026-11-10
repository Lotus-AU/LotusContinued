using AmongUs.GameOptions;

namespace Lotus.Roles.RoleGroups.Vanilla;

public class Influencer: CustomRole
{
    public virtual bool CanBeKilled() => false;

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.VanillaRole(RoleTypes.SpiritGuide);
}