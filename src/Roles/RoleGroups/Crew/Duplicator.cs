extern alias JBAnnotations;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Internals.Enums;
using Lotus.GUI;
using Lotus.API.Odyssey;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name.Components;
using Lotus.RPC.CustomObjects.Builtin;
using System.Collections.Generic;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using Lotus.Extensions;
using Lotus.Options;
using UnityEngine;
using Lotus.Roles.Interactions;
using Lotus.Managers.History.Events;
using System;
using Lotus.Utilities;

using static Lotus.Roles.RoleGroups.Impostors.Creeper.CreeperTranslations.Options;
using System.Linq;
using JBAnnotations::JetBrains.Annotations;
using Lotus.RPC.CustomObjects;
using Lotus.Roles.Operations;
using Lotus.API;
using Lotus.Factions;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Crew;

public class Duplicator : Crewmate, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Duplicator));

    [UIComponent(Lotus.GUI.Name.UI.Cooldown)]
    private Cooldown duplicateCooldown = null!;
    private Cooldown duplicateDuration = null!;

    private List<FakePlayer> fakePlayers = null!;
    private RandomSpawn randomSpawn = null!;
    private float killRadius;

    private FixedUpdateLock fixedUpdateLock = new();

    private bool spawnsRandomly;
    private bool onlyKillUnalliedPlayers;

    public RoleButton PetButton(IRoleButtonEditor petButton) => petButton
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/duplicator_duplicate.png", 130, true));

    protected override void PostSetup()
    {
        if (spawnsRandomly)
        {
            randomSpawn = new();
            randomSpawn.Refresh();
        }
        fakePlayers = new();
        base.PostSetup();
        MyPlayer.NameModel().GetComponentHolder<CooldownHolder>().Add(new CooldownComponent(duplicateDuration, GameState.Roaming, cdText: "Dur: ", viewers: MyPlayer));
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void OnRoundStart(bool gameStart)
    {
        duplicateDuration.Finish(true);
        duplicateCooldown.Start(gameStart ? 10 : float.MinValue);
        if (MyPlayer.AmOwner) UIManager.PetButton.BindCooldown(duplicateCooldown);
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateDuplicator)?.Send([MyPlayer.OwnerId], true, gameStart);
    }

    [RoleAction(LotusActionType.RoundEnd)]
    private void OnRoundEnd()
    {
        duplicateDuration.Finish();
        duplicateCooldown.Finish();
    }

    [RoleAction(LotusActionType.OnPet)]
    private void OnAbilityActivation()
    {
        if (duplicateCooldown.NotReady() || duplicateDuration.NotReady()) return;

        Vector2 endPosition = spawnsRandomly ? randomSpawn.GetRandomLocation() : MyPlayer.GetTruePosition();
        if (RoleUtils.GetPlayersWithinDistance(endPosition, killRadius).Any(p => p.PlayerId != MyPlayer.PlayerId))
        {
            log.Debug("Can't spawn clone while near people!");
            return;
        }

        log.Debug($"Creating fake player of {MyPlayer.name}");
        FakePlayer fakePlayer = new(MyPlayer.Data.DefaultOutfit, endPosition, MyPlayer.PlayerId);
        fakePlayers.Add(fakePlayer);
        duplicateDuration.StartThenRun(() =>
        {
            duplicateCooldown.Start();
            log.Debug($"Removing fake player of {MyPlayer.name}");
            if (fakePlayers.Remove(fakePlayer)) fakePlayer.Despawn();
            if (MyPlayer.AmOwner) UIManager.PetButton.BindCooldown(duplicateCooldown);
            else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateDuplicator)?.Send([MyPlayer.OwnerId], true, false);
        });
        if (MyPlayer.AmOwner) UIManager.PetButton.BindCooldown(duplicateDuration);
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateDuplicator)?.Send([MyPlayer.OwnerId], false, false);
    }

    [RoleAction(LotusActionType.RoundEnd)]
    [RoleAction(LotusActionType.PlayerDeath)]
    public override void HandleDisconnect()
    {
        log.Debug($"Removing ALL fake players of {MyPlayer.name}");
        fakePlayers.ForEach(p => p.Despawn());
        fakePlayers.Clear();
    }

    [RoleAction(LotusActionType.FixedUpdate)]
    private void CheckForPlayers()
    {
        if (!fixedUpdateLock.AcquireLock()) return;
        fakePlayers.ForEach(fp =>
        {
            RoleUtils.GetPlayersWithinDistance(fp.Position, killRadius).Where(p => p.PlayerId != MyPlayer.PlayerId).ForEach(p =>
            {
                if (PhysicsHelpers.AnythingBetween(fp.Position, p.NetTransform.body.position,
                        Constants.ShipOnlyMask, false)) return;
                if (onlyKillUnalliedPlayers && MyPlayer.Relationship(p) is not Relation.None) return;
                var cod = new CustomDeathEvent(p, MyPlayer, Translations.TrickedCauseOfDeath);
                MyPlayer.InteractWith(p, new IndirectInteraction(new FatalIntent(true, () => cod), this));
            });
        });
    }

    [UsedImplicitly]
    [ModRPC((uint)ModCalls.UpdateDuplicator, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateDuplicator(bool useCooldown, bool gameStart)
    {
        Duplicator? duplicator = PlayerControl.LocalPlayer.PrimaryRole<Duplicator>();
        if (duplicator == null) return;
        Cooldown targetCooldown = useCooldown ? duplicator.duplicateCooldown : duplicator.duplicateDuration;
        targetCooldown.Start(gameStart ? 10 : float.MinValue);
        duplicator.UIManager.PetButton.BindCooldown(targetCooldown);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Duplicate Cooldown", Translations.Options.DuplicateCooldown)
                .BindFloat(duplicateCooldown.SetDuration)
                .AddFloatRange(2.5f, 120, 2.5f, 11, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Duplicate Duration", Translations.Options.DuplicateDuration)
                .BindFloat(duplicateDuration.SetDuration)
                .AddFloatRange(2.5f, 120, 2.5f, 5, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Spawn Location", Translations.Options.SpawnLocation)
                .Value(v => v.Value(false).Text(Translations.Options.CurrentLocation).Color(Color.cyan).Build())
                .Value(v => v.Value(true).Text(Translations.Options.RandomLocation).Color(Color.red).Build())
                .BindBool(v => spawnsRandomly = v)
                .Build())
            .SubOption(sub => sub.KeyName("Kill Radius", Translations.Options.KillRadius)
                .Value(v => v.Value(1f).Text(SmallDistance).Build())
                .Value(v => v.Value(2f).Text(MediumDistance).Build())
                .Value(v => v.Value(3f).Text(LargeDistance).Build())
                .BindFloat(f => killRadius = f)
                .Build())
            .SubOption(sub => sub.KeyName("OnlyKillUnalliedPlayers", Translations.Options.OnlyKillUnalliedPlayers)
                .AddBoolean()
                .BindBool(b => onlyKillUnalliedPlayers = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.87f, 0.6f, 1f))
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet | RoleAbilityFlag.IsAbleToKill);

    [Localized(nameof(Duplicator))]
    public static class Translations
    {
        [Localized(nameof(TrickedCauseOfDeath))] public static string TrickedCauseOfDeath = "Tricked";
        [Localized(nameof(ButtonText))] public static string ButtonText = "Duplicate";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(OnlyKillUnalliedPlayers))] public static string OnlyKillUnalliedPlayers = "Only Kill Unallied Players";
            [Localized(nameof(DuplicateCooldown))] public static string DuplicateCooldown = "Duplicate Cooldown";
            [Localized(nameof(DuplicateDuration))] public static string DuplicateDuration = "Duplicate Duration";
            [Localized(nameof(SpawnLocation))] public static string SpawnLocation = "Spawn Location";
            [Localized(nameof(CurrentLocation))] public static string CurrentLocation = "Current";
            [Localized(nameof(RandomLocation))] public static string RandomLocation = "Random";
            [Localized(nameof(KillRadius))] public static string KillRadius = "Kill Radius";
        }
    }

    // unfortunately, if these guys kill you, you can't move anymore and an error appears. its very weird.
    // so we will just have to do with a ranged kill.
    private class DuplicatorFatalIntent : FatalIntent
    {
        public FakePlayer fakePlayer;
        public DuplicatorFatalIntent(FakePlayer fakePlayer, Func<IDeathEvent>? causeOfDeath = null) : base(true, causeOfDeath)
        {
            this.fakePlayer = fakePlayer;
        }
        public override void Action(PlayerControl duplicator, PlayerControl target)
        {
            if (!CustomNetObject.AllObjects.Contains(this.fakePlayer))
            {
                base.Action(duplicator, target);
                return;
            }
            var deathEvent = CauseOfDeath();
            duplicator.PrimaryRole().SyncOptions();

            deathEvent.IfPresent(ev => Game.MatchData.GameHistory.SetCauseOfDeath(target.PlayerId, ev));
            KillTargetDuplicator(target);
            // KillTarget(initiator, target);
            Game.MatchData.RegenerateFrozenPlayers(target);

            if (!target.IsAlive()) return;
            log.Debug($"After executing the fatal action. The target \"{target.name}\" was still alive. Killer: {duplicator.name}");
            RoleOperations.Current.Trigger(LotusActionType.SuccessfulAngelProtect, duplicator, target);
            Game.MatchData.GameHistory.ClearCauseOfDeath(target.PlayerId);
            Game.MatchData.RegenerateFrozenPlayers(target);
        }

        public void KillTargetDuplicator(PlayerControl target)
        {
            ProtectedRpc.CheckMurder(fakePlayer.playerControl, target);
        }
    }
}