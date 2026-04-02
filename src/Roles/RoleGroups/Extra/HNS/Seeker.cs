using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Vanilla;

namespace Lotus.Roles.RoleGroups.Extra.HNS;

public class Seeker: Impostor
{
    [RoleAction(LotusActionType.Attack, Subclassing = false)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleFlags(RoleFlag.Hidden | RoleFlag.DontRegisterOptions);
}