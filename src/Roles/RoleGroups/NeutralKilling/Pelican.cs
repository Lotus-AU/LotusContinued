using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Stats;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using Lotus.Roles.Internals.Enums;
using VentLib.Networking.RPC;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using HarmonyLib;
using Lotus.Logging;
using Priority = Lotus.API.Priority;
using Random = UnityEngine.Random;

namespace Lotus.Roles.RoleGroups.NeutralKilling;

public class Pelican : NeutralKillingBase, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Pelican));
    private static IAccumulativeStatistic<int> _gulpedPlayerStat = Statistic<int>.CreateAccumulative("Roles.Pelican.PlayersGulped", () => Translations.GulpedStatistic);

    private bool IsViper => killAfterTime > 0 && spawnBodiesOnPelicanOnDeath && decomposeBodyTime > 0;

    private bool allowPelicanEscape;
    private bool spawnBodiesOnPelicanWhenMeeting;

    private int killAfterTime;
    private bool spawnBodiesOnPelicanOnDeath;
    private float decomposeBodyTime;

    private bool isMeeting;

    [NewOnSetup] private Dictionary<byte, Remote<TextComponent>> gulpedPlayers;
    [NewOnSetup] private Dictionary<byte, DateTime> lastCaptured;

    private Vector2 lastLocation;

    public RoleButton KillButton(IRoleButtonEditor killButton) => killButton
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Neut/pelican_swallow.png", 130, true));

    protected override void PostSetup()
    {
        string identifier = $"{nameof(Pelican)}!{MyPlayer.PlayerId}";
        Hooks.PlayerHooks.PlayerTeleportedHook.Bind(identifier, CheckForTeleport, true);
        Game.GetWinDelegate().AddSubscriber(_ => Hooks.PlayerHooks.PlayerTeleportedHook.Unbind(identifier));
    }

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        InteractionResult result = MyPlayer.InteractWith(target, LotusInteraction.HostileInteraction.Create(MyPlayer));
        MyPlayer.RpcMark(target);
        if (result is InteractionResult.Halt)
        {
            log.Trace($"(Pelican.TryKill) Could not gulp {target.name}. Result is halt.");
            return false;
        }
        log.Trace($"(Pelican.TryKill) Swallowed {target.name}");

        LiveString swallowedText = new(Translations.DeathName.ToUpper(), RoleColor);
        gulpedPlayers[target.PlayerId] = target.NameModel().GCH<TextHolder>().Add(new TextComponent(swallowedText, GameState.Roaming, ViewMode.Additive, target));
        lastCaptured[target.PlayerId] = DateTime.Now;
        _gulpedPlayerStat.Update(MyPlayer.UniquePlayerId(), i => i + 1);
        TeleportPlayer(target);

        if (MyPlayer.AmOwner)
        {
            MyPlayer.SetScanner(true, ++MyPlayer.scannerCount);
            Async.Schedule(() => MyPlayer.SetScanner(false, ++MyPlayer.scannerCount), 0.8f);
        }
        else
        {
            RpcV3.Immediate(MyPlayer.NetId, RpcCalls.SetScanner, SendOption.None).Write(true).Write(++MyPlayer.scannerCount).Send(MyPlayer.GetClientId());
            Async.Schedule(() => RpcV3.Immediate(MyPlayer.NetId, RpcCalls.SetScanner, SendOption.None).Write(false).Write(++MyPlayer.scannerCount).Send(MyPlayer.GetClientId()), 0.8f);
        }

        if (killAfterTime > 0)
        {
            int currentMeetings = Game.MatchData.MeetingsCalled;
            Async.Schedule(() => KillTargetAfterTimer(target, currentMeetings), killAfterTime);
        }

        // CheckPelicanEarlyWin();

        return false;
    }

    [RoleAction(LotusActionType.Interaction, ActionFlag.GlobalDetector, priority: Priority.First)]
    public void InterceptKillers(PlayerControl target, PlayerControl interactor, Interaction interaction, ActionHandle handle)
    {
        if (interaction is not LotusInteraction lotusInteraction) return;
        if (lotusInteraction.Intent is not IKillingIntent killingIntent) return;
        if (gulpedPlayers.ContainsKey(interactor.PlayerId))
        {
            handle.Cancel();
            return;
        }
        if (!gulpedPlayers.ContainsKey(target.PlayerId)) return;
        if (killingIntent["PELICANED"] as bool? == true) return;

        Func<IDeathEvent>? codSupplier = killingIntent.CauseOfDeath().Exists() ? () => killingIntent.CauseOfDeath().Get() : null;
        FatalIntent fatalIntent = new(spawnBodiesOnPelicanWhenMeeting && isMeeting, codSupplier);
        lotusInteraction.Intent = fatalIntent;
        fatalIntent["PELICANED"] = true;

        if (spawnBodiesOnPelicanWhenMeeting && isMeeting || spawnBodiesOnPelicanOnDeath)
            Utils.Teleport(target.NetTransform, GetTruePosition(), this);
        else if (!isMeeting && !spawnBodiesOnPelicanOnDeath)
            Async.Schedule(() => Utils.Teleport(target.NetTransform, GetTruePosition(), this), .2f);
    }

    [RoleAction(LotusActionType.RoundEnd)]
    public void KillGulpedPlayers()
    {
        isMeeting = true;
        gulpedPlayers.Keys.Filter(Players.PlayerById).Where(p => p.IsAlive()).ForEach(p =>
        {
            IDeathEvent deathEvent = new CustomDeathEvent(p, MyPlayer, Translations.DeathName);
            MyPlayer.InteractWith(p, new UnblockedInteraction(new FatalIntent(true, () => deathEvent) { ["PELICANED"] = true }, this));
            gulpedPlayers.GetValueOrDefault(p.PlayerId)?.Delete();
        });
        gulpedPlayers.Clear();
        lastCaptured.Clear();
        isMeeting = false;
    }

    [RoleAction(LotusActionType.ReportBody, ActionFlag.GlobalDetector)]
    public void PreventReportsFromSwallowedPlayers(PlayerControl player, ActionHandle handle)
    {
        if (gulpedPlayers.ContainsKey(player.PlayerId)) handle.Cancel();
    }

    [RoleAction(LotusActionType.SabotageStarted, ActionFlag.GlobalDetector)]
    public void PreventSabotageFromSwallowedPlayers(ISabotage sabotage, ActionHandle handle)
    {
        if (sabotage.Caller().Compare(s => gulpedPlayers.ContainsKey(s.PlayerId))) handle.Cancel();
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    public override void HandleDisconnect()
    {
        Vector2 myLocation = GetTruePosition();
        gulpedPlayers.Keys.Filter(Players.PlayerById).ForEach(p =>
        {
            Utils.Teleport(p.NetTransform, myLocation, this);
            gulpedPlayers.GetValueOrDefault(p.PlayerId)?.Delete();
        });
        gulpedPlayers.Clear();
        lastCaptured.Clear();
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    [RoleAction(LotusActionType.Disconnect)]
    private void CheckPelicanEarlyWin()
    {
        if (Players.GetPlayers(PlayerFilter.Alive).Count(p => p.PlayerId != MyPlayer.PlayerId) == gulpedPlayers.Count) KillGulpedPlayers();
    }

    private void TeleportPlayer(PlayerControl player)
    {
        int randomX = Random.RandomRange(5000, 99999);
        int randomY = Random.RandomRange(5000, 99999);
        lastLocation = new Vector2(-randomX, -randomY);
        Utils.Teleport(player.NetTransform, lastLocation, this);
    }

    private void KillTargetAfterTimer(PlayerControl target, int meetingCount)
    {
        if (Game.State is not GameState.Roaming) return;
        if (meetingCount != Game.MatchData.MeetingsCalled) return;
        if (!gulpedPlayers.ContainsKey(target.PlayerId)) return; // they escaped or left
        if (!MyPlayer.IsAlive()) return;

        bool playerWillKill = !spawnBodiesOnPelicanOnDeath;
        Remote<GameOptionOverride> killOverride = null!;

        float delay = 0;

        if (!playerWillKill)
        {
            killOverride = Game.MatchData.Roles.AddOverride(MyPlayer.PlayerId,
                new GameOptionOverride(Override.KillCooldown, KillCooldown / 2));

            delay = NetUtils.DeriveDelay(.3f);
            MyPlayer.SyncAll();
        }

        Async.Schedule(() =>
        {
            DevLogger.Log($"{playerWillKill}");
            IDeathEvent deathEvent = new CustomDeathEvent(target, MyPlayer, Translations.DeathName);
            MyPlayer.InteractWith(target, new UnblockedInteraction(new FatalIntent(playerWillKill, () => deathEvent) { ["PELICANED"] = true }, this));
            gulpedPlayers.GetValueOrDefault(target.PlayerId)?.Delete();
            gulpedPlayers.Remove(target.PlayerId);
            lastCaptured.Remove(target.PlayerId);

            if (playerWillKill) return;
            killOverride?.Delete();
            MyPlayer.SyncAll();
        }, delay);
    }

    public void CheckForTeleport(PlayerTeleportedHookEvent teleportedHookEvent)
    {
        PlayerControl player = teleportedHookEvent.Player;
        if (!gulpedPlayers.ContainsKey(player.PlayerId)) return;
        bool roleExists = teleportedHookEvent.Initiator.Exists();

        if (roleExists && teleportedHookEvent.Initiator.Get().MyPlayer == MyPlayer)
        {
            DateTime recordedCapture = lastCaptured[player.PlayerId];
            if ((teleportedHookEvent.NewLocation - MyPlayer.GetTruePosition()).magnitude <= 10 &&
                 (DateTime.Now - recordedCapture).TotalSeconds >
                 (killAfterTime - 2f))
            {
                LiveString swallowedText = new(Translations.DeathName, RoleColor);
                gulpedPlayers[player.PlayerId] = player.NameModel().GCH<TextHolder>().Add(new TextComponent(swallowedText, GameState.Roaming, ViewMode.Additive, player));
                log.Trace($"(PelicanEnter) Player: {player.name} has teleported into the Pelican ({MyPlayer.name}) and is going to be eaten.");
            }
            else
                log.Trace($"(PelicanEat) Player: {player.name} has been teleported outside the map by ({MyPlayer.name}) during the game, and is currently held captive.");
            return;
        }



        log.Trace($"(PelicanEscape) Player: {player.name} has teleported out of the Pelican ({MyPlayer.name})");
        if (allowPelicanEscape)
        {
            gulpedPlayers.GetValueOrDefault(player.PlayerId)?.Delete();
            gulpedPlayers.Remove(player.PlayerId);
        }
        else TeleportPlayer(player);
    }

    private Vector2 GetTruePosition() => (Vector2)MyPlayer.transform.position - MyPlayer.Collider.offset;

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddKillCooldownOptions(base.RegisterOptions(optionStream), "Gulp Cooldown", Translations.Options.GulpCooldown)
            .SubOption(sub => sub.KeyName("Allow Escape from Pelican", Translations.Options.AllowEscapeFromPelican)
                .BindBool(b => allowPelicanEscape = b)
                .AddBoolean()
                .Build())
            .SubOption(sub => sub.KeyName("Spawn Bodies on Pelican", Translations.Options.SpawnBodiesOnPelican)
                .BindBool(b => spawnBodiesOnPelicanWhenMeeting = b)
                .AddBoolean()
                .Build())
            .SubOption(sub => sub
                .KeyName("Kill after Time Limit", Translations.Options.KillAfterTimeLimit)
                .Value(v => v.Text(GeneralOptionTranslations.DisabledText).Color(Color.red).Value(0).Build())
                .AddIntRange(5, 60, 5, 5, GeneralOptionTranslations.SecondsSuffix)
                .BindInt(i => killAfterTime = i)
                .ShowSubOptionPredicate(v => (int)v > 0)
                .SubOption(sub2 => sub2
                    .KeyName("Teleport Body to Pelican on Death", Translations.Options.SpawnBodiesOnTimerEnd)
                    .AddBoolean(false)
                    .BindBool(b => spawnBodiesOnPelicanOnDeath = b)
                    .ShowSubOptionPredicate(v => (bool)v)
                    .SubOption(sub3 => sub3
                        .KeyName("Decompose Body", Translations.Options.DecomposeBody)
                        .Value(v => v.Text(GeneralOptionTranslations.DisabledText).Color(Color.red).Value(0).Build())
                        .AddFloatRange(2.5f, 30f, 2.5f, 4, GeneralOptionTranslations.SecondsSuffix)
                        .BindFloat(f => decomposeBodyTime = f)
                        .Build())
                    .Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.2f, 0.78f, 0.29f))
            .VanillaRole(IsViper ? RoleTypes.Viper : RoleTypes.Impostor)
            .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.CannotVent)
            .OptionOverride(new IndirectKillCooldown(KillCooldown))
            .OptionOverride(new GameOptionOverride(Override.ViperDissolveTime, () => decomposeBodyTime));

    public override List<Statistic> Statistics() => new() { _gulpedPlayerStat };

    [Localized(nameof(Pelican))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))]
        public static string ButtonText = "Swallow";

        [Localized(nameof(GulpedStatistic))]
        public static string GulpedStatistic = "Players Gulped";

        [Localized(nameof(DeathName))]
        public static string DeathName = "Swallowed";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(GulpCooldown))]
            public static string GulpCooldown = "Gulp Cooldown";

            [Localized(nameof(AllowEscapeFromPelican))]
            public static string AllowEscapeFromPelican = "Allow Escape from Pelican";

            [Localized(nameof(SpawnBodiesOnPelican))]
            public static string SpawnBodiesOnPelican = "Spawn Bodies on Pelican when Meeting";

            [Localized(nameof(KillAfterTimeLimit))]
            public static string KillAfterTimeLimit = "Kill Gulped After # Seconds";

            [Localized(nameof(SpawnBodiesOnTimerEnd))]
            public static string SpawnBodiesOnTimerEnd = "Spawn Bodies on Timer End";

            [Localized(nameof(DecomposeBody))]
            public static string DecomposeBody = "Decompose Body";
        }
    }

}