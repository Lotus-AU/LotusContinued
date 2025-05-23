﻿using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Roles.RoleGroups.NeutralKilling;
using UnityEngine;

namespace Lotus.Roles.Factions;

public class JackalFaction: Faction<JackalFaction>
{
    public static JackalFaction Instance { get; } = new();

    public override string Name() => FactionTranslations.Neutral.Name;

    public override Relation Relationship(JackalFaction sameFaction) => Relation.FullAllies;

    public override bool CanSeeRole(PlayerControl player) => true;

    public override Color Color => Jackal.JackalColor;

    public override Relation RelationshipOther(IFaction other) => Relation.None;
}