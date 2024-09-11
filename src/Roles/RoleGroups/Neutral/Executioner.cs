#nullable enable
using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Factions.Impostors;
using Lotus.Factions.Interfaces;
using Lotus.Factions.Neutrals;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Victory.Conditions;
using Lotus.Extensions;
using Lotus.Roles.Internals;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;
using Lotus.API.Player;
using Lotus.GameModes.Standard;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Executioner : CustomRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Executioner));
    private bool canTargetImpostors;
    private bool canTargetNeutrals;
    private int roleChangeWhenTargetDies;

    private PlayerControl? target;

    [RoleAction(LotusActionType.RoundStart)]
    private void OnGameStart(bool gameStart)
    {
        if (!gameStart) return;
        target = Players.GetAllPlayers().Where(p =>
        {
            if (p.PlayerId == MyPlayer.PlayerId) return false;
            IFaction faction = p.PrimaryRole().Faction;
            if (!canTargetImpostors && faction is ImpostorFaction) return false;
            return canTargetNeutrals || faction is not Factions.Neutrals.Neutral;
        }).ToList().GetRandom();
        log.Trace($"Executioner ({MyPlayer.name}) Target: {target}");

        target.NameModel().GetComponentHolder<NameHolder>().Add(new ColoredNameComponent(target, RoleColor, Game.InGameStates, MyPlayer));
    }

    [RoleAction(LotusActionType.Exiled, ActionFlag.GlobalDetector)]
    private void CheckExecutionerWin(PlayerControl exiled)
    {
        if (target == null || target.PlayerId != exiled.PlayerId || !MyPlayer.IsAlive()) return;
        List<PlayerControl> winners = new() { MyPlayer };
        if (target.PrimaryRole() is Jester) winners.Add(target);
        ManualWin win = new(winners, ReasonType.SoloWinner);
        win.Activate();
    }

    [RoleAction(LotusActionType.Disconnect)]
    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void CheckChangeRole(PlayerControl dead)
    {
        if (roleChangeWhenTargetDies == 0 || target == null || target.PlayerId != dead.PlayerId) return;
        StandardRoles roleHolder = StandardGameMode.Instance.RoleManager.RoleHolder as StandardRoles;
        switch ((ExeRoleChange)roleChangeWhenTargetDies)
        {
            case ExeRoleChange.Jester:
                Game.AssignRole(MyPlayer, roleHolder.Static.Jester);
                break;
            case ExeRoleChange.Opportunist:
                Game.AssignRole(MyPlayer, roleHolder.Static.Opportunist);
                break;
            case ExeRoleChange.SchrodingersCat:
                Game.AssignRole(MyPlayer, roleHolder.Static.SchrodingersCat);
                break;
            case ExeRoleChange.Crewmate:
                Game.AssignRole(MyPlayer, roleHolder.Static.Crewmate);
                break;
            case ExeRoleChange.None:
            default:
                break;
        }

        target = null;
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub
                .Name("Can Target Impostors")
                .Bind(v => canTargetImpostors = (bool)v)
                .AddOnOffValues(false).Build())
            .SubOption(sub => sub
                .Name("Can Target Neutrals")
                .Bind(v => canTargetNeutrals = (bool)v)
                .AddOnOffValues(false).Build())
            .SubOption(sub => sub
                .Name("Role Change When Target Dies")
                .Bind(v => roleChangeWhenTargetDies = (int)v)
                .Value(v => v.Text("Jester").Value(1).Color(new Color(0.93f, 0.38f, 0.65f)).Build())
                .Value(v => v.Text("Opportunist").Value(2).Color(Color.green).Build())
                .Value(v => v.Text("Copycat").Value(3).Color(new Color(1f, 0.7f, 0.67f)).Build())
                .Value(v => v.Text("Crewmate").Value(4).Color(new Color(0.71f, 0.94f, 1f)).Build())
                .Value(v => v.Text("Off").Value(0).Color(Color.red).Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleColor(new Color(0.55f, 0.17f, 0.33f))
            .Faction(FactionInstances.Neutral)
            .RoleFlags(RoleFlag.CannotWinAlone)
            .SpecialType(SpecialType.Neutral)
            .IntroSound(AmongUs.GameOptions.RoleTypes.Shapeshifter);

    private enum ExeRoleChange
    {
        None,
        Jester,
        Opportunist,
        SchrodingersCat,
        Crewmate
    }
}