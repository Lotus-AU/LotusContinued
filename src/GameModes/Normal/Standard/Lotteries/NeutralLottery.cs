using System.Linq;
using Lotus.Roles.Internals.Enums;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Standard.Lotteries;

public class NeutralLottery : RoleLottery
{
    // TODO: maybe change this default role
    public NeutralLottery() : base(NormalStandardRoles.Instance.Special.IllegalRole)
    {
        NormalStandardRoles.Instance.AllRoles.Where(r => r.SpecialType is SpecialType.Neutral).ForEach(r => AddRole(r));
    }
}