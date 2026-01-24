using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.NeutralKilling;
using UnityEngine;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.Roles.Internals;
using Lotus.Utilities;
using VentLib.Utilities;
using VentLib.Localization.Attributes;
using VentLib.Utilities.Extensions;
using Lotus.GameModes.CTF;
using VentLib.Utilities.Collections;
using Lotus.GUI.Name.Components;
using Lotus.API.Odyssey;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name;
using Lotus.API.Player;
using Lotus.API;
using Lotus.RPC.CustomObjects.Builtin;
using Lotus.GameModes.Colorwars.Factions;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace Lotus.Roles.RoleGroups.Extra.CTF;

public class Striker : NeutralKillingBase, IRoleUI
{
    private Cooldown noticeTimer = null!;
    private Cooldown reviveTimer = null!;
    private Cooldown gameTimer = null!;
    private Cooldown gracePeriod = null!;

    private bool overtimeActive;
    private bool suddenDeath;

    [UIComponent(UI.Counter)]
    public string GameTimerText() => (overtimeActive ? Color.green : Color.white).Colorize($"\n{gameTimer}{GeneralOptionTranslations.SecondsSuffix}");

    [UIComponent(UI.Counter)]
    public string ReviveCooldownText() => reviveTimer.IsReady() ? "" : Color.yellow.Colorize(Translations.ReviveCooldown.Formatted(reviveTimer));

    [UIComponent(UI.Text)]
    public string GracePeriodText() => gracePeriod.IsReady() ? "" : Color.yellow.Colorize(Translations.GracePeriodTime.Formatted(gracePeriod));

    [UIComponent(UI.Text)]
    public string CurrentScore() => $"{Color.red.Colorize(CTFGamemode.Team0Score.ToString())} {Color.white.Colorize("|")} {Color.blue.Colorize(CTFGamemode.Team1Score.ToString())}";

    [UIComponent(UI.Text)]
    public string FlagGrabNotice() => noticeTimer.IsReady() ? "" : Color.yellow.Colorize(Translations.GrabbedFlag) + "\n";

    public RoleButton KillButton(IRoleButtonEditor killButton) => killButton.Default(false);
    public RoleButton PetButton(IRoleButtonEditor petButton) => petButton.Default(false);


    [NewOnSetup] private List<Remote<IndicatorComponent>> arrowComponents = null!;
    private Remote<Overrides.GameOptionOverride>? speedOverride;
    private Remote<IndicatorComponent>? warningIndicator;

    private string captureFlagImage = "Buttons/Extra/striker_capture_";
    private string killButtonImage = "Buttons/Extra/striker_attack_";
    private string takeFlagImage = "Buttons/Extra/striker_take_";
    private bool updatedImages;

    protected override void PostSetup()
    {
        KillCooldown = ExtraGamemodeOptions.CaptureOptions.KillCooldown * 2;
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void RoundStart()
    {
        // Here we start all cooldowns and setup everything.
        gracePeriod.SetDuration(ExtraGamemodeOptions.CaptureOptions.InvincibilityDuration);
        reviveTimer.SetDuration(ExtraGamemodeOptions.CaptureOptions.ReviveDuration);
        gameTimer.SetDuration(ExtraGamemodeOptions.CaptureOptions.GameLength);

        gameTimer.StartThenRun(CheckForOvertime);

        if (CTFGamemode.SpawnLocations == null)
        {
            if (ShipStatus.Instance is AirshipStatus) CTFGamemode.SpawnLocations = [RandomSpawn.AirshipLocations["Cockpit"], RandomSpawn.AirshipLocations["CargoBay"]];
            else CTFGamemode.SpawnLocations = ShipStatus.Instance.Type switch
            {
                ShipStatus.MapType.Ship => [RandomSpawn.SkeldLocations["Reactor"], RandomSpawn.SkeldLocations["Navigation"]],
                ShipStatus.MapType.Hq => [RandomSpawn.MiraLocations["Launchpad"], RandomSpawn.MiraLocations["Cafeteria"] - new Vector2(0, 2f)],
                ShipStatus.MapType.Pb => [RandomSpawn.PolusLocations["BoilerRoom"], RandomSpawn.PolusLocations["Laboratory"]],
                _ => throw new ArgumentOutOfRangeException(ShipStatus.Instance.Type.ToString())
            };
            CTFGamemode.RedFlag = new RedFlag(CTFGamemode.SpawnLocations[0]);
            CTFGamemode.BlueFlag = new BlueFlag(CTFGamemode.SpawnLocations[1]);
        }

        Utils.Teleport(MyPlayer.NetTransform, CTFGamemode.SpawnLocations[MyPlayer.cosmetics.bodyMatProperties.ColorId]);

        SetupImages();
        UpdateUI(StrikerRpcType.UpdateKillButton);
        UpdateUI(StrikerRpcType.ChangeToTake);
    }

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (MyPlayer.inVent || MyPlayer.walkingToVent) return false;
        gracePeriod.Finish();
        return base.TryKill(target);
    }

    [RoleAction(LotusActionType.Interaction)]
    private void FakeDie(ActionHandle handle)
    {
        if (suddenDeath)
        {
            // dont revive on sudden death
            PutDownFlag(false);
            return;
        }

        handle.Cancel(); // Stops me from dying and does RpcMark.
        if (reviveTimer.NotReady() || gracePeriod.NotReady() || MyPlayer.inVent || MyPlayer.walkingToVent) return;

        Utils.Teleport(MyPlayer.NetTransform, new Vector2(Random.RandomRange(5000, 99999), Random.RandomRange(5000, 99999)));
        reviveTimer.StartThenRun(RevivePlayer);

        PutDownFlag(false);
    }

    [RoleAction(LotusActionType.OnPet)]
    private void OnTouchFlag()
    {
        if (MyPlayer.inVent || MyPlayer.walkingToVent)
        {
            int ventId;

            ISystemType ventilation = ShipStatus.Instance.Systems[SystemTypes.Ventilation];
            if (ventilation.TryCast(out VentilationSystem ventilationSystem))
            {
                if (ventilationSystem.PlayersInsideVents.TryGetValue(MyPlayer.PlayerId, out byte byteId)) ventId = byteId;
                else ventId = Object.FindObjectsOfType<Vent>().ToList().GetRandom().Id;
            }
            else ventId = Object.FindObjectsOfType<Vent>().ToList().GetRandom().Id;

            MyPlayer.MyPhysics.RpcBootFromVent(ventId);
            return;
        }
        bool atRedFlag = RoleUtils.GetPlayersWithinDistance(CTFGamemode.SpawnLocations[0], 2f).Any(p => p.PlayerId == MyPlayer.PlayerId);
        bool atBlueFlag = RoleUtils.GetPlayersWithinDistance(CTFGamemode.SpawnLocations[1], 2f).Any(p => p.PlayerId == MyPlayer.PlayerId);
        if (!atRedFlag && !atBlueFlag) return;
        bool isRedTeam = MyPlayer.cosmetics.bodyMatProperties.ColorId == 0;

        if (isRedTeam)
        {
            if (atRedFlag) PutDownFlag(true);
            if (atBlueFlag)
            {
                // I am red team at blue's flag.
                if (CTFGamemode.Team1FlagCarrier != byte.MaxValue) return;
                GrabFlag(isRedTeam);
            }
        }
        else
        {
            if (atBlueFlag) PutDownFlag(true);
            if (atRedFlag)
            {
                // I am blue team at red's flag.
                if (CTFGamemode.Team0FlagCarrier != byte.MaxValue) return;
                GrabFlag(isRedTeam);
            }
        }
    }

    [RoleAction(LotusActionType.VentEntered)]
    private void CheckIfCarrying(ActionHandle handle)
    {
        if (ExtraGamemodeOptions.CaptureOptions.CarryingCanVent) return;

        if (CTFGamemode.Team0FlagCarrier == MyPlayer.PlayerId || CTFGamemode.Team1FlagCarrier == MyPlayer.PlayerId) handle.Cancel();
    }

    private void RevivePlayer()
    {
        gracePeriod.Start();
        Utils.Teleport(MyPlayer.NetTransform, CTFGamemode.SpawnLocations[MyPlayer.cosmetics.bodyMatProperties.ColorId]);
    }

    private void PutDownFlag(bool awardPoint)
    {
        UpdateUI(StrikerRpcType.ChangeToTake);
        if (CTFGamemode.Team0FlagCarrier == MyPlayer.PlayerId)
        {
            CTFGamemode.Team0FlagCarrier = byte.MaxValue;
            if (awardPoint) CTFGamemode.Team1Score += 1;
        }
        else if (CTFGamemode.Team1FlagCarrier == MyPlayer.PlayerId)
        {
            CTFGamemode.Team1FlagCarrier = byte.MaxValue;
            if (awardPoint) CTFGamemode.Team0Score += 1;
        }
        else return;

        warningIndicator?.Delete();
        speedOverride?.Delete();
        arrowComponents.ForEach(c => c.Delete());
        arrowComponents.Clear();
        noticeTimer.Finish();
        MyPlayer.SyncAll();
    }

    private void GrabFlag(bool isRedTeam)
    {
        if (isRedTeam)
            CTFGamemode.Team1FlagCarrier = MyPlayer.PlayerId;
        else
            CTFGamemode.Team0FlagCarrier = MyPlayer.PlayerId;

        warningIndicator = MyPlayer.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(new LiveString("⚠", RoleColor), Game.InGameStates));
        Color myColor = MyPlayer.PrimaryRole().RoleColor;
        Players.GetAllPlayers().ForEach(p =>
        {
            if (MyPlayer == p) return;
            LiveString liveString = new(() => RoleUtils.CalculateArrow(p, MyPlayer, myColor));
            var remote = p.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(liveString, GameState.Roaming, viewers: p));
            arrowComponents.Add(remote);
        });
        noticeTimer.Start(4);
        speedOverride = Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId, new(Overrides.Override.PlayerSpeedMod, AUSettings.PlayerSpeedMod() * ExtraGamemodeOptions.CaptureOptions.CarryingSpeedMultiplier));
        MyPlayer.SyncAll();
        UpdateUI(StrikerRpcType.ChangeToCapture);
    }

    private void CheckForOvertime()
    {
        if (CTFGamemode.Team0Score != CTFGamemode.Team1Score) return; // exit early if it is a tie
        var captureOptions = ExtraGamemodeOptions.CaptureOptions;
        if (!captureOptions.OvertimeOnTies) return; // exit early if no overtime
        overtimeActive = true;
        suddenDeath = captureOptions.SuddenDeath;
        gameTimer.Start(captureOptions.OvertimeLength);

        MyPlayer.NameModel().GetComponentHolder<TextHolder>()
            .Add(new TextComponent(new LiveString(Translations.OvertimeTitle, Color.green), Game.InGameStates,
                viewers: [MyPlayer]));

        KillCooldown /= 2;
        SyncOptions();
    }

    private void SetupImages()
    {
        if (!updatedImages)
        {
            bool isRed = MyPlayer.cosmetics.bodyMatProperties.ColorId == 0;
            captureFlagImage += isRed ? "red.png" : "blue.png";
            killButtonImage += isRed ? "red.png" : "blue.png";
            takeFlagImage += isRed ? "red.png" : "blue.png";
            updatedImages = true;
        }
    }

    protected void UpdateUI(StrikerRpcType updateType)
    {
        if (!MyPlayer.AmOwner)
        {
            if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateStriker)?.Send([MyPlayer.OwnerId], (int)updateType);
            return;
        }
        switch (updateType)
        {
            case StrikerRpcType.UpdateKillButton:
                UIManager.KillButton.SetSprite(() => LotusAssets.LoadSprite(killButtonImage, 130, true));
                break;
            case StrikerRpcType.ChangeToCapture:
                UIManager.PetButton
                    .SetText(Translations.CaptureButtonText)
                    .SetSprite(() => LotusAssets.LoadSprite(captureFlagImage, 130, true));
                break;
            case StrikerRpcType.ChangeToTake:
                UIManager.PetButton
                    .SetText(Translations.TakeButtonText)
                    .SetSprite(() => LotusAssets.LoadSprite(takeFlagImage, 130, true));
                break;
            case StrikerRpcType.Revert:
                UIManager.PetButton.RevertSprite().RevertText();
                break;
        }
    }

    [ModRPC(ModCalls.UpdateStriker, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateStriker(int rpcType)
    {
        Striker? striker = PlayerControl.LocalPlayer.PrimaryRole<Striker>();
        if (striker == null) return;
        striker.SetupImages();
        striker.UpdateUI((StrikerRpcType)rpcType);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleFlags(RoleFlag.DontRegisterOptions | RoleFlag.Hidden)
        .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage)
        .IntroSound(AmongUs.GameOptions.RoleTypes.Shapeshifter)
        .CanVent(ExtraGamemodeOptions.CaptureOptions.CanVent)
        .VanillaRole(AmongUs.GameOptions.RoleTypes.Impostor)
        .Faction(ColorFaction.Instance)
        .RoleColor(Color.white);

    [Localized(nameof(Striker))]
    public static class Translations
    {
        [Localized(nameof(CaptureButtonText))] public static string CaptureButtonText = "Capture";
        [Localized(nameof(TakeButtonText))] public static string TakeButtonText = "Take";

        [Localized(nameof(ReviveCooldown))] public static string ReviveCooldown = "Reviving In: {0}";
        [Localized(nameof(GracePeriodTime))] public static string GracePeriodTime = "Invincibility Leaving\nin: {0}";
        [Localized(nameof(GrabbedFlag))] public static string GrabbedFlag = "You grabbed the flag! The opposing team has arrows to your location!";
        [Localized(nameof(OvertimeTitle))] public static string OvertimeTitle = "<b>OVERTIME</b>";
        [Localized(nameof(OvertimeDeathMessage))] public static string OvertimeDeathMessage = "The host has enabled <b>'Sudden Death'</b> on Overtime, which means that respawning is disabled.";
    }

    protected enum StrikerRpcType
    {
        UpdateKillButton,
        ChangeToCapture,
        ChangeToTake,
        Revert
    }
}