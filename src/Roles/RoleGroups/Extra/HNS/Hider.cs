using Lotus.Roles.RoleGroups.Vanilla;

namespace Lotus.Roles.RoleGroups.Extra.HNS;

public class Hider: Engineer
{
    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleFlags(RoleFlag.Hidden | RoleFlag.DontRegisterOptions);
}