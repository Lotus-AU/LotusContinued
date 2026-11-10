namespace Lotus.Roles.Interfaces;

public interface IVariableRole
{
    public static CustomRole PickAssignedRole(CustomRole baseRole)
    {
        if (baseRole is not IVariableRole variableRole) return baseRole;
        return variableRole.AssignVariation() ? variableRole.Variation() : baseRole;
    }

    /// <summary>
    /// The variation role to assign, this can change per requirements
    /// </summary>
    /// <returns>The role that will be assigned if AssignVariation returns true</returns>
    public CustomRole Variation();

    /// <summary>
    /// Whether the game SHOULD assign the variation, this is where your random number generator should go.
    /// </summary>
    /// <returns>true if the variation will be assigned, otherwise false</returns>
    public bool AssignVariation();
}