using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.Chat;
using Lotus.Victory;
using VentLib.Options;
using VentLib.Options.UI;
using Lotus.Options;
using Lotus.Extensions;

using Lotus.GameModes.Standard;
using Lotus.GameModes.Colorwars;
using Lotus.GameModes.CTF;
using Lotus.GameModes.Draft;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes;

// As we move to the future we're going to try to use instances for managers rather than making everything static
public class GameModeManager
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(GameModeManager));

    private const string GameModeManagerStartHook = nameof(GameModeManager);

    internal readonly List<IGameMode> GameModes = new();

    public IGameMode CurrentGameMode
    {
        get => currentGameMode!;
        private set
        {
            currentGameMode?.InternalDeactivate();
            currentGameMode = value;
            currentGameMode?.InternalActivate();

            if (Game.State is GameState.InLobby) Players.GetAllPlayers().ForEach(p => AnnounceCurrentGameModeToPlayer(p, value));
        }
    }

    private IGameMode? currentGameMode;
    internal GameOption gamemodeOption = null!;

    public GameModeManager()
    {
        Hooks.GameStateHooks.GameStartHook.Bind(GameModeManagerStartHook, _ => CurrentGameMode.SetupWinConditions(Game.GetWinDelegate()));
    }

    public void SetGameMode(int id)
    {
        if (currentGameMode?.GetType() == GameModes[id].GetType()) return;
        CurrentGameMode = GameModes[id];
        log.High($"Setting GameMode {CurrentGameMode.Name}", "GameMode");
    }

    public IEnumerable<IGameMode> GetGameModes() => GameModes;
    public IGameMode GetGameMode(int id) => GameModes[id];
    public IGameMode? GetGameMode(Type type) => GameModes.FirstOrDefault(t => t.GetType() == type);

    internal void AddGamemodes() => GameModes.AddRange([
            new StandardGameMode(),
            new ColorwarsGamemode(),
            new CTFGamemode(),
            new DraftGameMode()
        ]);

    public void Setup()
    {
        GameOptionBuilder builder = new();

        for (int i = 0; i < GameModes.Count; i++)
        {
            IGameMode gameMode = GameModes[i];
            var index = i;
            builder.Value(v => v.Text(gameMode.Name).Value(index).Build());
        }

        gamemodeOption = builder.KeyName("GameMode", GamemodeTranslations.GamemodeText).IsHeader(true).BindInt(SetGameMode).Build();
        OptionManager.GetManager(file: "other.txt", managerFlags: OptionManagerFlags.SyncOverRpc).Register(gamemodeOption, OptionLoadMode.LoadOrCreate);
        if (currentGameMode == null) SetGameMode(0);
        GameModes.ForEach(gm => AddGamemodeSettingToOptions(gm.MainTab().GetOptions()));
    }

    public void StartGame(WinDelegate winDelegate)
    {
        CurrentGameMode.CoroutineManager.Start();
        CurrentGameMode.SetupWinConditions(winDelegate);
    }

    public void AnnounceCurrentGameModeToPlayer(PlayerControl player, IGameMode mode) =>
        ChatHandler.Of(mode.Description, GamemodeTranslations.GamemodeText + ": " + mode.Name);

    internal void AddGamemodeSettingToOptions(List<GameOption> options)
    {
        // Add gamemode switcher at top
        options.Insert(0, gamemodeOption);
        options.Insert(0, new GameOptionTitleBuilder()
            .Title(GamemodeTranslations.GamemodeSelection)
            .Build());

        // Add Admin Options
        options.InsertRange(2, GeneralOptions.AdminOptions.AllOptions);

        // Add Miscellaneous Options
        options.AddRange(GeneralOptions.MiscellaneousOptions.AllOptions);

        // Add Debug Options
        options.AddRange(GeneralOptions.DebugOptions.AllOptions);
    }
}