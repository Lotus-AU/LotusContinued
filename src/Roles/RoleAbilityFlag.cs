using System;

namespace Lotus.Roles;

[Flags]
public enum RoleAbilityFlag
{
    CanRevivePlayers = 1, // player can revive roles in some shape or form. roles with this flag will hide roles from being seen when dead.
    CannotVent = 2, // player cannot vent
    CannotSabotage = 4, // player cannot sabotage (excluding doors since we don't know who sent the rpc.)
    UsesPet = 8, // role uses the pet button (not used rn, but may be used later if innersloth doesnt like pet roles)
    IsAbleToKill = 16, // role is able to kill
}


