using System;
using AmongUs.GameOptions;
using UnityEngine;

namespace Lotus.Roles.Overrides;

public enum Override
{
    // Role overrides
    CanUseVent,


    // Game override
    AnonymousVoting,
    ConfirmEjects,

    DiscussionTime,
    VotingTime,
    PlayerSpeedMod,
    CrewLightMod,
    ImpostorLightMod,
    KillCooldown,
    KillDistance,
    VitalsCooldown,
    VitalsBatteryCharge,

    // Role specific overrides
    ShapeshiftDuration,
    ShapeshiftCooldown,
    ShapeshifterEvidence,

    GuardianAngelDuration,
    GuardianAngelCooldown,

    EngVentCooldown,
    EngVentDuration,

    TrackerCooldown,
    TrackerDuration,
    TrackerDelay,

    NoiseImpGetAlert,
    NoiseAlertDuration,

    PhantomVanishDuration,
    PhantomVanishCooldown,

    DetectiveSuspectsPerCase,

    ViperDissolveTime
}

public static class OverrideExtensions
{
    public static object GetValue(this Override __override, IGameOptions gameOptions)
    {
        return __override switch
        {
            Override.CanUseVent => true,
            Override.AnonymousVoting => gameOptions.GetBool(BoolOptionNames.AnonymousVotes),
            Override.ConfirmEjects => gameOptions.GetBool(BoolOptionNames.ConfirmImpostor),
            Override.DiscussionTime => gameOptions.GetInt(Int32OptionNames.DiscussionTime),
            Override.VotingTime => gameOptions.GetInt(Int32OptionNames.VotingTime),
            Override.PlayerSpeedMod => gameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod),
            Override.CrewLightMod => gameOptions.GetFloat(FloatOptionNames.CrewLightMod),
            Override.ImpostorLightMod => gameOptions.GetFloat(FloatOptionNames.ImpostorLightMod),
            Override.KillCooldown => gameOptions.GetFloat(FloatOptionNames.KillCooldown),
            Override.KillDistance => gameOptions.GetInt(Int32OptionNames.KillDistance),
            Override.ShapeshiftDuration => gameOptions.GetFloat(FloatOptionNames.ShapeshifterDuration),
            Override.ShapeshiftCooldown => gameOptions.GetFloat(FloatOptionNames.ShapeshifterCooldown),
            Override.ShapeshifterEvidence => gameOptions.GetBool(BoolOptionNames.ShapeshifterLeaveSkin),
            Override.GuardianAngelDuration => gameOptions.GetFloat(FloatOptionNames.ProtectionDurationSeconds),
            Override.GuardianAngelCooldown => gameOptions.GetFloat(FloatOptionNames.GuardianAngelCooldown),
            Override.EngVentCooldown => gameOptions.GetFloat(FloatOptionNames.EngineerCooldown),
            Override.EngVentDuration => gameOptions.GetFloat(FloatOptionNames.EngineerInVentMaxTime),
            Override.VitalsCooldown => gameOptions.GetFloat(FloatOptionNames.ScientistCooldown),
            Override.VitalsBatteryCharge => gameOptions.GetFloat(FloatOptionNames.ScientistBatteryCharge),
            Override.TrackerCooldown => gameOptions.GetFloat(FloatOptionNames.TrackerCooldown),
            Override.TrackerDuration => gameOptions.GetFloat(FloatOptionNames.TrackerDuration),
            Override.TrackerDelay => gameOptions.GetFloat(FloatOptionNames.TrackerDelay),
            Override.NoiseImpGetAlert => gameOptions.GetBool(BoolOptionNames.NoisemakerImpostorAlert),
            Override.NoiseAlertDuration => gameOptions.GetFloat(FloatOptionNames.NoisemakerAlertDuration),
            Override.PhantomVanishDuration => gameOptions.GetFloat(FloatOptionNames.PhantomDuration),
            Override.PhantomVanishCooldown => gameOptions.GetFloat(FloatOptionNames.PhantomCooldown),
            Override.DetectiveSuspectsPerCase => gameOptions.GetFloat(FloatOptionNames.DetectiveSuspectLimit),
            Override.ViperDissolveTime => gameOptions.GetFloat(FloatOptionNames.ViperDissolveTime),
            _ => throw new ArgumentOutOfRangeException(nameof(__override), __override, null)
        };
    }

    public static object SetValue(this Override __override, IGameOptions gameOptions, object? value, object? fallback = null)
    {
        value ??= fallback;
        if (value == null) throw new ArgumentNullException(nameof(value));
        object SetBoolOption(BoolOptionNames boolOptionNames)
        {
            gameOptions.SetBool(boolOptionNames, (bool)value);
            return value;
        }

        object SetFloatOption(FloatOptionNames floatOptionNames, float min = 0, float max = float.MaxValue)
        {
            gameOptions.SetFloat(floatOptionNames, Mathf.Clamp((float)value, min, max));
            return value;
        }

        object SetIntOption(Int32OptionNames int32OptionNames, int min = 0, int max = int.MaxValue)
        {
            gameOptions.SetInt(int32OptionNames, Mathf.Clamp((int)value, min, max));
            return value;
        }

        return __override switch
        {
            Override.CanUseVent => false,
            Override.AnonymousVoting => SetBoolOption(BoolOptionNames.AnonymousVotes),
            Override.ConfirmEjects => SetBoolOption(BoolOptionNames.ConfirmImpostor),
            Override.DiscussionTime => SetIntOption(Int32OptionNames.DiscussionTime),
            Override.VotingTime => SetIntOption(Int32OptionNames.VotingTime),
            Override.PlayerSpeedMod => SetFloatOption(FloatOptionNames.PlayerSpeedMod, int.MinValue),
            Override.CrewLightMod => SetFloatOption(FloatOptionNames.CrewLightMod),
            Override.ImpostorLightMod => SetFloatOption(FloatOptionNames.ImpostorLightMod),
            Override.KillCooldown => SetFloatOption(FloatOptionNames.KillCooldown, 0.1f),
            Override.KillDistance => SetIntOption(Int32OptionNames.KillDistance, 0, 3),
            Override.ShapeshiftDuration => SetFloatOption(FloatOptionNames.ShapeshifterDuration),
            Override.ShapeshiftCooldown => SetFloatOption(FloatOptionNames.ShapeshifterCooldown, 0.01f),
            Override.ShapeshifterEvidence => SetBoolOption(BoolOptionNames.ShapeshifterLeaveSkin),
            Override.GuardianAngelDuration => SetFloatOption(FloatOptionNames.ProtectionDurationSeconds),
            Override.GuardianAngelCooldown => SetFloatOption(FloatOptionNames.GuardianAngelCooldown),
            Override.EngVentCooldown => SetFloatOption(FloatOptionNames.EngineerCooldown),
            Override.EngVentDuration => SetFloatOption(FloatOptionNames.EngineerInVentMaxTime),
            Override.VitalsCooldown => SetFloatOption(FloatOptionNames.ScientistCooldown),
            Override.VitalsBatteryCharge => SetFloatOption(FloatOptionNames.ScientistBatteryCharge),
            Override.TrackerCooldown => SetFloatOption(FloatOptionNames.TrackerCooldown),
            Override.TrackerDuration => SetFloatOption(FloatOptionNames.TrackerDuration),
            Override.TrackerDelay => SetFloatOption(FloatOptionNames.TrackerDelay),
            Override.NoiseImpGetAlert => SetBoolOption(BoolOptionNames.NoisemakerImpostorAlert),
            Override.NoiseAlertDuration => SetFloatOption(FloatOptionNames.NoisemakerAlertDuration),
            Override.PhantomVanishDuration => SetFloatOption(FloatOptionNames.PhantomDuration),
            Override.PhantomVanishCooldown => SetFloatOption(FloatOptionNames.PhantomCooldown),
            Override.DetectiveSuspectsPerCase => SetFloatOption(FloatOptionNames.DetectiveSuspectLimit),
            Override.ViperDissolveTime => SetFloatOption(FloatOptionNames.ViperDissolveTime),
            _ => throw new ArgumentOutOfRangeException(nameof(__override), __override, null)
        };
    }
}