using Lotus.API.Odyssey;
using Lotus.API.Vanilla;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles.Interactions;
using Lotus.Extensions;
using Lotus.GUI.Menus.OptionsMenu.Patches;
using Lotus.Logging;
using Lotus.Roles;
using Lotus.Options;
using UnityEngine;
using VentLib.Options;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Debug.Profiling;
using static Lotus.Managers.Hotkeys.HotkeyManager;
using Lotus.API.Player;
using VentLib.Utilities.Extensions;
using System.Linq;
using Lotus.GUI.Menus.HistoryMenu2;
using VentLib.Localization;

namespace Lotus.Managers.Hotkeys;

[LoadStatic]
public class ModKeybindings
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ModKeybindings));

    private static bool hudActive = true;

    static ModKeybindings()
    {
        // Dump Log
        Bind(KeyCode.F, KeyCode.LeftControl).Do(DumpLog);
        Bind(KeyCode.D, KeyCode.LeftControl).Do(() => LogManager.WriteSessionLog(""));

        // Profile All
        Bind(KeyCode.F2).Do(ProfileAll);

        // Kill Player (Suicide)
        Bind(KeyCode.LeftShift, KeyCode.D, KeyCode.Return)
            .If(p => p.HostOnly().State(Game.InGameStates))
            .Do(Suicide);

        // Close Meeting
        Bind(KeyCode.LeftShift, KeyCode.M, KeyCode.Return)
            .If(p => p.HostOnly().State(GameState.InMeeting))
            .Do(() => MeetingHud.Instance.RpcClose());

        // Instant begin game
        Bind(KeyCode.LeftShift)
            .If(p => p.HostOnly().Predicate(() => MatchState.IsCountDown && !HudManager.Instance.Chat.IsOpenOrOpening))
            .Do(() =>
            {
                SoundManager.Instance.StopSound(GameStartManager.Instance.gameStartSound);
                GameStartManager.Instance.countDownTimer = 0;
            });

        // Restart countdown timer
        Bind(KeyCode.C)
            .If(p => p.HostOnly().Predicate(() => MatchState.IsCountDown && !HudManager.Instance.Chat.IsOpenOrOpening))
            .Do(() =>
            {
                GeneralOptions.AdminOptions.AutoStartMaxTime = -1;
                GameStartManager.Instance.ResetStartState();
            });

        // Bind(KeyCode.C)
        //     .If(p => p.HostOnly().Predicate(() => EndGameManagerPatch.IsRestarting))
        //     .Do(EndGameManagerPatch.CancelPlayAgain);

        // Reset Game Options
        Bind(KeyCode.LeftControl, KeyCode.Delete)
            .If(p => p.Predicate(() => Object.FindObjectOfType<GameOptionsMenu>()))
            .Do(ResetGameOptions);

        // Instant call meeting
        Bind(KeyCode.RightShift, KeyCode.M, KeyCode.Return)
            .If(p => p.HostOnly().State(GameState.Roaming))
            .Do(() => MeetingPrep.PrepMeeting(PlayerControl.LocalPlayer))
            .DevOnly();

        // Sets kill cooldown to 0
        Bind(KeyCode.X)
            .If(p => p.HostOnly().State(GameState.Roaming))
            .Do(InstantReduceTimer)
            .DevOnly();

        // Reload T
        Bind(KeyCode.LeftControl, KeyCode.T)
            .If(p => p.State(GameState.InLobby))
            .Do(ReloadAllFiles);

        // Change Hud Active
        Bind(KeyCode.F7)
            .If(p => p.State(GameState.InLobby, GameState.Roaming).Predicate(() => MeetingHud.Instance == null))
            .Do(() => HudManager.Instance.gameObject.SetActive(hudActive = !hudActive));

        // Close Options I think.
        Bind(KeyCode.Escape)
            .If(p => p.Predicate(() => GameOptionMenuOpenPatch.MenuBehaviour != null && GameOptionMenuOpenPatch.MenuBehaviour.IsOpen))
            .Do(() => GameOptionMenuOpenPatch.MenuBehaviour.Close());

        // Unblackscreen Everyone
        Bind(KeyCode.LeftShift, KeyCode.Z, KeyCode.Return)
            .If(p => p.HostOnly().State(GameState.Roaming))
            .If(() => ProjectLotus.AdvancedRoleAssignment)
            .Do(() => Players.GetPlayers().Where(player => !player.IsModded() && !player.IsHost()).ForEach(player => player.ResetPlayerCam()));

        // Toggle History Menu
        Bind(KeyCode.H)
            .If(p => p.State(GameState.InLobby))
            .Do(ToggleHistoryButton);
    }

    private static void DumpLog()
    {
        LogManager.OpenLogUI();
        /*BasicCommands.Dump(PlayerControl.LocalPlayer);*/
    }

    private static void ProfileAll()
    {
        Profilers.All.ForEach(p =>
        {
            p.Display();
            p.Clear();
        });
    }

    private static void Suicide()
    {
        PlayerControl.LocalPlayer.InteractWith(PlayerControl.LocalPlayer, LotusInteraction.FatalInteraction.Create(PlayerControl.LocalPlayer));
    }

    private static void ResetGameOptions()
    {
        log.High("Resetting Game Options", "ResetOptions");
        OptionManager.GetAllManagers().ForEach(m =>
        {
            m.GetOptions().ForEach(o =>
            {
                o.SetValue(o.DefaultIndex);
                OptionHelpers.GetChildren(o).ForEach(o2 => o2.SetValue(o2.DefaultIndex));
            });
            m.DelaySave(0);
        });
        LogManager.SendInGame("All options have been reset!");
    }

    private static void InstantReduceTimer()
    {
        PlayerControl.LocalPlayer.SetKillCooldown(0f);
    }

    private static void ReloadAllFiles()
    {
        log.Trace("Reloading every file into memory...");
        PluginDataManager.ReloadAll(errorInfo =>
        {
            log.Exception(errorInfo.ex);
            LogManager.SendInGame($"Error occured while reloading {errorInfo.erorrStuff}. Check log for more details.");
        });
        try
        {
            Localizer.Reload();
        }
        catch (System.Exception ex)
        {
            log.Exception(ex);
            LogManager.SendInGame("Error occured while reloading Translations. Check log for more details.");
        }
        LogManager.SendInGame("Reloaded Every file in LOTUS_DATA!");
    }

    private static void ToggleHistoryButton()
    {
        if (DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening) return;
        HM2 historyMenu = DestroyableSingleton<HudManager>.Instance.GetComponent<HM2>();
        if (historyMenu != null) historyMenu.ToggleMenu();
    }
}