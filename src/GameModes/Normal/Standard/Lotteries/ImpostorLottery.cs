using System.Linq;
using Lotus.Factions.Impostors;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Normal.Standard.Lotteries;

public class ImpostorLottery : RoleLottery
{
    public ImpostorLottery() : base(NormalStandardRoles.Instance.Static.Impostor)
    {
        NormalStandardRoles.Instance.AllRoles.Where(r => r.Faction is ImpostorFaction).ForEach(r => AddRole(r));
    }
}