using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hazel;
using Hazel.Udp;
using InnerNet;
using Lotus.Logging;
using VentLib.Utilities;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Network;

public class ConnectionManager
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ConnectionManager));

    private static readonly Dictionary<long, byte> IPAddressPlayerMapping = new();


    [QuickPrefix(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    public static void BindClientToIP(AmongUsClient __instance, ClientData data)
    {
        UnityUdpClientConnection connection = __instance.connection;
        Async.Schedule(() =>
        {
            if (data.Character == null) log.Trace($"Unable to map connection to player \"{data.PlayerName}\".", "ClientBinding");
            else
            {
                IPAddressPlayerMapping[connection.EndPoint.Address.Address] = data.Character.PlayerId;
                log.Trace($"Successfully bound connection of \"{data.Character.name} (ID={data.Character.PlayerId})", "ClientBinding");
            }
        }, 5f);
    }

    private static void TrackConnectionStatistics(Connection connection)
    {
        DevLogger.Log($"Packets Sent: {connection.Statistics.packetsSent} | Unreliable Packets Sent: {connection.Statistics.unreliableMessagesSent}");
    }


    public static bool IsVanillaServer
    {
        get
        {
            // From Reactor.gg
            const string Domain = "among.us";
            return ServerManager.Instance.CurrentRegion?.TryCast<StaticHttpRegionInfo>() is { } regionInfo &&
                   regionInfo.PingServer.EndsWith(Domain, System.StringComparison.Ordinal) &&
                   regionInfo.Servers.All(serverInfo => serverInfo.Ip.EndsWith(Domain, System.StringComparison.Ordinal));
        }
    }


    public static string GetRegionAbbreviation(string region) // everyone ships different names for the same regions
    {
        if (string.IsNullOrEmpty(region)) return region;

        Regex NikoRegionRegex = new(@"(?:\(|-)([^)-]+)\)?$", RegexOptions.Compiled);

        if (region.StartsWith("Modded ", StringComparison.OrdinalIgnoreCase)) // normal modded regions
        {
            var idx = region.IndexOf("(M", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && region.EndsWith(')'))
                return region[(idx + 1)..^1];

            if (region.Contains("North America", StringComparison.OrdinalIgnoreCase) || region.EndsWith("NA", StringComparison.OrdinalIgnoreCase)) return "MNA";
            if (region.Contains("Europe", StringComparison.OrdinalIgnoreCase) || region.EndsWith("EU", StringComparison.OrdinalIgnoreCase)) return "MEU";
            if (region.Contains("Asia", StringComparison.OrdinalIgnoreCase) || region.EndsWith("AS", StringComparison.OrdinalIgnoreCase)) return "MAS";

            return region;
        }

        if (region.StartsWith("Niko", StringComparison.OrdinalIgnoreCase)) // niko regions
        {
            var match = NikoRegionRegex.Match(region);
            return match.Success ? $"Niko{match.Groups[1].Value}" : region;
        }

        return region switch // vanilla/innersloth regions
        {
            "North America" => "NA",
            "Europe" => "EU",
            "Asia" => "AS",
            _ => region
        };
    }
}