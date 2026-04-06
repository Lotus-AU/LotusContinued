using System.Collections.Generic;
using Lotus.GameModes.Normal.Standard;

namespace Lotus.Roles.Distribution;

public interface IAdditionalAssignmentLogic
{
    /// <summary>
    /// Allows for injecting custom role assignment into the standard gamemode.
    /// <br/>
    /// <b>Important</b>
    /// <br/>
    /// You should have a rough understanding of how role assignment works and roles should <b>ALWAYS</b> call <see cref="CustomRole.Instantiate"/>
    /// Additionally, the standard-algorithm uses <see cref="NormalStandardGameMode.Assign"/> for actually applying the role to players. You should replicate
    /// this behaviour but DO NOT set sendToClient=true
    /// </summary>
    /// <param name="allPlayers">A list of all the players.</param>
    /// <param name="unassignedPlayers">List of players who don't have roles assigned. <b>You are responsible for updating this list so their roles aren't overwritten.</b></param>
    /// <param name="stage">The current stage of the Assignment logic.<br/>1 - Before Impostor<br/>2 - Before Neutral Killers<br/>3 - Before Neutral Passive<br/>4 - Before Crew<br/>5 - After Subroles</param>
    public void AssignRoles(List<PlayerControl> allPlayers, List<PlayerControl> unassignedPlayers, int stage);
}