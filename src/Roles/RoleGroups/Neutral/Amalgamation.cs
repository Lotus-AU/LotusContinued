using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Subroles;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Object = UnityEngine.Object;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Amalgamation : CustomRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Amalgamation));
    public static HashSet<Type> AmalgamationBannedModifier = new() { typeof(Oblivious), typeof(Sleuth) };
    public override HashSet<Type> BannedModifiers() => AmalgamationBannedModifier;

    private int maxRoles;
    private bool hasArrowsToBodies;
    private bool absorbModifiers;
    private bool newestRoleWinCondition;

    private List<Color> colorGradient;
    [NewOnSetup] private HashSet<byte> personalUneportableBodies = new();

    private bool assignedRole;

    private Remote<TextComponent>? textRemote;

    [UIComponent(UI.Indicator)]
    private string Arrows() => hasArrowsToBodies ? Object.FindObjectsOfType<DeadBody>()
        .Where(b => !Game.MatchData.UnreportableBodies.Contains(b.ParentId))
        .Select(b => RoleUtils.CalculateArrow(MyPlayer, b, RoleColor)).Fuse("") : "";

    protected override void PostSetup()
    {
        colorGradient = new List<Color> { new(0.51f, 0.87f, 0.99f) };
        RoleComponent rc = MyPlayer.NameModel().GCH<RoleHolder>().Last();
        RoleColorGradient = new ColorGradient(colorGradient.ToArray());
        rc.SetMainText(new LiveString(() => RoleColorGradient.Apply(RoleName)));
    }

    [RoleAction(LotusActionType.ReportBody, priority: Priority.First)]
    public void AmalgamationConsume(Optional<NetworkedPlayerInfo> reported, ActionHandle handle)
    {
        if (!reported.Exists()) return;
        log.Trace($" Reported: {reported.Get().GetNameWithRole()} | Self: {MyPlayer.name}", "");

        if (maxRoles != 0 && MyPlayer.GetSubroles().Count(sr => !sr.RoleFlags.HasFlag(RoleFlag.IsSubrole)) >= maxRoles) return;
        if (personalUneportableBodies.Contains(reported.Get().PlayerId)) return;
        personalUneportableBodies.Add(reported.Get().PlayerId);

        CustomRole targetRole = reported.Get().GetPrimaryRole()!;
        Copycat.FallbackTypes.GetOptional(targetRole.GetType()).IfPresent(r => targetRole = r());
        CustomRole newRole = ProjectLotus.GameModeManager.CurrentGameMode.RoleManager.GetCleanRole(targetRole);

        colorGradient.Add(newRole.RoleColor);
        RoleColorGradient = new ColorGradient(colorGradient.ToArray());
        if (newestRoleWinCondition || assignedRole == false)
        {
            assignedRole = true;
            CustomRole myPlayerRole = MyPlayer.PrimaryRole();
            this.ChangeRoleTo(newRole, false);
            MyPlayer.GetSubroles().Add(myPlayerRole);
            textRemote?.Delete();
            textRemote = MyPlayer.NameModel().GCH<TextHolder>().Add(new TextComponent(new LiveString(newRole.RoleName, myPlayerRole.RoleColor), Game.InGameStates, ViewMode.Overriden, MyPlayer));
            MyPlayer.NameModel().GCH<RoleHolder>().RemoveAt(MyPlayer.NameModel().GCH<RoleHolder>().Count - 1);
        }
        else
        {
            Game.AssignSubRole(MyPlayer, newRole);
            if (newRole is ISubrole) MyPlayer.NameModel().GCH<SubroleHolder>().RemoveLast();
            else MyPlayer.NameModel().GCH<RoleHolder>().RemoveLast();
        }

        CustomRole role = MyPlayer.PrimaryRole();
        role.DesyncRole = RoleTypes.Impostor;
        handle.Cancel();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Has Arrows To Bodies", Amnesiac.Translations.Options.HasArrowsToBody)
                .AddBoolean(false)
                .BindBool(b => hasArrowsToBodies = b)
                .Build())
            .SubOption(sub => sub.KeyName("Win Condition Determiner", Translations.Options.WinConditionDeterminer)
                .Value(v => v.Text(Translations.Options.OldestRoleValue).Value(false).Build())
                .Value(v => v.Text(Translations.Options.NewestRoleValue).Value(true).Build())
                .BindBool(b => newestRoleWinCondition = b)
                .Build())
            .SubOption(sub => sub.KeyName("Max Absorbed Roles", Translations.Options.MaxAbsorbedRoles)
                .Value(v => v.Text(ModConstants.Infinity).Color(ModConstants.Palette.InfinityColor).Value(0).Build())
                .AddIntRange(1, 20, 1, 0)
                .BindInt(i => maxRoles = i)
                .Build())
            .SubOption(sub => sub.KeyName("Absorbs Modifiers", Translations.Options.AbsorbModifiers)
                .AddBoolean(false)
                .BindBool(b => absorbModifiers = b)
                .Build());

    public override RoleType GetRoleType() => RoleType.Variation;

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleColor(new Color(0.51f, 0.87f, 0.99f))
            .RoleFlags(RoleFlag.VariationRole | RoleFlag.CannotWinAlone)
            .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.CannotVent)
            .SpecialType(SpecialType.Neutral)
            .DesyncRole(ProjectLotus.AdvancedRoleAssignment ? RoleTypes.Impostor : RoleTypes.Crewmate)
            .Faction(FactionInstances.Neutral);

    [Localized(nameof(Amalgamation))]
    private static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(WinConditionDeterminer))]
            public static string WinConditionDeterminer = "Win Condition Determiner";

            [Localized(nameof(OldestRoleValue))]
            public static string OldestRoleValue = "Oldest Role";

            [Localized(nameof(NewestRoleValue))]
            public static string NewestRoleValue = "Newest Role";

            [Localized(nameof(MaxAbsorbedRoles))]
            public static string MaxAbsorbedRoles = "Max Absorbed Roles";

            [Localized(nameof(AbsorbModifiers))]
            public static string AbsorbModifiers = "Absorb Modifiers";
        }
    }
}