using System.Linq;
using Lotus.API.Odyssey;
using Lotus.Chat;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization.Attributes;
using Lotus.Roles.Internals.Enums;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using System.Collections.Generic;
using System;
using Lotus.Roles.RoleGroups.Crew;

namespace Lotus.Roles.Subroles;

[Localized("Roles.Subroles.Sleuth")]
public class Sleuth : Subrole
{
    /// <summary>
    /// A list of roles that Sleuth is not compatiable with. Add your role to this list to make it not be assigned with your role.
    /// </summary>
    public static readonly List<Type> IncompatibleRoles = new()
    {
        typeof(Altruist)
    };
    [Localized("SleuthMessage")]
    private static string _sleuthMessage = "You've determined that {0} was a {1}! Great work, Detective!";

    [Localized("SleuthTitle")]
    private static string _sleuthMessageTitle = "Sleuth {0}";

    public override string Identifier() => "◯";

    [RoleAction(LotusActionType.ReportBody)]
    private void SleuthReportBody(Optional<NetworkedPlayerInfo> target)
    {
        if (!target.Exists()) return;
        NetworkedPlayerInfo deadBody = target.Get();
        if (deadBody.Object != null) deadBody.Object.NameModel().GetComponentHolder<RoleHolder>().Last().AddViewer(MyPlayer);

        ulong gameId = Game.GetGameID(deadBody.PlayerId);

        CustomRole role = Game.MatchData.Roles.GetMainRole(deadBody.PlayerId);
        string title = RoleColor.Colorize($"{_sleuthMessageTitle.Formatted(MyPlayer.name)}");
        ChatHandler handler = ChatHandler.Of(_sleuthMessage.Formatted(Game.MatchData.FrozenPlayers[gameId].Name, role), title);

        Async.Schedule(() => handler.Send(MyPlayer), NetUtils.DeriveDelay(1.5f));
    }

    public override HashSet<Type>? RestrictedRoles()
    {
        HashSet<Type>? restrictedRoles = base.RestrictedRoles();
        IncompatibleRoles.ForEach(r => restrictedRoles?.Add(r));
        return restrictedRoles;
    }

    public override CompatabilityMode RoleCompatabilityMode => CompatabilityMode.Blacklisted;

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => AddRestrictToCrew(base.RegisterOptions(optionStream));

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier).RoleColor(new Color(0.52f, 0.74f, 1f));
}