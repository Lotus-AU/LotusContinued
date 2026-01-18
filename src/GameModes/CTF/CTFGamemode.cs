using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Chat;
using Lotus.Chat.Commands;
using Lotus.GameModes.CTF.Conditions;
using Lotus.GameModes.CTF.Distributions;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.CTF;
using Lotus.RPC.CustomObjects.Builtin;
using Lotus.Utilities;
using Lotus.Victory;
using UnityEngine;
using VentLib.Options.UI.Tabs;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.CTF;

public class CTFGamemode : GameMode
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(CTFGamemode));
    private const string CTFGamemodeHookKey = nameof(CTFGamemodeHookKey);
    public static CTFGamemode Instance = null!;

    public override string Name { get; set; } = GamemodeTranslations.CaptureTheFlag.Name;
    public override string Description { get; set; } = GamemodeTranslations.CaptureTheFlag.GamemodeDescription;
    public override CTFRoleOperations RoleOperations { get; }
    public override CTFRoleManager RoleManager { get; }
    public override MatchData MatchData { get; set; }
    public CTFRoleAssignment RoleAssignment { get; }

    public override BlockableGameAction BlockedActions() => BlockableGameAction.CallSabotage | BlockableGameAction.CallMeeting
        | BlockableGameAction.CloseDoors | BlockableGameAction.ReportBody;
    public override GameModeFlags GameFlags() => GameModeFlags.AllowChatDuringGame;

    // Red Team // Blue Team
    public static Vector2[] SpawnLocations = [new(-20.625f, -5.5f), new(16.425f, -4.8f)];

    public static int Team0Score = 0;
    public static int Team1Score = 0;

    public static byte Team0FlagCarrier = byte.MaxValue;
    public static byte Team1FlagCarrier = byte.MaxValue;

    public static RedFlag RedFlag = null!;
    public static BlueFlag BlueFlag = null!;

    private bool hasReturnedRed;
    private bool hasReturnedBlue;

    public CTFGamemode()
    {
        Instance = this;
        MatchData = new();

        RoleOperations = new(this);
        RoleManager = new();

        RoleAssignment = new();
    }

    public override void Activate()
    {
        Hooks.PlayerHooks.PlayerDeathHook.Bind(CTFGamemodeHookKey, ShowInformationToGhost, priority: API.Priority.VeryLow);
    }

    public override void Deactivate()
    {
        Hooks.UnbindAll(CTFGamemodeHookKey);
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.CaptureTabs;
    public override MainSettingTab MainTab() => DefaultTabs.CaptureTab;

    public override void Setup()
    {
        MatchData = new MatchData();

        Team0Score = 0;
        Team1Score = 0;
        Team0FlagCarrier = byte.MaxValue;
        Team1FlagCarrier = byte.MaxValue;

        hasReturnedRed = true;
        hasReturnedBlue = true;

        SpawnLocations = null!;
    }
    public override void SetupWinConditions(WinDelegate winDelegate) => winDelegate.AddWinCondition(new PointWinCondition());

    public override void Assign(PlayerControl player, CustomRole role, bool addAsMainRole = true, bool sendToClient = false)
    {
        RoleOperations.Assign(role, player, addAsMainRole, sendToClient);
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        RoleAssignment.AssignRoles(players);
        base.AssignRoles(players);
    }

    public override void FixedUpdate()
    {
        if (Team0FlagCarrier != byte.MaxValue)
        {
            hasReturnedRed = false;
            PlayerControl? carrier = Utils.GetPlayerById(Team0FlagCarrier);
            if (carrier != null) RedFlag.SnapTo(carrier.NetTransform.body.position);
        } else if (!hasReturnedRed)
        {
            hasReturnedRed = true;
            RedFlag.Return();
        }

        if (Team1FlagCarrier != byte.MaxValue)
        {
            hasReturnedBlue = false;
            PlayerControl? carrier = Utils.GetPlayerById(Team1FlagCarrier);
            if (carrier != null) BlueFlag.SnapTo(carrier.NetTransform.body.position);
        } else if (!hasReturnedBlue)
        {
            hasReturnedBlue = true;
            BlueFlag.Return();
        }
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
            .Select(p => p.NameModel().GetComponentHolder<GUI.Name.Holders.RoleHolder>())
            .ForEach(h => h.Components().ForEach(components => components.AddViewer(player)));

        ChatHandler.Of(Striker.Translations.OvertimeDeathMessage, Color.red.Colorize(Striker.Translations.OvertimeTitle)).Send(player);

        player.NameModel().Render(force: true);
    }
}