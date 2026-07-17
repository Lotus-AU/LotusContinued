using System;
using System.Collections.Generic;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Optionals;
using Lotus.API.Player;
using Lotus.Roles.Operations;

namespace Lotus.Roles.Subroles;

public class Unstoppable : Subrole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Unstoppable));
    private bool canKillUntargetable;

    public override string Identifier() => "◇";

    [RoleAction(LotusActionType.Interaction, ActionFlag.GlobalDetector, priority: Priority.First)]
    public void InterceptAnyInteraction(PlayerControl target, PlayerControl player, Interaction interaction, ActionHandle handle)
    {
        DevLogger.Log($"Player: {player.name} Interaction: {interaction.Intent}");
        if (player.PlayerId != MyPlayer.PlayerId) return;
        if (interaction is not LotusInteraction lotusInteraction) return;
        if (lotusInteraction.Intent is not IFatalIntent fatalIntent) return;

        Func<IDeathEvent>? causeOfDeath = fatalIntent.CauseOfDeath().Exists() ? () => fatalIntent.CauseOfDeath().Get() : null;
        lotusInteraction.Intent = new UnstoppableIntent(fatalIntent.IsRanged(), causeOfDeath);
        lotusInteraction.IsPromised = canKillUntargetable;

        log.Debug($"Unstoppable Interaction Swap IsPromised={lotusInteraction.IsPromised}", "UnstoppableInterception");
    }

    public override bool IsAssignableTo(PlayerControl player)
    {
        return player.PrimaryRole().RoleAbilityFlags.HasFlag(RoleAbilityFlag.IsAbleToKill) && base.IsAssignableTo(player);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Can Kill Untargetable Players", Translations.Options.CanKillUntargetable)
                .AddBoolean(false)
                .BindBool(b => canKillUntargetable = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.55f, 0f, 0.3f, 1f));


    [Localized(nameof(Unstoppable))]
    private static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(CanKillUntargetable))]
            public static string CanKillUntargetable = "Can Kill Untargetable Players";
        }
    }


    public class UnstoppableIntent : IKillingIntent
    {
        private Func<IDeathEvent>? causeOfDeath;
        private bool ranged;

        public UnstoppableIntent(bool ranged = false, Func<IDeathEvent>? causeOfDeath = null)
        {
            this.ranged = ranged;
            this.causeOfDeath = causeOfDeath;
        }

        public Optional<IDeathEvent> CauseOfDeath() => Optional<IDeathEvent>.Of(causeOfDeath?.Invoke());

        public bool IsRanged() => ranged;

        public void Action(PlayerControl actor, PlayerControl target)
        {
            Optional<IDeathEvent> deathEvent = CauseOfDeath();
            actor.PrimaryRole().SyncOptions();

            Optional<IDeathEvent> currentDeathEvent = Game.MatchData.GameHistory.GetCauseOfDeath(target.PlayerId);
            deathEvent.IfPresent(death => Game.MatchData.GameHistory.SetCauseOfDeath(target.PlayerId, death));
            KillTarget(actor, target);

            ActionHandle ignored = ActionHandle.NoInit();
            if (target.IsAlive()) RoleOperations.Current.TriggerForAll(LotusActionType.SuccessfulAngelProtect, target, ignored, target, actor);
            else currentDeathEvent.IfPresent(de => Game.MatchData.GameHistory.SetCauseOfDeath(target.PlayerId, de));
        }

        public void KillTarget(PlayerControl actor, PlayerControl target)
        {
            ProtectedRpc.CheckMurder(!ranged ? actor : target, target);
        }

        public void Halted(PlayerControl actor, PlayerControl target)
        {
            actor.RpcMark(target);
        }

        private Dictionary<string, object?>? meta;
        public object? this[string key]
        {
            get => (meta ?? new Dictionary<string, object?>()).GetValueOrDefault(key);
            set => (meta ?? new Dictionary<string, object?>())[key] = value;
        }
    }
}