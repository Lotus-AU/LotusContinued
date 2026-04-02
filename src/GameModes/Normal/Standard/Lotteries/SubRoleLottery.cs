using System.Linq;
using Lotus.Roles;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Standard.Lotteries;

public class SubRoleLottery : RoleLottery
{
    public SubRoleLottery() : base(NormalStandardRoles.Instance.Special.IllegalRole)
    {
        NormalStandardRoles.Instance.AllRoles.Where(r => r.RoleFlags.HasFlag(RoleFlag.IsSubrole)).ForEach(r => AddRole(r));
    }
}