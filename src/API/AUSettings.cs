using AmongUs.GameOptions;
using Lotus.API.Reactive;

namespace Lotus.API;

public static class AUSettings
{
    private const string GameOptionHookKey = nameof(GameOptionHookKey);

    public static IGameOptions StaticOptions
    {
        get => _originalHostOptions ?? GameOptionsManager.Instance?.CurrentGameOptions!;
        private set => _originalHostOptions = value;
    }

    private static IGameOptions? _originalHostOptions;

    static AUSettings()
    {
        Hooks.GameStateHooks.GameStartHook.Bind(GameOptionHookKey, _ => StaticOptions = GameOptionsManager.Instance.CurrentGameOptions);
        Hooks.GameStateHooks.GameEndHook.Bind(GameOptionHookKey, _ => GameOptionsManager.Instance.CurrentGameOptions = StaticOptions);
    }

    public static float KillCooldown(float defaultValue = 60) => StaticOptions?.GetFloat(FloatOptionNames.KillCooldown) ?? defaultValue;
    public static float PlayerSpeedMod(float defaultValue = 1) => StaticOptions?.GetFloat(FloatOptionNames.PlayerSpeedMod) ?? defaultValue;
    public static float ImpostorLightMod(float defaultValue = 1) => StaticOptions?.GetFloat(FloatOptionNames.ImpostorLightMod) ?? defaultValue;
    public static float CrewLightMod(float defaultValue = 1) => StaticOptions?.GetFloat(FloatOptionNames.CrewLightMod) ?? defaultValue;
    public static float ShapeshifterCooldown() => StaticOptions.GetFloat(FloatOptionNames.ShapeshifterCooldown);
    public static float ShapeshifterDuration() => StaticOptions.GetFloat(FloatOptionNames.ShapeshifterDuration);
    public static float ProtectionDurationSeconds() => StaticOptions.GetFloat(FloatOptionNames.ProtectionDurationSeconds);
    public static float GuardianAngelCooldown() => StaticOptions.GetFloat(FloatOptionNames.GuardianAngelCooldown);
    public static float ScientistCooldown() => StaticOptions.GetFloat(FloatOptionNames.ScientistCooldown);
    public static float ScientistBatteryCharge() => StaticOptions.GetFloat(FloatOptionNames.ScientistBatteryCharge);
    public static float EngineerCooldown() => StaticOptions.GetFloat(FloatOptionNames.EngineerCooldown);
    public static float EngineerInVentMaxTime() => StaticOptions.GetFloat(FloatOptionNames.EngineerInVentMaxTime);
    public static float TrackerCooldown() => StaticOptions.GetFloat(FloatOptionNames.TrackerCooldown);
    public static float TrackerDuration() => StaticOptions.GetFloat(FloatOptionNames.TrackerDuration);
    public static float TrackerDelay() => StaticOptions.GetFloat(FloatOptionNames.TrackerDelay);
    public static float NoisemakerAlertDuration() => StaticOptions.GetFloat(FloatOptionNames.NoisemakerAlertDuration);
    public static float PhantomCooldown() => StaticOptions.GetFloat(FloatOptionNames.PhantomCooldown);
    public static float PhantomDuration() => StaticOptions.GetFloat(FloatOptionNames.PhantomDuration);
    public static float DetectiveSuspectsPerCase() => StaticOptions.GetFloat(FloatOptionNames.DetectiveSuspectLimit);
    public static float ViperDissolveTime() => StaticOptions.GetFloat(FloatOptionNames.ViperDissolveTime);
    public static float JudgeTaskRequirementPercentage() => StaticOptions.GetFloat(FloatOptionNames.JudgeTaskRequirementPercentage);

    public static int NumImpostors() => StaticOptions.GetInt(Int32OptionNames.NumImpostors);
    public static int KillDistance() => StaticOptions.GetInt(Int32OptionNames.KillDistance);
    public static int NumEmergencyMeetings() => StaticOptions.GetInt(Int32OptionNames.NumEmergencyMeetings);
    public static int EmergencyCooldown() => StaticOptions.GetInt(Int32OptionNames.EmergencyCooldown);
    public static int DiscussionTime() => StaticOptions.GetInt(Int32OptionNames.DiscussionTime);
    public static int VotingTime() => StaticOptions.GetInt(Int32OptionNames.VotingTime);
    public static int MaxImpostors() => StaticOptions.GetInt(Int32OptionNames.MaxImpostors);
    public static int MinPlayers() => StaticOptions.GetInt(Int32OptionNames.MinPlayers);
    public static int MaxPlayers() => StaticOptions.GetInt(Int32OptionNames.MaxPlayers);
    public static int NumCommonTasks() => StaticOptions.GetInt(Int32OptionNames.NumCommonTasks);
    public static int NumShortTasks() => StaticOptions.GetInt(Int32OptionNames.NumShortTasks);
    public static int NumLongTasks() => StaticOptions.GetInt(Int32OptionNames.NumLongTasks);
    public static int TaskBarMode() => StaticOptions.GetInt(Int32OptionNames.TaskBarMode);
    public static int CrewmatesRemainingForVitals() => StaticOptions.GetInt(Int32OptionNames.CrewmatesRemainingForVitals);

    public static float CrewmateTimeInVent() => StaticOptions.GetFloat(FloatOptionNames.CrewmateTimeInVent);
    public static float FinalEscapeTime() => StaticOptions.GetFloat(FloatOptionNames.FinalEscapeTime);
    public static float EscapeTime() => StaticOptions.GetFloat(FloatOptionNames.EscapeTime);
    public static float SeekerFinalSpeed() => StaticOptions.GetFloat(FloatOptionNames.SeekerFinalSpeed);
    public static float MaxPingTime() => StaticOptions.GetFloat(FloatOptionNames.MaxPingTime);
    public static int CrewmateVentUses() => StaticOptions.GetInt(Int32OptionNames.CrewmateVentUses);
    public static int ImpostorPlayerID() => StaticOptions.GetInt(Int32OptionNames.ImpostorPlayerID);
    public static float CrewmateFlashlightSize() => StaticOptions.GetFloat(FloatOptionNames.CrewmateFlashlightSize);
    public static float ImpostorFlashlightSize() => StaticOptions.GetFloat(FloatOptionNames.ImpostorFlashlightSize);
    public static bool IsDefaults() => StaticOptions.GetBool(BoolOptionNames.IsDefaults);
    public static bool UseFlashlight() => StaticOptions.GetBool(BoolOptionNames.UseFlashlight);
    public static bool SeekerFinalVents() => StaticOptions.GetBool(BoolOptionNames.SeekerFinalVents);
    public static bool SeekerFinalMap() => StaticOptions.GetBool(BoolOptionNames.SeekerFinalMap);
    public static bool SeekerPings() => StaticOptions.GetBool(BoolOptionNames.SeekerPings);
    public static bool ShowCrewmateNames() => StaticOptions.GetBool(BoolOptionNames.ShowCrewmateNames);

    public static int[] MaxImpostorArray() => StaticOptions.GetIntArray(Int32ArrayOptionNames.MaxImpostors);
    public static int[] MinPlayerArray() => StaticOptions.GetIntArray(Int32ArrayOptionNames.MinPlayers);

    public static bool VisualTasks() => StaticOptions.GetBool(BoolOptionNames.VisualTasks);
    public static bool GhostsDoTasks() => StaticOptions.GetBool(BoolOptionNames.GhostsDoTasks);
    public static bool ConfirmImpostor() => StaticOptions.GetBool(BoolOptionNames.ConfirmImpostor);
    public static bool AnonymousVotes() => StaticOptions.GetBool(BoolOptionNames.AnonymousVotes);
    public static bool ShapeshifterLeaveSkin() => StaticOptions.GetBool(BoolOptionNames.ShapeshifterLeaveSkin);
    public static bool ImpostorsCanSeeProtect() => StaticOptions.GetBool(BoolOptionNames.ImpostorsCanSeeProtect);
    public static bool NoisemakerImpostorAlert() => StaticOptions.GetBool(BoolOptionNames.NoisemakerImpostorAlert);
    public static float[] KillDistances() => StaticOptions.GetFloatArray(FloatArrayOptionNames.KillDistances);
    public static byte MapId() => StaticOptions.MapId;
}