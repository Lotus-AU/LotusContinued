using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.API.Stats;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Options;
using Lotus.Roles.Events;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Networking.RPC;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Crew;

public class Chameleon : Engineer, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Chameleon));
    private static IAccumulativeStatistic<int> _timesInvisible = Statistic<int>.CreateAccumulative($"Roles.{nameof(Chameleon)}.TimesInvisible", () => Translations.TimesInvisibleStatistic);
    public static readonly List<Statistic> ChameleonStatistics = new() { _timesInvisible };
    public override List<Statistic> Statistics() => ChameleonStatistics;

    private ReturnLocation returnLocation;
    private float invisibilityCooldown;
    private Cooldown invisibleTimer;

    private Optional<Vent> initialVent = null!;

    public RoleButton AbilityButton(IRoleButtonEditor abilityButton) => abilityButton
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/chameleon_disappear.png", 130, true));

    [UIComponent(UI.Text)]
    public string HiddenTimer() => invisibleTimer.Format(TranslationUtil.Colorize(Translations.HiddenText, RoleColor), autoFormat: true);

    [RoleAction(LotusActionType.VentEntered)]
    private void ChameleonEnterVent(Vent vent, ActionHandle handle)
    {
        if (invisibleTimer.NotReady())
        {
            handle.Cancel();
            return;
        }

        _timesInvisible.Update(MyPlayer.UniquePlayerId(), i => i + 1);
        initialVent = Optional<Vent>.Of(vent);
        invisibleTimer.StartThenRun(EndInvisibility);
        Game.MatchData.GameHistory.AddEvent(new GenericAbilityEvent(MyPlayer, $"{MyPlayer.name} became invisible as Chameleon."));
        Async.Schedule(() => RpcV3.Immediate(MyPlayer.MyPhysics.NetId, RpcCalls.BootFromVent).WritePacked(vent.Id).Send(MyPlayer.GetClientId()), NetUtils.DeriveDelay(0.5f));
    }

    private void EndInvisibility()
    {
        if (Game.State is not GameState.Roaming) return;
        int ventId = initialVent.Map(v => v.Id).OrElse(0);
        log.Trace($"Ending Invisible (ID: {ventId})");
        switch (returnLocation)
        {
            case ReturnLocation.Start:
                Async.Schedule(() => MyPlayer.MyPhysics.RpcBootFromVent(ventId), 0.4f);
                break;
            case ReturnLocation.Current:
                Vector2 currentLocation = MyPlayer.GetTruePosition();
                Async.Schedule(() => MyPlayer.MyPhysics.RpcBootFromVent(ventId), 0.4f);
                Async.Schedule(() => Utils.Teleport(MyPlayer.NetTransform, currentLocation, this), NetUtils.DeriveDelay(0.8f));
                break;
        }
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Invisibility Duration", Translations.Options.InvisibilityDuration)
                .AddFloatRange(0, 120, 2.5f, 4, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(invisibleTimer.SetDuration)
                .Build())
            .SubOption(sub => sub.KeyName("Invisibility Cooldown", Translations.Options.InvisibilityCooldown)
                .AddFloatRange(0, 120, 2.5f, 4, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => invisibilityCooldown = f)
                .Build())
            .SubOption(sub => sub
                .KeyName("Return Location on Invisibility End", Swooper.Translations.Options.ReturnLocation)
                .Value(v => v.Text(Swooper.Translations.Options.CurrentLocation).Color(Color.cyan).Value(0).Build())
                .Value(v => v.Text(Swooper.Translations.Options.StartLocation).Color(Color.blue).Value(1).Build())
                .BindInt(i => returnLocation = (ReturnLocation)i)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.59f, 1f, 0.71f))
            .OptionOverride(Override.EngVentCooldown, () => invisibleTimer.Duration + invisibilityCooldown);

    [Localized(nameof(Chameleon))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))] public static string ButtonText = "Conceal";
        [Localized(nameof(TimesInvisibleStatistic))] public static string TimesInvisibleStatistic = "Times Invisible";
        [Localized(nameof(HiddenText))] public static string HiddenText = "Hidden::0 {0}";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(InvisibilityDuration))] public static string InvisibilityDuration = "Invisibility Duration";
            [Localized(nameof(InvisibilityCooldown))] public static string InvisibilityCooldown = "Invisibility Cooldown";
        }
    }

    private enum ReturnLocation
    {
        Current,
        Start
    }
}