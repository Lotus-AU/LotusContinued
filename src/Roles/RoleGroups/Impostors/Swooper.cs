using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Extensions;
using Lotus.Options;
using UnityEngine;
using VentLib.Logging;
using VentLib.Networking.RPC;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Optionals;
using Lotus.API.Player;
using VentLib.Localization.Attributes;
using System.Numerics;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using Lotus.Utilities;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Swooper : Impostor, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Swooper));
    private ReturnLocation returnLocation;
    private bool remainInvisibleOnKill;
    private bool canBeSeenByAllied;
    private bool canVentNormally;

    [UIComponent(UI.Cooldown)]
    private Cooldown swoopingDuration = null!;

    [UIComponent(UI.Cooldown)]
    private Cooldown swooperCooldown = null!;
    private Optional<Vent> initialVent = null!;

    public RoleButton VentButton(IRoleButtonEditor ventButton) => ventButton
        .BindCooldown(swooperCooldown)
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Imp/swooper_disappear.png", 130, true));

    [RoleAction(LotusActionType.RoundStart)]
    private void OnRoundStart(bool gameStart)
    {
        if (MyPlayer.AmOwner) UIManager.VentButton.BindCooldown(swooperCooldown);
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateSwooper)?.Send([MyPlayer.OwnerId], true, gameStart);
        swooperCooldown.Start(gameStart ? 10 : float.MinValue);
    }

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (!remainInvisibleOnKill || swoopingDuration.IsReady()) return base.TryKill(target);
        MyPlayer.RpcMark(target);
        InteractionResult result = MyPlayer.InteractWith(target, new LotusInteraction(new FatalIntent(true, () => new DeathEvent(target, MyPlayer)), this));
        Game.MatchData.GameHistory.AddEvent(new KillEvent(MyPlayer, target, result is InteractionResult.Proceed));
        return result is InteractionResult.Proceed;
    }

    protected override void PostSetup()
    {
        MyPlayer.NameModel().GetComponentHolder<CooldownHolder>()[0].SetTextColor(new Color(0.2f, 0.63f, 0.29f));
        LiveString swoopingString = new(() => swoopingDuration.NotReady() ? "Swooping" : "", Color.red);
        MyPlayer.NameModel().GetComponentHolder<TextHolder>().Add(new TextComponent(swoopingString, [GameState.Roaming], viewers: GetUnaffected));
        MyPlayer.NameModel().GetComponentHolder<TextHolder>().Add(new TextComponent(LiveString.Empty, GameState.Roaming, ViewMode.Replace, MyPlayer));
    }

    [RoleAction(LotusActionType.VentEntered)]
    private void SwooperInvisible(Vent vent, ActionHandle handle)
    {
        if (swooperCooldown.NotReady() || swoopingDuration.NotReady())
        {
            if (canVentNormally) return;
            if (swoopingDuration.IsReady()) handle.Cancel();
            return;
        }

        List<PlayerControl> unaffected = GetUnaffected();
        initialVent = Optional<Vent>.Of(vent);

        if (MyPlayer.AmOwner) UIManager.VentButton.BindCooldown(swoopingDuration);
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateSwooper)?.Send([MyPlayer.OwnerId], false, false);
        swoopingDuration.StartThenRun(EndSwooping);
        Game.MatchData.GameHistory.AddEvent(new GenericAbilityEvent(MyPlayer, $"{MyPlayer.name} began swooping."));
        Async.Schedule(() => KickFromVent(vent, unaffected), NetUtils.DeriveDelay(0.4f));
    }

    private void KickFromVent(Vent vent, List<PlayerControl> unaffected)
    {
        if (MyPlayer.AmOwner) MyPlayer.MyPhysics.BootFromVent(vent.Id);
        RpcV3.Immediate(MyPlayer.MyPhysics.NetId, RpcCalls.BootFromVent).WritePacked(vent.Id).SendInclusive(unaffected.Select(p => p.GetClientId()).ToArray());
    }

    private void EndSwooping()
    {
        if (Game.State is not GameState.Roaming) return;
        int ventId = initialVent.Map(v => v.Id).OrElse(0);
        log.Trace($"Ending Swooping (ID: {ventId})");
        switch (returnLocation)
        {
            case ReturnLocation.Start:
                Async.Schedule(() => MyPlayer.MyPhysics.RpcBootFromVent(ventId), NetUtils.DeriveDelay(0.4f));
                break;
            case ReturnLocation.Current:
                UnityEngine.Vector2 currentLocation = MyPlayer.GetTruePosition();
                Async.Schedule(() => MyPlayer.MyPhysics.RpcBootFromVent(ventId), NetUtils.DeriveDelay(0.4f));
                Async.Schedule(() => Utils.Teleport(MyPlayer.NetTransform, currentLocation, this), NetUtils.DeriveDelay(0.8f));
                break;
        }
        if (MyPlayer.AmOwner) UIManager.VentButton.BindCooldown(swooperCooldown);
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateSwooper)?.Send([MyPlayer.OwnerId], true, false);
        swooperCooldown.Start();
    }

    [RoleAction(LotusActionType.RoundEnd)]
    private void EndSwoopOnRoundEnd()
    {
        swooperCooldown.Finish();
        swoopingDuration.Finish(true);
    }

    [ModRPC((uint)ModCalls.UpdateSwooper, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateSwooper(bool useCooldown, bool gameStart)
    {
        Swooper? swooper = PlayerControl.LocalPlayer.PrimaryRole<Swooper>();
        if (swooper == null) return;
        swooper.UIManager.VentButton.BindCooldown(useCooldown ? swooper.swooperCooldown : swooper.swoopingDuration);
        float targetDur = gameStart ? 10 : float.MinValue;
        if (useCooldown) swooper.swooperCooldown.Start(targetDur);
        else swooper.swoopingDuration.Start(targetDur);
    }

    private List<PlayerControl> GetUnaffected() => Players.GetAllPlayers().Where(p => !p.IsAlive() || canBeSeenByAllied && p.Relationship(MyPlayer) is Relation.FullAllies).AddItem(MyPlayer).ToList();

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => base.RegisterOptions(optionStream)
        .SubOption(sub => sub
            .KeyName("Invisibility Cooldown", Translations.Options.InvisibilityCooldown)
            .AddFloatRange(5, 120, 2.5f, 16, GeneralOptionTranslations.SecondsSuffix)
            .BindFloat(swooperCooldown.SetDuration)
            .Build())
        .SubOption(sub => sub
            .KeyName("Swooping Duration", Translations.Options.SwoopingDuration)
            .AddFloatRange(5, 60, 1f, 5, GeneralOptionTranslations.SecondsSuffix)
            .BindFloat(swoopingDuration.SetDuration)
            .Build())
        .SubOption(sub => sub
            .KeyName("Can be Seen By Allies", Translations.Options.SeenByAllies)
            .AddBoolean()
            .BindBool(b => canBeSeenByAllied = b)
            .Build())
        .SubOption(sub => sub
            .KeyName("Can Vent During Cooldown", Translations.Options.VentDuringCooldown)
            .AddBoolean(false)
            .BindBool(b => canVentNormally = b)
            .Build())
        .SubOption(sub => sub
            .KeyName("Remain Invisible on Kill", Translations.Options.InvisibleOnKill)
            .AddBoolean()
            .BindBool(b => remainInvisibleOnKill = b)
            .Build())
        .SubOption(sub => sub
            .KeyName("Return Location on Invisibility End", Translations.Options.ReturnLocation)
            .Value(v => v.Text(Translations.Options.CurrentLocation).Color(Color.cyan).Value(0).Build())
            .Value(v => v.Text(Translations.Options.StartLocation).Color(Color.blue).Value(1).Build())
            .BindInt(i => returnLocation = (ReturnLocation)i)
            .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(new IndirectKillCooldown(KillCooldown, () => remainInvisibleOnKill && swoopingDuration.NotReady()));

    [Localized(nameof(Swooper))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))] public static string ButtonText = "Swoop";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(ReturnLocation))] public static string ReturnLocation = "Return Location on Invisibility End";
            [Localized(nameof(InvisibilityCooldown))] public static string InvisibilityCooldown = "Invisibility Cooldown";
            [Localized(nameof(VentDuringCooldown))] public static string VentDuringCooldown = "Can Vent During Cooldown";
            [Localized(nameof(InvisibleOnKill))] public static string InvisibleOnKill = "Remain Invisible on Kill";
            [Localized(nameof(SwoopingDuration))] public static string SwoopingDuration = "Swooping Duration";
            [Localized(nameof(SeenByAllies))] public static string SeenByAllies = "Can Be Seen By Allies";

            [Localized(nameof(CurrentLocation))] public static string CurrentLocation = "Current";
            [Localized(nameof(StartLocation))] public static string StartLocation = "Start";
        }
    }

    private enum ReturnLocation
    {
        Current,
        Start
    }
}