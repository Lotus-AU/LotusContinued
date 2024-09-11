using Lotus.Roles.Internals.Enums;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Interfaces;
using Lotus.Utilities;
using VentLib.Utilities.Optionals;
using Lotus.Extensions;
using UnityEngine;
using Lotus.Roles.Internals;
using VentLib.Options.UI;
using VentLib.Localization.Attributes;
using Lotus.Options;
using VentLib.Utilities;
using System.Collections.Generic;
using VentLib.Utilities.Extensions;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using VentLib.Utilities.Collections;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Holders;

namespace Lotus.Roles.RoleGroups.Crew;

public class Altruist : Scientist, IRoleCandidate
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Altruist));
    public bool ShouldSkip() => !ProjectLotus.AdvancedRoleAssignment; // skip assignment if we dont allow role changes mid game
    [NewOnSetup] private HashSet<PlayerControl> deadPlayers = null!;

    private float reviveDelay;
    private bool altruistHasVitals;
    private bool arrowForRevivedKiller;

    private byte reviedPlayerId = 255;
    private Remote<IndicatorComponent> arrowComponent = null!;

    [RoleAction(LotusActionType.ReportBody)]
    private void OnResurect(Optional<NetworkedPlayerInfo> body, ActionHandle handle)
    {
        if (!body.Exists() || reviedPlayerId != 255 || arrowComponent != null) return; // if its a meeting or we already revived someone this game.
        PlayerControl? reviedPlayer = Players.GetPlayers(PlayerFilter.Dead).FirstOrDefault(p => p.PlayerId == body.Get().PlayerId);
        if (reviedPlayer == null || reviedPlayer.Data.Disconnected) return; // if they dont exist or left the game already
        log.Debug($"Starting revive of player {reviedPlayer.name}. Waiting for countdown.");
        handle.Cancel();
        reviedPlayerId = reviedPlayer.PlayerId;
        MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(causeOfDeath: () => new SuicideEvent(MyPlayer)), this));
        if (reviveDelay <= 0f) RevivePlayer(reviedPlayer);
        else Async.Schedule(() =>
        {
            if (MeetingHud.Instance == null) RevivePlayer(reviedPlayer);
        }, reviveDelay);
    }

    private void RevivePlayer(PlayerControl player)
    {
        log.Debug($"Reviving player: {player.name}");
        reviedPlayerId = player.PlayerId;
        if (MyPlayer.IsAlive()) MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(causeOfDeath: () => new SuicideEvent(MyPlayer)), this));
        Game.MatchData.UnreportableBodies.Add(MyPlayer.PlayerId);
        Game.MatchData.UnreportableBodies.Add(reviedPlayerId);
        Vector2 position = MyPlayer.GetTruePosition();
        // get dead body that is my or the dead player's position
        Object.FindObjectsOfType<DeadBody>().FirstOrDefault(b =>
        {
            if (b.ParentId == MyPlayer.PlayerId || b.ParentId == player.PlayerId)
            {
                position = b.transform.position;
                return true;
            }
            else return false;
        });
        Utils.Teleport(player.NetTransform, position);
        player.PrimaryRole().Assign(); // resends their role info to all players
        deadPlayers.Remove(player);
        // act like they never died smh
        IDeathEvent causeofDeath = Game.MatchData.GameHistory.GetCauseOfDeath(player.PlayerId).OrElse(null!);
        Game.MatchData.GameHistory.ClearCauseOfDeath(player.PlayerId);
        if (player.PrimaryRole().RealRole.IsImpostor())
        {
            // reassign tasks because they get removed in the code
            // why innersloth smh
            // unfortunately, because some tasks have client-side random positions (wires, diret power), those tasks will be in different positions
            player.Data.RpcSetTasks(new Il2CppStructArray<byte>(player.myTasks.ToArray().Select(t => (byte)t.Index).ToArray()));
        }
        if (!arrowForRevivedKiller || causeofDeath == null) return;
        if (!causeofDeath.Instigator().Exists()) return;
        PlayerControl killer = causeofDeath.Instigator().Get().MyPlayer;
        if (killer == null || killer == player) return;
        LiveString liveString = new(() => RoleUtils.CalculateArrow(killer, player, RoleColor));
        arrowComponent = killer.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(liveString, GameState.Roaming, viewers: killer));
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath)]
    private void OnPlayerDeath(PlayerControl victim)
    {
        if (MyPlayer.IsAlive()) deadPlayers.Add(victim);
        if (victim.PlayerId == reviedPlayerId) OnMeeting();
    }

    [RoleAction(LotusActionType.RoundEnd, ActionFlag.WorksAfterDeath)]
    private void OnMeeting()
    {
        // show stuff to all dead players
        deadPlayers.RemoveWhere(deadPlayer =>
        {
            if (deadPlayer.IsAlive()) return true;
            Players.GetAllPlayers().Where(p => p.PlayerId != deadPlayer.PlayerId)
                .SelectMany(p => p.NameModel().ComponentHolders())
                .ForEach(holders =>
                    {
                        holders.AddListener(component => component.AddViewer(deadPlayer));
                        holders.Components().ForEach(components => components.AddViewer(deadPlayer));
                    }
                );
            return true;
        });
        arrowComponent?.Delete();
        if (reviedPlayerId != 255) return;
        Game.MatchData.UnreportableBodies.Remove(reviedPlayerId);
        reviedPlayerId = 255;
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => base.RegisterOptions(optionStream)
        .SubOption(sub => AddVitalsOptions(sub
            .KeyName("Altruist Has Vitals", AltruistTranslations.Options.PortableVitals)
            .AddBoolean()
            .BindBool(b => altruistHasVitals = b)
            .ShowSubOptionPredicate(b => (bool)b))
            .Build())
        .SubOption(sub => sub
            .KeyName("Revive Delay", AltruistTranslations.Options.ReviveDelay)
            .BindFloat(v => reviveDelay = v)
            .Value(v => v.Color(Color.red).Text(AltruistTranslations.Options.InstantText).Value(0f).Build())
            .AddFloatRange(.5f, 30f, .5f, 0, GeneralOptionTranslations.SecondsSuffix)
            .Build())
        .SubOption(sub => sub
            .KeyName("Killer Gets Arrow", AltruistTranslations.Options.KillerArrow)
            .BindBool(b => arrowForRevivedKiller = b)
            .AddBoolean(false)
            .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
        .RoleColor(new Color(0.4f, 0f, 0f))
        .VanillaRole(altruistHasVitals ? RoleTypes.Scientist : RoleTypes.Crewmate);

    [Localized(nameof(Altruist))]
    public static class AltruistTranslations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(KillerArrow))] public static string KillerArrow = "Killer Gets Arrow to Revived Player";
            [Localized(nameof(PortableVitals))] public static string PortableVitals = "Has Portable Vitals";
            [Localized(nameof(ReviveDelay))] public static string ReviveDelay = "Revive Delay";
            [Localized(nameof(InstantText))] public static string InstantText = "Instant";
        }
    }
}