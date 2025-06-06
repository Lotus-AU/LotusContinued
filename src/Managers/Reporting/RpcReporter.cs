using System;
using System.Collections.Generic;
using System.Linq;
using InnerNet;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Utilities;
using Lotus.Extensions;
using VentLib.Networking.RPC;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using EnumerableExtensions = VentLib.Utilities.Extensions.EnumerableExtensions;

namespace Lotus.Managers.Reporting;

[LoadStatic]
class RpcReporter: IReportProducer
{
    private const string ReporterHookKey = nameof(RpcReporter);
    private static RpcReporter _reporter = new();
    private List<(DateTime, RpcMeta)> rpcs = new();

    private RpcReporter()
    {
        Hooks.GameStateHooks.GameStartHook.Bind(ReporterHookKey, RefreshOnState);
        Hooks.GameStateHooks.GameEndHook.Bind(ReporterHookKey, RefreshOnState);
        Hooks.NetworkHooks.RpcHook.Bind(ReporterHookKey, HandleRpcEvent);
        ReportManager.AddProducer(this, ReportTag.KickByAnticheat, ReportTag.KickByPacket);
    }

    private void RefreshOnState(GameStateHookEvent hookEvent)
    {
        rpcs.Clear();
    }

    private void HandleRpcEvent(RpcHookEvent rpcHookEvent)
    {
        rpcs.Add((DateTime.Now, rpcHookEvent.Meta));
    }

    public ReportInfo ProduceReport() => ReportInfo.Create("Rpc History", "rpc-history").Attach(CreateRpcContent());

    private string CreateRpcContent()
    {
        string content = "";
        rpcs.ForEach(tuple =>
        {
            string timestamp = tuple.Item1.ToString("hh:mm:ss.fff");
            RpcMeta meta = tuple.Item2;
            if (meta is RpcMassMeta massMeta)
            {
                content += GenerateMassContent(timestamp, massMeta);
                return;
            }

            string targetName = FindObjectByNetId(meta.NetId)?.name ?? "(null target)";
            string recipient = Utils.PlayerByClientId(meta.Recipient).Map(p => p.name).OrElse("Unknown") + $" (Id: {meta.Recipient})";
            string rpc = ((RpcCalls)meta.CallId).Name();

            content += $"[{timestamp}] (Target: {targetName}, Recipient: {recipient}, RPC: {rpc}, SendOptions: {meta.SendOption}, PacketSize: {meta.PacketSize}, Arguments: [{meta.Arguments.Select(i => i?.ToString()?.RemoveHtmlTags()).Fuse()}])\n";
        });
        return content;
    }

    private string GenerateMassContent(string timestamp, RpcMassMeta massMeta)
    {
        string content = "";
        // string target = AmongUsClient.Instance.FindObjectByNetId<PlayerControl>(massMeta.NetId)?.name ?? Players.GetPlayers().FirstOrDefault(p => p.NetId == massMeta.NetId)?.name ?? "Unknown";
        string target = FindObjectByNetId(massMeta.NetId)?.name ?? "(null target)";
        string recipient = Utils.PlayerByClientId(massMeta.Recipient).Map(p => p.name).OrElse("Unknown") + $" (Id: {massMeta.Recipient})";

        content += $"[{timestamp}] MASS RPC => (Target: {target}, Recipient: {recipient}, SendOptions: {massMeta.SendOption}, PacketSize: {massMeta.PacketSize})";
        massMeta.ChildMeta.ForEach(meta =>
        {
            // string targ = AmongUsClient.Instance.FindObjectByNetId<PlayerControl>(meta.NetId)?.name ?? Players.GetPlayers().FirstOrDefault(p => p.NetId == meta.NetId)?.name ?? "Unknown";
            string targ = FindObjectByNetId(massMeta.NetId)?.name ?? "(null target)";
            string recip = Utils.PlayerByClientId(meta.Recipient).Map(p => p.name).OrElse("Unknown") + $" (Id: {meta.Recipient})";
            string rpc = ((RpcCalls)meta.CallId).Name();

            content += $"\n- [{timestamp}] (Target: {targ}, Recipient: {recip}, RPC: {rpc}, SendOptions: {meta.SendOption}, PacketSize: {meta.PacketSize}, Arguments: [{meta.Arguments.Select(i => i?.ToString()?.RemoveHtmlTags()).Fuse()}])";
        });
        return content;
    }

    private InnerNetObject? FindObjectByNetId(uint netId)
    {
        InnerNetObjectCollection allObjects = AmongUsClient.Instance.allObjects;
        lock (allObjects)
            if (allObjects.allObjectsFast.TryGetValue(netId, out InnerNetObject innerNetObject)) return innerNetObject;
        return null;
    }
}