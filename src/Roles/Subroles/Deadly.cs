using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Managers;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;

namespace Lotus.Roles.Subroles;

public class Deadly : Subrole
{
    public int CooldownReduction;

    public override string Identifier() => "乂";

    [RoleAction(LotusActionType.RoundStart)]
    private void GameStart(bool isStart)
    {
        if (!isStart) return;
        MultiplicativeOverride multiplicativeOverride = new(Override.KillCooldown, (100f - CooldownReduction) / 100f);
        Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId, multiplicativeOverride);
    }

    public override bool IsAssignableTo(PlayerControl player)
    {
        if (!player.GetVanillaRole().IsImpostor()) return false;
        if (!player.PrimaryRole().RoleAbilityFlags.HasFlag(RoleAbilityFlag.IsAbleToKill)) return false;
        return base.IsAssignableTo(player);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Cooldown Reduction", DeadlyTranslations.Options.CooldownReduction)
                .AddIntRange(0, 100, 5, 5, "%")
                .BindInt(i => CooldownReduction = i)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.45f, 0.64f, 0.4f));

    [Localized(nameof(Deadly))]
    public static class DeadlyTranslations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(CooldownReduction))]
            public static string CooldownReduction = "Cooldown Reduction";

            [Localized(nameof(AssignableToNonKilling))]
            public static string AssignableToNonKilling = "Assignable to Non-Killing";
        }
    }
}