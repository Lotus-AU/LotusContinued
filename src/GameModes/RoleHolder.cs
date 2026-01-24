using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.Roles;

namespace Lotus.GameModes;
public abstract class RoleHolder : IRoleHolder
{
    public abstract List<Action> FinishedCallbacks();

    public List<CustomRole> MainRoles { get; set; }

    public List<CustomRole> ModifierRoles { get; set; }

    public List<CustomRole> SpecialRoles { get; set; }

    public List<CustomRole> AllRoles { get; set; }

    protected static List<CustomRole> AddonRoles = new();

    public bool Intialized
    {
        get { return _initialized; }
        set
        {
            if (value)
                FinishedCallbacks().ForEach(func => func());

            _initialized = value;
        }
    }
    // ReSharper disable once InconsistentNaming
    private bool _initialized;

    public virtual void AddAddonRole(CustomRole addonRole)
    {}
}