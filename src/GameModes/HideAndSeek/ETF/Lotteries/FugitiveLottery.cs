using System.Linq;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.ETF.Lotteries;

public class FugitiveLottery : RoleLottery
{
    public FugitiveLottery() : base(ETFRoles.Instance.Static.Fugitive)
    {
        ETFRoles.Instance.AllRoles.Where(r => r is Fugitive && !r.RoleFlags.HasFlag(RoleFlag.IsSubrole)).ForEach(r => AddRole(r));
    }
}