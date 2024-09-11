#nullable enable
using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.API.Player;
using Lotus.GameModes.Standard;
using Lotus.Factions;
using VentLib.Localization.Attributes;
using static Lotus.Roles.RoleGroups.Neutral.Archangel.Translations;
using static Lotus.Roles.RoleGroups.Neutral.Archangel.Translations.Options;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using VentLib.Utilities.Collections;
using Lotus.Victory;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Archangel : CustomRole
{
    [UIComponent(UI.Cooldown)]
    private Cooldown protectCooldown = null!;

    private Cooldown protectDuration = null!;

    private bool TargetKnowsArchangelExists;
    private bool ArchangelKnowsTargetRole;
    private ArchangelRoleChange roleChangeWhenTargetDies;
    private List<InteractionCancels> cancels = new();
    private bool shouldCancelAllInteractions;

    private PlayerControl? target;
    private Remote<IndicatorComponent> indicatorRemote = null!;

    private static string Identifier = "<b>△</b>"; // this is the symbol that shows up next to the target's name when they know there is an Archangel

    [UIComponent(UI.Text)]
    private string TargetDisplay() => target == null ? "" : RoleColor.Colorize("Target: ") + Color.white.Colorize(target.name);

    protected override void PostSetup()
    {
        base.PostSetup();
        List<PlayerControl> eligiblePlayers = Players.GetAllPlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).ToList();
        if (eligiblePlayers.Any()) target = eligiblePlayers.GetRandom();

        if (target == null) return;
        MyPlayer.NameModel().GetComponentHolder<CooldownHolder>().Add(new CooldownComponent(protectDuration, GameState.Roaming, cdText: "Dur: ", viewers: MyPlayer));

        if (ArchangelKnowsTargetRole)
        {
            var roleHolder = target.NameModel().GetComponentHolder<RoleHolder>();
            roleHolder.AddListener(component => component.AddViewer(MyPlayer));
            roleHolder.Components().ForEach(components => components.AddViewer(MyPlayer));
        }
        PlayerControl[] viewers;
        if (TargetKnowsArchangelExists)
            viewers = [MyPlayer, target];
        else
            viewers = [MyPlayer];

        indicatorRemote = target.NameModel().GCH<IndicatorHolder>().Add(new IndicatorComponent(new LiveString(Identifier, RoleColor), [GameState.Roaming, GameState.InMeeting], viewers: viewers));
        Game.GetWinDelegate().AddSubscriber(OnGameEnd);
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void Restart(bool gameStart)
    {
        protectCooldown.Start(gameStart ? 10f : float.MinValue);
        protectDuration.Finish();
    }

    [RoleAction(LotusActionType.OnPet)]
    private void OnPet()
    {
        if (protectCooldown.NotReady() || target == null || protectDuration.NotReady()) return;
        protectDuration.StartThenRun(() => protectCooldown.Start());
        SendProtection();
    }

    [RoleAction(LotusActionType.Interaction, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath)]
    private void OnInteraction(PlayerControl targetedPlayer, PlayerControl actor, Interaction interaction, ActionHandle handle)
    {
        if (protectDuration.IsReady() || target == null) return;
        if (targetedPlayer.PlayerId != target.PlayerId) return;
        bool shouldCancel = false;

        switch (interaction.Intent)
        {
            case IHostileIntent when ContainsInteraction(InteractionCancels.Hostile):
            case NeutralIntent when ContainsInteraction(InteractionCancels.Neutral):
            case IFatalIntent when ContainsInteraction(InteractionCancels.Fatal):
                shouldCancel = true;
                break;
        }

        switch (interaction)
        {
            case IndirectInteraction when ContainsInteraction(InteractionCancels.Indirect):
            case DelayedInteraction when ContainsInteraction(InteractionCancels.Delayed):
            case RangedInteraction when ContainsInteraction(InteractionCancels.Ranged):
                shouldCancel = true;
                break;
        }

        if (!shouldCancel) return;
        Game.MatchData.GameHistory.AddEvent(new GenericAbilityEvent(MyPlayer, $"{Game.GetName(MyPlayer)} protected {target.GetNameWithRole()} from {actor.GetNameWithRole()}."));
        handle.Cancel();
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void PlayerDeath(PlayerControl killed, PlayerControl killer)
    {
        if (!MyPlayer.IsAlive() || target == null || target.PlayerId != killed.PlayerId) return;
        target = null;
        indicatorRemote?.Delete();
        if (roleChangeWhenTargetDies is ArchangelRoleChange.None) return;

        StandardRoles roleHolder = StandardGameMode.Instance.RoleManager.RoleHolder;

        switch (roleChangeWhenTargetDies)
        {
            case ArchangelRoleChange.Jester:
                Game.AssignRole(MyPlayer, roleHolder.Static.Jester);
                break;
            case ArchangelRoleChange.Opportunist:
                Game.AssignRole(MyPlayer, roleHolder.Static.Opportunist);
                break;
            case ArchangelRoleChange.SchrodingerCat:
                Game.AssignRole(MyPlayer, roleHolder.Static.Copycat);
                break;
            case ArchangelRoleChange.Crewmate:
                Game.AssignRole(MyPlayer, roleHolder.Static.Crewmate);
                break;
            case ArchangelRoleChange.None:
            default:
                break;
        }
    }

    private void SendProtection()
    {
        if (!TargetKnowsArchangelExists) return;
        GameOptionOverride[] overrides = { new(Override.GuardianAngelDuration, protectDuration.Duration) };
        if (target == null) return;
        target.PrimaryRole().SyncOptions(overrides);
        target.RpcProtectPlayer(target, 0);
    }

    private bool ContainsInteraction(InteractionCancels cancelCheck)
    {
        if (shouldCancelAllInteractions) return true;
        else return cancels.Contains(cancelCheck);
    }

    private void OnGameEnd(WinDelegate winDelegate)
    {
        if (MyPlayer.PrimaryRole() is not Archangel) return;
        if (target == null) return;
        if (winDelegate.GetWinners().Any(p => p.PlayerId == target.PlayerId)) winDelegate.AddAdditionalWinner(MyPlayer);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub
                .KeyName("Protect Duration", ProtectDuration)
                .BindFloat(v => protectDuration.Duration = v)
                .AddFloatRange(2.5f, 180f, 2.5f, 5, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Protect Cooldown", ProtectCooldown)
                .BindFloat(v => protectCooldown.Duration = v)
                .AddFloatRange(2.5f, 180f, 2.5f, 11, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Target Knows They have an Archangel", TargetKnowsArchAngelExists)
                .Bind(v => TargetKnowsArchangelExists = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .KeyName("Archangel Knows Target Role", ArchAngelKnowsTargetRole)
                .Bind(v => ArchangelKnowsTargetRole = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .KeyName("Role Change When Target Dies", RoleChangeWhenTargetDies)
                .BindInt(v => roleChangeWhenTargetDies = (ArchangelRoleChange)v)
                .Value(v => v.Text("Jester").Value(1).Color(new Color(0.93f, 0.38f, 0.65f)).Build())
                .Value(v => v.Text("Opportunist").Value(2).Color(Color.green).Build())
                .Value(v => v.Text("Schrodinger's Cat").Value(3).Color(Color.black).Build())
                .Value(v => v.Text("Crewmate").Value(4).Color(new Color(0.71f, 0.94f, 1f)).Build())
                .Value(v => v.Text("Off").Value(0).Color(Color.red).Build())
                .Build())
            .SubOption(sub => sub
                .KeyName("Should Cancel Which Interactions", ShouldCancelWhichInteractions)
                .Value(v => v.Text(GeneralOptionTranslations.AllText).Color(Color.green).Value(true).Build())
                .Value(v => v.Text(GeneralOptionTranslations.CustomText).Color(new Color(0.73f, 0.58f, 1f)).Value(false).Build())
                .BindBool(v => shouldCancelAllInteractions = v)
                .ShowSubOptionPredicate(v => (bool)v == false)
                .SubOption(sub1 => sub1
                    .KeyName("Cancel Indirect", InteractionFormatter.Formatted(Indirect))
                    .AddBoolean()
                    .BindBool(RoleUtils.BindOnOffListSetting(cancels, InteractionCancels.Indirect))
                    .Build())
                .SubOption(sub1 => sub1
                    .KeyName("Cancel Ranged", InteractionFormatter.Formatted(Ranged))
                    .AddBoolean()
                    .BindBool(RoleUtils.BindOnOffListSetting(cancels, InteractionCancels.Ranged))
                    .Build())
                .SubOption(sub1 => sub1
                    .KeyName("Cancel Delayed", InteractionFormatter.Formatted(Delayed))
                    .AddBoolean()
                    .BindBool(RoleUtils.BindOnOffListSetting(cancels, InteractionCancels.Delayed))
                    .Build())
                .SubOption(sub1 => sub1
                    .KeyName("Cancel Neutral", InteractionFormatter.Formatted(Translations.Options.Neutral))
                    .AddBoolean()
                    .BindBool(RoleUtils.BindOnOffListSetting(cancels, InteractionCancels.Neutral))
                    .Build())
                .SubOption(sub1 => sub1
                    .KeyName("Cancel Fatal", InteractionFormatter.Formatted(Fatal))
                    .AddBoolean()
                    .BindBool(RoleUtils.BindOnOffListSetting(cancels, InteractionCancels.Fatal))
                    .Build())
                .SubOption(sub1 => sub1
                    .KeyName("Cancel Hostile", InteractionFormatter.Formatted(Hostile))
                    .AddBoolean()
                    .BindBool(RoleUtils.BindOnOffListSetting(cancels, InteractionCancels.Hostile))
                    .Build())
                .Build()
            );

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .SpecialType(SpecialType.Neutral)
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet)
            .RoleFlags(RoleFlag.CannotWinAlone)
            .Faction(FactionInstances.Neutral)
            .RoleColor("#B3FFFF")
            .VanillaRole(AmongUs.GameOptions.RoleTypes.Crewmate);

    private enum ArchangelRoleChange
    {
        None,
        Jester,
        Opportunist,
        SchrodingerCat,
        Crewmate,
        Fatal
    }

    private enum InteractionCancels
    {
        Indirect,
        Ranged,
        Delayed,
        Neutral,
        Fatal,
        Hostile
    }

    [Localized(nameof(Archangel))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(ProtectDuration))] public static string ProtectDuration = "Protect Duration";
            [Localized(nameof(ProtectCooldown))] public static string ProtectCooldown = "Protect Cooldown";
            [Localized(nameof(TargetKnowsArchAngelExists))] public static string TargetKnowsArchAngelExists = "Target Knows They have an Archangel";
            [Localized(nameof(ArchAngelKnowsTargetRole))] public static string ArchAngelKnowsTargetRole = "Archangel Knows Target Role";
            [Localized(nameof(RoleChangeWhenTargetDies))] public static string RoleChangeWhenTargetDies = "Role Change When Target Dies";
            [Localized(nameof(ShouldCancelWhichInteractions))] public static string ShouldCancelWhichInteractions = "Should Protect Which Interactions";

            [Localized(nameof(InteractionFormatter))] public static string InteractionFormatter = "Cancel {0} Interactions";

            [Localized(nameof(Indirect))] public static string Indirect = "Indirect";
            [Localized(nameof(Ranged))] public static string Ranged = "Ranged";
            [Localized(nameof(Delayed))] public static string Delayed = "Delayed";
            [Localized(nameof(Neutral))] public static string Neutral = "Neutral";
            [Localized(nameof(Fatal))] public static string Fatal = "Fatal";
            [Localized(nameof(Hostile))] public static string Hostile = "Hostile";
        }
    }
}