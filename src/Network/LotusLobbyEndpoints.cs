using System.Collections.Generic;
using AmongUs.Data;
using Lotus.API.Odyssey;
using Lotus.Options;
using VentLib.Lobbies;

namespace Lotus.Network;

public class LotusLobbyEndpoints : ILobbyServerInfo
{
    private static Dictionary<string, string> CreationHeaders => new()
    {
        ["Authorization"] = $"Bearer {EOSManager.Instance.UserIDToken}",
        ["X-Language"] = DataManager.Settings.language.language,
        ["X-Current-Gamemode"] = Game.CurrentGameMode.Name,
        ["X-AutoHost"] = GeneralOptions.AdminOptions.AutoStartEnabled.ToString()
    };

    private static Dictionary<string, string> UpdateHeaders => new()
    {
        ["Authorization"] = $"Bearer {EOSManager.Instance.UserIDToken}",
        ["X-Current-GameMode"] = Game.CurrentGameMode.Name,
        ["X-AutoHost"] = GeneralOptions.AdminOptions.AutoStartEnabled.ToString()
    };

    private const bool IsDebug = false;

    public string CreateEndpoint() => IsDebug ? $"https://testing-lotus.eps.lol/lobbies/{AmongUsClient.Instance.GameId}" : $"https://lobbies.lotusau.top/lobbies/{AmongUsClient.Instance.GameId}";
    public string UpdatePlayerStatusEndpoint() => IsDebug ? $"https://testing-lotus.eps.lol/lobbies/{AmongUsClient.Instance.GameId}/update" : $"https://lobbies.lotusau.top/lobbies/{AmongUsClient.Instance.GameId}/update";
    public string UpdateMapEndpoint() => UpdatePlayerStatusEndpoint();
    public Dictionary<string, string> AddCustomHeaders(LobbyUpdateType _)
    {
        return _ switch
        {
            LobbyUpdateType.Creation => CreationHeaders,
            _ => UpdateHeaders
        };
    }
}