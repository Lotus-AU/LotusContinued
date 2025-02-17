using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using UnityEngine;
using VentLib.Localization.Attributes;
using Lotus.Roles.Internals.Enums;
using VentLib.Options.UI;

namespace Lotus.Roles.RoleGroups.NeutralKilling;

public class Juggernaut : NeutralKillingBase
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Juggernaut));
    private bool canVent;
    private bool impostorVision;
    private float decreaseBy;

    private int kills;

    [UIComponent(UI.Counter, ViewMode.Additive, GameState.Roaming, GameState.InMeeting)]
    public string KillCounter() => RoleUtils.Counter(kills, color: RoleColor);

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (!base.TryKill(target)) return false;
        kills++;
        log.Trace($"Juggernaut Kill Cooldown {KillCooldown - (decreaseBy * kills)}");
        SyncOptions();
        return true;
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddKillCooldownOptions(base.RegisterOptions(optionStream))
            .SubOption(sub => sub
                .KeyName("Decrease Amount Each Kill", Translations.Options.CooldownReductionPerKill)
                .BindFloat(v => decreaseBy = v)
                .AddFloatRange(0, 30, 0.5f, 5)
                .Build())
            .SubOption(sub => sub
                .KeyName("Can Vent", RoleTranslations.CanVent)
                .BindBool(v => canVent = v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .KeyName("Can Sabotage", RoleTranslations.CanSabotage)
                .BindBool(v => canSabotage = v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .KeyName("Impostor Vision", RoleTranslations.ImpostorVision)
                .BindBool(v => impostorVision = v)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.55f, 0f, 0.3f, 1f))
            .CanVent(canVent)
            .OptionOverride(Override.ImpostorLightMod, () => AUSettings.CrewLightMod(), () => !impostorVision)
            .OptionOverride(Override.KillCooldown, () => KillCooldown - (kills * decreaseBy));

    [Localized(nameof(Juggernaut))]
    private static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(CooldownReductionPerKill))]
            public static string CooldownReductionPerKill = "Cooldown Reduction per Kill";
        }
    }
}