using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Chat.Commands;
using Lotus.GameModes.Colorwars.Conditions;
using Lotus.GameModes.Colorwars.Distributions;
using Lotus.Options;
using Lotus.Options.Gamemodes;
using Lotus.Roles;
using Lotus.Victory;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.Extensions;
using VentLib.Options.UI;
using VentLib.Options.UI.Controllers;
using VentLib.Options.UI.Tabs;
using VentLib.Ranges;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Colorwars;

public class ColorwarsGamemode : GameMode
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ColorwarsGamemode));
    private const string ColorwarsGamemodeHookKey = nameof(ColorwarsGamemodeHookKey);
    public static ColorwarsGamemode Instance = null!;

    public override string Name { get; set; } = GamemodeTranslations.Colorwars.Name;
    public override string Description { get; set; } = GamemodeTranslations.Colorwars.Description;
    public override ColorwarsRoleOperations RoleOperations { get; }
    public override ColorwarsRoleManager RoleManager { get; }
    public override MatchData MatchData { get; set; }
    public ColorwarsRoleAssignment RoleAssignment;

    public override BlockableGameAction BlockedActions() => BlockableGameAction.CallSabotage | BlockableGameAction.CallMeeting | BlockableGameAction.ReportBody | BlockableGameAction.CloseDoors;

    public Dictionary<byte, int> PlayerToTeam = new();

    private List<GameOption> playerOptions = new();
    private bool refreshing = false;


    public ColorwarsGamemode()
    {
        Instance = this;
        MatchData = new();

        RoleOperations = new(this);
        RoleManager = new();

        RoleAssignment = new();
    }

    public override void Activate()
    {
        PlayerToTeam = new();
        Hooks.PlayerHooks.PlayerDeathHook.Bind(ColorwarsGamemodeHookKey, ShowInformationToGhost, priority: API.Priority.VeryLow);
        Hooks.GameStateHooks.RoundStartHook.Bind(ColorwarsGamemodeHookKey, CheckForRandomSpawn);
        Hooks.NetworkHooks.GameJoinHook.Bind(ColorwarsGamemodeHookKey, RefreshOptions);

        Hooks.NetworkHooks.ReceiveVersionHook.Bind(ColorwarsGamemodeHookKey, _ => Async.Schedule(RefreshOptions, 1f));
        Hooks.PlayerHooks.PlayerDisconnectHook.Bind(ColorwarsGamemodeHookKey, _ => Async.Schedule(RefreshOptions, 1f));
        if (LobbyBehaviour.Instance) RefreshOptions();
    }

    public override void Deactivate()
    {
        Hooks.UnbindAll(ColorwarsGamemodeHookKey);
        playerOptions.Do(DefaultTabs.ColorwarsTab.RemoveOption);
        playerOptions.Do(opt =>
        {
            if (opt.BehaviourExists()) opt.GetBehaviour().gameObject.Destroy();
        });
        playerOptions.Clear();
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.ColorwarsTabs;
    public override MainSettingTab MainTab() => DefaultTabs.ColorwarsTab;

    public override void Setup()
    {
        MatchData = new MatchData();
    }
    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.AddWinCondition(new ColorWarsWinCondition());
    }

    public override void Assign(PlayerControl player, CustomRole role, bool addAsMainRole = true, bool sendToClient = false)
    {
        RoleOperations.Assign(role, player, addAsMainRole, sendToClient);
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        RoleAssignment.AssignRoles(players);
        base.AssignRoles(players);
    }

    private void RefreshOptions()
    {
        if (refreshing) return;
        refreshing = true;

        // Remove Options from list which effectively destroys them.
        playerOptions.Do(opt =>
        {
            ExtraGamemodeOptions.ColorwarsOptions.CustomTeamsOption.Children.Remove(opt);
            if (opt.BehaviourExists()) opt.GetBehaviour().gameObject.Destroy();
        });
        playerOptions.Clear();

        IEnumerable<PlayerControl> allPlayers = Players.GetAllPlayers();

        int playerCount = allPlayers.Count();
        int teams = Mathf.Max(Mathf.CeilToInt((float)playerCount / ExtraGamemodeOptions.ColorwarsOptions.TeamSize), 1);

        List<OptionValue> teamOptions = new IntRangeGen(1, teams)
            .AsEnumerable()
            .Select(i => new GameOptionValueBuilder().Text(ColorwarsOptions.Translations.TeamOption.Formatted(i)).Value(i - 1).Build())
            .ToList();

        if (teamOptions.Count == 0) teamOptions = new() { new GameOptionValueBuilder().Text(ColorwarsOptions.Translations.TeamOption.Formatted(1)).Value(0).Build() };

        int indexOf = DefaultTabs.ColorwarsTab.GetOptions().IndexOf(ExtraGamemodeOptions.ColorwarsOptions.CustomTeamsOption);
        allPlayers.Do(p =>
        {
            var newOption = new GameOptionBuilder()
                .Name(p.name)
                .Color((Color)Palette.PlayerColors[p.cosmetics.bodyMatProperties.ColorId])
                .Values(teamOptions)
                .BindInt(i => PlayerToTeam[p.PlayerId] = i)
                .Build();
            playerOptions.Add(newOption);
            ExtraGamemodeOptions.ColorwarsOptions.CustomTeamsOption.AddChild(newOption);
            // DefaultTabs.ColorwarsTab.GetOptions().Insert(indexOf + playerOptions.Count, newOption);
        });

        refreshing = false;
    }

    private static void CheckForRandomSpawn()
    {
        if (!ExtraGamemodeOptions.ColorwarsOptions.RandomSpawn) return;
        Players.GetPlayers().ForEach(Game.RandomSpawn.Spawn);
    }

    public static void ShowInformationToGhost(PlayerDeathHookEvent hookEvent)
    {
        PlayerControl player = hookEvent.Player;
        ShowInformationToGhost(player);
    }

    public static void ShowInformationToGhost(PlayerControl player)
    {
        if (player == null) return;

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