using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Chat.Commands;
using Lotus.GameModes.Normal.Speedrun.Conditions;
using Lotus.GameModes.Normal.Standard;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Victory;
using VentLib.Options.UI.Tabs;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Speedrun;

public class SpeedrunGamemode : GameMode
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(SpeedrunGamemode));
    private const string SpeedrunGamemodeHookKey = nameof(SpeedrunGamemodeHookKey);
    public static SpeedrunGamemode Instance = null!;

    public override string Name { get; set; } = GamemodeTranslations.Speedrun.Name;
    public override string Description { get; set; } = GamemodeTranslations.Speedrun.GamemodeDescription;
    public override SpeedrunRoleOperations RoleOperations { get; }
    public override SpeedrunRoleManager RoleManager { get; }
    public override MatchData MatchData { get; set; }

    public override BlockableGameAction BlockedActions() => BlockableGameAction.CallSabotage | BlockableGameAction.CallMeeting | BlockableGameAction.ReportBody | BlockableGameAction.CloseDoors;

    public SpeedrunGamemode()
    {
        Instance = this;
        MatchData = new();

        RoleOperations = new(this);
        RoleManager = new();
    }

    public override void Activate()
    {
        Hooks.PlayerHooks.PlayerDeathHook.Bind(SpeedrunGamemodeHookKey, NormalStandardGameMode.ShowInformationToGhost, priority: API.Priority.VeryLow);
    }

    public override void Deactivate()
    {
        Hooks.UnbindAll(SpeedrunGamemodeHookKey);
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.EmptyTabs;
    public override MainSettingTab MainTab() => DefaultTabs.SpeedrunTab;

    public override void Setup()
    {
        MatchData = new MatchData();
    }
    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.AddWinCondition(new LastAliveWinCondition());
    }

    public override void Assign(PlayerControl player, CustomRole role, bool addAsMainRole = true, bool sendToClient = false)
    {
        RoleOperations.Assign(role, player, addAsMainRole, sendToClient);
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        players.ForEach(p => Assign(p, RoleManager.RoleHolder.Static.Tasker));
        base.AssignRoles(players);
    }
}