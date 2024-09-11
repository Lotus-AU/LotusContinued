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
using Lotus.GameModes.Standard;
using Sentry.Unity.NativeUtils;
using System.Linq;

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
        if (Game.CurrentGameMode is StandardGameMode) taskTypeIds = RemoveIllegalTasks(taskTypeIds);

        CustomRole? role = Utils.GetPlayerById(__instance.PlayerId)?.PrimaryRole();
        // This function mostly deals with override, so if not overriding immediately exit

        TasksOverride? tasksOverride = TaskQueue.Count == 0 ? null : TaskQueue.Dequeue();

        int shortTaskCount = -1;
        int longTaskCount = -1;
        bool overrideTasks = false;
        bool hasCommonTasks = false;

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
        log.Debug($"Setting tasks for player {__instance.Object?.name ?? __instance.PlayerName}");

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

        Il2CppSystem.Collections.Generic.List<byte> tasksList = new();
        foreach (byte num in taskTypeIds) tasksList.Add(num);

        if (hasCommonTasks) tasksList.RemoveRange(AUSettings.NumCommonTasks(), tasksList.Count - AUSettings.NumCommonTasks());
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
        if (Game.CurrentGameMode is not StandardGameMode) return false;
        return IsTaskIllegal(task);
    }

    private static bool IsTaskIllegal(NormalPlayerTask task)
    {
        switch (task.TaskType)
        {
            case TaskTypes.SubmitScan when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.MedScan):
            case TaskTypes.UnlockSafe when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.UnlockSafe):
            case TaskTypes.UploadData when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.UploadData):
            case TaskTypes.StartReactor when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.StartReactor):
            case TaskTypes.ResetBreakers when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.ResetBreaker):
            case TaskTypes.SwipeCard when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.CardSwipe):
            case TaskTypes.FixWiring when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.FixWiring):
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
                case TaskTypes.SubmitScan when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.MedScan):
                case TaskTypes.UnlockSafe when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.UnlockSafe):
                case TaskTypes.UploadData when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.UploadData):
                case TaskTypes.StartReactor when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.StartReactor):
                case TaskTypes.ResetBreakers when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.ResetBreaker):
                    switch (taskById.Length)
                    {
                        case NormalPlayerTask.TaskLength.Long:
                            NormalPlayerTask? replacedLongTask = ShipStatus.Instance.LongTasks.Where(t => !IsTaskIllegal(t)).FirstOrDefault();
                            if (replacedLongTask != null) newTasks.Add((byte)replacedLongTask.Index);
                            else newTasks.Add(idx);
                            break;
                        case NormalPlayerTask.TaskLength.Short:
                            NormalPlayerTask? replacedShortTask = ShipStatus.Instance.ShortTasks.Where(t => !IsTaskIllegal(t)).FirstOrDefault();
                            if (replacedShortTask != null) newTasks.Add((byte)replacedShortTask.Index);
                            else newTasks.Add(idx);
                            break;
                    }
                    break;
                // common tasks are a bit more in depth. as we need to align with everyone elses tasks
                case TaskTypes.SwipeCard when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.CardSwipe):
                case TaskTypes.FixWiring when GeneralOptions.GameplayOptions.DisabledTaskFlag.HasFlag(DisabledTask.FixWiring):
                    if (ReplacedCommonTasks.TryGetValue(idx, out byte replacedTaskId)) newTasks.Add(replacedTaskId);
                    else
                    {
                        byte newTaskId = idx; // just take the L if we dont have enough common tasks
                        NormalPlayerTask? replacedTask = ShipStatus.Instance.CommonTasks.Where(t => !IsTaskIllegal(t)).FirstOrDefault();
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