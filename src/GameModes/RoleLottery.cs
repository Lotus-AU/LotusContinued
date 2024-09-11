using System;
using System.Collections;
using System.Collections.Generic;
using Lotus.GameModes.Standard;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes;

public class RoleLottery : IEnumerator<CustomRole>, IEnumerable<CustomRole>
{
    protected UuidList<CustomRole> Roles = new();
    protected List<Ticket> Tickets = new();
    protected uint BatchNumber;
    protected Random rng;

    private CustomRole defaultRole;
    private CustomRole? current;

    private Dictionary<int, int> roleLimitTracker = new();

    public RoleLottery(CustomRole defaultRole)
    {
        this.defaultRole = defaultRole;
        Roles.Add(defaultRole);
        // rng = new Random(Guid.NewGuid().GetHashCode());
        rng = new Random();
    }

    public virtual void AddRole(CustomRole role, bool useSubsequentChance = false)
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


    public bool MoveNext()
    {
        current = Next();
        return HasNext();
    }

    public bool HasNext()
    {
        return Tickets.Count > 0;
    }

    public void Reset()
    {
    }

    public CustomRole Next()
    {
        while (true)
        {
            current = null;
            // Infinite loop break condition (bc we'll exhaust the ticket pool eventually)
            if (Tickets.Count == 0) return defaultRole;

            Ticket ticket = Tickets.PopRandom();
            Tickets.RemoveAll(t => t.Batch == ticket.Batch);

            CustomRole associatedRole = Roles.Get(ticket.Id);
            int count = roleLimitTracker.GetOrCompute(ticket.RoleId, () => 0);
            if (count >= associatedRole.Count) continue;

            roleLimitTracker[ticket.RoleId] += 1;
            return associatedRole;
        }
    }

    public CustomRole Current => current ??= Next();

    object IEnumerator.Current => Current;

    protected struct Ticket
    {
        public uint Id;
        public uint Batch;
        public int RoleId;

        public override bool Equals(object? obj)
        {
            if (obj is not Ticket ticket) return false;
            return ticket.Batch == Batch;
        }

        public override int GetHashCode() => HashCode.Combine(Id, Batch);
    }

    public void Dispose()
    {
    }

    public IEnumerator<CustomRole> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => this;
}