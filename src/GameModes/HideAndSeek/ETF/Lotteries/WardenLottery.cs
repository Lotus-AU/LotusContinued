using System.Linq;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.ETF.Lotteries;

public class WardenLottery : RoleLottery
{
    public WardenLottery() : base(ETFRoles.Instance.Static.Warden)
    {
        ETFRoles.Instance.AllRoles.Where(r => r is Warden && !r.RoleFlags.HasFlag(RoleFlag.IsSubrole)).ForEach(r => AddRole(r));
    }
}