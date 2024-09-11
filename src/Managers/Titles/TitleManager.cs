using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using VentLib.Utilities.Extensions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lotus.Managers.Titles;

public class TitleManager
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(TitleManager));

    private DirectoryInfo directory;
    private Dictionary<string, List<CustomTitle>> titles = null!;

    private static readonly IDeserializer TitleDeserializer = new DeserializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();

    public TitleManager(DirectoryInfo directory)
    {
        Hooks.NetworkHooks.ReceiveVersionHook.Bind(nameof(TitleManager), _ => Players.GetPlayers().ForEach(ApplyTitleWithChatFix), replace: true);
        if (!directory.Exists) directory.Create();
        this.directory = directory;
        LoadAll();
        if (AmongUsClient.Instance.IsInGame) Players.GetPlayers().ForEach(p => p.RpcSetName(p.name));
    }

    public string ApplyTitle(string friendCode, string playerName, bool nameOnly = false)
    {
        if (!AmongUsClient.Instance.AmHost) return playerName;
        if (Game.State is not GameState.InLobby) return playerName;
        if (friendCode == "") return playerName;
        return titles.GetOptional(friendCode).CoalesceEmpty(() => titles.GetOptional("_MASS"))
            .Transform(p => p.LastOrDefault()?.ApplyTo(playerName, nameOnly) ?? playerName, () => playerName);
    }

    public bool HasTitle(PlayerControl player)
    {
        if (Game.State is not GameState.InLobby) return false;
        return player.FriendCode != "" && titles.ContainsKey(player.FriendCode);
    }

    public void ApplyTitleWithChatFix(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        string playerName = player.name;
        player.RpcSetName(ApplyTitle(player.FriendCode, playerName));
        player.Data.Outfits[PlayerOutfitType.Default].PlayerName = ApplyTitle(player.FriendCode, playerName, true);
        player.name = playerName;
    }

    public void ClearTitle(PlayerControl player)
    {
        player.RpcSetName(player.name);
    }

    public void Reload()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        LoadAll();
        Players.GetPlayers().ForEach(p => p.RpcSetName(p.name));
    }

    public void LoadAll()
    {
        // Loading from manifest
        try
        {
            titles = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Where(n => n.Contains("Lotus.assets.Titles"))
                .Select(s =>
                {
                    string friendcode = s.Replace("Lotus.assets.Titles.", "").Replace(".yaml", "");
                    return (friendcode, LoadTitleFromManifest(s));
                })
                .Where(t => t.Item2 != null)
                .ToDict(t => t.friendcode, t => new List<CustomTitle> { t.Item2! });

        }
        catch (Exception exception)
        {
            log.Exception("Error loading in manifest (global) titles.", exception);
            titles = new Dictionary<string, List<CustomTitle>>();
        }


        // Load from titles directory
        directory.GetFiles("*.yaml")
            .Select(f =>
            {
                try
                {
                    string friendCode = f.Name.Replace(".yaml", "");
                    return (friendCode, LoadFromFileInfo(f));
                }
                catch (Exception exception)
                {
                    log.Exception($"Error loading title file: {f.Name}.", exception);
                    return (null!, new CustomTitle());
                }
            })
            .ForEach(pair =>
            {
                if (!string.IsNullOrEmpty(pair.Item1)) titles.GetOrCompute(pair.Item1, () => new List<CustomTitle>()).Add(pair.Item2);
            });
    }

    private static CustomTitle? LoadTitleFromManifest(string manifestResource)
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestResource);
        return stream == null ? null : LoadFromStream(stream);
    }

    private static CustomTitle LoadFromFileInfo(FileInfo file) => LoadFromStream(file.Open(FileMode.Open));

    private static CustomTitle LoadFromStream(Stream stream)
    {
        string result;
        using (StreamReader reader = new(stream)) result = reader.ReadToEnd();

        return TitleDeserializer.Deserialize<CustomTitle>(result);
    }
}