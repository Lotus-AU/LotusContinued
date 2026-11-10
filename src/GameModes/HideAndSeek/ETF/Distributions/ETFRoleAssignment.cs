extern alias JBAnnotations;
using System.Collections.Generic;
using System.Linq;
using JBAnnotations::JetBrains.Annotations;
using Lotus.Extensions;
using Lotus.GameModes.HideAndSeek.ETF.Lotteries;
using Lotus.Options;
using Lotus.Roles.Distribution;
using Lotus.Roles.Interfaces;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.ETF.Distributions;

public class ETFRoleAssignment
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ETFRoleAssignment));
    public static ETFRoleAssignment Instance = null!;
    public ETFRoleAssignment()
    {
        Instance = this;
    }

    private static readonly List<IAdditionalAssignmentLogic> _additionalAssignmentLogics = [];

    [UsedImplicitly]
    public static void AddAdditionalAssignmentLogic(IAdditionalAssignmentLogic logic) => _additionalAssignmentLogics.Add(logic);

    public void AssignRoles(List<PlayerControl> allPlayers)
    {
        List<PlayerControl> unassignedPlayers = new(allPlayers);
        unassignedPlayers.Shuffle();

        log.Debug("Assigning Roles...");

        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 1);

        log.Debug("Assigning Impostor (Warden) Roles...");
        WardenLottery wardenLottery = new();
        int impostorCount = 0;
        while (impostorCount < ExtraGamemodeOptions.EscapeOptions.WardenCount && unassignedPlayers.Count > 0)
        {
            var role = wardenLottery.Next();
            if (role.GetType() == typeof(Warden) && wardenLottery.HasNext()) continue;

            var variant = IVariableRole.PickAssignedRole(role);
            PlayerControl targetPlayer = unassignedPlayers.PopRandom();
            ETFGameMode.Instance.Assign(targetPlayer, variant);
            impostorCount++;
        }

        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 2);
        log.Debug("Assigning remaining players Crewmate (Fugitive) Roles.");


        FugitiveLottery fugitiveLottery = new();
        while (unassignedPlayers.Count > 0)
        {
            var role = fugitiveLottery.Next();
            if (role.GetType() == typeof(Fugitive) && fugitiveLottery.HasNext()) continue;
            var variant = IVariableRole.PickAssignedRole(role);
            PlayerControl targetPlayer = unassignedPlayers.PopRandom();
            ETFGameMode.Instance.Assign(targetPlayer, variant);
        }
        log.Debug("Finished!");

        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 3);
    }

    private void RunAdditionalAssignmentLogic(List<PlayerControl> allPlayers, List<PlayerControl> unassignedPlayers, int stage)
        => _additionalAssignmentLogics.ForEach(logic => logic.AssignRoles(allPlayers, unassignedPlayers, stage));
}