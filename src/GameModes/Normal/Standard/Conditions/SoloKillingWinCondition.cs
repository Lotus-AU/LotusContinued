using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.Factions;
using Lotus.Factions.Neutrals;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Internals.Enums;
using Lotus.Victory.Conditions;
using VentLib.Localization.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Standard.Conditions;

public class SoloKillingWinCondition : IWinCondition
{
    [Localized($"{ModConstants.Localization.WinConditions}.{nameof(SoloWin)}")]
    public static string SoloWin = "Killed All Other Players";

    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null;

        int aliveThatCanKill = 0;
        int alivePlayers = 0;
        List<CustomRole> aliveKillers = [];

        Players.GetAliveRoles().ForEach(r =>
        {
            alivePlayers++;
            if (r.RoleFlags.HasFlag(RoleFlag.CannotWinAlone)) return;
            if (r.RoleAbilityFlags.HasFlag(RoleAbilityFlag.IsAbleToKill))
            {
                if (r.Faction.Relationship(FactionInstances.Crewmates) is Relation.FullAllies && !GeneralOptions.GameplayOptions.CrewCanSoloKillers) return;
                aliveThatCanKill++;
            }
            if (r.Faction is not Neutral) return;
            if (r.SpecialType != SpecialType.NeutralKilling) return;
            aliveKillers.Add(r);
        });

        if (alivePlayers - aliveKillers.Count > 1 || aliveThatCanKill - aliveKillers.Count >= 1 || aliveKillers.Count == 0) return false;

        foreach (CustomRole killer in aliveKillers)
        {
            foreach (CustomRole killer2 in aliveKillers.Where(k => k.MyPlayer.PlayerId != killer.MyPlayer.PlayerId))
            {
                if (killer.Relationship(killer2) is Relation.None) return false;
            }
        }

        winners = aliveKillers.Select(k => k.MyPlayer).ToList();
        return true;
    }

    public WinReason GetWinReason() => new(ReasonType.SoloWinner, SoloWin);
}