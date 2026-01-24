using System;
using System.Collections.Generic;
using System.Linq;
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
using Random = UnityEngine.Random;

namespace Lotus.Roles.RoleGroups.NeutralKilling;

public class Pelican : NeutralKillingBase, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Pelican));
    private static IAccumulativeStatistic<int> _gulpedPlayerStat = Statistic<int>.CreateAccumulative("Roles.Pelican.PlayersGulped", () => Translations.GulpedStatistic);
    private static HashSet<string> _boundHooks = new();

    private bool allowPelicanEscape;
    private bool spawnBodiesOnPelican;

    [NewOnSetup]
    private Dictionary<byte, Remote<TextComponent>> gulpedPlayers;

    private Vector2 lastLocation;

    public RoleButton KillButton(IRoleButtonEditor killButton) => killButton
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Neut/pelican_swallow.png", 130, true));

    protected override void PostSetup()
    {
        _boundHooks.ForEach(bh => Hooks.PlayerHooks.PlayerTeleportedHook.Unbind(bh));
        _boundHooks.Clear();
        string identifier = $"{nameof(Pelican)}!{MyPlayer.PlayerId}";
        Hooks.PlayerHooks.PlayerTeleportedHook.Bind(identifier, CheckForTeleport, true);
        _boundHooks.Add(identifier);
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

        CheckPelicanEarlyWin();

        return false;
    }

    [RoleAction(LotusActionType.Interaction, ActionFlag.GlobalDetector, priority: Priority.First)]
    public void InterceptKillers(PlayerControl target, PlayerControl _, Interaction interaction, ActionHandle handle)
    {
        if (interaction is not LotusInteraction lotusInteraction) return;
        if (lotusInteraction.Intent is not IKillingIntent killingIntent) return;
        if (!gulpedPlayers.ContainsKey(target.PlayerId)) return;
        if (killingIntent["PELICANED"] as bool? == true) return;

        Func<IDeathEvent>? codSupplier = killingIntent.CauseOfDeath().Exists() ? () => killingIntent.CauseOfDeath().Get() : null;
        FatalIntent fatalIntent = new(true, codSupplier);
        lotusInteraction.Intent = fatalIntent;
        fatalIntent["PELICANED"] = true;

        if (spawnBodiesOnPelican) Utils.Teleport(target.NetTransform, MyPlayer.GetTruePosition());
    }

    [RoleAction(LotusActionType.RoundEnd)]
    public void KillGulpedPlayers()
    {
        gulpedPlayers.Keys.Filter(Players.PlayerById).Where(p => p.IsAlive()).ForEach(p =>
        {
            IDeathEvent deathEvent = new CustomDeathEvent(p, MyPlayer, Translations.DeathName);
            MyPlayer.InteractWith(p, new UnblockedInteraction(new FatalIntent(true, () => deathEvent) { ["PELICANED"] = true }, this));
            gulpedPlayers.GetValueOrDefault(p.PlayerId)?.Delete();
        });
        gulpedPlayers.Clear();
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
        Vector2 myLocation = MyPlayer.GetTruePosition();
        gulpedPlayers.Keys.Filter(Players.PlayerById).ForEach(p =>
        {
            Utils.Teleport(p.NetTransform, myLocation);
            gulpedPlayers.GetValueOrDefault(p.PlayerId)?.Delete();
        });
        gulpedPlayers.Clear();
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
        Utils.Teleport(player.NetTransform, lastLocation);
    }

    public void CheckForTeleport(PlayerTeleportedHookEvent teleportedHookEvent)
    {
        PlayerControl player = teleportedHookEvent.Player;
        if (!gulpedPlayers.ContainsKey(player.PlayerId)) return;
        if (teleportedHookEvent.NewLocation == lastLocation)
        {
            log.Trace($"(PelicanEat) Player: {player.name} has been teleported outside the map by ({MyPlayer.name}) during the game, and is currently held captive.");
            return;
        }
        if (teleportedHookEvent.NewLocation is { x: < -1000, y: < -1000 })
        {
            LiveString swallowedText = new(Translations.DeathName, RoleColor);
            gulpedPlayers[player.PlayerId] = player.NameModel().GCH<TextHolder>().Add(new TextComponent(swallowedText, GameState.Roaming, ViewMode.Additive, player));
            log.Trace($"(PelicanEnter) Player: {player.name} has teleported into the Pelican ({MyPlayer.name}) and is going to be eaten.");
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

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddKillCooldownOptions(base.RegisterOptions(optionStream), "Gulp Cooldown", Translations.Options.GulpCooldown)
            .SubOption(sub => sub.KeyName("Allow Escape from Pelican", Translations.Options.AllowEscapeFromPelican)
                .BindBool(b => allowPelicanEscape = b)
                .AddBoolean()
                .Build())
            .SubOption(sub => sub.KeyName("Spawn Bodies on Pelican", Translations.Options.SpawnBodiesOnPelican)
                .BindBool(b => spawnBodiesOnPelican = b)
                .AddBoolean()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.2f, 0.78f, 0.29f))
            .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.CannotVent)
            .OptionOverride(new IndirectKillCooldown(KillCooldown));

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
        }
    }

}