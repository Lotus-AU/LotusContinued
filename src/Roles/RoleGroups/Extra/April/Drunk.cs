using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.Subroles;
using UnityEngine;
using VentLib.Utilities.Collections;

namespace Lotus.Roles.RoleGroups.Extra.April;

public class Drunk: Subrole, IAprilFoolsRole
{
    private Remote<GameOptionOverride> reverseOverride;
    public override string Identifier() => "¿";

    public override string GetRoleOutfitPath() => "RoleOutfits/Neutral/Jester".ToLower();

    protected override void PostSetup()
    {
        base.PostSetup();
        reverseOverride = AddOverride(new MultiplicativeOverride(Override.PlayerSpeedMod, -1f));
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    private void MyDeath()
    {
        if (!reverseOverride.IsDeleted()) reverseOverride.Delete();
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleFlags(((IAprilFoolsRole)this).HideIfNotAprilFools(RoleFlags))
            .RoleColor(new Color(0.46f, 0.5f, 0f));
}