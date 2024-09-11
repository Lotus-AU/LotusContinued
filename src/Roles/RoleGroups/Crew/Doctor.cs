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
using VentLib.Utilities.Collections;

namespace Lotus.Roles.RoleGroups.Crew;

public class Doctor : Scientist
{
    [NewOnSetup] private Dictionary<byte, Remote<TextComponent>> codComponents = null!;

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void DoctorAnyDeath(PlayerControl dead, IDeathEvent causeOfDeath)
    {
        if (codComponents.ContainsKey(dead.PlayerId)) codComponents[dead.PlayerId].Delete();
        string coloredString = "<size=1.6>" + Color.white.Colorize($"({RoleColor.Colorize(causeOfDeath.SimpleName())})") + "</size>";

        TextComponent textComponent = new(new LiveString(coloredString), GameState.InMeeting, viewers: MyPlayer);
        codComponents[dead.PlayerId] = dead.NameModel().GetComponentHolder<TextHolder>().Add(textComponent);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddVitalsOptions(base.RegisterOptions(optionStream));

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.5f, 1f, 0.87f));
}