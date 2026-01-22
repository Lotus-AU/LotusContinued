using VentLib.Localization.Attributes;

namespace Lotus.GameModes;

[Localized("Options.GameMode")]
public class GamemodeTranslations
{
    [Localized(nameof(GamemodeText))] public static string GamemodeText = "GameMode";
    [Localized(nameof(GamemodeSelection))] public static string GamemodeSelection = "Gamemode Selection";

    [Localized(nameof(Standard))]
    public class Standard
    {
        [Localized(nameof(Name))] public static string Name = "Standard";

        [Localized(nameof(ButtonText))] public static string ButtonText = "Lotus Settings";
        [Localized(nameof(Description))] public static string Description = "Modify all the Standard settings here!";

        [Localized(nameof(ImpostorTab))] public static string ImpostorTab = "Impostor Settings";
        [Localized(nameof(CrewmateTab))] public static string CrewmateTab = "Crewmate Settings";
        [Localized(nameof(NeutralTab))] public static string NeutralTab = "Neutral Settings";
        [Localized(nameof(MiscTab))] public static string MiscTab = "Misc Settings";
        [Localized(nameof(HiddenTab))] public static string HiddenTab = "Hidden";

        [Localized(nameof(GamemodeDescription))] public static string GamemodeDescription = "This text is shown to players when they join to explain the current gamemode.";
    }

    [Localized(nameof(CaptureTheFlag))]
    public class CaptureTheFlag
    {
        [Localized(nameof(Name))] public static string Name = "Capture The Flag";

        [Localized(nameof(ButtonText))] public static string ButtonText = "CTF Settings";
        [Localized(nameof(Description))] public static string Description = "Change anything about CTF here!";

        [Localized(nameof(GamemodeDescription))] public static string GamemodeDescription = "This text is shown to players when they join to explain the current gamemode.";
    }

    [Localized(nameof(Colorwars))]
    public class Colorwars
    {
        [Localized(nameof(Name))] public static string Name = "Colorwars";

        [Localized(nameof(ButtonText))] public static string ButtonText = "Colorwars Settings";
        [Localized(nameof(Description))] public static string Description = "Modify the Colorwars settings here!";

        [Localized(nameof(GamemodeDescription))] public static string GamemodeDescription = "This text is shown to players when they join to explain the current gamemode.";
    }

    [Localized(nameof(Draft))]
    public class Draft
    {
        [Localized(nameof(Name))] public static string Name = "Draft";

        [Localized(nameof(ButtonText))] public static string ButtonText = "Draft Settings";
        [Localized(nameof(Description))] public static string Description = "Modify the Draft gamemode settings here!";

        [Localized(nameof(DecisionTime))] public static string DecisionTime = "Decision Time";
        [Localized(nameof(RoleChoiceCount))] public static string RoleChoiceCount = "Role Choice Count";

        [Localized(nameof(DraftTitle))] public static string DraftTitle = "Draft GameMode";

        [Localized(nameof(RandomOption))] public static string RandomOption = "Random";
        [Localized(nameof(DraftTitle))] public static string StartingInText = "Starting In {0}...";
        [Localized(nameof(PlayerEndTurn))] public static string PlayerEndTurn = "Player {0} has chose {1}.";
        [Localized(nameof(DraftTitle))] public static string PlayerIdText = "Your ID is {1}. Wait for your turn.";
        [Localized(nameof(PlayerSwapChoice))] public static string PlayerSwapChoice = "Your decision is now: {0}. Skip to confirm.";
        [Localized(nameof(PlayerChoiceOptions))] public static string PlayerChoiceOptions = "Your options to choose between from are: {0}\nVote any player to change your selection.{1}";
        [Localized(nameof(PlayerStartsTurn))] public static string PlayerStartsTurn = "Player {0} is now choosing their role. They have {1} seconds to make a decision before random is picked automatically.";

        [Localized(nameof(GamemodeDescription))] public static string GamemodeDescription = "This text is shown to players when they join to explain the current gamemode.";
    }
}