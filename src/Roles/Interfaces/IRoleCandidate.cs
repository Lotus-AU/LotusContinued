namespace Lotus.Roles.Interfaces;

/// <summary>
/// Use this if you want your role to only be assigned on certain conditions.<br></br>
/// Note that this is in the <i>actual</i> role assigning area.<br></br>
/// So, depending on the order in which roles are assigned, players might have the fallback role.<br></br>
/// Don't check for roles here unless you are absolutely sure.
/// </summary>
public interface IRoleCandidate
{
    /// <summary>
    /// Determines whether the role should be skipped.
    /// </summary>
    /// <returns>True if the role should be skipped, otherwise false.</returns>
    bool ShouldSkip();
}
