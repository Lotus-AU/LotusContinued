using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Vanilla.Meetings;
using Lotus.Utilities;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.Internals.Enums;

public enum LotusActionType
{
    /// <summary>
    /// Represents no action
    /// </summary>
    None,
    /// <summary>
    /// Any action specifically taken by a player
    /// Parameters: (RoleAction action, object[] parameters)
    /// </summary>
    /// <param name="source"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player doing the action</param>
    PlayerAction,
    /// <summary>
    /// Triggers when a player pets
    /// </summary>
    /// <param name="source"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player petting</param>
    OnPet,
    /// <summary>
    /// Triggers when the pet button is held down. This gets sent every 0.4 seconds if the button is held down. The
    /// times parameter indicates how many times the button has been held down during the current span.
    /// <br/>
    /// Example: if times = 3 then the button has been held down for 1.2 seconds because 3 x 0.4 = 1.2
    /// </summary>
    /// <param name="source"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player holding the pet</param>
    /// <param name="times">the number of times the button has been detected in the down state (+1 every 0.4 seconds)</param>
    OnHoldPet,
    /// <summary>
    /// Triggers when the pet button has been held then released. Similar to <see cref="OnHoldPet"/>, the
    /// times parameter indicates how many times the button has been held down during the current span.
    /// </summary>
    /// <param name="source"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player holding the pet</param>
    /// <param name="times">the number of times the button has been detected in the down state (+1 every 0.4 seconds)</param>
    OnPetRelease,
    /// <summary>
    /// Triggers whenever the player enters a vent (this INCLUDES vent activation)
    /// Parameters: (Vent vent)
    /// </summary>
    /// <param name="source"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player doing the action</param>
    VentEntered,
    VentExit,
    SuccessfulAngelProtect,
    SabotageStarted,
    /// <summary>
    /// Triggered when any one player fixes any part of a sabotage (I.E MiraHQ Comms) <br></br>
    /// Parameters: (ISabotage CurrentSabotage)
    /// </summary>
    /// <param name="fixer"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player fixing sabotage</param>
    SabotagePartialFix,
    SabotageFixed,
    Shapeshift,
    Unshapeshift,
    /// <summary>
    /// Triggered when a player uses the kill button on another player.<br/>
    /// </summary>
    /// <param name="attacker"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player who is attacking</param>
    /// <param name="target"><see cref="PlayerControl"/> the player who is being attacked</param>
    Attack,
    /// <summary>
    /// Triggers when a player dies. This cannot be canceled
    /// </summary>
    /// <param name="victim"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the dead player</param>
    /// <param name="killer"><see cref="Optional"/> an optional of <see cref="FrozenPlayer"/> the killing player</param>
    /// <param name="deathEvent"><see cref="Lotus.Managers.History.Events.IDeathEvent"/> the related death event </param>
    PlayerDeath,
    /// <summary>
    /// Triggers when any player gets exiled (by being voted out)
    /// </summary>
    /// <param name="victim"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the exiled player</param>
    Exiled,
    /// <summary>
    /// Triggers on Round Start (end of meetings, and start of game)
    /// Parameters: (bool isRoundOne)
    /// </summary>
    RoundStart,
    RoundEnd,
    /// <summary>
    /// Triggers when any player reports a body or calls a meeting. <br></br>Parameters: (<see cref="Optional{NetworkedPlayerInfo}"/>)
    /// </summary>
    /// <param name="reporter"><b>(GLOBAL ONLY)</b><see cref="PlayerControl"/> the player reporting the body.</param>
    /// <param name="target"><see cref="Optional"/> an optional of <see cref="NetworkedPlayerInfo"/>, containing the player being reported. Null if meeting.</param>
    ReportBody,
    /// <summary>
    /// Triggers when any player completes a task. This cannot be canceled (Currently)
    /// </summary>
    /// <param name="player"><b>(GLOBAL ONLY)</b><see cref="PlayerControl"/> the player completing the task</param>
    /// <param name="task"><see cref="Optional"/> an optional of <see cref="PlayerTask"/>, containing the task that was done</param>
    /// <param name="taskLength"><see cref="NormalPlayerTask.TaskLength"/> the length of the completed task</param>
    TaskComplete,
    /// <summary>
    /// Fires for every player in the FixedUpdate of PlayerControl.
    /// Called every 0.02 seconds, or 50 times per second.<br/>
    /// Use with <see cref="FixedUpdateLock"/> to limit how often your function runs.
    /// </summary>
    FixedUpdate,
    /// <summary>
    /// Triggers when my player votes for someone (or skips)
    /// </summary>
    /// <param name="voter"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player voting</param>
    /// <param name="voted">An <see cref="Optional{T}"/> of <see cref="PlayerControl"/> the player voted for, or null if skipped</param>
    /// <param name="delegate"><see cref="MeetingDelegate"/> the meeting delegate for the current meeting</param>
    Vote,
    /// <summary>
    /// Triggers whenever another player interacts with THIS role. (Use GlobalDetector for any interaction)
    /// </summary>
    /// <param name="target"><b>(GLOBAL ONLY)</b> <see cref="PlayerControl"/> the player being interacted with</param>
    /// <param name="interactor"><see cref="PlayerControl"/> the player starting the interaction</param>
    /// <param name="interaction"><see cref="Interaction"/> the interaction</param>
    Interaction,
    /// <summary>
    /// Triggers whenever a player sends a chat message. This action cannot be canceled. Messages starting with "/cmd" are hidden from everyone.
    /// </summary>
    /// <param name="sender"><b>(GLOBAL ONLY)</b><see cref="PlayerControl"/> the player who sent the chat message</param>
    /// <param name="message"><see cref="string"/> the message sent</param>
    /// <param name="state"><see cref="GameState"/> the current state of the game (for checking in meeting)</param>
    /// <param name="isAlive"><see cref="bool"/> if the chatting player is alive</param>
    Chat,
    /// <summary>
    /// Triggers whenever a player leaves the game. This action cannot be canceled
    /// </summary>
    /// <param name="player"><b>(GLOBAL ONLY)</b><see cref="PlayerControl"/> the player who disconnected</param>
    Disconnect,
    /// <summary>
    /// Triggers when voting session ends. This action cannot be canceled.
    /// <b>IMPORTANT</b><br/>
    /// You CAN modify the meeting delegate at this time to change the results of the meeting. HOWEVER,
    /// modifying the votes will only change what is displayed during the meeting. You MUST also update the exiled player to change
    /// the exiled player, as the votes WILL NOT be recalculated automatically at this point.
    /// </summary>
    /// <param name="meetingDelegate"><see cref="MeetingDelegate"/> the meeting delegate for the current meeting</param>
    VotingComplete,
    /// <summary>
    /// Triggers when the meeting ends, this does not pass the meeting delegate as at this point everything has been finalized.
    /// </summary>
    /// <param name="Exiled Player"><see cref="Optional{T}"/> the optional exiled player</param>
    /// <param name="isTie"><see cref="bool"/> a boolean representing if the meeting tied</param>
    /// <param name="player vote counts"><see cref="Dictionary{TKey,TValue}"/> a dictionary containing (byte, int) representing the amount of votes a player got</param>
    /// <param name="playerVoteStatus"><see cref="Dictionary{TKey,TValue}"/> a dictionary containing (byte, List[Optional[byte]] containing the voting statuses of all players)</param>
    /// <param name="isForceEnd"><see cref="bool"/> a boolean representing whether the meeting ended naturally. </param>
    MeetingEnd,
    /// <summary>
    /// Triggers when the player tries to Vanish as Phantom.
    /// </summary>
    /// <param name="player"><b>(GLOBAL ONLY)</b><see cref="PlayerControl"/>the player who is vanishing</param>
    Vanish,
    /// <summary>
    /// Triggers when the player tries to Appear as Phantom.
    /// </summary>
    /// <param name="player"><b>(GLOBAL ONLY)</b><see cref="PlayerControl"/>the player who is appearing</param>
    Appear,
    /// <summary>
    /// Triggers when a player uses the Zipline in the Fungle map.
    /// </summary>
    /// <param name="player"><b>(GLOBAL ONLY)</b><see cref="PlayerControl"/> the player who used the zipline</param>
    /// <param name="ziplineBehaviour"><see cref="ZiplineBehaviour"/>The zipline the player is using.</param>
    /// <param name="fromTop">A boolean representing whether the player is coming from the top.</param>
    Zipline
}