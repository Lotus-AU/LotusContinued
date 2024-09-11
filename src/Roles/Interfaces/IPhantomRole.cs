namespace Lotus.Roles.Interfaces;

/// <summary>
/// Despite the naming of this interface a "Phantom" role is any role that should be treated as "not really existing" in the game.
/// Currently this is only applicable to the actual <see cref="Roles.RoleGroups.Neutral.Taskrunner"/> role. But can be used in any class
/// where the role should not be counted in methods that do role aggregations. One such example is plague bearer. Roles marked by this class
/// may allow the PB to skip over them in their overall "infected" player total
/// </summary>
public interface IPhantomRole
{
    /// <summary>
    /// Dictates if this role is counted as a player which is used in logic such as win conditions & player counting
    /// </summary>
    /// <returns></returns>
    public bool IsCountedAsPlayer();
}