using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.RPC;
using VentLib;
using VentLib.Networking;
using VentLib.Networking.Interfaces;
using VentLib.Networking.RPC;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.API.Reactive;

public class NetworkedHook<T> : OrderedDictionary<string, HookAction<T>>, Hook<T> where T: INetworkedHookEvent, IRpcWritable
{
    private static ModRPC? hookEventRpc = Vents.FindRPC((uint)ModCalls.HookEvent);

    internal uint HookId;

    public NetworkedHook(uint hookId)
    {
        HookId = hookId;
        Hooks.Events[hookId] = hookEvent => this.Propagate((T)hookEvent);
    }

    public bool Exists(string key) => base.ContainsKey(key);

    public bool TryGet(string key, out Action<T>? eventConsumer)
    {
        eventConsumer = null;
        if (!Exists(key)) return false;
        eventConsumer = base[key].Action;
        return true;
    }

    public bool TryGet(string key, out HookAction<T>? eventConsumer)
    {
        eventConsumer = null;
        if (!Exists(key)) return false;
        eventConsumer = base[key];
        return true;
    }

    public Hook<T> Bind(string key, Action<T> eventConsumer, bool replace = false, Priority priority = Priority.Normal)
    {
        HookAction<T> hookAction = new() { Action = eventConsumer, Priority = priority };
        if (replace) base[key] = hookAction;
        else base.Add(key, hookAction);
        return this;
    }

    public bool Unbind(string key)
    {
        if (!Exists(key)) return false;
        base.Remove(key);
        return true;
    }

    public void Propagate(T hookEvent)
    {
        if (Game.State is not GameState.None && PlayerControl.LocalPlayer)
        {
            if (PlayerControl.LocalPlayer.IsHost()) hookEventRpc?.Send(null, HookId, hookEvent);
            else StaticLogger.Warn("Calling networked hook while not host. Cannot send hook to other clients.");
        }
        base.GetValues().OrderBy(o => o.Priority).ForEach(ha => ha.Action.Invoke(hookEvent));
    }
}