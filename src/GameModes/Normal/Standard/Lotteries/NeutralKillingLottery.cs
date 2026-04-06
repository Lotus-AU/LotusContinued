using System.Linq;
using Lotus.Roles.Internals.Enums;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Standard.Lotteries;

public class NeutralKillingLottery : RoleLottery
{
    // TODO: maybe change this default role
    public NeutralKillingLottery() : base(NormalStandardRoles.Instance.Special.IllegalRole)
    {
        NormalStandardRoles.Instance.AllRoles.Where(r => r.SpecialType is SpecialType.NeutralKilling or SpecialType.Undead).ForEach(r => AddRole(r));
    }
}