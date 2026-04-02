using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Chat.Commands;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Neutrals;
using Lotus.GameModes.HideAndSeek.Standard.Distributions;
using Lotus.GameModes.Normal.Standard.Conditions;
using Lotus.GameModes.Normal.Standard.Distributions;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Undead;
using Lotus.Victory;
using Lotus.Victory.Conditions;
using VentLib.Options.UI.Tabs;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.Standard;

public class HNSStandardGameMode : GameMode
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(HNSStandardGameMode));
    private const string HNSStandardGamemodeHookKey = nameof(HNSStandardGamemodeHookKey);
    public static HNSStandardGameMode Instance = null!;

    public override string Name { get; set; } = GamemodeTranslations.HNSStandard.Name;
    public override string Description { get; set; } = GamemodeTranslations.HNSStandard.GamemodeDescription;
    public override HNSStandardRoleOperations RoleOperations { get; }
    public override HNSStandardRoleManager RoleManager { get; }
    public override MatchData MatchData { get; set; }

    public override BaseGameMode BaseGameMode => BaseGameMode.HideAndSeek;

    public HNSRoleAssignment RoleAssignment { get; }

    public HNSStandardGameMode() : base()
    {
        Instance = this;
        MatchData = new();
        RoleAssignment = new();

        RoleOperations = new(this);
        RoleManager = new();
    }

    public override void Activate()
    {
        // RoleManager.RoleHolder.AllRoles.ForEach(RoleManager.RegisterRole);
        Hooks.PlayerHooks.PlayerDeathHook.Bind(HNSStandardGamemodeHookKey, ShowInformationToGhost, priority: API.Priority.VeryLow);
    }

    public override void Deactivate()
    {
        // EnabledTabs().ForEach(tb => tb.GetOptions().ForEach(tb.RemoveOption));
        Hooks.UnbindAll(HNSStandardGamemodeHookKey);
    }

    public override void Assign(PlayerControl player, CustomRole role, bool addAsMainRole = true, bool sendToClient = false)
    {
        RoleOperations.Assign(role, player, addAsMainRole, sendToClient);
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.HNSStandardTabs;
    public override MainSettingTab MainTab() => DefaultTabs.HNSStandardTab;

    public override void Setup()
    {
        MatchData = new MatchData();
    }

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        new List<IWinCondition> {
            new HNSImpostorWin(), new HNSCrewmateWin()
        }.ForEach(winDelegate.AddWinCondition);
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        RoleAssignment.AssignRoles(players);
        base.AssignRoles(players);
    }

    public static void ShowInformationToGhost(PlayerDeathHookEvent hookEvent)
    {
        PlayerControl player = hookEvent.Player;
        ShowInformationToGhost(player);
    }

    public static void ShowInformationToGhost(PlayerControl player)
    {
        if (player == null) return;
        if (!GeneralOptions.GameplayOptions.GhostsSeeInfo) return;

        if (Players.GetAlivePlayers().Any(p => p.PrimaryRole() is Altruist))
        {
            log.Trace($"Not showing all name components to ghost {player.name} because there is an alive altruist.");
            return;
        }

        log.Trace($"Showing all name components to ghost {player.name}");
        if (GeneralOptions.MiscellaneousOptions.AutoDisplayCOD)
        {
            FrozenPlayer? fp = Game.MatchData.FrozenPlayers.GetValueOrDefault(player.GetGameID());
            if (fp != null) DeathCommand.ShowMyDeath(player, fp);
        }
        Players.GetAllPlayers().Where(p => p.PlayerId != player.PlayerId)
            .SelectMany(p => p.NameModel().ComponentHolders())
            .ForEach(holders =>
                {
                    holders.AddListener(component => component.AddViewer(player));
                    holders.Components().ForEach(components => components.AddViewer(player));
                }
            );

        player.NameModel().Render(force: true);
    }
}