using System;

namespace Lotus.src.Managers.Blackscreen.Interfaces;

public interface IBlackscreenResolver
{
    /// <summary>
    /// This function is ran after the Meeting ends.
    /// </summary>
    void OnMeetingEnd();
    /// <summary>
    /// This function is ran in the postifx of ExileController.WrapUp.
    /// </summary>
    void FixBlackscreens(Action runOnFinish);
}