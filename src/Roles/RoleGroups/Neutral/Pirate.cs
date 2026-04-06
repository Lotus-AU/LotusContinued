using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Subroles;
using Lotus.Utilities;
using Lotus.Victory.Conditions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;
using Lotus.Roles.Builtins;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Pirate : GuesserRole
{
    private int pirateGuessesToWin;
    private bool pirateDiesOnMissguess;

    private int correctGuesses;

    [UIComponent(UI.Counter, ViewMode.Additive, GameState.Roaming, GameState.InMeeting)]
    public string ShowGuessTotal() => RoleUtils.Counter(correctGuesses, pirateGuessesToWin, RoleColor);

    [RoleAction(LotusActionType.RoundStart)]
    public void RoundStartCheckWinCondition()
    {
        if (correctGuesses < pirateGuessesToWin) return;
        ManualWin.Activate(MyPlayer, ReasonType.RoleSpecificWin, 999);
    }

    protected override void HandleBadGuess()
    {
        if (!pirateDiesOnMissguess)
        {
            guessesThisMeeting = 0;
            GuesserMessage(Guesser.Translations.NoGuessesLeft).Send(MyPlayer);
            return;
        }
        base.HandleBadGuess();
    }

    protected override void HandleCorrectGuess(PlayerControl guessedPlayer, CustomRole guessedRole)
    {
        correctGuesses++;
        base.HandleCorrectGuess(guessedPlayer, guessedRole);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Pirate Guess Win Amount", TranslationUtil.Colorize(Translations.Options.GuessWinAmount, RoleColor))
                .AddIntRange(0, ModConstants.MaxPlayers, 1, 3)
                .BindInt(i => pirateGuessesToWin = i)
                .Build())
            .SubOption(sub => sub
                .KeyName("Pirate Dies on Missguess", TranslationUtil.Colorize(Translations.Options.DieOnMisguess, RoleColor))
                .BindBool(b => pirateDiesOnMissguess = b)
                .AddOnOffValues()
                .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.93f, 0.76f, 0.25f))
            .Faction(FactionInstances.Neutral)
            .SpecialType(SpecialType.Neutral);


    [Localized(nameof(Pirate))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(GuessWinAmount))]
            public static string GuessWinAmount = "Pirate Guess Win Amount";

            [Localized(nameof(DieOnMisguess))]
            public static string DieOnMisguess = "Pirate Dies on Misguess";
        }
    }
}