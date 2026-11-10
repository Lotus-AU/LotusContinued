using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions.Impostors;
using Lotus.GUI.Name.Holders;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Madmates.Roles;

public class MadSnitch : MadCrewmate
{
    protected override void OnTaskComplete(Optional<NormalPlayerTask> _)
    {
        int remainingTasks = TotalTasks - TasksComplete;
        if (remainingTasks != 0) return;
        Players.GetAllPlayers()
            .Where(p => p.PrimaryRole().Faction is ImpostorFaction)
            .ForEach(p => p.NameModel().GetComponentHolder<RoleHolder>().Components().ForEach(rc => rc.AddViewer(MyPlayer)));
    }

    protected override string ForceRoleImageDirectory() => "RoleOutfits/Imposter/madsnitch";
}