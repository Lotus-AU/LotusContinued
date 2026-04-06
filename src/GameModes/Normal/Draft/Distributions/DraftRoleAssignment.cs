extern alias JBAnnotations;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions.Impostors;
using Lotus.GameModes.Normal.Standard.Distributions;
using Lotus.GameModes.Normal.Standard.Lotteries;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Roles.Interfaces;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Subroles;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Draft.Distributions;

public class DraftRoleAssignment
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(DraftRoleAssignment));
    public static DraftRoleAssignment Instance = null!;
    public DraftRoleAssignment()
    {
        Instance = this;
    }

    public List<CustomRole> AssignRoles(int playerCount, int excessRoles)
    {
        RoleDistribution roleDistribution = GeneralOptions.GameplayOptions.OptimizeRoleAssignment
            ? OptimizeRoleAlgorithm.OptimizeDistribution()
            : OptimizeRoleAlgorithm.NonOptimizedDistribution();

        List<CustomRole> possibleRoles = [];

        ImpostorLottery impostorLottery = new();
        int impostorCount = 0;
        int madmateCount = 0;

        while (impostorCount < roleDistribution.Impostors || madmateCount < roleDistribution.MinimumMadmates)
        {
            CustomRole role = impostorLottery.Next();
            if (role.GetType() == typeof(Impostor) && impostorLottery.HasNext()) continue;

            if (role.Faction is Madmates)
            {
                if (madmateCount >= roleDistribution.MaximumMadmates) continue;
                possibleRoles.Add(IVariableRole.PickAssignedRole(role));
                madmateCount++;
                playerCount--;
                if (RoleOptions.MadmateOptions.MadmatesTakeImpostorSlots) impostorCount++;
                continue;
            }

            if (impostorCount >= roleDistribution.Impostors)
            {
                if (!impostorLottery.HasNext()) break;
                continue;
            }

            possibleRoles.Add(IVariableRole.PickAssignedRole(role));
            impostorCount++;
            playerCount--;
        }

        // =====================

        // ASSIGN NEUTRAL KILLING ROLES
        NeutralKillingLottery neutralKillingLottery = new();
        int nkRoles = 0;
        int loops = 0;
        while (nkRoles < roleDistribution.MaximumNeutralKilling)
        {
            if (loops > 0 && nkRoles >= roleDistribution.MinimumNeutralKilling) break;
            CustomRole role = neutralKillingLottery.Next();
            if (role is IllegalRole)
            {
                if (nkRoles >= roleDistribution.MinimumNeutralKilling || loops >= 10) break;
                loops++;
                if (!neutralKillingLottery.HasNext())
                    neutralKillingLottery = new NeutralKillingLottery(); // Refresh the lottery again to fulfill the minimum requirement
                continue;
            }
            possibleRoles.Add(IVariableRole.PickAssignedRole(role));
            nkRoles++;
            playerCount--;
        }

        // --------------------------

        // ASSIGN NEUTRAL PASSIVE ROLES
        log.Debug("Assigning Neutral Passive Roles");

        NeutralLottery neutralLottery = new();
        int neutralRoles = 0;
        loops = 0;
        while (neutralRoles < roleDistribution.MaximumNeutralPassive)
        {
            if (loops > 0 && neutralRoles >= roleDistribution.MinimumNeutralPassive) break;
            CustomRole role = neutralLottery.Next();
            if (role is IllegalRole)
            {
                if (neutralRoles >= roleDistribution.MinimumNeutralPassive || loops >= 10) break;
                loops++;
                if (!neutralLottery.HasNext())
                    neutralLottery = new NeutralLottery(); // Refresh the lottery again to fulfill the minimum requirement
                continue;
            }
            possibleRoles.Add(IVariableRole.PickAssignedRole(role));
            neutralRoles++;
            playerCount--;
        }

        // =====================

        // ASSIGN CREWMATE ROLES
        CrewmateLottery crewmateLottery = new();
        while ((playerCount + excessRoles) > 0)
        {
            CustomRole role = crewmateLottery.Next();
            if (role.GetType() == typeof(Crewmate) && crewmateLottery.HasNext()) continue;
            possibleRoles.Add(IVariableRole.PickAssignedRole(role));
            playerCount--;
        }


        return possibleRoles;
    }

    public void AssignSubroles(List<PlayerControl> allPlayers)
    {
        if (RoleOptions.SubroleOptions.ModifierLimits == 0) return;
        log.Debug("Assigning Subroles...");
        SubRoleLottery subRoleLottery = new();

        int evenDistribution = RoleOptions.SubroleOptions.EvenlyDistributeModifiers ? 0 : 9999;

        bool CanAssignTo(PlayerControl player)
        {
            int count = player.GetSubroles().Count;
            if (count > evenDistribution) return false;
            return RoleOptions.SubroleOptions.UncappedModifiers || count < RoleOptions.SubroleOptions.ModifierLimits;
        }

        while (subRoleLottery.HasNext())
        {
            CustomRole role = subRoleLottery.Next();
            if (role is IllegalRole) continue;
            CustomRole variant = role is Subrole sr ? IVariantSubrole.PickAssignedRole(sr) : IVariableRole.PickAssignedRole(role);
            if (variant is IRoleCandidate candidate)
                if (candidate.ShouldSkip()) continue;
            List<PlayerControl> players = Players.GetAllPlayers().Where(CanAssignTo).ToList();
            if (players.Count == 0)
            {
                evenDistribution++;
                if (!RoleOptions.SubroleOptions.UncappedModifiers && evenDistribution >= RoleOptions.SubroleOptions.ModifierLimits) break;
                players = Players.GetAllPlayers().Where(p => p.GetSubroles().Count <= evenDistribution).ToList();
                if (players.Count == 0) break;
            }

            bool assigned = false;
            while (players.Count > 0 && !assigned)
            {
                PlayerControl victim = players.PopRandom();
                if (victim.GetSubroles().Any(r => r.GetType() == variant.GetType())) continue;
                if (variant is ISubrole subrole && !(assigned = subrole.IsAssignableTo(victim))) continue;
                DraftGameMode.Instance.Assign(victim, variant, false);
            }
        }
    }
}