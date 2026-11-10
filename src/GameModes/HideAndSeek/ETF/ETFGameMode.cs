using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Chat.Commands;
using Lotus.GameModes.HideAndSeek.ETF.Conditions;
using Lotus.GameModes.HideAndSeek.ETF.Distributions;
using Lotus.GameModes.HideAndSeek.ETF.NetObjects;
using Lotus.GameModes.Normal.Standard;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using Lotus.Victory;
using Lotus.Victory.Conditions;
using UnityEngine;
using VentLib.Options.UI.Tabs;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.ETF;

public sealed class ETFGameMode : GameMode
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ETFGameMode));
    private const string ETFGamemodeHookKey = nameof(ETFGamemodeHookKey);
    public static ETFGameMode Instance = null!;

    public override string Name { get; set; } = GamemodeTranslations.EscapeTheFacility.Name;
    public override string Description { get; set; } = GamemodeTranslations.EscapeTheFacility.GamemodeDescription;
    public override ETFRoleOperations RoleOperations { get; }
    public override ETFRoleManager RoleManager { get; }
    public override MatchData MatchData { get; set; }

    public override BaseGameMode BaseGameMode => BaseGameMode.HideAndSeek;
    public override BlockableGameAction BlockedActions() => BlockableGameAction.CloseDoors |
                                                            BlockableGameAction.CallSabotage |
                                                            BlockableGameAction.CallMeeting |
                                                            BlockableGameAction.ReportBody;

    private ETFRoleAssignment RoleAssignment { get; }
    private ETFPositions Positions;

    public bool ObjectsSpawned;

    public Dictionary<Vector2, TerminalObject> TerminalObjects = [];
    public Dictionary<int, CapturedObject> CapturedObjects = [];
    public Dictionary<Vector2, ExitObject> ExitObjects = [];

    public ETFGameMode() : base()
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
        Hooks.PlayerHooks.PlayerDeathHook.Bind(ETFGamemodeHookKey, ShowInformationToGhost, priority: API.Priority.VeryLow);
        Hooks.GameStateHooks.RoundStartHook.Bind(ETFGamemodeHookKey, SpawnAllObjects);
    }

    public override void Deactivate()
    {
        // EnabledTabs().ForEach(tb => tb.GetOptions().ForEach(tb.RemoveOption));
        ExitObjects = [];
        CapturedObjects = [];
        TerminalObjects = [];
        Hooks.UnbindAll(ETFGamemodeHookKey);
    }

    public override void Assign(PlayerControl player, CustomRole role, bool addAsMainRole = true, bool sendToClient = false)
    {
        RoleOperations.Assign(role, player, addAsMainRole, sendToClient);
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.ETFTabs;
    public override MainSettingTab MainTab() => DefaultTabs.EscapeTab;

    public override void Setup()
    {
        MatchData = new MatchData();
        Positions = new ETFPositions();
        ExitObjects = [];
        CapturedObjects = [];
        TerminalObjects = [];

        ObjectsSpawned = false;
    }

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        new List<IWinCondition> {
            new ETFWardenWin(), new ETFFugitiveWin()
        }.ForEach(winDelegate.AddWinCondition);
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        RoleAssignment.AssignRoles(players);
        base.AssignRoles(players);
    }

    private void SpawnAllObjects(GameStateHookEvent _)
    {
        int wardenCount = 0;
        int fugitiveCount = 0;

        Players.GetAliveRoles().ForEach(r =>
        {
            switch (r)
            {
                case Warden _:
                    wardenCount++;
                    break;
                case Fugitive _:
                    fugitiveCount++;
                    break;
            }
        });

        log.Debug($"{wardenCount} wardens spawned. {fugitiveCount} fugitive spawned.");
        int termsToSpawn = fugitiveCount + 2;
        int exitsToSpawn = wardenCount + 1;

        int current = termsToSpawn;

        while (current > 0)
        {
            Vector2? newTermPos = Positions.NewTerminalPosition();
            if (!newTermPos.HasValue) break;
            Vector2 value = newTermPos.Value;
            TerminalObjects.Add(value, new TerminalObject(value));
            current--;
        }

        current = exitsToSpawn;

        while (current > 0)
        {
            Vector2? newExitPos = Positions.NewExitPosition();
            if (!newExitPos.HasValue) break;
            Vector2 value = newExitPos.Value;
            ExitObjects.Add(value, new ExitObject(value));
            current--;
        }

        ObjectsSpawned = true;

        Players.GetAllRoles().ForEach(r =>
        {
            if (r is Fugitive fugitive) fugitive.SetTermsRemaining(termsToSpawn);
            else if (r is Warden warden) warden.SetTermsRemaining(termsToSpawn);
        });
    }

    private static void ShowInformationToGhost(PlayerDeathHookEvent hookEvent) =>
        ShowInformationToGhost(hookEvent.Player);

    private static void ShowInformationToGhost(PlayerControl player)
    {
        if (!player) return;

        log.Trace($"Showing all name components to ghost {player.name}");
        if (GeneralOptions.MiscellaneousOptions.IsSubscribed(Game.CurrentGameMode.MainTab()) && GeneralOptions.MiscellaneousOptions.AutoDisplayCOD)
        {
            FrozenPlayer? fp = Game.MatchData.FrozenPlayers.GetValueOrDefault(player.GetGameID());
            if (fp != null) DeathCommand.ShowMyDeath(player, fp);
        }
        Players.GetAllPlayers().Where(p => p.PlayerId != player.PlayerId)
            .Select(p => p.NameModel().GetComponentHolder<NameHolder>())
            .ForEach(holder =>
            {
                holder.AddListener(component => component.AddViewer(player));
                holder.Components().ForEach(components => components.AddViewer(player));
            });

        player.NameModel().Render(force: true);
    }
}