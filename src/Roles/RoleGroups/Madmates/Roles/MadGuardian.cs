using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;

namespace Lotus.Roles.RoleGroups.Madmates.Roles;

public class MadGuardian : MadCrewmate
{
    [RoleAction(LotusActionType.Interaction)]
    private void MadGuardianAttacked(PlayerControl actor, Interaction interaction, ActionHandle handle)
    {
        if (interaction.Intent is not (IFatalIntent or IHostileIntent)) return;
        handle.Cancel();
    }
    protected override string ForceRoleImageDirectory() => "RoleOutfits/Imposter/madguardian";
}