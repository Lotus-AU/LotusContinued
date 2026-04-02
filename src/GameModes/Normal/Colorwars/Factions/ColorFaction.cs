using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Options;
using UnityEngine;

namespace Lotus.GameModes.Normal.Colorwars.Factions;

public class ColorFaction : Faction<ColorFaction>
{
    public static ColorFaction Instance = new(0, Color.red);
    private int teamId;
    private Color teamColor;

    public ColorFaction(int teamId, Color teamColor)
    {
        this.teamId = teamId;
        this.teamColor = teamColor;
    }
    public int TeamId => teamId;

    public override string Name() => FactionTranslations.ColorTeam.Name;
    public override Color Color => teamColor;

    public override Relation Relationship(ColorFaction sameFaction) => sameFaction.teamId == teamId ? Relation.FullAllies : Relation.None;
    public override bool CanSeeRole(PlayerControl player) => !ExtraGamemodeOptions.ColorwarsOptions.ConvertColorMode;
    public override Relation RelationshipOther(IFaction other) => Relation.None;
}