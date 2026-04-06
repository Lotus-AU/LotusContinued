using Lotus.API.Odyssey;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Internals.Enums;
using Lotus.Managers.History.Events;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Options.UI;
using VentLib.Utilities;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Crew;
using Lotus.Roles.Interfaces;
using Lotus.Utilities;
using VentLib.Localization.Attributes;
using VentLib.Utilities.Collections;

namespace Lotus.Roles.RoleGroups.Crew;

public class Doctor : Scientist, ISubrole
{
    [NewOnSetup] private Dictionary<byte, Remote<TextComponent>> codComponents = null!;

    private bool isSubrole;
    private bool restrictedToCrew;

    public string Identifier() => "요";

    protected override void PostSetup()
    {
        if (!isSubrole) return;
        CustomRole role = MyPlayer.PrimaryRole();
        if (role.RealRole == RoleTypes.Crewmate) role.VirtualRole = RoleTypes.Scientist;
    }

    public bool IsAssignableTo(PlayerControl player)
    {
        return !restrictedToCrew || player.PrimaryRole().Faction is Crewmates;
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void DoctorAnyDeath(PlayerControl dead, IDeathEvent causeOfDeath)
    {
        if (codComponents.ContainsKey(dead.PlayerId)) codComponents[dead.PlayerId].Delete();
        string coloredString = "<size=1.6>" + Color.white.Colorize($"({RoleColor.Colorize(causeOfDeath.SimpleName())})") + "</size>";

        TextComponent textComponent = new(new LiveString(coloredString), GameState.InMeeting, viewers: MyPlayer);
        codComponents[dead.PlayerId] = dead.NameModel().GetComponentHolder<TextHolder>().Add(textComponent);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddVitalsOptions(base.RegisterOptions(optionStream))
            .SubOption(sub => sub
                .KeyName("Doctor is a Modifier", Translations.Options.DoctorIsModifier)
                .BindBool(b =>
                {
                    isSubrole = b;
                    if (b) RoleFlags |= RoleFlag.IsSubrole;
                    else RoleFlags &= ~RoleFlag.IsSubrole;
                })
                .ShowSubOptionPredicate(b => (bool)b)
                .SubOption(sub2 => sub2.KeyName("Restricted to Crewmates",
                        TranslationUtil.Colorize(Mystic.Translations.Options.RestrictedToCrewmates,
                            FactionInstances.Crewmates.Color))
                .AddBoolean()
                .BindBool(b => restrictedToCrew = b)
                .Build())
                .AddBoolean(false)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.5f, 1f, 0.87f));

    [Localized(nameof(Doctor))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(DoctorIsModifier))]
            public static string DoctorIsModifier = "Doctor is a Modifier";
        }
    }
}