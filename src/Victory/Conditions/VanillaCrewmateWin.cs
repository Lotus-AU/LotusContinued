using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Extensions;
using Lotus.Options;
using VentLib.Localization.Attributes;

namespace Lotus.Victory.Conditions;


public class VanillaCrewmateWin : IFactionWinCondition
{
    [Localized($"{ModConstants.Localization.WinConditions}.{nameof(TaskWin)}")]
    public static string TaskWin = "Task Win";

    [Localized($"{ModConstants.Localization.WinConditions}.{nameof(LastFactionStanding)}")]
    public static string LastFactionStanding = "Last Faction Standing";

    private static readonly List<IFaction> CrewmateFaction = new() { FactionInstances.Crewmates };
    private WinReason winReason = new(ReasonType.TasksComplete, TaskWin);

    public List<IFaction> Factions() => CrewmateFaction;

    public bool IsConditionMet(out List<IFaction> factions)
    {
        factions = CrewmateFaction;

        if (Game.State is not (GameState.Roaming or GameState.InMeeting)) return false;

        winReason = new WinReason(ReasonType.TasksComplete, TaskWin);

        bool hasAliveEnemy = false;
        bool hasOneTaskDoer = false;
        foreach (CustomRole role in Players.GetAllPlayers().Select(p => p.PrimaryRole()))
        {
            if (role is ITaskHolderRole taskHolder && taskHolder.TasksApplyToTotal() && taskHolder.HasTasks()) hasOneTaskDoer = true;
            if (IsEligibleEnemy(role)) hasAliveEnemy = true;
            if (hasOneTaskDoer && hasAliveEnemy) break;
        }

        if (hasAliveEnemy && hasOneTaskDoer) return CheckTaskCompletion();

        winReason = new WinReason(ReasonType.FactionLastStanding, LastFactionStanding);
        return !hasAliveEnemy;
    }

    // Determines if the given role is an "enemy role"
    private static bool IsEligibleEnemy(CustomRole role)
    {
        PlayerControl player = role.MyPlayer;
        if (!player.IsAlive()) return false;
        if (role.Faction.Relationship(FactionInstances.Crewmates) is not Relation.None) return false;
        return (player.GetVanillaRole().IsImpostor() || role.RoleAbilityFlags.HasFlag(RoleAbilityFlag.IsAbleToKill)) && !role.RoleFlags.HasFlag(RoleFlag.CannotWinAlone);
    }

    private static bool CheckTaskCompletion()
    {
        if (GeneralOptions.GameplayOptions.DisableTaskWin) return false;
        GameData.Instance.RecomputeTaskCounts();
        return GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks;
    }

    public WinReason GetWinReason() => winReason;

    public int Priority() => -1;
}