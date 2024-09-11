using System.Linq;
using Lotus.Factions.Crew;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Standard.Lotteries;

public class CrewmateLottery : RoleLottery
{
    public CrewmateLottery() : base(StandardRoles.Instance.Static.Crewmate)
    {
        StandardRoles.Instance.AllRoles.Where(r => r.Faction is Crewmates && !r.RoleFlags.HasFlag(RoleFlag.IsSubrole)).ForEach(r => AddRole(r));
    }

    public override void AddRole(CustomRole role, bool useSubsequentChance = false)
    {
        if (role is IRoleCandidate candidate)
        {
            if (candidate.ShouldSkip()) return;
        }
        int chance = useSubsequentChance ? role.AdditionalChance : role.Chance;
        if (chance == 0 || role.RoleFlags.HasFlag(RoleFlag.Unassignable)) return;
        uint id = Roles.Add(role);
        uint batch = BatchNumber++;

        int roleId = StandardGameMode.Instance.RoleManager.GetRoleId(role);

        // Add Roles using Subsequenct chances.
        if (!useSubsequentChance && role.Count > 1)
        {
            for (int i = 0; i < role.Count - 1; i++) AddRole(role, true);
        }

        if (chance >= 100)
        {
            Tickets.Add(new Ticket { Id = id, Batch = batch, RoleId = roleId });
        }
        else
        {
            if (rng.Next(0, 100) > chance) return;
            Tickets.Add(new Ticket { Id = id, Batch = batch, RoleId = roleId });
        }

        // // Add tickets for the new role first
        // for (int i = 0; i < chance; i++) Tickets.Add(new Ticket { Id = id, Batch = batch, RoleId = roleId });

        // // Add tickets for the new role second
        // for (int i = 0; i < 100 - chance; i++) Tickets.Add(new Ticket { Id = 0, Batch = batch, RoleId = roleId });
    }
}