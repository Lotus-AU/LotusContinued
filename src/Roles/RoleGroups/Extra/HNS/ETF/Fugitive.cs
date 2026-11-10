using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Stats;
using Lotus.Extensions;
using Lotus.Factions;
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
using Lotus.RPC;
using Lotus.RPC.CustomObjects.Builtin;
using Lotus.Utilities;
using UnityEngine;
using VentLib;
using VentLib.Localization.Attributes;
using VentLib.Networking.RPC;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Extra.HNS.ETF;

public class Fugitive: Hider, IRoleUI
{
    private const float UseDistance = 1.5f;
    private const int ExitOpenTime = 12;

    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Fugitive));
    private static IAccumulativeStatistic<int> _terminalsCompleted =
        Statistic<int>.CreateAccumulative(
            $"Options.GameMode.{nameof(GamemodeTranslations.EscapeTheFacility)}.TerminalsCompleted",
            () => GamemodeTranslations.EscapeTheFacility.TerminalsCompleted
            );
    private static IAccumulativeStatistic<int> _playersRescued =
        Statistic<int>.CreateAccumulative(
            $"Options.GameMode.{nameof(GamemodeTranslations.EscapeTheFacility)}.PlayersRescued",
            () => GamemodeTranslations.EscapeTheFacility.PlayersRescued
        );
    public override List<Statistic> Statistics() => [_terminalsCompleted, _playersRescued];

    [NewOnSetup] private Dictionary<int, Remote<IndicatorComponent>> capturedArrows;

    private FixedUpdateLock fixedUpdateLock = new();

    private FloatingText? currentlyInteracting;

    private DateTime exitStart = DateTime.Now;
    private PlayerControl? followTarget;
    private Cooldown downedTimer;

    private bool isTerminal;
    private bool isPhantom;
    private bool isInvisible;

    private bool isDowned;
    private bool isInVent;
    private float timeLeft;

    private bool didDie;
    private bool didEscape;

    private bool canUsePet;

    private bool skipExitEvent;

    private int termsRemaining = -1;

    private bool showFailedTerminalInfo;
    private string defaultPet;

    public override string GetRoleOutfitPath() => $"RoleOutfits/Extra/{EnglishRoleName}".ToLower();

    public RoleButton AbilityButton(IRoleButtonEditor button) => button
        .SetText(Translations.VentButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Extra/fugitive_rescue.png", 130, true));

    public RoleButton PetButton(IRoleButtonEditor button) => button
        .SetText(Translations.HackButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Extra/fugitive_hack.png", 130, true));

    protected override void PostSetup()
    {
        base.PostSetup();
        HasOverridenTasks = true;
        HasCommonTasks = false;
        LongTasks = 0;
        ShortTasks = 0;
    }

    [UIComponent(UI.Text)]
    public string TerminalsLeftText() => Color.white.Colorize(TextUtils.ApplySize(1.5f, termsRemaining > 1
        ? GamemodeTranslations.EscapeTheFacility.TerminalsRemainingText.Formatted(termsRemaining - 1)
        : ((didEscape || didDie) ? PlayersLeftText() : GamemodeTranslations.EscapeTheFacility.FindExitText)));

    // [UIComponent(UI.Name)]
    public string FugitiveInfoText()
    {
        string statusText = GetStatusText();
        if (statusText != string.Empty) statusText = $"{TextUtils.ApplySize(1.3f, statusText)}\n";

        if (isDowned)
            return statusText + $"<cspace=-0.15em>{RoleUtils.HealthBar((int)downedTimer.TimeRemaining(),
                (int)downedTimer.Duration, Color.white)}</cspace>\n";
        if (currentlyInteracting == null) return string.Empty;

        if (!isTerminal)
            return statusText + $"<cspace=-0.15em>{RoleUtils.HealthBar((int)(DateTime.Now - exitStart).TotalSeconds,
                ExitOpenTime, Color.white)}</cspace>\n";

        TerminalObject terminalObject = (TerminalObject)currentlyInteracting;
        return statusText + $"<cspace=-0.15em>{RoleUtils.HealthBar((int)terminalObject.ComputerProgress,
            (int)ExtraGamemodeOptions.EscapeOptions.TerminalHackTime, Color.white)}</cspace>\n";
    }

    public override bool TasksApplyToTotal() => false;

    [RoleAction(LotusActionType.RoundStart)]
    private void SetupOnStart()
    {
        timeLeft = ExtraGamemodeOptions.EscapeOptions.CapturedDuration;
        downedTimer.SetDuration(ExtraGamemodeOptions.EscapeOptions.DownedDuration);

        Async.Schedule(() =>
        {
            defaultPet = MyPlayer.HasPet() ? MyPlayer.Data.DefaultOutfit.PetId : ModConstants.Pets.Values.ToList().GetRandom();
            MyPlayer.RpcSetPet(defaultPet);
        }, NetUtils.DeriveDelay(0.2f));

        LiveString liveString = new(() =>
        {
            string statusText = GetStatusText();
            if (statusText != string.Empty) statusText = $"{TextUtils.ApplySize(1.3f, statusText)}\n";

            if (isDowned)
                return statusText + $"<cspace=-0.15em>{RoleUtils.HealthBar((int)downedTimer.TimeRemaining(),
                    (int)downedTimer.Duration, Color.white)}</cspace>\n";
            if (currentlyInteracting == null) return statusText;

            if (!isTerminal)
                return statusText + $"<cspace=-0.15em>{RoleUtils.HealthBar((int)(DateTime.Now - exitStart).TotalSeconds,
                    ExitOpenTime, Color.white)}</cspace>\n";

            TerminalObject terminalObject = (TerminalObject)currentlyInteracting;
            return statusText + $"<cspace=-0.15em>{RoleUtils.HealthBar((int)terminalObject.ComputerProgress,
                (int)ExtraGamemodeOptions.EscapeOptions.TerminalHackTime, Color.white)}</cspace>\n";
        });
        MyPlayer.NameModel().GetComponentHolder<NameHolder>()
            .Insert(0, new NameComponent(liveString, [GameState.Roaming], ViewMode.Additive, MyPlayer));

        Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId, new GameOptionOverride(Override.ImpostorLightMod,
            () => GetOverride(Override.CrewLightMod)?.GetValue() as float? ?? AUSettings.CrewLightMod()));
        SyncOptions();
    }

    [RoleAction(LotusActionType.OnPet)]
    private void Interact()
    {
        if (currentlyInteracting != null)
        {
            StopInteracting();
            return;
        }

        if (isDowned || isInVent || didDie || didEscape) return;

        var nearestTerminal =
            ETFGameMode.Instance.TerminalObjects.FirstOrDefault(kvp =>
            {
                Vector2 location = kvp.Key;
                TerminalObject terminalObject = kvp.Value;

                if (terminalObject == null!)
                    return false;

                // if computer is finished.
                if (terminalObject.ComputerProgress >= ExtraGamemodeOptions.EscapeOptions.TerminalHackTime)
                    return false;

                // check if anything between
                if (PhysicsHelpers.AnythingBetween(location, MyPlayer.NetTransform.body.position,
                        Constants.ShipOnlyMask, false)) return false;

                float distance = Vector2.Distance(MyPlayer.NetTransform.body.position, location);
                return distance <= UseDistance;
            });

        if (nearestTerminal.Value != null)
        {
            TerminalObject terminalObject = nearestTerminal.Value;
            if (terminalObject.TryStartHacking(MyPlayer.PlayerId)) // check if we are allowed to hack this term
            {
                isTerminal = true;
                currentlyInteracting = terminalObject;
                MyPlayer.NetTransform.RpcSnapTo(nearestTerminal.Key);
                return;
            }
        }

        var nearestExit =
            ETFGameMode.Instance.ExitObjects.FirstOrDefault(kvp =>
            {
                Vector2 location = kvp.Key;
                ExitObject exitObject = kvp.Value;

                // if the exit is open
                if (exitObject.CanEscape)
                    return false;

                // check if anything between
                if (PhysicsHelpers.AnythingBetween(location, MyPlayer.NetTransform.body.position,
                        Constants.ShipOnlyMask, false)) return false;

                float distance = Vector2.Distance(MyPlayer.NetTransform.body.position, location);
                return distance <= UseDistance;
            });

        if (nearestExit.Value == null) return;
        {
            ExitObject exitObject = nearestExit.Value;
            if (!exitObject.TryOpenExit(MyPlayer.PlayerId)) return;
            BeginOpeningExit(exitObject);
        }
    }

    [RoleAction(LotusActionType.VentEntered)]
    private void SavePlayer(ActionHandle handle, Vent vent)
    {
        // isInvisible = false;
        if (didDie || didEscape) return;
        if (isInVent) return;
        handle.Cancel();
        // if (isInvisible)
        //     Async.Schedule(() => RpcV3.Immediate(MyPlayer.MyPhysics.NetId, RpcCalls.BootFromVent).WritePacked(vent.Id).SendInclusive(GetInVisiblePlayers()),
        //         NetUtils.DeriveDelay(0.4f));

        if (currentlyInteracting != null) return;
        log.Debug($"Checking vent: {vent.Id}");
        if (!ETFGameMode.Instance.CapturedObjects.TryGetValue(vent.Id, out CapturedObject? value)) return;
        log.Debug("Checking if time is up.");
        if (value.TimeRemaining <= 0) return;
        value.Rescue();
        _playersRescued.Increment(MyPlayer.UniquePlayerId());
    }

    [RoleAction(LotusActionType.VentExit)]
    private void TryLeaveCaptivity(ActionHandle handle)
    {
        if (didDie || didEscape) return;
        if (isInVent) handle.Cancel();
    }

    [RoleAction(LotusActionType.Vanish)]
    private void DoSkillCheck(ActionHandle handle)
    {
        handle.Cancel();
        if (isPhantom)
        {
            isPhantom = false;
            Async.Schedule(() => MyPlayer.RpcSetRoleDesync(RoleTypes.Engineer, MyPlayer), 0.2f);
            Async.Schedule(() => UpdateButtonImage(false), NetUtils.DeriveDelay(0.3f));
        }

        if (currentlyInteracting != null && isTerminal)
            ((TerminalObject)currentlyInteracting).HitSkillcheck(MyPlayer.PlayerId);
    }

    [RoleAction(LotusActionType.FixedUpdate)]
    private void OnFixedUpdate()
    {
        if (!fixedUpdateLock.AcquireLock()) return;
        if (isInVent || didDie || didEscape) return;

        if (followTarget || currentlyInteracting != null) canUsePet = false;
        else
        // copy and paste go brrrrr
            canUsePet = (
                termsRemaining is >= 0 and <= 1 && ETFGameMode.Instance.ExitObjects.Any(kvp =>
                {
                    Vector2 location = kvp.Key;
                    ExitObject exitObject = kvp.Value;

                    // if the exit is open
                    if (exitObject.CanEscape)
                        return false;

                    // check if anything between
                    if (PhysicsHelpers.AnythingBetween(location, MyPlayer.NetTransform.body.position,
                            Constants.ShipOnlyMask, false)) return false;

                    float distance = Vector2.Distance(MyPlayer.NetTransform.body.position, location);
                    return distance <= UseDistance;
                })) || (
                ETFGameMode.Instance.TerminalObjects.Any(kvp =>
                {
                    Vector2 location = kvp.Key;
                    TerminalObject terminalObject = kvp.Value;

                    if (terminalObject == null!)
                        return false;

                    // if computer is finished.
                    if (terminalObject.ComputerProgress >= ExtraGamemodeOptions.EscapeOptions.TerminalHackTime)
                        return false;

                    // check if anything between
                    if (PhysicsHelpers.AnythingBetween(location, MyPlayer.NetTransform.body.position,
                            Constants.ShipOnlyMask, false)) return false;

                    float distance = Vector2.Distance(MyPlayer.NetTransform.body.position, location);
                    return distance <= UseDistance;
                })
            );

        if (currentlyInteracting != null)
        {
            float distance = Vector2.Distance(MyPlayer.NetTransform.body.position, currentlyInteracting.Position);
            if (distance > UseDistance) StopInteracting(); // stop interacting when we get out of range.
            else if (!isTerminal && (DateTime.Now - exitStart).TotalSeconds > ExitOpenTime)
            {
                ((ExitObject)currentlyInteracting).OpenExit();
                StopInteracting();
            } else if (isTerminal && ((TerminalObject)currentlyInteracting).ComputerProgress >=
                       ExtraGamemodeOptions.EscapeOptions.TerminalHackTime)
            {
                StopInteracting();
                _terminalsCompleted.Increment(MyPlayer.UniquePlayerId());
            }
        }
        else if (termsRemaining is >= 0 and <= 1 && !followTarget)
        {
            if (ETFGameMode.Instance.ExitObjects.Any(kvp =>
                {
                    Vector2 location = kvp.Key;
                    ExitObject exitObject = kvp.Value;

                    if (!exitObject.CanEscape) return false;

                    if (PhysicsHelpers.AnythingBetween(location, MyPlayer.NetTransform.body.position,
                            Constants.ShipOnlyMask, false)) return false;

                    float distance = Vector2.Distance(MyPlayer.NetTransform.body.position, location);
                    return distance <= 1f;
                })) Escape();
        }

        if (!isDowned) return;

        if (followTarget && Vector2.Distance(MyPlayer.NetTransform.body.position, followTarget!.NetTransform.body.position) > 0.1f)
            Utils.Teleport(MyPlayer.NetTransform, followTarget!.NetTransform.body.position, this);

        if (downedTimer.IsReady()) CheckIfCanGetUp();
    }

    [RoleAction(LotusActionType.Zipline)]
    private void TryUsable(ActionHandle handle)
    {
        if (isDowned || isInvisible || isInVent) handle.Cancel();
    }

    public override void HandleDisconnect()
    {
        if (currentlyInteracting != null)
        {
            if (isTerminal) ((TerminalObject)currentlyInteracting).HandlePlayerDisconnected(MyPlayer.PlayerId);
            else ((ExitObject)currentlyInteracting).CancelOpen(MyPlayer.PlayerId);
        }
        if (isDowned && followTarget) followTarget!.PrimaryRole<Warden>()!.OnCarryDisconnect();
        if (isInVent)
            ETFGameMode.Instance.CapturedObjects.Values.FirstOrOptional(o => o.Captured == this)
                .IfPresent(o => o.OnDisconnect());

        base.HandleDisconnect();
    }

    // public functions
    public float GetCapturedTimeLeft() => timeLeft;
    public bool IsDowned() => isDowned;
    public bool CanInteractWithWarden() => !(didDie || didEscape || isInvisible);
    public bool CanGrab() => !followTarget;
    public bool Captured() => isInVent;
    public bool Escaped() => didEscape;
    public bool Died() => didDie;

    public void PropagateSkillCheck()
    {
        isPhantom = true;
        MyPlayer.RpcSetRoleDesync(RoleTypes.Phantom, MyPlayer);
        Async.Schedule(() => UpdateButtonImage(true), NetUtils.DeriveDelay(0.2f));
    }

    public void EndSkillCheck(bool isSuccessful)
    {
        if (isSuccessful) return;
        StopInteracting();
        showFailedTerminalInfo = true;
        Async.Schedule(() => showFailedTerminalInfo = false, 15f);
    }

    public void FollowPlayer(PlayerControl? target)
    {
        followTarget = target;
        SyncOptions();
    }

    public void FreezeToDeath(Warden capturer)
    {
        didDie = true;
        capturer.MyPlayer.InteractWith(MyPlayer, new LotusInteraction(new FatalIntent(true, () =>
            new CustomDeathEvent(MyPlayer, capturer.MyPlayer, ModConstants.DeathNames.Incinerated)), capturer));

        log.Debug($"{MyPlayer.name} froze to death. {didDie}");
        Async.Schedule(HideForOtherPlayers, 1.2f);
    }

    public void AlertFailedTerminal(TerminalObject terminalObject)
    {
        float distance = Vector2.Distance(MyPlayer.NetTransform.body.position, terminalObject.Position);
        if (distance <= 10) DoReactorFlash();
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

    public void DownPlayer()
    {
        if (isDowned) return;
        isDowned = true;
        StopInteracting();
        MyPlayer.RpcMark(MyPlayer);
        MyPlayer.RpcSetPet("pet_EmptyPet");
        MyPlayer.RpcSetRoleDesync(RoleTypes.Crewmate, MyPlayer);
        followTarget = null;
        downedTimer.Start();
        SyncOptions();
    }

    private void CheckIfCanGetUp()
    {
        if (isInVent) return;
        if (followTarget && (followTarget!.onLadder || followTarget!.inMovingPlat)) return;
        MyPlayer.RpcSetPet(defaultPet);
        MyPlayer.RpcSetRoleDesync(RoleTypes.Engineer, MyPlayer);
        if (followTarget) followTarget!.PrimaryRole<Warden>()?.OnCarryDisconnect();
        followTarget = null;
        isDowned = false;
        SyncOptions();
    }

    public void PutIntoVent(Vent target)
    {
        followTarget = null;
        isDowned = false;
        isInVent = true;
        downedTimer.Finish(true);
        SyncOptions();
        MyPlayer.NetTransform.RpcSnapTo(target.transform.position + target.Offset);
        Async.Schedule(() => MyPlayer.MyPhysics.RpcEnterVent(target.Id), NetUtils.DeriveDelay(0.2f));
    }

    public void Rescue(Vent holder, float newTimeLeft)
    {
        timeLeft = newTimeLeft;
        isInVent = false;
        isDowned = false;
        SyncOptions();
        MyPlayer.RpcSetPet(defaultPet);
        log.Debug($"{MyPlayer.name} was rescued.");
        MyPlayer.RpcSetRoleDesync(RoleTypes.Engineer, MyPlayer);

        float duration = ExtraGamemodeOptions.EscapeOptions.RescuedPlayerInvisDuration;
        log.Debug($"Rescue Duration: {duration}");
        if (duration <= 0)
        {
            MyPlayer.MyPhysics.RpcBootFromVent(holder.Id);
            return;
        }

        isInvisible = true;

        // if (MyPlayer.AmOwner) MyPlayer.MyPhysics.BootFromVent(holder.Id);
        // log.Debug("who can see me?");

        MyPlayer.MyPhysics.RpcBootFromVent(holder.Id);
        Async.Schedule(() =>
        {
            int[] invisiblePlayers = GetInVisiblePlayers();
            bool beInvis = false;
            if (invisiblePlayers.Contains(PlayerControl.LocalPlayer.OwnerId))
            {
                beInvis = true;
                invisiblePlayers = invisiblePlayers.Without(PlayerControl.LocalPlayer.OwnerId).ToArray();
            }
            RpcV3.Mass()
                .Start(MyPlayer.NetTransform.NetId, RpcCalls.SnapTo).Write(new Vector2(-9999, -9999)).Write((ushort)(MyPlayer.NetTransform.lastSequenceId + 32)).End()
                .Start(MyPlayer.NetId, RpcCalls.MurderPlayer).Write(MyPlayer).Write((int)MurderResultFlags.Succeeded).End()
                .SendInclusive(invisiblePlayers);
            MyPlayer.NetTransform.lastSequenceId += 8;
            Async.Schedule(() =>
            {
                if (!isInvisible) return;
                if (Game.State is not GameState.Roaming) return;
                isInvisible = false;

                invisiblePlayers = GetInVisiblePlayers();
                bool reappear = false;
                if (invisiblePlayers.Contains(PlayerControl.LocalPlayer.OwnerId))
                {
                    reappear = true;
                    invisiblePlayers = invisiblePlayers.Without(PlayerControl.LocalPlayer.OwnerId).ToArray();
                }
                RpcV3.Immediate(MyPlayer.NetId, RpcCalls.SetRole)
                    .Write((ushort)(didEscape ? RoleTypes.Crewmate :  RoleTypes.Engineer))
                    .Write(ProjectLotus.AdvancedRoleAssignment)
                    .SendInclusive(invisiblePlayers);
                if (!reappear) return;
                MyPlayer.cosmetics.currentBodySprite.BodySprite.color = Color.white;
                MyPlayer.cosmetics.colorBlindText.color = Color.white;
                MyPlayer.cosmetics.nameText.color = Color.white;
                ReverseEngineeredRPC.RpcChangeSkin(MyPlayer, MyPlayer.Data.DefaultOutfit, sendToClients:false);
                MyPlayer.SetHatAndVisorAlpha(1f);
                MyPlayer.cosmetics.skin.layer.color = MyPlayer.cosmetics.skin.layer.color.SetAlpha(1);
                foreach (var rend in MyPlayer.cosmetics.currentPet.renderers)
                    rend.color = rend.color.SetAlpha(1f);
                foreach (var shadow in MyPlayer.cosmetics.currentPet.shadows)
                    shadow.color = shadow.color.SetAlpha(1f);
            }, NetUtils.DeriveDelay(duration));
            if (!beInvis) return;
            MyPlayer.cosmetics.currentBodySprite.BodySprite.color = Color.clear;
            MyPlayer.cosmetics.colorBlindText.color = Color.clear;
            MyPlayer.cosmetics.nameText.color = Color.clear;
            int colorId = MyPlayer.CurrentOutfit.ColorId;
            MyPlayer.RawSetHat("", colorId);
            MyPlayer.RawSetSkin("",colorId);
            MyPlayer.RawSetPet("", colorId);
            MyPlayer.RawSetVisor("", colorId);
        }, NetUtils.DeriveDelay(0.4f));
        // RpcV3.Immediate(MyPlayer.MyPhysics.NetId, RpcCalls.BootFromVent).WritePacked(holder.Id).SendInclusive(GetVisiblePlayers());
    }

    // private functions
    private string GetStatusText()
    {
        if (didDie) return GamemodeTranslations.EscapeTheFacility.FugitiveDied;
        if (didEscape) return GamemodeTranslations.EscapeTheFacility.FugitiveEscaped;

        if (isDowned) return GamemodeTranslations.EscapeTheFacility.FugitiveDowned;
        if (isPhantom) return GamemodeTranslations.EscapeTheFacility.FugitiveVanish;
        if (showFailedTerminalInfo) return GamemodeTranslations.EscapeTheFacility.TerminalFailed;

        if (isInvisible) return GamemodeTranslations.EscapeTheFacility.FugitiveInvisible;
        if (canUsePet) return GamemodeTranslations.EscapeTheFacility.FugitiveInteractHint;

        return string.Empty;
    }

    private int[] GetVisiblePlayers() => Players.GetAllPlayers()
        .Where(p => !p.IsAlive() || p.PrimaryRole() is Fugitive || p.PlayerId == MyPlayer.PlayerId)
        .Select(p => p.OwnerId)
        .ToArray();

    private int[] GetInVisiblePlayers() => Players.GetAllPlayers()
        .Where(p => p.PrimaryRole() is Warden)
        .Select(p => p.OwnerId)
        .ToArray();

    private void BeginOpeningExit(ExitObject exitObject)
    {
        exitStart = DateTime.Now;
        isTerminal = false;
        currentlyInteracting = exitObject;
    }

    private void StopInteracting()
    {
        if (currentlyInteracting == null) return;
        if (isTerminal) StopHackingTerminal((TerminalObject)currentlyInteracting);
        else
        {
            ExitObject exitObject = (ExitObject)currentlyInteracting;
            if (exitObject.CancelOpen(MyPlayer.PlayerId)) currentlyInteracting = null;
        }
    }

    private string PlayersLeftText()
    {
        var allCrew = Players.GetAllRoles().Where(r => r is Fugitive).OfType<Fugitive>().ToList();
        return GamemodeTranslations.EscapeTheFacility.PlayersLeftText.Formatted(allCrew.Count(p => !(p.Died() || p.Escaped())), allCrew.Count);
    }

    private void StopHackingTerminal(TerminalObject terminalObject)
    {
        if (!terminalObject.TryStopHacking(MyPlayer.PlayerId)) return;
        currentlyInteracting = null;
        if (!isPhantom) return;
        isPhantom = false;
        MyPlayer.RpcSetRoleDesync(RoleTypes.Engineer, MyPlayer);
        Async.Schedule(() => UpdateButtonImage(false), NetUtils.DeriveDelay(0.2f));
    }

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

    private void Escape()
    {
        isDowned = false;
        isInVent = false;
        didEscape = true;
        log.Debug($"{MyPlayer.name} escaped.");

        HideForOtherPlayers();
    }

    private void HideForOtherPlayers()
    {
        canUsePet = false;
        MyPlayer.RpcSetPet("pet_EmptyPet");
        MyPlayer.RpcSetRole(RoleTypes.Crewmate, true);
        Vent targetVent = ShipStatus.Instance.AllVents.First();
        Vector2 currentPosition = MyPlayer.GetTruePosition();

        Utils.Teleport(MyPlayer.NetTransform, targetVent.transform.position + targetVent.Offset, this);
        MyPlayer.MyPhysics.RpcEnterVent(targetVent.Id);
        Async.Schedule(() =>
        {
            if (MyPlayer.AmOwner) MyPlayer.MyPhysics.BootFromVent(targetVent.Id);
            else RpcV3.Immediate(MyPlayer.MyPhysics.NetId, RpcCalls.BootFromVent).WritePacked(targetVent.Id).Send(MyPlayer.OwnerId);
            Async.Schedule(() => Utils.Teleport(MyPlayer.NetTransform, currentPosition, this), NetUtils.DeriveDelay(0.7f));
        }, NetUtils.DeriveDelay(0.4f));

        Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId, new GameOptionOverride(Override.CrewLightMod, 10f));
        Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId, new GameOptionOverride(Override.PlayerSpeedMod, AUSettings.PlayerSpeedMod() * 4.5f / 3f));
        if (AUSettings.UseFlashlight())
            Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId, new GameOptionOverride(Override.UseFlashlight,
                () => false));
        SyncOptions();
    }

    private void UpdateButtonImage(bool showSkillcheck)
    {
        if (!MyPlayer.AmOwner)
        {
            if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateFugitive)?.Send([MyPlayer.OwnerId], showSkillcheck);
            return;
        }
        UIManager.AbilityButton
            .SetText(showSkillcheck ? Translations.SkillCheckText : Translations.VentButtonText)
            .SetSprite(() => LotusAssets.LoadSprite(showSkillcheck ? "Buttons/Extra/fugitive_skillcheck.png" : "Buttons/Extra/fugitive_rescue.png", 130, true));
    }

    [ModRPC(ModCalls.UpdateFugitive, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateFugitive(bool showSkillcheck)
    {
        Fugitive? fugitive = PlayerControl.LocalPlayer.PrimaryRole<Fugitive>();
        if (fugitive == null) return;
        fugitive.UpdateButtonImage(showSkillcheck);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleAbilityFlags(RoleAbilityFlag.UsesPet)
        .OptionOverride(Override.CrewmateVentUses, 999)
        .OptionOverride(Override.CrewmateTimeInVent, 999f)
        .OptionOverride(Override.EscapeTime, ExtraGamemodeOptions.EscapeOptions.GameLength)
        .OptionOverride(Override.FinalEscapeTime, 0f)
        .OptionOverride(new MultiplicativeOverride(Override.PlayerSpeedMod, ExtraGamemodeOptions.EscapeOptions.DownedSpeedMultiplier, () => isDowned))
        .OptionOverride(Override.PlayerSpeedMod, 0f, () => followTarget)
        .RoleFlags(GetType() == typeof(Fugitive) ? RoleFlag.Hidden | RoleFlag.DontRegisterOptions : RoleFlags);

    [Localized(nameof(Runner))]
    public static class Translations
    {
        [Localized(nameof(VentButtonText))]
        public static string VentButtonText = "Rescue";
        [Localized(nameof(HackButtonText))]
        public static string HackButtonText = "Hack";
        [Localized(nameof(SkillCheckText))]
        public static string SkillCheckText = "Respond";
    }
}