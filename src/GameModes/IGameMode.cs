using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.Managers;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Roles.Overrides;
using Lotus.Victory;
using Lotus.Roles.Managers.Interfaces;
using VentLib.Options.UI;
using VentLib.Options.UI.Tabs;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Options.UI.Controllers;

namespace Lotus.GameModes;

public interface IGameMode
{
    public string Name { get; protected internal set; }
    public CoroutineManager CoroutineManager { get; }
    public MatchData MatchData { get; protected internal set; }
    public RoleOperations RoleOperations { get; }
    public Roles.Managers.RoleManager RoleManager { get; }

    IEnumerable<GameOptionTab> EnabledTabs();
    MainSettingTab MainTab();

    void Assign(PlayerControl player, CustomRole role, bool addAsMainRole = true, bool sendToClient = false);
    void AssignRoles(List<PlayerControl> players);
    // void OnShowRole();

    protected internal void Activate();

    protected internal void Deactivate();

    protected internal void FixedUpdate();

    protected internal void Setup();

    protected internal void SetupWinConditions(WinDelegate winDelegate);

    BlockableGameAction BlockedActions();

    GameModeFlags GameFlags();

    Remote<GameOptionOverride> AddOverride(byte playerId, GameOptionOverride optionOverride) => MatchData.Roles.AddOverride(playerId, optionOverride);

    internal void InternalActivate()
    {
        Activate();
        SettingsOptionController.SetMainTab(MainTab());
        EnabledTabs().ForEach(RoleOptionController.AddTab);
    }

    internal void InternalDeactivate()
    {
        Deactivate();
        EnabledTabs().ForEach(RoleOptionController.RemoveTab);
    }

    void Trigger(LotusActionType action, ActionHandle handle, params object[] arguments);
}