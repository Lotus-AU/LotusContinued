using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.API;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Utilities;
using Lotus.Extensions;
using Lotus.Options.General;
using Lotus.Options;
using VentLib.Utilities.Extensions;
using Lotus.API.Odyssey;
using Sentry.Unity.NativeUtils;
using System.Linq;
using Lotus.GameModes.Normal.Draft;
using Lotus.GameModes.Normal.Standard;

namespace Lotus.Patches.Systems;

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
public class RpcSetTasksPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(RpcSetTasksPatch));
    internal static Dictionary<byte, byte> ReplacedCommonTasks = new();
    internal static readonly Queue<TasksOverride> TaskQueue = new();

    public static void OnGameStart()
    {
        ReplacedCommonTasks = new();
    }

    public static bool Prefix(NetworkedPlayerInfo __instance, ref Il2CppStructArray<byte> taskTypeIds)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (GeneralOptions.GameplayOptions.IsSubscribed(Game.CurrentGameMode.MainTab()))
            taskTypeIds = RemoveIllegalTasks(taskTypeIds);

        CustomRole? role = Utils.GetPlayerById(__instance.PlayerId)?.PrimaryRole();
        // This function mostly deals with override, so if not overriding immediately exit

        TasksOverride? tasksOverride = TaskQueue.Count == 0 ? null : TaskQueue.Dequeue();

        bool hasCommonTasks = false;
        bool overrideTasks = false;
        int shortTaskCount = -1;
        int longTaskCount = -1;

        bool hasTasks = tasksOverride != null;

        switch (role)
        {
            case IOverridenTaskHolderRole overridenTaskRole:
                hasCommonTasks = overridenTaskRole.AssignCommonTasks();
                shortTaskCount = overridenTaskRole.ShortTaskAmount();
                longTaskCount = overridenTaskRole.LongTaskAmount();
                overrideTasks = overridenTaskRole.OverrideTasks();
                hasTasks = overridenTaskRole.HasTasks();
                break;
            case ITaskHolderRole holderRole:
                hasTasks = holderRole.HasTasks();
                break;
        }

        if (!hasTasks) return true;
        log.Debug($"Setting tasks for player {__instance.Object?.name ?? __instance.PlayerName}.");

        if (shortTaskCount == -1 || !overrideTasks) shortTaskCount = AUSettings.NumShortTasks();
        if (longTaskCount == -1 || !overrideTasks) longTaskCount = AUSettings.NumLongTasks();

        if (tasksOverride != null)
        {
            if (tasksOverride.ShortTasks == -1) tasksOverride.ShortTasks = shortTaskCount;
            if (tasksOverride.LongTasks == -1) tasksOverride.LongTasks = longTaskCount;
            if (tasksOverride.TaskAssignmentMode is TaskAssignmentMode.Add)
            {
                shortTaskCount += tasksOverride.ShortTasks;
                longTaskCount += tasksOverride.LongTasks;
            }
            else
            {
                shortTaskCount = tasksOverride.ShortTasks;
                longTaskCount = tasksOverride.LongTasks;
            }
        }
        else if (!overrideTasks) return true;
        log.Debug($"Overriding tasks for player {__instance.Object?.name ?? __instance.PlayerName}.");

        Il2CppSystem.Collections.Generic.List<byte> tasksList = new();
        foreach (byte num in taskTypeIds) tasksList.Add(num);

        if (hasCommonTasks && tasksList.Count > 0) tasksList.RemoveRange(AUSettings.NumCommonTasks(), tasksList.Count - AUSettings.NumCommonTasks());
        else tasksList.Clear();

        Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();

        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> longTasks = new();
        foreach (var task in ShipStatus.Instance.LongTasks.Where(t => !CheckIllegalTask(t)))
            longTasks.Add(task);
        Shuffle(longTasks);

        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> shortTasks = new();
        foreach (var task in ShipStatus.Instance.ShortTasks.Where(t => !CheckIllegalTask(t)))
            shortTasks.Add(task);
        Shuffle(shortTasks);

        int start2 = 0;
        ShipStatus.Instance.AddTasksFromList(
            ref start2,
            longTaskCount,
            tasksList,
            usedTaskTypes,
            longTasks
        );

        int start3 = 0;
        ShipStatus.Instance.AddTasksFromList(
            ref start3,
            !hasCommonTasks && shortTaskCount == 0 && longTaskCount == 0 ? 1 : shortTaskCount,
            tasksList,
            usedTaskTypes,
            shortTasks
        );

        taskTypeIds = new Il2CppStructArray<byte>(tasksList.Count);
        for (int i = 0; i < tasksList.Count; i++) taskTypeIds[i] = tasksList[i];
        // If tasks apply to total then we're good, otherwise do our custom sending
        return true;
    }

    private static bool CheckIllegalTask(NormalPlayerTask task)
    {
        if (Game.CurrentGameMode is not NormalStandardGameMode) return false;
        return IsTaskIllegal(task);
    }

    private static bool IsTaskIllegal(NormalPlayerTask task)
    {
        switch (task.TaskType)
        {
            case TaskTypes.SwipeCard when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.SwipeCard) && GeneralOptions.GameplayOptions.DisableCommonTasks:
            case TaskTypes.FixWiring when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.FixWiring) && GeneralOptions.GameplayOptions.DisableCommonTasks:
            case TaskTypes.EnterIdCode when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.EnterIdCode) && GeneralOptions.GameplayOptions.DisableCommonTasks:
            case TaskTypes.ScanBoardingPass when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.ScanBoardingPass) && GeneralOptions.GameplayOptions.DisableCommonTasks:
            case TaskTypes.InsertKeys when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.InsertKeys) && GeneralOptions.GameplayOptions.DisableCommonTasks:

            case TaskTypes.AlignTelescope when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.AlignTelescope) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.AssembleArtifact when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.AssembleArtifact) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.BuyBeverage when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.BuyBeverage) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.CalibrateDistributor when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.CalibrateDistributor) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.ChartCourse when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.ChartCourse) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.CleanO2Filter when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.CleanO2Filter) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.CleanToilet when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.CleanToilet) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.VentCleaning when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.CleanVent) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.ClearAsteroids when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.ClearAsteroids) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.Decontaminate when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.Decontaminate) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.DivertPower when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.DivertPower) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.DressMannequin when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.DressMannequin) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.FixShower when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.FixShower) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.MakeBurger when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.MakeBurger) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.MeasureWeather when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.MeasureWeather) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.MonitorOxygen when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.MonitorOxygen) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.PickUpTowels when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PickUpTowels) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.PolishRuby when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PolishRuby) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.PrimeShields when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PrimeShields) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.ProcessData when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.ProcessData) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.PutAwayPistols when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PutAwayPistols) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.PutAwayRifles when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PutAwayRifles) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.RecordTemperature when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.RecordTemperature) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.RepairDrill when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.RepairDrill) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.RunDiagnostics when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.RunDiagnostics) &&GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.SortRecords when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.SortRecords) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.SortSamples when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.SortSamples) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.StabilizeSteering when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.StabilizeSteering) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.StoreArtifacts when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.StoreArtifacts) && GeneralOptions.GameplayOptions.DisableShortTasks:
            case TaskTypes.UnlockManifolds when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.UnlockManifolds) && GeneralOptions.GameplayOptions.DisableShortTasks:


            case TaskTypes.AlignEngineOutput when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.AlignEngineOutput) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.DevelopPhotos when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.DevelopPhotos) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.EmptyGarbage when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.EmptyGarbage) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.EmptyChute when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.EmptyGarbage) && GeneralOptions.GameplayOptions.DisableLongTasks: // variation of EmptyGarbage
            case TaskTypes.FuelEngines when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.FuelEngines) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.InspectSample when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.InspectSample) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.OpenWaterways when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.OpenWaterways) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.RebootWifi when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.RebootWifi) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.ReplaceWaterJug when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.ReplaceWaterJug) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.ResetBreakers when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.ResetBreaker) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.RewindTapes when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.RewindTapes) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.StartFans when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.StartFans) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.SubmitScan when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.SubmitScan) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.StartReactor when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.StartReactor) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.UnlockSafe when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.UnlockSafe) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.UploadData when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.UploadData) && GeneralOptions.GameplayOptions.DisableLongTasks:
            case TaskTypes.WaterPlants when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.WaterPlants) && GeneralOptions.GameplayOptions.DisableLongTasks:
                return true;
            default:
                return false;
        }
    }

    public static Il2CppStructArray<byte> RemoveIllegalTasks(Il2CppStructArray<byte> taskTypeIds)
    {
        if (!GeneralOptions.GameplayOptions.DisableTasks) return taskTypeIds;
        List<byte> newTasks = new(taskTypeIds.Count);
        taskTypeIds.ForEach(idx =>
        {
            NormalPlayerTask taskById = ShipStatus.Instance.GetTaskById(idx);
            switch (taskById.TaskType)
            {
                // long and short tasks
                case TaskTypes.AlignTelescope when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.AlignTelescope):
                case TaskTypes.AssembleArtifact when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.AssembleArtifact):
                case TaskTypes.BuyBeverage when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.BuyBeverage):
                case TaskTypes.CalibrateDistributor when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.CalibrateDistributor):
                case TaskTypes.ChartCourse when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.ChartCourse):
                case TaskTypes.CleanO2Filter when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.CleanO2Filter):
                case TaskTypes.CleanToilet when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.CleanToilet):
                case TaskTypes.VentCleaning when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.CleanVent):
                case TaskTypes.ClearAsteroids when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.ClearAsteroids):
                case TaskTypes.Decontaminate when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.Decontaminate):
                case TaskTypes.DivertPower when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.DivertPower):
                case TaskTypes.DressMannequin when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.DressMannequin):
                case TaskTypes.FixShower when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.FixShower):
                case TaskTypes.MakeBurger when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.MakeBurger):
                case TaskTypes.MeasureWeather when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.MeasureWeather):
                case TaskTypes.MonitorOxygen when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.MonitorOxygen):
                case TaskTypes.PickUpTowels when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PickUpTowels):
                case TaskTypes.PolishRuby when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PolishRuby):
                case TaskTypes.PrimeShields when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PrimeShields):
                case TaskTypes.ProcessData when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.ProcessData):
                case TaskTypes.PutAwayPistols when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PutAwayPistols):
                case TaskTypes.PutAwayRifles when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.PutAwayRifles):
                case TaskTypes.RecordTemperature when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.RecordTemperature):
                case TaskTypes.RepairDrill when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.RepairDrill):
                case TaskTypes.RunDiagnostics when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.RunDiagnostics):
                case TaskTypes.SortRecords when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.SortRecords):
                case TaskTypes.SortSamples when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.SortSamples):
                case TaskTypes.StabilizeSteering when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.StabilizeSteering):
                case TaskTypes.StoreArtifacts when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.StoreArtifacts):
                case TaskTypes.UnlockManifolds when GeneralOptions.GameplayOptions.DisabledShortTaskFlag.HasFlag(DisabledShortTask.UnlockManifolds):

                case TaskTypes.AlignEngineOutput when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.AlignEngineOutput):
                case TaskTypes.DevelopPhotos when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.DevelopPhotos):
                case TaskTypes.EmptyGarbage when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.EmptyGarbage):
                case TaskTypes.EmptyChute when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.EmptyGarbage): // variation of EmptyGarbage
                case TaskTypes.FuelEngines when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.FuelEngines):
                case TaskTypes.InspectSample when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.InspectSample):
                case TaskTypes.OpenWaterways when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.OpenWaterways):
                case TaskTypes.RebootWifi when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.RebootWifi):
                case TaskTypes.ReplaceWaterJug when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.ReplaceWaterJug):
                case TaskTypes.ResetBreakers when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.ResetBreaker):
                case TaskTypes.RewindTapes when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.RewindTapes):
                case TaskTypes.StartFans when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.StartFans):
                case TaskTypes.SubmitScan when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.SubmitScan):
                case TaskTypes.StartReactor when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.StartReactor):
                case TaskTypes.UnlockSafe when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.UnlockSafe):
                case TaskTypes.UploadData when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.UploadData):
                case TaskTypes.WaterPlants when GeneralOptions.GameplayOptions.DisabledLongTaskFlag.HasFlag(DisabledLongTask.WaterPlants):
                    switch (taskById.Length)
                    {
                        case NormalPlayerTask.TaskLength.Long:
                            NormalPlayerTask? replacedLongTask =
                                ShipStatus.Instance.LongTasks.FirstOrDefault(t => !IsTaskIllegal(t));
                            if (replacedLongTask != null) newTasks.Add((byte)replacedLongTask.Index);
                            else newTasks.Add(idx);
                            break;
                        case NormalPlayerTask.TaskLength.Short:
                            NormalPlayerTask? replacedShortTask =
                                ShipStatus.Instance.ShortTasks.FirstOrDefault(t => !IsTaskIllegal(t));
                            if (replacedShortTask != null) newTasks.Add((byte)replacedShortTask.Index);
                            else newTasks.Add(idx);
                            break;
                    }
                    break;
                // common tasks are a bit more in depth. as we need to align with everyone else's tasks
                case TaskTypes.SwipeCard when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.SwipeCard):
                case TaskTypes.FixWiring when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.FixWiring):
                case TaskTypes.EnterIdCode when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.EnterIdCode):
                case TaskTypes.ScanBoardingPass when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.ScanBoardingPass):
                case TaskTypes.InsertKeys when GeneralOptions.GameplayOptions.DisabledCommonTaskFlag.HasFlag(DisabledCommonTask.InsertKeys):
                    if (ReplacedCommonTasks.TryGetValue(idx, out byte replacedTaskId)) newTasks.Add(replacedTaskId);
                    else
                    {
                        byte newTaskId = idx; // just take the L if we don't have enough common tasks
                        NormalPlayerTask? replacedTask =
                            ShipStatus.Instance.CommonTasks.FirstOrDefault(t => !IsTaskIllegal(t));
                        if (replacedTask != null) newTaskId = (byte)replacedTask.Index;
                        ReplacedCommonTasks.Add(idx, newTaskId);
                        newTasks.Add(newTaskId);
                    }
                    break;
                default:
                    newTasks.Add(idx);
                    break;
            }
        });

        taskTypeIds = new Il2CppStructArray<byte>(newTasks.Count);
        for (int i = 0; i < newTasks.Count; i++) taskTypeIds[i] = newTasks[i];
        return taskTypeIds;
    }

    public static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            T obj = list[i];
            int rand = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[rand];
            list[rand] = obj;
        }
    }

    public class TasksOverride
    {
        public int ShortTasks;
        public int LongTasks;
        public TaskAssignmentMode TaskAssignmentMode;

        public TasksOverride(int shortTasks, int longTasks, TaskAssignmentMode taskAssignmentMode)
        {
            ShortTasks = shortTasks;
            LongTasks = longTasks;
            TaskAssignmentMode = taskAssignmentMode;
        }
    }
}
