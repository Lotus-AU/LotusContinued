using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.API;
using Lotus.Extensions;
using Lotus.Options;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Localization.Attributes;
using Lotus.Factions.Undead;

namespace Lotus.Roles.RoleGroups.Undead.Roles;

public class Deathknight : UndeadRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Deathknight));
    public bool CanBecomeNecromancer;
    private float influenceRange;
    private bool multiInfluence;

    private Cooldown influenceCooldown;

    [NewOnSetup]
    private List<PlayerControl> inRangePlayers;
    private DateTime lastCheck = DateTime.Now;

    private const float UpdateTimeout = 0.25f;

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        LiveString liveString = new(() => inRangePlayers.Count > 0 ? "★" : "", Color.white);
        player.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(liveString, GameState.Roaming, viewers: player));
    }

    [RoleAction(LotusActionType.FixedUpdate)]
    private void DeathknightFixedUpdate()
    {
        if (DateTime.Now.Subtract(lastCheck).TotalSeconds < UpdateTimeout) return;
        lastCheck = DateTime.Now;
        inRangePlayers.Clear();
        if (influenceCooldown.NotReady()) return;
        inRangePlayers = (influenceRange < 0
            ? MyPlayer.GetPlayersInAbilityRangeSorted().Where(p => !IsConvertedUndead(p) && !IsUnconvertedUndead(p))
            : RoleUtils.GetPlayersWithinDistance(MyPlayer, influenceRange).Where(p => !IsConvertedUndead(p) && !IsUnconvertedUndead(p)))
            .ToList();
    }
    [RoleAction(LotusActionType.OnPet, priority: Priority.First)]
    private void InitiatePlayer(ActionHandle handle)
    {
        log.Trace("Deathknight Influence Ability", "DeathknightAbility");
        if (influenceCooldown.NotReady()) return;
        int influenceCount = Math.Min(inRangePlayers.Count, multiInfluence ? int.MaxValue : 1);
        if (influenceCount == 0) return;
        influenceCooldown.Start();
        handle.Cancel();
        for (int i = 0; i < influenceCount; i++) InitiateConvertToUndead(inRangePlayers[i]);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Can Become Necromancer", Translations.Options.CanBecomeNecromancer)
                .AddOnOffValues()
                .BindBool(b => CanBecomeNecromancer = b)
                .Build())
            .SubOption(sub => sub
                .KeyName("Influence Cooldown", Translations.Options.InfluenceCooldown)
                .AddFloatRange(5f, 120f, 2.5f, 7, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(influenceCooldown.SetDuration)
                .Build())
            .SubOption(sub => sub
                .KeyName("Influence Range", Translations.Options.InfluenceRange)
                .BindFloat(v => influenceRange = v)
                .Value(v => v.Text("Kill Distance").Value(-1f).Build())
                .AddFloatRange(1.5f, 3f, 0.1f, 4)
                .Build())
            .SubOption(sub => sub
                .KeyName("Influences Many", Translations.Options.InfluenceMany)
                .BindBool(b => multiInfluence = b)
                .AddOnOffValues()
                .Build());

    public override RoleType GetRoleType() => RoleType.Transformation;

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.34f, 0.34f, 0.39f))
            .RoleFlags(RoleFlag.TransformationRole)
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet);

    [Localized(nameof(Deathknight))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(CanBecomeNecromancer))]
            public static string CanBecomeNecromancer = "Can Become Necromancer";

            [Localized(nameof(InfluenceCooldown))]
            public static string InfluenceCooldown = "Influence Cooldown";

            [Localized(nameof(InfluenceRange))]
            public static string InfluenceRange = "Influence Range";

            [Localized(nameof(InfluenceMany))]
            public static string InfluenceMany = "Influence Many";
        }
    }
}