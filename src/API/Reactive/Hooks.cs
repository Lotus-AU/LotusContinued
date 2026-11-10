using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Lotus.API.Reactive.HookEvents;
using Lotus.Managers.History.Events;
using VentLib.Networking;
using VentLib.Networking.RPC;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.API.Reactive;

[LoadStatic]
public class Hooks
{
    internal static readonly Dictionary<uint, Action<IHookEvent>> Events = new();
    // TODO: Allow for async hooking

    static Hooks()
    {
        AbstractConstructors.Register(typeof(INetworkedHookEvent), reader =>
        {
            string qualifiedName;
            try
            {
                qualifiedName = reader.ReadString();
            }
            catch
            {
                qualifiedName = "N/A";
            }

            if (qualifiedName == "N/A") throw new InvalidOperationException("A networked event's Write function wasn't setup to send a type name!");
            Type? targetType = Type.GetType(qualifiedName);
            if (targetType == null) throw new InvalidOperationException($"Unknown hook event type '{qualifiedName}'.");
            object? targetEvent = Activator.CreateInstance(targetType, nonPublic: true);
            if (targetEvent is not INetworkedHookEvent @event) throw new InvalidOperationException($"{qualifiedName} is not an INetworkedHookEvent.");
            @event.Read(reader);
            return @event;
        });
        AbstractConstructors.Register(typeof(IDeathEvent), IDeathEvent.ReadDeathEvent);
    }

    public static class GameStateHooks
    {
        public static readonly Hook<GameStateHookEvent> GameStartHook = new SimpleHook<GameStateHookEvent>();
        public static readonly Hook<GameStateHookEvent> RoundStartHook = new SimpleHook<GameStateHookEvent>();
        public static readonly Hook<GameStateHookEvent> RoundEndHook = new SimpleHook<GameStateHookEvent>();
        public static readonly Hook<GameStateHookEvent> GameEndHook = new SimpleHook<GameStateHookEvent>();
    }

    public static class ResultHooks
    {
        public static readonly Hook<WinnersHookEvent> WinnersHook = new SimpleHook<WinnersHookEvent>();
        public static readonly Hook<LosersHookEvent> LosersHook = new SimpleHook<LosersHookEvent>();
        public static readonly Hook<EmptyHookEvent> ForceEndGameHook = new NetworkedHook<EmptyHookEvent>(3);
    }

    public static class NetworkHooks
    {
        public static readonly Hook<GameJoinHookEvent> GameJoinHook = new SimpleHook<GameJoinHookEvent>();
        public static readonly Hook<RpcHookEvent> RpcHook = new SimpleHook<RpcHookEvent>();
        public static readonly Hook<ClientConnectHookEvent> ClientConnectHook = new SimpleHook<ClientConnectHookEvent>();
        public static readonly Hook<ReceiveVersionHookEvent> ReceiveVersionHook = new SimpleHook<ReceiveVersionHookEvent>();

        static NetworkHooks()
        {
            RpcMeta.AddSubscriber(meta => RpcHook.Propagate(new RpcHookEvent(meta)));
        }
    }

    public static class PlayerHooks
    {
        public static readonly Hook<PlayerHookEvent> PlayerJoinHook = new SimpleHook<PlayerHookEvent>();
        public static readonly Hook<PlayerHookEvent> PlayerDisconnectHook = new SimpleHook<PlayerHookEvent>();

        public static readonly Hook<PlayerMessageHookEvent> PlayerMessageHook = new NetworkedHook<PlayerMessageHookEvent>(4);
        public static readonly Hook<PlayerActionHookEvent> PlayerActionHook = new SimpleHook<PlayerActionHookEvent>();
        public static readonly Hook<PlayerTaskHookEvent> PlayerTaskCompleteHook = new NetworkedHook<PlayerTaskHookEvent>(6);

        public static readonly Hook<PlayerMurderHookEvent> PlayerMurderHook = new NetworkedHook<PlayerMurderHookEvent>(7);
        public static readonly Hook<PlayerDeathHookEvent> PlayerDeathHook = new NetworkedHook<PlayerDeathHookEvent>(8);
        public static readonly Hook<PlayerShapeshiftHookEvent> PlayerShapeshiftHook = new NetworkedHook<PlayerShapeshiftHookEvent>(9);
        public static readonly Hook<PlayerPhantomHookEvent> PlayerPhantomHook =  new NetworkedHook<PlayerPhantomHookEvent>(10);
        public static readonly Hook<PlayerHookEvent> PlayerRevivedHook = new NetworkedHook<PlayerHookEvent>(11);

        public static readonly Hook<PlayerHookEvent> PlayerExiledHook = new NetworkedHook<PlayerHookEvent>(12);
        public static readonly Hook<PlayerTeleportedHookEvent> PlayerTeleportedHook = new NetworkedHook<PlayerTeleportedHookEvent>(13);
    }

    public static class ModHooks
    {
        public static readonly Hook<EmptyHookEvent> LotusInitializedHook = new SimpleHook<EmptyHookEvent>();
        public static readonly Hook<PlayerStatusReceivedHook> StatusReceivedHook = new SimpleHook<PlayerStatusReceivedHook>();
        public static readonly Hook<CustomCommandHookEvent> CustomCommandHook = new SimpleHook<CustomCommandHookEvent>();
    }

    public static class MeetingHooks
    {
        public static readonly Hook<MeetingHookEvent> MeetingCalledHook = new NetworkedHook<MeetingHookEvent>(14);
        public static readonly Hook<CastVoteHookEvent> CastVoteHook = new NetworkedHook<CastVoteHookEvent>(15);
        public static readonly Hook<ExiledHookEvent> ExiledHook = new NetworkedHook<ExiledHookEvent>(16);
    }

    public static class SabotageHooks
    {
        public static readonly Hook<SabotageHookEvent> SabotageCalledHook = new NetworkedHook<SabotageHookEvent>(17);
        public static readonly Hook<SabotageHookEvent> SabotagePartialFixHook = new NetworkedHook<SabotageHookEvent>(18);
        public static readonly Hook<SabotageFixHookEvent> SabotageFixedHook = new NetworkedHook<SabotageFixHookEvent>(19);
    }

    public static void UnbindAll(string hookKey)
    {
        typeof(Hooks).GetNestedTypes().AddItem(typeof(Hooks))
            .SelectMany(type => type.GetFields())
            .Select(f => f.GetValue(null))
            .OfType<Hook>()
            .ForEach(hook => hook.Unbind(hookKey));
    }
}