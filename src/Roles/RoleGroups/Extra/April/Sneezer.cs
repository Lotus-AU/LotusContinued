using System;
using Lotus.API.Odyssey;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.Subroles;
using UnityEngine;
using Lotus.Extensions;
using VentLib.Utilities;
using VentLib.Utilities.Collections;

namespace Lotus.Roles.RoleGroups.Extra.April;

public class Sneezer: Subrole, IAprilFoolsRole
{
    private System.Random randomObject;

    public override string Identifier() => "☯";

    public override string GetRoleOutfitPath() => "RoleOutfits/Neutral/Jester".ToLower();

    [RoleAction(LotusActionType.RoundStart)]
    private void StartMeetingDelay(bool isRoundOne)
    {
        if (randomObject == null!) randomObject = new System.Random(DateTime.Now.Millisecond);
        if (isRoundOne) return;

        int currentMeetings = Game.MatchData.MeetingsCalled;
        Async.Schedule(() =>
        {
            if (currentMeetings != Game.MatchData.MeetingsCalled) return;
            if (!MyPlayer.IsAlive()) return;

            MeetingPrep.PrepMeeting(MyPlayer, null, checkReportBodyCancel: false);
        }, randomObject.Next(30f, 120f));
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleFlags(((IAprilFoolsRole)this).HideIfNotAprilFools(RoleFlags))
            .RoleColor(new Color(0.46f, 0.6f, 0.3f));
}