using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InnerNet;
using Lotus.Logging;
using Lotus.Extensions;
using Lotus.Managers.Models;
using VentLib.Localization;
using VentLib.Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Reflection;

namespace Lotus.Managers;

public class BanManager
{
    private IDeserializer deserializer = new DeserializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();

    private ISerializer serializer = new SerializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();

    private FileInfo banFile;
    private BanPlayerFile banPlayerFile;

    public BanManager(FileInfo banFile)
    {
        this.banFile = banFile;

        if (!banFile.Exists) WriteFile(new BanPlayerFile());

        string content;
        using (StreamReader reader = new(banFile.Open(FileMode.OpenOrCreate))) content = reader.ReadToEnd();
        banPlayerFile = deserializer.Deserialize<BanPlayerFile>(content);
    }

    public void BanWithReason(PlayerControl player, string internalReason, string displayedReason)
    {
        ClientData? clientData = player.GetClient();
        if (clientData != null) AddBanPlayer(clientData, internalReason);
        AmongUsClient.Instance.KickPlayerWithMessage(player, displayedReason, true);
    }

    public void AddBanPlayer(ClientData player, string? reason = null)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.FriendCode == "") return;
        if (CheckBanList(player)) return;

        banPlayerFile.Players.Add(new BannedPlayer(reason) { FriendCode = player.FriendCode, Name = player.PlayerName });
        WriteFile(banPlayerFile);
    }

    public bool CheckBanPlayer(PlayerControl player, ClientData client)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!CheckBanList(client)) return false;

        string message = string.Format(Localizer.Translate("ModerationActions.BannedByBanList", assembly: Assembly.GetExecutingAssembly()), client.PlayerName);

        LogManager.SendInGame(message);
        AmongUsClient.Instance.KickPlayerWithMessage(player, message, true);
        return true;
    }

    public bool CheckBanList(ClientData player)
    {
        if (banPlayerFile == null!) banPlayerFile = new BanPlayerFile();
        if (banPlayerFile.Players == null!) banPlayerFile.Players = new List<BannedPlayer>();
        return player.FriendCode != "" && banPlayerFile.Players.Any(p => p.FriendCode == player.FriendCode);
    }

    private void WriteFile(BanPlayerFile banPlayerFile)
    {
        string emptyFile = serializer.Serialize(banPlayerFile);
        using FileStream stream = banFile.Open(FileMode.Create);
        stream.Write(Encoding.UTF8.GetBytes(emptyFile));
    }
}