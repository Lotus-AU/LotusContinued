using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Factions;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.NeutralKilling;
using Lotus.Extensions;
using Lotus.GUI.Name;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.Roles.Subroles;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.Roles.Editors;
using MonoMod.Utils;
using Lotus.GameModes.Standard;
using Lotus.Logging;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Copycat : CustomRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Copycat));
    /// <summary>
    /// A dict of role types and roles for the cat to fallback upon if the role cannot be copied properly (ex: Crewpostor bc Copycat cannot gain tasks)
    /// </summary>
    public static readonly Dictionary<Type, Func<CustomRole>> FallbackTypes = new();

    public bool KillerKnowsCopycat;
    private bool copyRoleProgress;
    private bool turned;

    public override bool CanVent() => false;

    protected override void PostSetup()
    {
        StandardRoles? roleHolder = StandardGameMode.Instance?.RoleManager?.RoleHolder ?? null;
        if (roleHolder != null)
        {
            if (FallbackTypes.Keys.Count != 0) return;
            FallbackTypes.AddRange(new Dictionary<Type, Func<CustomRole>>(){
            {typeof(CrewPostor), () => roleHolder.Static.Amnesiac },
            {typeof(Mafioso), () => roleHolder.Static.Amnesiac },
            {typeof(Snitch), () => roleHolder.Static.Amnesiac },
            {typeof(Postman), () => roleHolder.Static.Amnesiac },
            {typeof(Taskrunner), () => roleHolder.Static.Amnesiac },
        });
        }
        else log.Fatal("StandardGameMode.Instance?.RoleManager?.RoleHolder was null.");

    }

    [RoleAction(LotusActionType.Interaction)]
    protected void CopycatAttacked(PlayerControl actor, Interaction interaction, ActionHandle handle)
    {
        if (turned || interaction.Intent is not (IFatalIntent or Unstoppable.UnstoppableIntent)) return;
        turned = true;
        AssignRole(actor);
        handle.Cancel();
    }

    [RoleAction(LotusActionType.Shapeshift)]
    private void PreventShapeshift(ActionHandle handle) => handle.Cancel();

    private void AssignRole(PlayerControl attacker)
    {
        CustomRole? attackerRole;
        if (attacker.PrimaryRole().GetActions(LotusActionType.Attack).Any()) attackerRole = attacker.PrimaryRole();
        else attackerRole = attacker.GetSubroles().FirstOrDefault(sub => sub.GetActions(LotusActionType.Attack).Any());
        if (attackerRole == null)
        {
            if (!FallbackTypes.ContainsKey(attacker.PrimaryRole().GetType()))
            {
                turned = false;
                log.Debug($"{attacker.GetNameWithRole()} has no attack attributes. So we will just cancel the kill and do nothing.");
                return;
            }
            attackerRole = attacker.PrimaryRole();
        }
        FallbackTypes.GetOptional(attackerRole.GetType()).IfPresent(r => attackerRole = r());
        CustomRole role = copyRoleProgress ? attackerRole : ProjectLotus.GameModeManager.CurrentGameMode.RoleManager.GetCleanRole(attackerRole);

        log.Trace($"Copycat ({MyPlayer.name}) copying role of {attacker.name} : {role.RoleName}", "Copycat::AssignRole");
        Game.AssignRole(MyPlayer, role);

        role = MyPlayer.PrimaryRole();
        role.RoleColor = RoleColor;

        role.OverridenRoleName = Translations.CatFactionChangeName.Formatted(role.RoleName);
        RoleComponent roleComponent = MyPlayer.NameModel().GCH<RoleHolder>().Last();
        roleComponent.SetMainText(new LiveString(role.RoleName, RoleColor));
        roleComponent.AddViewer(attacker);

        if (attackerRole.Relationship(MyPlayer) is Relation.FullAllies) attacker.NameModel().GCH<RoleHolder>().LastOrDefault()?.AddViewer(MyPlayer);

        Game.MatchData.GameHistory.AddEvent(new RoleChangeEvent(MyPlayer, role, this));

        float killCooldown = role.GetOverride(Override.KillCooldown)?.GetValue() as float? ?? AUSettings.KillCooldown();
        role.SyncOptions(new[] { new GameOptionOverride(Override.KillCooldown, killCooldown * 2) });
        Async.Schedule(() =>
        {
            MyPlayer.RpcMark(MyPlayer);
            role.SyncOptions();
        }, NetUtils.DeriveDelay(0.05f));


        if (role.GetActions(LotusActionType.Shapeshift).Any() || role.RealRole is RoleTypes.Shapeshifter) return;

        log.Trace("Adding shapeshift action to base role", "Copycat::AssignRole");
        RoleAction action = this.GetActions(LotusActionType.Shapeshift).First().Item1.Clone();
        action.Executer = this;

        role.Editor = new BasicRoleEditor(role);
        role.Editor!.AddAction(action);

    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub2 => sub2.KeyName("Copy Role's Progress", Translations.Options.CopyRoleProgress)
                    .AddOnOffValues(false)
                    .BindBool(b => copyRoleProgress = b)
                    .Build())
            .SubOption(sub => sub.KeyName("Killer Knows Copycat", TranslationUtil.Colorize(Translations.Options.KillerKnowsCopycat, RoleColor))
                .AddOnOffValues()
                .BindBool(b => KillerKnowsCopycat = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor(new Color(1f, 0.7f, 0.67f))
            .DesyncRole(RoleTypes.Shapeshifter)
            .Faction(FactionInstances.Neutral)
            .RoleFlags(RoleFlag.CannotWinAlone)
            .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage)
            .SpecialType(SpecialType.Neutral)
            .IntroSound(AmongUs.GameOptions.RoleTypes.Shapeshifter)
            .OptionOverride(Override.ShapeshiftCooldown, 30f);

    [Localized(nameof(Copycat))]
    private static class Translations
    {
        [Localized(nameof(CatFactionChangeName))]
        public static string CatFactionChangeName = "{0}cat";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            public static string CopyAttackersRole = "Copy Attacker's Role";
            public static string CopyRoleProgress = "Copy Role's Progress";
            public static string KillerKnowsCopycat = "Killer Knows Copycat::0";
        }
    }
}