using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Utilities;
using Lotus.Extensions;
using Lotus.Logging;
using UnityEngine;
using VentLib.Localization.Attributes;
using Lotus.Roles.Internals.Enums;
using VentLib.Options.UI;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;

namespace Lotus.Roles.RoleGroups.NeutralKilling;

public class PlagueBearer : NeutralKillingBase, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PlagueBearer));
    private static readonly Pestilence Pestilence = new();

    [NewOnSetup] private List<Remote<IndicatorComponent>> indicatorRemotes = new();
    [NewOnSetup] private HashSet<byte> infectedPlayers = null!;
    private int alivePlayers;

    public RoleButton KillButton(IRoleButtonEditor killButton) => killButton
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Neut/plague_bearer_infect.png", 130, true));

    public override bool CanSabotage() => false;

    protected override void PostSetup()
    {
        RelatedRoles.Add(typeof(Pestilence));
        CheckPestilenceTransform(new ActionHandle(LotusActionType.RoundStart));
    }

    [UIComponent(UI.Counter)]
    private string InfectionCounter() => RoleUtils.Counter(infectedPlayers.Count, alivePlayers, RoleColor);

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (infectedPlayers.Contains(target.PlayerId))
        {
            MyPlayer.RpcMark(target);
            // in case the game bugs check pestilence
            CheckPestilenceTransform(ActionHandle.NoInit());
            return false;
        }

        if (MyPlayer.InteractWith(target, LotusInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt) return false;
        MyPlayer.RpcMark(target);

        string historyMessage = Translations.InfectedHistoryMessage.Formatted(MyPlayer.name, target.name);
        historyMessage = TranslationUtil.Colorize(historyMessage, RoleColor, target.GetRoleColor());
        Game.MatchData.GameHistory.AddEvent(new GenericTargetedEvent(MyPlayer, target, historyMessage));
        log.Debug(historyMessage.RemoveColorTags());

        infectedPlayers.Add(target.PlayerId);

        IndicatorComponent indicator = new SimpleIndicatorComponent("☀", RoleColor, Game.InGameStates, MyPlayer);
        indicatorRemotes.Add(target.NameModel().GetComponentHolder<IndicatorHolder>().Add(indicator));

        CheckPestilenceTransform(ActionHandle.NoInit());

        return false;
    }

    [RoleAction(LotusActionType.RoundEnd)]
    [RoleAction(LotusActionType.RoundStart)]
    [RoleAction(LotusActionType.Disconnect, ActionFlag.GlobalDetector)]
    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    public void CheckPestilenceTransform(ActionHandle handle)
    {
        PlayerControl[] allCountedPlayers = GetAlivePlayers().ToArray();
        if (handle.ActionType is LotusActionType.RoundStart or LotusActionType.RoundEnd)
        {
            alivePlayers = allCountedPlayers.Length;
        }
        if (allCountedPlayers.Any(p => !infectedPlayers.Contains(p.PlayerId))) return;

        indicatorRemotes.ForEach(remote => remote.Delete());
        var counterHolder = MyPlayer.NameModel().GetComponentHolder<CounterHolder>();
        if (counterHolder.Count > 0) counterHolder.RemoveAt(0);
        Game.CurrentGameMode.Assign(MyPlayer, Pestilence);

        Game.MatchData.GameHistory.AddEvent(new RoleChangeEvent(MyPlayer, Pestilence));
    }

    private IEnumerable<PlayerControl> GetAlivePlayers()
    {
        return Players.GetPlayers(PlayerFilter.Alive | PlayerFilter.NonPhantom)
            .Where(p => p.PlayerId != MyPlayer.PlayerId && Relationship(p) is not Relation.FullAllies);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddKillCooldownOptions(base.RegisterOptions(optionStream), key: "Infect Cooldown", name: Translations.Options.InfectCooldown);

    public override List<CustomRole> LinkedRoles() => base.LinkedRoles().Concat(new List<CustomRole>() { Pestilence }).ToList();

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.9f, 1f, 0.7f))
            .RoleAbilityFlags(RoleAbilityFlag.CannotVent | RoleAbilityFlag.CannotSabotage)
            .OptionOverride(new IndirectKillCooldown(KillCooldown));

    [Localized(nameof(PlagueBearer))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))]
        public static string ButtonText = "Infect";

        [Localized(nameof(InfectedHistoryMessage))]
        public static string InfectedHistoryMessage = "{0}::0 infected {1}::1.";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(InfectCooldown))]
            public static string InfectCooldown = "Infect Cooldown";
        }
    }

}
