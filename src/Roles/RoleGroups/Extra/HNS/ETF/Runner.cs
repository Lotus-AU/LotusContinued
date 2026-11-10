using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Options;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;

namespace Lotus.Roles.RoleGroups.Extra.HNS.ETF;

public class Runner: Warden, IRoleUI
{
    private bool abilityActive;

    private float speedMultiplier;
    private float abilityCooldown;
    private float abilityDuration;

    private Cooldown visibleCooldown;

    private bool onCooldown;

    public override string GetRoleOutfitPath() => $"RoleOutfits/Extra/{EnglishRoleName}".ToLower();

    public RoleButton AbilityButton(IRoleButtonEditor button) => button
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Extra/runner_boost.png", 130, true));

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(LotusActionType.RoundStart)]
    protected override void SetupOnStart()
    {
        base.SetupOnStart();
        LiveString liveString = new(() =>
        {
            if (visibleCooldown.IsReady()) return string.Empty;

            return $"<cspace=-0.15em>{RoleUtils.HealthBar((int)visibleCooldown.TimeRemaining(),
                (int)visibleCooldown.Duration, Color.white)}</cspace>\n";
        });
        MyPlayer.NameModel().GetComponentHolder<NameHolder>()
            .Insert(0, new NameComponent(liveString, [GameState.Roaming], ViewMode.Additive, MyPlayer));
    }


    [RoleAction(LotusActionType.Vanish)]
    private void TryAbility(ActionHandle handle)
    {
        handle.Cancel();
        if (onCooldown) return;
        onCooldown = true;
        abilityActive = true;
        SyncOptions();
        visibleCooldown.SetDuration(abilityDuration);
        visibleCooldown.StartThenRun(() =>
        {
            abilityActive = false;
            SyncOptions();
            visibleCooldown.SetDuration(abilityCooldown);
            visibleCooldown.StartThenRun(() => onCooldown = false);
        });
    }

    [RoleAction(LotusActionType.FixedUpdate)]
    protected override void OnFixedUpdate() => base.OnFixedUpdate();

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Ability Speed Multiplier", Translations.Options.AbilitySpeedMultiplier)
                .AddFloatRange(1f, 2f, 0.05f, 10, "x")
                .BindFloat(f => speedMultiplier = f)
                .Build())
            .SubOption(sub => sub.KeyName("Ability Cooldown", Translations.Options.AbilityCooldown)
                .AddFloatRange(2.5f, 120, 2.5f, 11, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => abilityCooldown = f)
                .Build())
            .SubOption(sub => sub.KeyName("Ability Duration", Translations.Options.AbilityDuration)
                .AddFloatRange(2.5f, 120, 2.5f, 0, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => abilityDuration = f)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .VanillaRole(RoleTypes.Phantom)
        .OptionOverride(Override.PlayerSpeedMod,
            () => AUSettings.PlayerSpeedMod() *
                  (speedMultiplier / ExtraGamemodeOptions.EscapeOptions.WardenSpeedMultiplier),
            () => abilityActive);

    [Localized(nameof(Runner))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))]
        public static string ButtonText = "Boost";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(AbilitySpeedMultiplier))]
            public static string AbilitySpeedMultiplier = "Ability Speed Multiplier";

            [Localized(nameof(AbilityCooldown))]
            public static string AbilityCooldown = "Ability Cooldown";

            [Localized(nameof(AbilityDuration))]
            public static string AbilityDuration = "Ability Duration";
        }
    }
}