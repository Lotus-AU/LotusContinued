using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Stats;
using Lotus.Extensions;
using Lotus.GameModes;
using Lotus.GameModes.HideAndSeek.ETF;
using Lotus.GameModes.HideAndSeek.ETF.NetObjects;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.RPC.CustomObjects.Builtin;
using Lotus.Utilities;
using UnityEngine;
using VentLib;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Extra.HNS.ETF;

public class Warden: Seeker, IRoleUI
{
    private const float VentDistance = 1f;
    private const float MaxVentTime = 0.5f;

    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Warden));

    private static IAccumulativeStatistic<int> _playersKilled =
        Statistic<int>.CreateAccumulative(
            $"Options.GameMode.{nameof(GamemodeTranslations.EscapeTheFacility)}.PlayersIncinerated",
            () => GamemodeTranslations.EscapeTheFacility.PlayersIncinerated
        );
    public override List<Statistic> Statistics() => [_playersKilled];

    [NewOnSetup] private Dictionary<int, Remote<IndicatorComponent>> capturedArrows;

    private FixedUpdateLock fixedUpdateLock = new();

    private byte currentlyCarrying = byte.MaxValue;
    private int termsRemaining = -1;

    private Vent closestVent;
    private DateTime timeReachedVent;

    private bool isTooClose;
    private bool justCaptured;

    public override string GetRoleOutfitPath() => $"RoleOutfits/Extra/{EnglishRoleName}".ToLower();

    public RoleButton KillButton(IRoleButtonEditor button) => button
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Extra/warden_down.png", 130, true));

    protected override void PostSetup()
    {
        base.PostSetup();
        KillCooldown = 3f;
    }

    [UIComponent(UI.Text)]
    public string TerminalsLeftText() => termsRemaining == -1
        ? string.Empty
        : Color.white.Colorize(TextUtils.ApplySize(1.5f, termsRemaining > 1
            ? GamemodeTranslations.EscapeTheFacility.TerminalsRemainingText.Formatted(termsRemaining - 1)
            : PlayersLeftText()
        ));

    // [UIComponent(UI.Text)]
    public string WardenInfoText()
    {
        string message = string.Empty;

        if (currentlyCarrying != byte.MaxValue) message = GamemodeTranslations.EscapeTheFacility.WardenDrag;
        else if (justCaptured) message = GamemodeTranslations.EscapeTheFacility.WardenStunned;
        else if (isTooClose) message = GamemodeTranslations.EscapeTheFacility.WardenTooClose;

        return message != string.Empty ? $"{TextUtils.ApplySize(1.3f, message)}\n" : string.Empty;
        // return $"{TextUtils.ApplySize(1.3f, message)}\n";
    }

    [RoleAction(LotusActionType.RoundStart, Subclassing = false)]
    protected virtual void SetupOnStart()
    {
        LiveString liveString = new(() =>
        {
            string message = string.Empty;

            if (currentlyCarrying != byte.MaxValue) message = GamemodeTranslations.EscapeTheFacility.WardenDrag;
            else if (justCaptured) message = GamemodeTranslations.EscapeTheFacility.WardenStunned;
            else if (isTooClose) message = GamemodeTranslations.EscapeTheFacility.WardenTooClose;

            return message != string.Empty ? $"{TextUtils.ApplySize(1.3f, message)}\n" : string.Empty;
        });
        MyPlayer.NameModel().GetComponentHolder<NameHolder>()
            .Insert(0, new NameComponent(liveString, [GameState.Roaming], ViewMode.Additive, MyPlayer));

        if (MyPlayer.AmOwner && GeneralOptions.AdminOptions.HandicapHost && !ExtraGamemodeOptions.EscapeOptions.ForceWardenFlashlight)
        {
            Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId, new GameOptionOverride(Override.UseFlashlight, true));
            MyPlayer.AdjustLighting();
        }
    }

    [RoleAction(LotusActionType.Attack, Subclassing = false)]
    public override bool TryKill(PlayerControl target)
    {
        if (justCaptured) return false;
        Fugitive targetRole = target.PrimaryRole<Fugitive>()!;
        if (!targetRole.CanInteractWithWarden()) return DebugLog($"c1 {targetRole.CanInteractWithWarden()}");
        if (targetRole.IsDowned())
        {
            if (currentlyCarrying != target.PlayerId && !targetRole.CanGrab()) return DebugLog($"c2 {currentlyCarrying}|{target.PlayerId}|{targetRole.CanGrab()}");
            if (currentlyCarrying != byte.MaxValue)
                Utils.PlayerById(currentlyCarrying)
                    .IfPresent(p => p.PrimaryRole<Fugitive>()!.FollowPlayer(null));
            if (currentlyCarrying == target.PlayerId)
            {
                currentlyCarrying = byte.MaxValue;
                return false;
            }
            log.Debug("Started carrying player.");
            closestVent = null!;

            currentlyCarrying = target.PlayerId;
            targetRole.FollowPlayer(MyPlayer);
        }
        else
        {
            log.Debug("Downed player.");
            MyPlayer.RpcMark(target);
            targetRole.DownPlayer();
            justCaptured = true;
            var remote = Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId, new GameOptionOverride(Override.PlayerSpeedMod, 0f));
            SyncOptions();
            Async.Schedule(() =>
            {
                remote.Delete();
                justCaptured = false;
                SyncOptions();
            }, 1.5f);
        }
        return false;
    }

    [RoleAction(LotusActionType.FixedUpdate, Subclassing = false)]
    protected virtual void OnFixedUpdate()
    {
        if (currentlyCarrying == byte.MaxValue) return;
        if (!fixedUpdateLock.AcquireLock()) return;

        if (closestVent)
        {
            if ((DateTime.Now - timeReachedVent).TotalSeconds > MaxVentTime) TrapCarriedPlayer();
            else if (Vector2.Distance(MyPlayer.NetTransform.body.position, closestVent.transform.position + closestVent.Offset) >
                     VentDistance) closestVent = null!;
            return;
        }

        Vector2 myPos = MyPlayer.NetTransform.body.position;
        closestVent = ShipStatus.Instance.AllVents.FirstOrDefault(v => Vector2.Distance(myPos, v.transform.position + v.Offset) < VentDistance)!;
        if (closestVent) timeReachedVent = DateTime.Now;
    }

    public override void HandleDisconnect()
    {
        base.HandleDisconnect();
        Utils.PlayerById(currentlyCarrying)
            .IfPresent(target=>
            {
                Fugitive targetRole = target.PrimaryRole<Fugitive>()!;
                targetRole.FollowPlayer(null);
            });
        currentlyCarrying = byte.MaxValue;
    }

    // public functions

    public void AlertFailedTerminal(TerminalObject terminalObject)
    {
        float distance = (MyPlayer.NetTransform.body.position - terminalObject.Position).magnitude;
        if (distance <= 15) DoReactorFlash();
        LiveString liveString = new(() => RoleUtils.CalculateArrow(MyPlayer, terminalObject.Position, Color.red));
        var remote = MyPlayer.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(liveString, GameState.Roaming, viewers: MyPlayer));
        Async.Schedule(() => remote.Delete(), 30f);
    }

    public void AlertCapturedPlayer(Vent holder, bool rightNow)
    {
        if (!rightNow)
        {
            if (!capturedArrows.TryGetValue(holder.Id, out var arrowRemote)) return;
            arrowRemote.Delete();
            capturedArrows.Remove(holder.Id);
            return;
        }
        LiveString liveString = new(() => RoleUtils.CalculateArrow(MyPlayer, holder.transform.position, Palette.CrewmateBlue));
        capturedArrows.Add(holder.Id,
            MyPlayer.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(liveString, GameState.Roaming, viewers: MyPlayer)));
    }


    public void SetTermsRemaining(int newCount)
    {
        termsRemaining = newCount;
    }

    public void OnCarryDisconnect()
    {
        currentlyCarrying = byte.MaxValue;
    }

    public bool SetTooClose(bool enabled) => isTooClose = enabled;

    public void IncrementKilledPlayers() => _playersKilled.Increment(MyPlayer.UniquePlayerId());

    // private functions

    private void DoReactorFlash()
    {
        var reactorFlashRpc = Vents.FindRPC((uint)RoleRPC.ReactorFlash)!;
        if (!MyPlayer.IsModded()) RoleUtils.PlayReactorsForPlayer(MyPlayer);
        else if (MyPlayer.IsHost()) reactorFlashRpc.InvokeTrampoline(false);
        else reactorFlashRpc.Send([MyPlayer.OwnerId], false);
        Async.Schedule(() =>
        {
            if (!MyPlayer.IsModded()) RoleUtils.EndReactorsForPlayer(MyPlayer);
            else if (MyPlayer.IsHost()) reactorFlashRpc.InvokeTrampoline(true);
            else reactorFlashRpc.Send([MyPlayer.OwnerId], true);
        }, NetUtils.DeriveDelay(0.5f));
    }

    private void TrapCarriedPlayer()
    {
        var capturedObjects = ETFGameMode.Instance.CapturedObjects;
        Vent targetVent = closestVent;
        log.Debug($"Checking vent: {targetVent.Id}");
        if (capturedObjects.ContainsKey(targetVent.Id))
        {
            closestVent = null!;
            return;
        }
        closestVent = null!;
        log.Debug($"trapping player. Warden: {MyPlayer.name}");
        byte cache = currentlyCarrying;
        currentlyCarrying = byte.MaxValue;
        log.Debug("set carrying to max value");
        Utils.PlayerById(cache)
            .IfPresent(target =>
            {
                Fugitive targetRole = target.PrimaryRole<Fugitive>()!;
                targetRole.FollowPlayer(null);
                targetRole.PutIntoVent(targetVent);
                log.Debug($"adding player to vent: {targetVent.Id}");
                capturedObjects.Add(targetVent.Id, new CapturedObject(targetRole, this, targetVent));
            });
    }

    private string PlayersLeftText()
    {
        var allCrew = Players.GetAllRoles().Where(r => r is Fugitive).OfType<Fugitive>().ToList();
        return GamemodeTranslations.EscapeTheFacility.PlayersLeftText.Formatted(allCrew.Count(p => !(p.Died() || p.Escaped())), allCrew.Count);
    }

    private bool DebugLog(string message)
    {
        log.Debug(message);
        return false;
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => base
        .RegisterOptions(optionStream)
        .Tab(GetType() != typeof(Warden) ? DefaultTabs.WardenTab : null!);

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .OptionOverride(Override.EscapeTime, ExtraGamemodeOptions.EscapeOptions.GameLength)
        .OptionOverride(Override.FinalEscapeTime, 0f)
        .OptionOverride(Override.SeekerFinalMap, false)
        // .OptionOverride(Override.SeekerPings, false)
        .OptionOverride(Override.MaxPingTime, 0f)
        .OptionOverride(Override.UseFlashlight, true, () => ExtraGamemodeOptions.EscapeOptions.ForceWardenFlashlight)
        .OptionOverride(new MultiplicativeOverride(Override.PlayerSpeedMod, ExtraGamemodeOptions.EscapeOptions.WardenSpeedMultiplier))
        .RoleFlags(GetType() == typeof(Warden) ? RoleFlag.Hidden | RoleFlag.DontRegisterOptions : RoleFlags);

    [Localized(nameof(Warden))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))]
        public static string ButtonText = "Down";
    }
}