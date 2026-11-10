using AmongUs.GameOptions;
using Lotus.Factions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Options.UI;

namespace Lotus.Roles.RoleGroups.Madmates.Roles;

public class Madmate : Impostor
{
    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Can Sabotage", RoleTranslations.CanSabotage)
                .BindBool(b => canSabotage = b)
                .AddOnOffValues()
                .Build());

    protected override string ForceRoleImageDirectory() => "RoleOutfits/Imposter/madmate";

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .CanVent(true)
            .VanillaRole(canSabotage ? RoleTypes.Impostor : RoleTypes.Engineer)
            .SpecialType(SpecialType.Madmate)
            .RoleColor(ModConstants.Palette.MadmateColor)
            .Faction(FactionInstances.Madmates);
}