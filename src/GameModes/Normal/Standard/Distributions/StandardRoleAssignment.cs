extern alias JBAnnotations;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions.Impostors;
using Lotus.GameModes.Normal.Standard.Lotteries;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Roles.Distribution;
using Lotus.Roles.Interfaces;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Subroles;
using Lotus.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Standard.Distributions;

public class StandardRoleAssignment
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(StandardRoleAssignment));
    public static StandardRoleAssignment Instance = null!;
    public StandardRoleAssignment()
    {
        Instance = this;
    }

    private static List<IAdditionalAssignmentLogic> _additionalAssignmentLogics = new();

    [JBAnnotations::JetBrains.Annotations.UsedImplicitlyAttribute]
    public static void AddAdditionalAssignmentLogic(IAdditionalAssignmentLogic logic) => _additionalAssignmentLogics.Add(logic);

    public void AssignRoles(List<PlayerControl> allPlayers)
    {
        List<PlayerControl> unassignedPlayers = new(allPlayers);
        unassignedPlayers.Shuffle();

        RoleDistribution roleDistribution = GeneralOptions.GameplayOptions.OptimizeRoleAssignment
            ? OptimizeRoleAlgorithm.OptimizeDistribution()
            : OptimizeRoleAlgorithm.NonOptimizedDistribution();

        log.Debug("Assigning Roles..");

        DoNameBasedAssignment(unassignedPlayers);

        // ASSIGN FORCED ROLES (COMBO ROLES)
        log.Debug("Checking for Forced Roles");
        List<string> forcedRoles = [];
        List<Subrole> subrolesToAssign = [];
        PluginDataManager.ComboListManager.ListCombos
            .Where(c => c.ComboType is 0)
            .Where(c => (c.Role1EnglishName == string.Empty) ^ (c.Role2EnglishName == string.Empty))
            .ForEach(rci =>
            {
                string targetRoleName = rci.Role1EnglishName == string.Empty ? rci.Role2EnglishName : rci.Role1EnglishName;
                CustomRole roleInstance = GlobalRoleManager.Instance.AllCustomRoles()
                    .FirstOrDefault(r => r.EnglishRoleName == targetRoleName, EmptyRole.Instance);
                if (roleInstance is EmptyRole) return;
                if (roleInstance is Subrole subrole)
                {
                    subrolesToAssign.Add(subrole);
                    return;
                }
                // WE DO NOT CHECK COUNT OR CHANCE!! It's called "Forced" for a reason. These roles are automatically assigned.
                forcedRoles.Add(roleInstance.EnglishRoleName); // Keep track of added roles so we don't add them

                // WE ALSO DO NOT CHECK FOR VARIABLE ROLES!! Maybe some people want this to change, but again, it's FORCED! So no alternate versions.
                PlayerControl targetPlayer = unassignedPlayers.PopRandom();
                NormalStandardGameMode.Instance.Assign(targetPlayer, roleInstance);
                log.Debug($"{roleInstance.EnglishRoleName} was assigned to {targetPlayer.name} because it's a forced single combo.");
                AssignForcedSubroles(targetPlayer, roleInstance);
            });

        log.Debug($"Not assigning these roles again: {string.Join(", ", forcedRoles)}");

        // ASSIGN IMPOSTOR ROLES
        log.Debug("Assigning Impostor Roles");
        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 1);
        ImpostorLottery impostorLottery = new();
        int impostorCount = 0;
        int madmateCount = 0;

        while ((impostorCount < roleDistribution.Impostors || madmateCount < roleDistribution.MinimumMadmates) && unassignedPlayers.Count > 0)
        {
            CustomRole role = impostorLottery.Next();
            if (role.GetType() == typeof(Impostor) && impostorLottery.HasNext()) continue;
            if (forcedRoles.Remove(role.EnglishRoleName)) continue; // Remove from list in case count is more than 2.
            if (IsRoleBannedWithOtherRole(role)) continue;

            if (role.Faction is Madmates)
            {
                if (madmateCount >= roleDistribution.MaximumMadmates) continue;
                CustomRole variantMadmate = IVariableRole.PickAssignedRole(role);
                if (!CanAssignRolesForcedWithRole(variantMadmate, unassignedPlayers)) continue;
                PlayerControl targetMadmate = unassignedPlayers.PopRandom();
                NormalStandardGameMode.Instance.Assign(targetMadmate, variantMadmate);
                AssignForcedSubroles(targetMadmate, variantMadmate);
                madmateCount++;
                if (RoleOptions.MadmateOptions.MadmatesTakeImpostorSlots) impostorCount++;
                continue;
            }

            if (impostorCount >= roleDistribution.Impostors)
            {
                if (!impostorLottery.HasNext()) break;
                continue;
            }

            CustomRole variant = IVariableRole.PickAssignedRole(role);
            if (!CanAssignRolesForcedWithRole(variant, unassignedPlayers)) continue;
            PlayerControl targetPlayer = unassignedPlayers.PopRandom();
            NormalStandardGameMode.Instance.Assign(targetPlayer, variant);
            AssignForcedSubroles(targetPlayer, variant);
            impostorCount++;
        }

        // =====================

        // ASSIGN NEUTRAL KILLING ROLES
        log.Debug("Assigning Neutral Killing Roles");
        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 2);
        NeutralKillingLottery neutralKillingLottery = new();
        int nkRoles = 0;
        int loops = 0;
        while (unassignedPlayers.Count > 0 && nkRoles < roleDistribution.MaximumNeutralKilling)
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
            if (forcedRoles.Remove(role.EnglishRoleName)) continue; // Remove from list in case count is more than 2.
            if (IsRoleBannedWithOtherRole(role)) continue;
            CustomRole variant = IVariableRole.PickAssignedRole(role);
            if (!CanAssignRolesForcedWithRole(variant, unassignedPlayers)) continue;
            PlayerControl targetPlayer = unassignedPlayers.PopRandom();
            NormalStandardGameMode.Instance.Assign(targetPlayer, variant);
            AssignForcedSubroles(targetPlayer, variant);
            nkRoles++;
        }

        // --------------------------

        // ASSIGN NEUTRAL PASSIVE ROLES
        log.Debug("Assigning Neutral Passive Roles");
        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 3);
        NeutralLottery neutralLottery = new();
        int neutralRoles = 0;
        loops = 0;
        while (unassignedPlayers.Count > 0 && neutralRoles < roleDistribution.MaximumNeutralPassive)
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
            if (forcedRoles.Remove(role.EnglishRoleName)) continue; // Remove from list in case count is more than 2.
            if (IsRoleBannedWithOtherRole(role)) continue;
            CustomRole variant = IVariableRole.PickAssignedRole(role);
            if (!CanAssignRolesForcedWithRole(variant, unassignedPlayers)) continue;
            PlayerControl targetPlayer = unassignedPlayers.PopRandom();
            NormalStandardGameMode.Instance.Assign(targetPlayer, variant);
            AssignForcedSubroles(targetPlayer, variant);
            neutralRoles++;
        }

        // =====================

        // ASSIGN CREWMATE ROLES
        log.Debug("Assigning Crewmate Roles");
        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 4);
        CrewmateLottery crewmateLottery = new();
        while (unassignedPlayers.Count > 0)
        {
            CustomRole role = crewmateLottery.Next();
            if (role.GetType() == typeof(Crewmate) && crewmateLottery.HasNext()) continue;
            if (forcedRoles.Remove(role.EnglishRoleName)) continue; // Remove from list in case count is more than 2.
            if (IsRoleBannedWithOtherRole(role)) continue;
            CustomRole variant = IVariableRole.PickAssignedRole(role);
            if (!CanAssignRolesForcedWithRole(variant, unassignedPlayers)) continue;
            PlayerControl targetPlayer = unassignedPlayers.PopRandom();
            NormalStandardGameMode.Instance.Assign(targetPlayer, variant);
            AssignForcedSubroles(targetPlayer, variant);
        }

        // ====================

        // ASSIGN SUB-ROLES
        AssignSubroles(allPlayers, subrolesToAssign);
        // ================

        log.Debug("Finishing up...");
        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 5);
        log.Debug("Finished assigning roles!");
    }

    private void DoNameBasedAssignment(List<PlayerControl> unassignedPlayers)
    {
        if (!GeneralOptions.DebugOptions.NameBasedRoleAssignment) return;
        log.Debug("Doing Name Based Role Assigment!");
        int j = 0;
        while (j < unassignedPlayers.Count)
        {
            PlayerControl player = unassignedPlayers[j];
            CustomRole? role = NormalStandardGameMode.Instance.RoleManager.RoleHolder.AllRoles.FirstOrDefault(r => r.RoleName.RemoveHtmlTags().ToLower().StartsWith(player.name.ToLower() ?? "HEHXD"));
            if (role != null && role.GetType() != typeof(Crewmate))
            {
                NormalStandardGameMode.Instance.Assign(player, role);
                unassignedPlayers.Pop(j);
            }
            else j++;
        }
    }

    private void AssignSubroles(List<PlayerControl> allPlayers, List<Subrole> forcedRoles)
    {
        if (RoleOptions.SubroleOptions.ModifierLimits == 0) return; // no modifiers
        log.Debug("Assigning Subroles...");
        SubRoleLottery subRoleLottery = new();

        int evenDistribution = RoleOptions.SubroleOptions.EvenlyDistributeModifiers ? 0 : 9999;

        bool CanAssignTo(PlayerControl player)
        {
            int count = player.GetSubroles().Count;
            if (count > evenDistribution) return false;
            return RoleOptions.SubroleOptions.UncappedModifiers || count < RoleOptions.SubroleOptions.ModifierLimits;
        }

        forcedRoles.ForEach(role =>
        {
            List<PlayerControl> players = Players.GetAllPlayers().ToList();
            bool assigned = false;
            while (players.Count > 0 && !assigned)
            {
                PlayerControl victim = players.PopRandom();
                if (victim.GetSubroles().Any(r => r.GetType() == role.GetType())) continue;
                if (AreRolesBlockedWithEachOther(role, victim.PrimaryRole())) continue; // Check if the player's primary role and subrole are blocked.
                if (victim.GetSubroles().Any(cr => AreRolesBlockedWithEachOther(role, cr))) continue; // Check if any of their subroles are blocked with this one.
                if (role is ISubrole subrole && !(assigned = subrole.IsAssignableTo(victim))) continue;
                NormalStandardGameMode.Instance.Assign(victim, role, false);
                AssignForcedSubroles(victim, role);
            }
        });

        List<string> forcedRoleNames = forcedRoles.Select(r => r.EnglishRoleName).ToList();

        while (subRoleLottery.HasNext())
        {
            CustomRole role = subRoleLottery.Next();
            if (role is IllegalRole) continue;
            CustomRole variant = role is Subrole sr ? IVariantSubrole.PickAssignedRole(sr) : IVariableRole.PickAssignedRole(role);
            if (forcedRoleNames.Remove(variant.EnglishRoleName)) continue; // Remove from list in case count is more than 2.
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
            // log.Debug($"testing role {role.EnglishRoleName}");

            bool assigned = false;
            while (players.Count > 0 && !assigned)
            {
                PlayerControl victim = players.PopRandom();
                if (victim.GetSubroles().Any(r => r.GetType() == variant.GetType())) continue;
                if (AreRolesBlockedWithEachOther(variant, victim.PrimaryRole())) continue; // Check if the player's primary role and subrole are blocked.
                if (victim.GetSubroles().Any(cr => AreRolesBlockedWithEachOther(variant, cr))) continue; // Check if any of their subroles are blocked with this one.
                if (variant is ISubrole subrole && !(assigned = subrole.IsAssignableTo(victim))) continue;
                NormalStandardGameMode.Instance.Assign(victim, variant, false);
                AssignForcedSubroles(victim, variant);
            }
        }
    }

    // COMBO HELPER FUNCTIONS
    private bool IsRoleBannedWithOtherRole(CustomRole targetRole) => PluginDataManager.ComboListManager.ListCombos
        .Where(c => c.ComboType is 1)
        .Where(c => (c.Role1EnglishName == targetRole.EnglishRoleName) ^ (c.Role2EnglishName == targetRole.EnglishRoleName))
        .Any(rci =>
        {
            string otherRoleName = rci.Role1EnglishName == targetRole.EnglishRoleName // get the other role's english name
                ? rci.Role2EnglishName
                : rci.Role1EnglishName;
            return Game.MatchData.Roles.MainRoles.Values.Any(cr => cr.EnglishRoleName == otherRoleName); // Check if any currently assigned roles match.
        });

    private bool AreRolesBlockedWithEachOther(CustomRole subRole, CustomRole targetRole) => PluginDataManager.ComboListManager.ListCombos
        .Where(c => c.ComboType is 1)
        .Any(rci => (rci.Role1EnglishName == subRole.EnglishRoleName && rci.Role2EnglishName == targetRole.EnglishRoleName)
                    || (rci.Role1EnglishName == targetRole.EnglishRoleName && rci.Role2EnglishName == subRole.EnglishRoleName));

    /// <summary>
    /// Checks the list of combos of the current preset to see if there are any roles that should always spawn when this one spawns.<br/>
    /// Any roles assigned as the result of this function will not call this function again to prevent recursiveness.<br/>
    /// Modifiers will still be assigned as normal however.
    /// </summary>
    /// <param name="targetRole">The role to look for when checking the combo list.</param>
    /// <param name="unassignedPlayers">The remaining </param>
    /// <returns>Whether there are enough players left to assign the roles that always spawn with 'targetRole'. <br/>Also returns true if there are no roles forced at all.</returns>
    private bool CanAssignRolesForcedWithRole(CustomRole targetRole, List<PlayerControl> unassignedPlayers)
    {
        List<CustomRole> rolesToAssign = [];
        PluginDataManager.ComboListManager.ListCombos
            .Where(c => c.ComboType is 0)
            .Where(c => (c.Role1EnglishName == targetRole.EnglishRoleName) ^ (c.Role2EnglishName == targetRole.EnglishRoleName))
            .ForEach(rci =>
            {
                string otherRoleName = rci.Role1EnglishName == targetRole.EnglishRoleName // get the other role's english name
                    ? rci.Role2EnglishName
                    : rci.Role1EnglishName;
                CustomRole roleInstance = GlobalRoleManager.Instance.AllCustomRoles()
                    .FirstOrDefault(r => r.EnglishRoleName == otherRoleName, EmptyRole.Instance);
                if (roleInstance is EmptyRole) return;
                if (roleInstance is Subrole) return; // Skip subroles.
                if (IsRoleBannedWithOtherRole(roleInstance)) return;
                rolesToAssign.Add(roleInstance);
            });
        if (rolesToAssign.Count >= unassignedPlayers.Count) return false; // >= we also need to assign 'targetRole'
        rolesToAssign.ForEach(r =>
        {
            PlayerControl player = unassignedPlayers.PopRandom();
            NormalStandardGameMode.Instance.Assign(player, r);
            log.Debug($"{r.EnglishRoleName} was assigned to {player.name} because its in a forced role combo with {targetRole.EnglishRoleName}.");
        });
        return true;
    }

    private void AssignForcedSubroles(PlayerControl player, CustomRole mainRole)
    {
        PluginDataManager.ComboListManager.ListCombos
            .Where(c => c.ComboType == 0)
            .Where(c => (c.Role1EnglishName == mainRole.EnglishRoleName) ^ (c.Role2EnglishName == mainRole.EnglishRoleName))
            .ForEach(rci =>
            {
                string otherRoleName = rci.Role1EnglishName == mainRole.EnglishRoleName // get the other role's english name
                    ? rci.Role2EnglishName
                    : rci.Role1EnglishName;
                CustomRole roleInstance = GlobalRoleManager.Instance.AllCustomRoles()
                    .FirstOrDefault(r => r.EnglishRoleName == otherRoleName, EmptyRole.Instance);
                if (roleInstance is EmptyRole) return;
                if (roleInstance is not Subrole) return; // Skip actual roles.
                log.Debug($"{roleInstance.EnglishRoleName} was assigned to {player.name} because of a forced modifier/role combo with {mainRole.EnglishRoleName}.");
                NormalStandardGameMode.Instance.Assign(player, roleInstance, false);
            });
    }
    // -----------------------------------------------

    private void RunAdditionalAssignmentLogic(List<PlayerControl> allPlayers, List<PlayerControl> unassignedPlayers, int stage)
        => _additionalAssignmentLogics.ForEach(logic => logic.AssignRoles(allPlayers, unassignedPlayers, stage));
}