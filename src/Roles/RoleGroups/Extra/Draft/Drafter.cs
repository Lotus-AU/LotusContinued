using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.API.Vanilla.Meetings;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.GameModes;
using Lotus.GameModes.Draft;
using Lotus.Logging;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Extra.Draft;

public class Drafter: CustomRole
{
    [NewOnSetup] private List<CustomRole> myRoleOptions;

    private CustomRole roleToAssign;

    private bool isMyTurn;
    private int curSelected;

    private int myPlayerId;

    [RoleAction(LotusActionType.Vote)]
    private void SelectAction(Optional<PlayerControl> voted, MeetingDelegate @delegate, ActionHandle handle)
    {
        handle.Cancel();
        if (!isMyTurn) return;

        if (voted.Exists()) ChangeSelection(1);
        else EndMyTurn();
    }

    public override void HandleDisconnect()
    {
        if (isMyTurn)
        {
            isMyTurn = false;

            DraftGameMode.Instance.AvailableRoles.AddRange(myRoleOptions);
            myRoleOptions = [];
            DraftGameMode.Instance.SetNextPlayersTurn();
        }
        base.HandleDisconnect();
    }

    public void SetMyTurn()
    {
        CHandler(GamemodeTranslations.Draft.PlayerStartsTurn.Formatted(myPlayerId, DraftGameMode.Instance.DecisionTime)).Send();

        myRoleOptions = [];
        for (int i = 0; i < DraftGameMode.Instance.RoleChoiceCount; i++)
            myRoleOptions.Add(DraftGameMode.Instance.AvailableRoles.PopRandom());


        CHandler(GamemodeTranslations.Draft.PlayerChoiceOptions.Formatted("\n" + string.Join("\n", myRoleOptions.Select(r => r.ColoredRoleName())
            .Append(GamemodeTranslations.Draft.RandomOption)))).Send(MyPlayer);

        isMyTurn = true;
        curSelected = 0;

        Async.Schedule(() => ChangeSelection(0), .3f);
        Async.Schedule(() =>
        {
            if (!isMyTurn) return;
            curSelected = myRoleOptions.Count;
            EndMyTurn();
        }, DraftGameMode.Instance.DecisionTime);
    }

    public void EndMyTurn()
    {
        if (!isMyTurn) return;
        isMyTurn = false;

        if (curSelected == myRoleOptions.Count)
        {
            DraftGameMode.Instance.AvailableRoles.AddRange(myRoleOptions);
            myRoleOptions = [];
            roleToAssign = DraftGameMode.Instance.AvailableRoles.PopRandom();
            CHandler(GamemodeTranslations.Draft.PlayerEndTurn.Formatted(myPlayerId, GamemodeTranslations.Draft.RandomOption)).Send();
        }
        else
        {
            roleToAssign = myRoleOptions.Pop(curSelected);
            DraftGameMode.Instance.AvailableRoles.AddRange(myRoleOptions);
            myRoleOptions = [];
            CHandler(GamemodeTranslations.Draft.PlayerEndTurn.Formatted(myPlayerId, roleToAssign.ColoredRoleName())).Send();
        }

        DraftGameMode.Instance.SetNextPlayersTurn();
    }

    public void AssignRole()
    {
        if (MyPlayer == null || !MyPlayer.IsAlive()) return;
        ChangeRoleTo(roleToAssign);
    }

    public void SetFakePlayerId(int id) => myPlayerId = id;

    public void AnnounceID()
    {
        if (myPlayerId == 1) return;
        CHandler(GamemodeTranslations.Draft.PlayerIdText.Formatted(myPlayerId)).Send(MyPlayer);
    }

    private void ChangeSelection(int change)
    {
        curSelected += change;
        if (curSelected < 0) curSelected = myRoleOptions.Count + 1;
        if (curSelected > myRoleOptions.Count) curSelected = 0;

        if (curSelected == myRoleOptions.Count)
            CHandler(GamemodeTranslations.Draft.PlayerSwapChoice.Formatted(GamemodeTranslations.Draft.RandomOption)).Send(MyPlayer);
        else
        {
            var curSelectedRole = myRoleOptions[curSelected];
            CHandler(GamemodeTranslations.Draft.PlayerSwapChoice.Formatted(curSelectedRole.ColoredRoleName(), "\n" + curSelectedRole.Description)).Send(MyPlayer);
        }
    }

    public ChatHandler CHandler(string message) => ChatHandler.Of(message, GamemodeTranslations.Draft.DraftTitle);

    public override RoleType GetRoleType() => RoleType.DontShow;

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleColor(Color.white)
            .SpecialType(SpecialType.NeutralKilling)
            .OptionOverride(Override.DiscussionTime, 0)
            .OptionOverride(Override.VotingTime, -1)
            .RoleFlags(RoleFlag.DontRegisterOptions | RoleFlag.Hidden);
}