using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lotus.Roles;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes;
public abstract class RoleHolder : IRoleHolder
{
    public abstract List<Action> FinishedCallbacks();

    public List<CustomRole> MainRoles { get; set; }

    public List<CustomRole> ModifierRoles { get; set; }

    public List<CustomRole> SpecialRoles { get; set; }

    public List<CustomRole> AllRoles { get; set; }

    public RoleHolder(Roles.Managers.RoleManager manager)
    {

    }

    public void AddOnFinishCall(Action action) => this.FinishedCallbacks().Add(action);
    public bool Intialized
    {
        get { return _initialized; }
        set
        {
            if (_initialized != true)
                FinishedCallbacks().All(fc => { fc(); return true; });

            _initialized = true;
        }
    }
    private bool _initialized;
}