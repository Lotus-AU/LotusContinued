using Lotus.API.Odyssey;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Managers.History.Events;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interfaces;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities;

namespace Lotus.Roles.RoleGroups.Impostors;
// IModdable
public class SerialKiller : Impostor
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(SerialKiller));
    private bool paused = true;
    public Cooldown DeathTimer = null!;
    private bool beginsAfterFirstKill;

    private bool hasKilled;

    [UIComponent(UI.Counter)]
    private string CustomCooldown() => (!MyPlayer.IsAlive() || DeathTimer.IsReady()) ? "" : Color.white.Colorize(DeathTimer + "s");

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        bool success = base.TryKill(target);
        if (!success) return false;

        hasKilled = true;
        paused = false;
        DeathTimer.Start();
        return success;
    }

    [RoleAction(LotusActionType.FixedUpdate)]
    private void CheckForSuicide()
    {
        if (MyPlayer == null) return;
        if (paused || DeathTimer.NotReady() || !MyPlayer.IsAlive()) return;

        if (Game.State is GameState.InMeeting)
        {
            paused = true;
            return;
        }

        log.Trace($"Serial Killer ({MyPlayer.name}) Commiting Suicide", "SerialKiller::CheckForSuicide");

        MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(), this));
        Game.MatchData.GameHistory.AddEvent(new SuicideEvent(MyPlayer));
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void SetupSuicideTimer(bool gameStart)
    {
        paused = beginsAfterFirstKill && !hasKilled;
        if (!paused)
        {
            DevLogger.Log("Restarting Timer");
            DeathTimer.Start(gameStart ? GeneralOptions.GameplayOptions.GetFirstKillCooldown(MyPlayer) + DeathTimer.Duration : float.MinValue);
        }
    }

    [RoleAction(LotusActionType.RoundEnd)]
    private void StopDeathTimer() => paused = true;

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddKillCooldownOptions(base.RegisterOptions(optionStream), defaultIndex: 4)
            .SubOption(sub => sub
                .KeyName("Time Until Suicide", SerialKillerTranslations.SerialKillerOptionTranslations.TimeUntilSuicide)
                .Bind(v => DeathTimer.Duration = (float)v)
                .AddFloatRange(5, 120, 2.5f, 30, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Timer Begins After First Kill", SerialKillerTranslations.SerialKillerOptionTranslations.TimerAfterFirstKill)
                .BindBool(b => beginsAfterFirstKill = b)
                .AddOnOffValues(false)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).OptionOverride(Override.KillCooldown, () => KillCooldown);


    [Localized(nameof(SerialKiller))]
    private static class SerialKillerTranslations
    {
        [Localized(ModConstants.Options)]
        public static class SerialKillerOptionTranslations
        {
            [Localized(nameof(TimeUntilSuicide))]
            public static string TimeUntilSuicide = "Time Until Suicide";

            [Localized(nameof(TimerAfterFirstKill))]
            public static string TimerAfterFirstKill = "Timer Begins After First Kill";
        }
    }
}