using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Vanilla.Meetings;
using Lotus.Chat;
using Lotus.RPC;
using Lotus.Utilities;
using Lotus.Victory;
using Lotus.Extensions;
using Lotus.Logging;
using UnityEngine;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using AmongUs.GameOptions;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Lotus.Managers.Blackscreen.Interfaces;
using Lotus.Victory.Patches;
using Sentry.Internal.Extensions;

namespace Lotus.Managers.Blackscreen;

internal class BlackscreenResolver : IBlackscreenResolver
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(BlackscreenResolver));

    private Dictionary<byte, (bool isDead, bool isDisconnected)> playerStates = null!;
    private MeetingDelegate meetingDelegate;
    private HashSet<byte> unpatchable;

    private bool _patching;

    internal BlackscreenResolver(MeetingDelegate meetingDelegate)
    {
        this.meetingDelegate = meetingDelegate;

        Hooks.PlayerHooks.PlayerMessageHook.Bind(nameof(BlackscreenResolver), _ => BlockLavaChat(), true);
        Hooks.GameStateHooks.GameEndHook.Bind(nameof(BlackscreenResolver), _ => _patching = false, true);
    }

    public virtual bool Patching() => _patching;

    public virtual void OnMeetingDestroy()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        _patching = true;

        playerStates = new();
        unpatchable = new();

        // Store and save the data of each player.
        byte exiledPlayer = meetingDelegate.ExiledPlayer?.PlayerId ?? byte.MaxValue;
        NetworkedPlayerInfo[] playerInfos = GameData.Instance.AllPlayers.ToArray().Where(p => p != null).ToArray();
        playerInfos.FirstOrOptional(p => p.PlayerId == exiledPlayer).IfPresent(info => info.IsDead = true);

        playerStates = playerInfos.ToDict(i => i.PlayerId, i => (i.IsDead, i.Disconnected));

        // Update and send new data for each player.
        foreach (PlayerControl player in Players.GetPlayers())
        {
            if (player == null) continue;
            if (playerStates[player.PlayerId].isDisconnected) continue;
            if (player.IsHost() || player.IsModded()) continue;
            int clientId = player.GetClientId();
            if (clientId == -1) continue;
            Dictionary<byte, (bool isDead, bool isDisconnected)> myStates;
            try
            {
                myStates = AntiBlackoutLogic.PatchedData(player, exiledPlayer, playerStates.Clone());
            }
            catch (Exception ex)
            {
                myStates = null!;
                if (ex.Message == "unpatchable.") unpatchable.Add(player.PlayerId);
                else log.Exception(ex);
            }
            if (myStates == null) continue;

            GameData.Instance.AllPlayers.ToArray().Where(i => i != null).ForEach(info =>
            {
                if (!myStates.TryGetValue(info.PlayerId, out var val)) return;
                info.IsDead = val.isDead;
                info.Disconnected = val.isDisconnected;
                try
                {
                    if (info.Object != null) info.PlayerName = info.Object.name;
                }
                catch { }
            });
            GeneralRPC.SendGameData(clientId);
        }
        GameData.Instance.AllPlayers.ToArray().Where(i => i != null).ForEach(info =>
        {
            if (!playerStates.TryGetValue(info.PlayerId, out var val)) return;
            info.IsDead = val.isDead;
            info.Disconnected = val.isDisconnected;
            try
            {
                if (info.Object != null) info.PlayerName = info.Object.name;
            }
            catch { }
        });
        if (unpatchable.Count == 0) return;

        log.Debug($"Un-patchable Players: {unpatchable.Filter(Utils.PlayerById).Select(p => p.name).Fuse()}");
    }

    public virtual void FixBlackscreens(Action runOnFinish)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        log.Debug($"Trying to patch Unpatchable Players: {unpatchable.Filter(Utils.PlayerById).Select(p => p.name).Fuse()}");
        unpatchable.Filter(Utils.PlayerById).Do(p =>
        {
            if (!LegacyResolver.PerformForcedReset(p)) log.Debug($"Could not patch {p.name}.");
        });
        Async.Schedule(() =>
        {
            _patching = false;
            GeneralRPC.SendGameData();
            CheckEndGamePatch.Deferred = false;
            runOnFinish();
        }, 1f);
    }

    private void BlockLavaChat()
    {
        if (!_patching) return;
        if (Game.State is GameState.InLobby)
        {
            _patching = false;
            return;
        }
        ChatHandler chatHandler = ChatHandler.Of("", "- Lava Chat Fix -");
        foreach (PlayerControl player in Players.GetPlayers())
        {
            (bool isDead, bool isDisconnected) = playerStates[player.PlayerId];
            if (isDead || isDisconnected) continue;
            for (int i = 0; i < 20; i++) chatHandler.Send(player);
        }
    }
}