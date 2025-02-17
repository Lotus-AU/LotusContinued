using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Factions.Impostors;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API;
using Lotus.Extensions;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using Lotus.API.Player;

namespace Lotus.Roles.RoleGroups.Crew;

public class Psychic : Crewmate
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Psychic));
    private int numberOfPlayers;
    private bool nonImpostorAreEvil;

    [NewOnSetup]
    private List<Remote<NameComponent>> remotes;


    [RoleAction(LotusActionType.RoundEnd)]
    private void MarkMeetingPlayers()
    {
        List<PlayerControl> eligiblePlayers = Players.GetAlivePlayers().Where(IsEvil).ToList();
        if (eligiblePlayers.Count == 0) return;

        log.Debug($"Psychic Eligible Evil Players: {eligiblePlayers.Select(p => p.name).Fuse()}", "Psychic Ability");
        PlayerControl evilPlayer = eligiblePlayers.GetRandom();
        List<PlayerControl> targetPlayers = new() { evilPlayer };
        List<PlayerControl> remainingPlayers = Players.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId && p.PlayerId != evilPlayer.PlayerId).ToList();
        log.Debug($"Psychic Remaining Players: {remainingPlayers.Select(p => p.name).Fuse()}", "Psychic Ability");

        while (targetPlayers.Count < numberOfPlayers && remainingPlayers.Count != 0) targetPlayers.Add(remainingPlayers.PopRandom());

        targetPlayers.ForEach(p =>
            remotes.Add(p.NameModel().GetComponentHolder<NameHolder>().Add(new ColoredNameComponent(p, Color.red, GameState.InMeeting, viewers: MyPlayer))));
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void CleanupMarkedPlayers()
    {
        remotes.ForEach(r => r.Delete());
        remotes.Clear();
    }

    private bool IsEvil(PlayerControl player) => nonImpostorAreEvil
        ? Relationship(player) is Relation.None || player.PrimaryRole().Faction is ImpostorFaction
        : player.PrimaryRole().Faction is ImpostorFaction;

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Highlighted Players", Translations.Options.MaxHighlightedPlayers)
                .AddIntRange(1, 14, 1, 2)
                .BindInt(i => numberOfPlayers = i)
                .Build())
            .SubOption(sub => sub.KeyName("Non-impostor Killing Are Evil", TranslationUtil.Colorize(Translations.Options.AllKillingRolesAreEvil, ModConstants.Palette.KillingColor))
                .AddOnOffValues()
                .BindBool(b => nonImpostorAreEvil = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.44f, 0.41f, 0.55f));

    [Localized(nameof(Psychic))]
    internal static class Translations
    {
        [Localized(ModConstants.Options)]
        internal static class Options
        {
            [Localized(nameof(MaxHighlightedPlayers))]
            public static string MaxHighlightedPlayers = "Maximum Highlighted Players";

            [Localized(nameof(AllKillingRolesAreEvil))]
            public static string AllKillingRolesAreEvil = "All Killing::0 Roles Are Evil";
        }
    }
}