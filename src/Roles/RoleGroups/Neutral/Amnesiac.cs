using System.Linq;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Extensions;
using Lotus.Factions.Crew;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using Object = UnityEngine.Object;
using Lotus.Logging;
using Lotus.GameModes.Standard;
using System.Collections.Generic;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Amnesiac : CustomRole, IVariableRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Amnesiac));
    private static Amalgamation _amalgamation = new();

    private bool stealExactRole;
    private bool hasArrowsToBodies;

    private Remote<IndicatorComponent>? arrowComponent;

    protected override void PostSetup()
    {
        if (!hasArrowsToBodies) return;
        IndicatorComponent? indicator = MyPlayer.NameModel().GCH<IndicatorHolder>().LastOrDefault();
        if (indicator == null) return;
        arrowComponent = MyPlayer.NameModel().GCH<IndicatorHolder>().GetRemote(indicator);
    }

    [UIComponent(UI.Indicator)]
    private string Arrows() => hasArrowsToBodies ? Object.FindObjectsOfType<DeadBody>()
        .Where(b => !Game.MatchData.UnreportableBodies.Contains(b.ParentId))
        .Select(b => RoleUtils.CalculateArrow(MyPlayer, b, RoleColor)).Fuse("") : "";

    [RoleAction(LotusActionType.ReportBody)]
    public void AmnesiacRememberAction(Optional<NetworkedPlayerInfo> reported, ActionHandle handle)
    {
        if (!reported.Exists()) return;
        log.Trace($"AmnesiacRememberAction | Reported: {reported.Get().GetNameWithRole()} | Self: {MyPlayer.name}", "");

        CustomRole targetRole = reported.Get().GetPrimaryRole()!;
        if (!ProjectLotus.AdvancedRoleAssignment) Copycat.FallbackTypes.GetOptional(targetRole.GetType()).IfPresent(r => targetRole = r());

        StandardRoles roleHolder = StandardGameMode.Instance.RoleManager.RoleHolder;

        if (!stealExactRole)
        {
            if (targetRole.SpecialType == SpecialType.NeutralKilling)
                targetRole = roleHolder.Static.Hitman;
            else if (targetRole.SpecialType == SpecialType.Neutral)
                targetRole = roleHolder.Static.Opportunist;
            else if (targetRole.Faction is Crewmates)
                targetRole = roleHolder.Static.Sheriff;
            else
                targetRole = roleHolder.Static.Impostor;
        }

        CustomRole newRole = StandardGameMode.Instance.RoleManager.GetCleanRole(targetRole);

        Game.AssignRole(MyPlayer, newRole);

        CustomRole role = MyPlayer.PrimaryRole();
        if (ProjectLotus.AdvancedRoleAssignment) role.Assign();
        else role.DesyncRole = RoleTypes.Impostor;

        arrowComponent?.Delete();
        handle.Cancel();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub.KeyName("Steals Exact Role", Translations.Options.StealsExactRole)
                .Bind(v => stealExactRole = (bool)v)
                .AddOnOffValues(false).Build())
            .SubOption(sub => sub.KeyName("Has Arrows to Bodies", Translations.Options.HasArrowsToBody)
                .AddOnOffValues()
                .BindBool(b => hasArrowsToBodies = b)
                .Build());

    protected override List<CustomRole> LinkedRoles() => base.LinkedRoles().Concat(new List<CustomRole>() { _amalgamation }).ToList();

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor(new Color(0.51f, 0.87f, 0.99f))
            .RoleFlags(RoleFlag.CannotWinAlone)
            .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.CannotVent)
            .SpecialType(SpecialType.Neutral)
            .VanillaRole(ProjectLotus.AdvancedRoleAssignment ? RoleTypes.Crewmate : RoleTypes.Impostor)
            .Faction(FactionInstances.Neutral);

    [Localized(nameof(Amnesiac))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(StealsExactRole))]
            public static string StealsExactRole = "Steals Exact Role";

            [Localized(nameof(HasArrowsToBody))]
            public static string HasArrowsToBody = "Has Arrows to Bodies";
        }
    }

    public CustomRole Variation() => _amalgamation;

    public bool AssignVariation() => RoleUtils.RandomSpawn(_amalgamation);
}