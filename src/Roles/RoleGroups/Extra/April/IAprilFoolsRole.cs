using System;

namespace Lotus.Roles.RoleGroups.Extra.April;

public interface IAprilFoolsRole
{
    private bool ShouldShowRole()
    {
        DateTime approximateServerTime = DateTime.FromBinary(AmongUsDateTime.UtcNow.ToBinaryRaw());
        DateTime t = new DateTime(approximateServerTime.Year, 4, 1, 7, 0, 0, 0, DateTimeKind.Utc);
        DateTime t2 = new DateTime(approximateServerTime.Year, 4, 8, 7, 0, 0, 0, DateTimeKind.Utc);
        return approximateServerTime >= t && approximateServerTime <= t2;
    }

    public RoleFlag HideIfNotAprilFools(RoleFlag defaultFlags)
    {
        if (ShouldShowRole()) return defaultFlags;
        // only hide if not april fools
        return defaultFlags | RoleFlag.Hidden | RoleFlag.Unassignable | RoleFlag.DontRegisterOptions;
    }
}