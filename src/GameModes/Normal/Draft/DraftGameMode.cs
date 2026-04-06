using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Meetings;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.GameModes.Normal.Draft.Distributions;
using Lotus.GameModes.Normal.Standard;
using Lotus.GameModes.Normal.Standard.Conditions;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Extra.Draft;
using Lotus.Roles.RoleGroups.Undead;
using Lotus.Victory;
using Lotus.Victory.Conditions;
using VentLib.Options.UI;
using VentLib.Options.UI.Tabs;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Draft;

public class DraftGameMode : GameMode
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(DraftGameMode));
    private const string DraftGameModeHookKey = nameof(DraftGameModeHookKey);

    public static DraftGameMode Instance = null!;
    public static Drafter DrafterRole;

    public override string Name { get; set; } = GamemodeTranslations.Draft.Name;
    public override string Description { get; set; } = GamemodeTranslations.Draft.GamemodeDescription;
    public override DraftRoleOperations RoleOperations { get; }
    public override NormalStandardRoleManager RoleManager { get; }
    public DraftRoleAssignment RoleAssignment { get; }
    public override MatchData MatchData { get; set; }

    public List<CustomRole> AvailableRoles;

    public int RoleChoiceCount = 3;
    public int DecisionTime = 25;

    public int TotalPlayers;
    public int CurrentPlayersTurn;

    public List<Drafter> RoleOrder;

    public bool RanFirstRound;

    public DraftGameMode() : base()
    {
        Instance = this;
        MatchData = new MatchData();
        RoleAssignment = new DraftRoleAssignment();

        RoleOperations = new DraftRoleOperations(this);
        RoleManager = NormalStandardGameMode.Instance.RoleManager;

        DrafterRole = new Drafter();
        NormalStandardRoles.Instance.AllRoles.Add(DrafterRole);
        DrafterRole.Solidify();
        RoleManager.RegisterRole(DrafterRole);

        DefaultTabs.DraftTab.AddOption(new GameOptionTitleBuilder()
            .Title(GamemodeTranslations.Draft.ButtonText)
            .Build());
        DefaultTabs.DraftTab.AddOption(new GameOptionBuilder()
            .KeyName("Choice Count", GamemodeTranslations.Draft.RoleChoiceCount)
            .IsHeader(true)
            .AddIntRange(2, 4, 1, 1)
            .BindInt(i => RoleChoiceCount = i)
            .Build());
        DefaultTabs.DraftTab.AddOption(new GameOptionBuilder()
            .KeyName("Decision Time", GamemodeTranslations.Draft.DecisionTime)
            .AddIntRange(10, 60, 5, 3)
            .BindInt(i => DecisionTime = i)
            .Build());


        GeneralOptions.StandardOptions.ForEach(DefaultTabs.DraftTab.AddOption);
    }

    public override void Activate()
    {
        Hooks.PlayerHooks.PlayerDeathHook.Bind(DraftGameModeHookKey, NormalStandardGameMode.ShowInformationToGhost, priority: API.Priority.VeryLow);
        Hooks.GameStateHooks.RoundStartHook.Bind(DraftGameModeHookKey, CallStartingMeeting);
    }

    public override void Deactivate()
    {
        // EnabledTabs().ForEach(tb => tb.GetOptions().ForEach(tb.RemoveOption));
        Hooks.UnbindAll(DraftGameModeHookKey);
    }

    public override void Assign(PlayerControl player, CustomRole role, bool addAsMainRole = true, bool sendToClient = false)
    {
        RoleOperations.Assign(role, player, addAsMainRole, sendToClient);
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.NormalStandardTabs;
    public override MainSettingTab MainTab() => DefaultTabs.DraftTab;

    public override void Setup()
    {
        TotalPlayers = 0;
        CurrentPlayersTurn = 0;
        RanFirstRound = false;

        MatchData = new MatchData();
        Game.GetWinDelegate().AddSubscriber(NormalStandardGameMode.FixNeutralTeamingWinners);
    }

    public override void SetupWinConditions(WinDelegate winDelegate)
    {

    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        AvailableRoles = RoleAssignment.AssignRoles(players.Count, RoleChoiceCount - 1);
        players.ForEach(p => Assign(p, DrafterRole));
        base.AssignRoles(players);
    }

    private static void CallStartingMeeting(GameStateHookEvent @event)
    {
        TimeSpan elasped = DateTime.Now - @event.MatchData.StartTime;
        if (elasped.TotalSeconds > 5)
        {
            Instance.HandleRealFirstRound();
            return;
        }

        var allRoles = Players.GetAllRoles().Where(r => r is Drafter).Cast<Drafter>().ToList();
        allRoles.Shuffle();

        Instance.TotalPlayers = allRoles.Count;
        int index = 1;
        foreach (var role in allRoles)
            role.SetFakePlayerId(index++);

        Instance.RoleOrder = allRoles;

        Async.Schedule(() =>
        {
            MeetingPrep.Reported = null;
            MeetingPrep.PrepMeeting(PlayerControl.LocalPlayer, checkReportBodyCancel: false);
        }, 1f);
        Async.Schedule(() =>
        {
            foreach (var role in allRoles) role.AnnounceID();
            Instance.SetNextPlayersTurn();
        }, 7f);
    }

    private void HandleRealFirstRound()
    {
        if (RanFirstRound) return;
        RanFirstRound = true;

        new List<IWinCondition> {
            new VanillaCrewmateWin(), new VanillaImpostorWin(), new SoloPassiveWinCondition(),
            new UndeadWinCondition(), new SoloKillingWinCondition(), new SabotageWin(),
            new NeutralFactionWin()
        }.ForEach(Game.GetWinDelegate().AddWinCondition);

        Roles.Operations.RoleOperations.Current.TriggerForAll(LotusActionType.RoundStart, null, true);
        log.Debug("true first round has begun.");
    }

    public void SetNextPlayersTurn()
    {
        CurrentPlayersTurn += 1;
        if (CurrentPlayersTurn == TotalPlayers + 1)
        {
            BeginCountdown();
            return;
        }

        Drafter currentPlayer = RoleOrder[CurrentPlayersTurn - 1];
        if (currentPlayer.MyPlayer && currentPlayer.MyPlayer.IsAlive())
            currentPlayer.SetMyTurn();
        else
            SetNextPlayersTurn();
    }

    private void BeginCountdown()
    {
        int countdownLength = 3;
        int timeLeft = countdownLength;
        ChatHandler.Of(GamemodeTranslations.Draft.StartingInText.Formatted(countdownLength)).Send();
        for (int i = 0; i < countdownLength; i++)
            Async.Schedule(() =>
            {
                timeLeft--;
                if (timeLeft == 0) EndDrafterMeeting();
                else ChatHandler.Of(GamemodeTranslations.Draft.StartingInText.Formatted(timeLeft)).Send();
            }, countdownLength - i);
    }

    private void EndDrafterMeeting()
    {
        MeetingHud.Instance.RpcClose();
        foreach (Drafter drafter in RoleOrder) drafter.AssignRole();
        RoleAssignment.AssignSubroles(Players.GetPlayers(PlayerFilter.Alive).ToList());
        foreach (Drafter drafter in RoleOrder) drafter.ChangeRoleTo(MatchData.Roles.GetMainRole(drafter.MyPlayer.PlayerId), false);
        RoleOrder = [];
        ShipStatus.Instance.Begin();
        GameData.Instance.RecomputeTaskCounts();
    }
}