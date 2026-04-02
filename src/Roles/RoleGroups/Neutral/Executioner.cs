#nullable enable
using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Factions.Impostors;
using Lotus.Factions.Interfaces;
using Lotus.Factions.Neutrals;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Victory.Conditions;
using Lotus.Extensions;
using Lotus.Roles.Internals;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;
using Lotus.API.Player;
using Lotus.GameModes.Normal.Standard;
using Lotus.Managers.History.Events;
using VentLib.Localization;
using VentLib.Localization.Attributes;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Executioner : CustomRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Executioner));
    private bool canTargetImpostors;
    private bool canTargetNeutrals;
    private int roleChangeWhenTargetDies;

    private PlayerControl? target;

    [RoleAction(LotusActionType.RoundStart)]
    private void OnGameStart(bool gameStart)
    {
        if (!gameStart) return;
        target = Players.GetAllPlayers().Where(p =>
        {
            if (p.PlayerId == MyPlayer.PlayerId) return false;
            IFaction faction = p.PrimaryRole().Faction;
            if (!canTargetImpostors && faction is ImpostorFaction) return false;
            return canTargetNeutrals || faction is not Lotus.Factions.Neutrals.Neutral;
        }).ToList().GetRandom();
        log.Trace($"Executioner ({MyPlayer.name}) Target: {target}");

        target.NameModel().GetComponentHolder<NameHolder>().Add(new ColoredNameComponent(target, RoleColor, Game.InGameStates, MyPlayer));
    }

    [RoleAction(LotusActionType.Exiled, ActionFlag.GlobalDetector)]
    private void CheckExecutionerWin(PlayerControl exiled)
    {
        if (target == null || target.PlayerId != exiled.PlayerId || !MyPlayer.IsAlive()) return;
        List<PlayerControl> winners = new() { MyPlayer };
        if (target.PrimaryRole() is Jester) winners.Add(target);
        ManualWin win = new(winners, ReasonType.SoloWinner);
        win.Activate();
    }

    [RoleAction(LotusActionType.Disconnect, ActionFlag.GlobalDetector)]
    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void CheckChangeRole(PlayerControl dead)
    {
        if (roleChangeWhenTargetDies == 0 || target == null || target.PlayerId != dead.PlayerId) return;

        NormalStandardRoles roleHolder = NormalStandardRoles.Instance;
        CustomRole newRole = ((ExeRoleChange)roleChangeWhenTargetDies) switch
        {
            ExeRoleChange.Jester => roleHolder.Static.Jester,
            ExeRoleChange.Opportunist => roleHolder.Static.Opportunist,
            ExeRoleChange.SchrodingerCat => roleHolder.Static.SchrodingersCat,
            ExeRoleChange.Crewmate => roleHolder.Static.Crewmate,
            ExeRoleChange.Amnesiac => roleHolder.Static.Amnesiac,
            ExeRoleChange.Survivor => roleHolder.Static.Survivor,
            ExeRoleChange.Madmate => roleHolder.Static.Madmate,
            _ => this
        };

        target = null;
        if (newRole == this) return;
        this.ChangeRoleTo(newRole);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub
                .KeyName("Can Target Impostors", Translations.Options.TargetImpostors)
                .Bind(v => canTargetImpostors = (bool)v)
                .AddBoolean(false).Build())
            .SubOption(sub => sub
                .KeyName("Can Target Neutrals", Translations.Options.TargetNeutrals)
                .Bind(v => canTargetNeutrals = (bool)v)
                .AddBoolean(false).Build())
            .SubOption(sub => sub
                .KeyName("Role Change When Target Dies", Translations.Options.RoleChange)
                .Bind(v => roleChangeWhenTargetDies = (int)v)
                .Value(v => v.Text(Localizer.Translate("Roles.Jester.RoleName")).Value(1).Color(new Color(0.93f, 0.38f, 0.65f)).Build())
                .Value(v => v.Text(Localizer.Translate("Roles.Opportunist.RoleName")).Value(2).Color(Color.green).Build())
                .Value(v => v.Text(Localizer.Translate("Roles.SchrodingersCat.RoleName")).Value(3).Color(new Color(0.41f, 0.41f, 0.41f)).Build())
                .Value(v => v.Text(Localizer.Translate("Roles.Crewmate.RoleName")).Value(4).Color(new Color(0.71f, 0.94f, 1f)).Build())
                .Value(v => v.Text(Localizer.Translate("Roles.Amnesiac.RoleName")).Value(5).Color(new Color(0.51f, 0.87f, 0.99f)).Build())
                .Value(v => v.Text(Localizer.Translate("Roles.Survivor.RoleName")).Value(6).Color(new Color(1f, 0.9f, 0.3f)).Build())
                .Value(v => v.Text(Localizer.Translate("Roles.Madmate.RoleName")).Value(7).Color(ModConstants.Palette.MadmateColor).Build())
                .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleColor(new Color(0.55f, 0.17f, 0.33f))
            .Faction(FactionInstances.Neutral)
            .RoleFlags(RoleFlag.CannotWinAlone)
            .SpecialType(SpecialType.Neutral)
            .IntroSound(AmongUs.GameOptions.RoleTypes.Shapeshifter);

    private enum ExeRoleChange
    {
        None,
        Jester,
        Opportunist,
        SchrodingerCat,
        Crewmate,
        Amnesiac,
        Survivor,
        Madmate
    }

    [Localized(nameof(Executioner))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(TargetImpostors))]
            public static string TargetImpostors = "Can Target Impostors";

            [Localized(nameof(TargetNeutrals))]
            public static string TargetNeutrals = "Can Target Neutrals";

            [Localized(nameof(RoleChange))]
            public static string RoleChange = "Role Change When Target Dies";
        }
    }
}