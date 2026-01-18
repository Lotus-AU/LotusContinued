using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lotus.Managers;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Roles.Exceptions;
using Lotus.Roles.Operations;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.Standard;

public class StandardRoleManager : Roles.Managers.RoleManager
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(StandardRoleManager));
    private readonly Dictionary<Assembly, Dictionary<ulong, CustomRole>> rolesbyAssembly = new();

    protected OrderedDictionary<string, CustomRole> OrderedCustomRoles { get; } = new();
    public override CustomRole FallbackRole() => EmptyRole.Instance;
    public override StandardRoles RoleHolder { get; }
    internal virtual bool IsGlobal => false;
    public override IEnumerable<CustomRole> AllCustomRoles() => OrderedCustomRoles.GetValues();

    public StandardRoleManager()
    {
        RoleHolder = new StandardRoles();
        RoleHolder.AllRoles.ForEach(RegisterRole);
        Async.Schedule(() =>
        {
            RoleHolder.Intialized = true;
        }, 0.5f);
    }

    public override void RegisterRole(CustomRole role)
    {
        base.RegisterRole(role);
        ulong roleID = role.RoleID;

        log.Log(LogLevel.All, $"Registering Role Definition (name={role.EnglishRoleName}, RoleID={role.RoleID}, Assembly={role.Assembly.GetName().Name}, AddonID={role.Addon?.UUID ?? 0})");

        Dictionary<ulong, CustomRole> assemblyDefinitions = rolesbyAssembly.GetOrCompute(role.Assembly, () => new Dictionary<ulong, CustomRole>());
        if (!assemblyDefinitions.TryAdd(roleID, role)) throw new DuplicateRoleIdException(roleID, assemblyDefinitions[roleID], role);
        if (!OrderedCustomRoles.TryAdd(role.GlobalRoleID, role)) throw new DuplicateRoleIdException(role.GlobalRoleID, OrderedCustomRoles[role.GlobalRoleID], role);
        log.Log(LogLevel.All, "Registered Role!");
    }

    public override CustomRole GetRole(ulong assemblyRoleID, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        if (rolesbyAssembly[assembly].TryGetValue(assemblyRoleID, out CustomRole? role)) return role;
        throw new NoSuchRoleException($"Could not find role with ID \"{assemblyRoleID}\" from roles defined by: \"{assembly.FullName}\"");
    }

    public override CustomRole GetRole(string globalRoleID)
    {
        if (!IsGlobal && globalRoleID.StartsWith("G")) return GlobalRoleManager.Instance.GetRole(globalRoleID);
        if (OrderedCustomRoles.TryGetValue(globalRoleID, out CustomRole? role)) return role;
        throw new NoSuchRoleException($"Could not find role with global-ID \"{globalRoleID}\"");
    }
}