using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Factions.Undead;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Undead.Events;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Extensions;
using Lotus.Roles.Internals;
using UnityEngine;
using Lotus.API.Player;

namespace Lotus.Roles.RoleGroups.Undead.Roles;

public class UndeadRole : Impostor
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(UndeadRole));
    public static Color UndeadColor = new(0.33f, 0.46f, 0.76f);

    public override bool CanSabotage() => false;

    public void InitiateConvertToUndead(PlayerControl target)
    {
        CustomRole role = target.PrimaryRole();
        if (IsUnconvertedUndead(target)) return; //already converted
        List<PlayerControl> viewers = Players.GetAlivePlayers().Where(IsConvertedUndead).ToList();

        INameModel nameModel = target.NameModel();
        IndicatorComponent indicatorComponent = new(new LiveString("◎", new Color(0.46f, 0.58f, 0.6f)), Game.InGameStates, ViewMode.Additive, () => viewers);

        role.Faction = new TheUndead.Unconverted(role.Faction, nameModel.GetComponentHolder<IndicatorHolder>().Add(indicatorComponent));
        role.SpecialType = SpecialType.Undead;
        Game.MatchData.GameHistory.AddEvent(new InitiateEvent(MyPlayer, target));
    }

    public void FinishConversionToUndead(PlayerControl target)
    {
        CustomRole role = target.PrimaryRole();
        if (role.SpecialType != SpecialType.Undead) return;
        if (role.Faction is not TheUndead.Unconverted undeadFaction) return;
        List<PlayerControl> undead = Players.GetAlivePlayers().Where(IsConvertedUndead).ToList();

        undeadFaction.Indicator.Delete();

        INameModel nameModel = target.NameModel();
        role.Faction = FactionInstances.TheUndead;

        undead.ForEach(p =>
        {
            nameModel.GetComponentHolder<RoleHolder>()[0].AddViewer(p);
            p.NameModel().GetComponentHolder<RoleHolder>().Last().AddViewer(target);
        });
        Game.MatchData.GameHistory.AddEvent(new ConvertEvent(MyPlayer, target));
    }

    protected static bool IsUnconvertedUndead(PlayerControl player) => player.PrimaryRole().Faction is TheUndead.Unconverted;
    protected static bool IsConvertedUndead(PlayerControl player)
    {
        IFaction faction = player.PrimaryRole().Faction;
        if (faction is not TheUndead) return false;
        return faction is not TheUndead.Unconverted;
    }

    protected static bool IsUndeadOrigin(PlayerControl player) => player.PrimaryRole().Faction is TheUndead.Origin;

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .SpecialType(SpecialType.Undead)
            .Faction(FactionInstances.TheUndead);
}