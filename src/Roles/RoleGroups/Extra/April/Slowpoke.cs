using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.Subroles;
using UnityEngine;
using VentLib.Utilities.Collections;

namespace Lotus.Roles.RoleGroups.Extra.April;

public class Slowpoke: Subrole, IAprilFoolsRole
{
    private Remote<GameOptionOverride> speedOverride;
    public override string Identifier() => "《";

    public override string GetRoleOutfitPath() => "RoleOutfits/Neutral/Jester".ToLower();


    protected override void PostSetup()
    {
        base.PostSetup();
        speedOverride = AddOverride(new MultiplicativeOverride(Override.PlayerSpeedMod, 0.25f));
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    private void MyDeath()
    {
        if (!speedOverride.IsDeleted()) speedOverride.Delete();
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleFlags(((IAprilFoolsRole)this).HideIfNotAprilFools(RoleFlags))
            .RoleColor(new Color(0.5f, 0.7f, 0.9f));
}