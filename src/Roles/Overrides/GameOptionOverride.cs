#nullable enable
using System;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.Logging;
using UnityEngine;

namespace Lotus.Roles.Overrides;

public class GameOptionOverride
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(GameOptionOverride));

    public readonly Override Option;
    protected readonly Func<bool>? Condition;
    private readonly object? value;
    private readonly Func<object>? supplier;
    public object? ForceValue;

    protected object? DebugValue;

    public GameOptionOverride(Override option, object? value, Func<bool>? condition = null)
    {
        this.Option = option;
        this.value = value;
        this.Condition = condition;
    }

    public GameOptionOverride(Override option, Func<object> valueSupplier, Func<bool>? condition = null)
    {
        this.Option = option;
        this.supplier = valueSupplier;
        this.Condition = condition;
    }

    public virtual bool CanApply() => Condition == null ? true : Condition.Invoke();

    public virtual void ApplyTo(IGameOptions options)
    {
        if (!CanApply()) return;

        object? value = GetValue();

        switch (Option)
        {
            case Override.AnonymousVoting:
                options.SetBool(BoolOptionNames.AnonymousVotes, (bool)(value ?? AUSettings.AnonymousVotes()));
                break;
            case Override.ConfirmEjects:
                options.SetBool(BoolOptionNames.ConfirmImpostor, (bool)(value ?? AUSettings.ConfirmImpostor()));
                break;
            case Override.DiscussionTime:
                options.SetInt(Int32OptionNames.DiscussionTime, (int)(value ?? AUSettings.DiscussionTime()));
                break;
            case Override.VotingTime:
                options.SetInt(Int32OptionNames.VotingTime, (int)(value ?? AUSettings.VotingTime()));
                break;
            case Override.PlayerSpeedMod:
                options.SetFloat(FloatOptionNames.PlayerSpeedMod, Mathf.Clamp((float)(value ?? AUSettings.PlayerSpeedMod()), 0, ModConstants.MaxPlayerSpeed));
                break;
            case Override.CrewLightMod:
                options.SetFloat(FloatOptionNames.CrewLightMod, (float)(value ?? AUSettings.CrewLightMod()));
                break;
            case Override.ImpostorLightMod:
                options.SetFloat(FloatOptionNames.ImpostorLightMod, (float)(value ?? AUSettings.ImpostorLightMod()));
                break;
            case Override.KillCooldown:
                options.SetFloat(FloatOptionNames.KillCooldown, Mathf.Clamp((float)(value ?? AUSettings.KillCooldown()), 0.1f, float.MaxValue));
                break;
            case Override.ShapeshiftDuration:
                options.SetFloat(FloatOptionNames.ShapeshifterDuration, (float)(value ?? AUSettings.ShapeshifterDuration()));
                break;
            case Override.ShapeshiftCooldown:
                options.SetFloat(FloatOptionNames.ShapeshifterCooldown, (float)(value ?? AUSettings.ShapeshifterCooldown()));
                break;
            case Override.GuardianAngelDuration:
                options.SetFloat(FloatOptionNames.ProtectionDurationSeconds, (float)(value ?? AUSettings.ProtectionDurationSeconds()));
                break;
            case Override.GuardianAngelCooldown:
                options.SetFloat(FloatOptionNames.GuardianAngelCooldown, (float)(value ?? AUSettings.GuardianAngelCooldown()));
                break;
            case Override.KillDistance:
                options.SetInt(Int32OptionNames.KillDistance, (int)(value ?? AUSettings.KillDistance()));
                break;
            case Override.EngVentCooldown:
                options.SetFloat(FloatOptionNames.EngineerCooldown, (float)(value ?? AUSettings.EngineerCooldown()));
                break;
            case Override.EngVentDuration:
                options.SetFloat(FloatOptionNames.EngineerInVentMaxTime, (float)(value ?? AUSettings.EngineerInVentMaxTime()));
                break;
            case Override.VitalsCooldown:
                options.SetFloat(FloatOptionNames.ScientistCooldown, (float)(value ?? AUSettings.ScientistCooldown()));
                break;
            case Override.VitalsBatteryCharge:
                options.SetFloat(FloatOptionNames.ScientistBatteryCharge, (float)(value ?? AUSettings.ScientistBatteryCharge()));
                break;
            case Override.TrackerCooldown:
                options.SetFloat(FloatOptionNames.TrackerCooldown, (float)(value ?? AUSettings.TrackerCooldown()));
                break;
            case Override.TrackerDelay:
                options.SetFloat(FloatOptionNames.TrackerDelay, (float)(value ?? AUSettings.TrackerDelay()));
                break;
            case Override.TrackerDuration:
                options.SetFloat(FloatOptionNames.TrackerDuration, (float)(value ?? AUSettings.TrackerDuration()));
                break;
            case Override.NoiseAlertDuration:
                options.SetFloat(FloatOptionNames.NoisemakerAlertDuration, (float)(value ?? AUSettings.NoisemakerAlertDuration()));
                break;
            case Override.NoiseImpGetAlert:
                options.SetBool(BoolOptionNames.NoisemakerImpostorAlert, (bool)(value ?? AUSettings.NoisemakerImpostorAlert()));
                break;
            case Override.PhantomVanishCooldown:
                options.SetFloat(FloatOptionNames.PhantomCooldown, (float)(value ?? AUSettings.PhantomCooldown()));
                break;
            case Override.PhantomVanishDuration:
                options.SetFloat(FloatOptionNames.PhantomDuration, (float)(value ?? AUSettings.PhantomDuration()));
                break;
            case Override.DetectiveSuspectsPerCase:
                options.SetFloat(FloatOptionNames.DetectiveSuspectLimit, (float)(value ?? AUSettings.DetectiveSuspectsPerCase()));
                break;
            case Override.ViperDissolveTime:
                options.SetFloat(FloatOptionNames.ViperDissolveTime, (float)(value ?? AUSettings.ViperDissolveTime()));
                break;
            case Override.ShapeshifterEvidence:
                options.SetBool(BoolOptionNames.ShapeshifterLeaveSkin, (bool)(value ?? AUSettings.ShapeshifterLeaveSkin()));
                break;
            case Override.CanUseVent:
            default:
                log.Warn($"Invalid Option Override: {this}", "ApplyOverride");
                break;
        }

        DevLogger.Low($"Applying Override: {Option} => {DebugValue}");
    }

    public virtual object? GetValue() => DebugValue = supplier == null ? (ForceValue ?? value) : supplier.Invoke();

    public override bool Equals(object? obj)
    {
        if (obj is not GameOptionOverride @override) return false;
        return @override.Option == this.Option;
    }

    public override int GetHashCode()
    {
        return this.Option.GetHashCode();
    }

    public override string ToString()
    {
        return $"GameOptionOverride(override={Option}, value={value})";
    }
}