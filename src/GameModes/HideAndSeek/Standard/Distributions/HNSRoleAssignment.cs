extern alias JBAnnotations;
using System.Collections.Generic;
using System.Linq;
using JBAnnotations::JetBrains.Annotations;
using Lotus.Extensions;
using Lotus.Roles.Distribution;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.Standard.Distributions;

public class HNSRoleAssignment
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(HNSRoleAssignment));
    public static HNSRoleAssignment Instance = null!;
    public HNSRoleAssignment()
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
        log.Debug("choosing seeker.");
        PlayerControl seeker;

        bool flag = !AmongUsClient.Instance.IsGamePublic;
        HideAndSeekManager hnsManager = GameManager.Instance.Cast<HideAndSeekManager>();
        if (flag && hnsManager.LogicOptionsHnS.HasImpostorPlayerID() &&
            unassignedPlayers.Any(p => p.PlayerId == hnsManager.LogicOptionsHnS.ImpostorPlayerID())
            )
            seeker = unassignedPlayers.Find(p => p.PlayerId == hnsManager.LogicOptionsHnS.ImpostorPlayerID());
        else
        {
            PseudoRandomList<PlayerControl> pseudoRandomList = new(AmongUsClient.Instance.GameId);
            var il2cppList = new Il2CppSystem.Collections.Generic.List<PlayerControl>();

            foreach (var player in unassignedPlayers)
                il2cppList.Add(player);

            var enumerable = il2cppList.TryCast<Il2CppSystem.Collections.Generic.IEnumerable<PlayerControl>>();
            if (enumerable != null)
            {
                pseudoRandomList.AddRange(enumerable);
                for (int i = 0; i < GameData.RoundsPlayedInSession; i++)
                    pseudoRandomList.PickRandom();
                seeker = pseudoRandomList.PickRandom();
                unassignedPlayers.RemoveAll(p => p.PlayerId == seeker.PlayerId);
            }
            else seeker = unassignedPlayers.PopRandom();
        }


        log.Debug($"{seeker.name} is chosen.");
        HNSStandardGameMode.Instance.Assign(seeker, HNSStandardRoles.Instance.Static.Seeker);

        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 2);
        log.Debug("assigning remaining players to hider.");

        while (unassignedPlayers.Count > 0)
        {
            PlayerControl player = unassignedPlayers.Pop();
            HNSStandardGameMode.Instance.Assign(player, HNSStandardRoles.Instance.Static.Hider);
        }
        log.Debug("finished");

        RunAdditionalAssignmentLogic(allPlayers, unassignedPlayers, 3);
    }

    private void RunAdditionalAssignmentLogic(List<PlayerControl> allPlayers, List<PlayerControl> unassignedPlayers, int stage)
        => _additionalAssignmentLogics.ForEach(logic => logic.AssignRoles(allPlayers, unassignedPlayers, stage));
}