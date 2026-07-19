using System.Collections.Generic;

namespace Lotus.Managers.Models;

public class BannedWordFile
{
    public List<string> GlobalBannedWords { get; set; } = new() { "YourGlobally", "BannedWordsHere" };
    public List<string> LobbyBannedWords { get; set; } = new() { "YourLobby", "BannedWordsHere" };
    public List<string> BannedNames { get; set; } = new() { "YourBanned", "NamesHere" };

}