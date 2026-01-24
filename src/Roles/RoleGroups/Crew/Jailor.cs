using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Vanilla.Meetings;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Patches.Systems;
using Lotus.Roles.Events;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.RPC;
using Lotus.Utilities;
using UnityEngine;
using VentLib;
using VentLib.Localization.Attributes;
using VentLib.Networking.RPC.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using static Lotus.Roles.RoleGroups.Crew.Sheriff.Translations.Options;

namespace Lotus.Roles.RoleGroups.Crew;

public class Jailor: Crewmate, IRoleUI
{
    // Settings
    [UIComponent(UI.Cooldown)] private Cooldown jailCooldown;
    private int maxExecutionTimes;
    private bool suicideOnCrewmateExecuted;
    private bool jailedPlayerVoteCounts;
    private bool jailedShownToAll;
    private bool useKillButton;

    // Variables
    private int curExecutionTimes;
    private byte jailedPlayer;
    private bool canExecute;

    private Remote<TextComponent>? jailedText;

    public RoleButton KillButton(IRoleButtonEditor editor) => useKillButton
        ? editor
            .SetText(Translations.ButtonText)
            .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/jailor_jail.png", 130, true))
        : editor.Default(true);

    public RoleButton PetButton(IRoleButtonEditor editor) => useKillButton
        ? editor.Default(true)
        : editor
            .BindCooldown(jailCooldown)
            .SetText(Translations.ButtonText)
            .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/jailor_jail.png", 130, true));

    protected override void PostSetup()
    {
        base.PostSetup();
        curExecutionTimes = maxExecutionTimes;
        canExecute = true;
        jailedPlayer = byte.MaxValue;
    }

    [RoleAction(LotusActionType.RoundStart, ActionFlag.WorksAfterDeath)]
    private void RoundStart(bool isRoundOne)
    {
        jailedPlayer = byte.MaxValue;
        jailedText?.Delete();
        if (!useKillButton)
        {
            jailCooldown.Start(isRoundOne ? 10 : float.MinValue);
            if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateJailor)?.Send([MyPlayer.OwnerId], isRoundOne);
        }
    }

    [RoleAction(LotusActionType.OnPet)]
    private void TryPet(ActionHandle handle)
    {
        if (useKillButton) return;
        List<PlayerControl> closestPlayers = MyPlayer.GetPlayersInAbilityRangeSorted();
        if (closestPlayers.Count == 0) return;
        if (jailCooldown.NotReady()) return;
        jailCooldown.Start();
        if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateJailor)?.Send([MyPlayer.OwnerId], false);
        TryKill(closestPlayers[0]);
    }

    [RoleAction(LotusActionType.Attack)]
    private bool TryKill(PlayerControl target)
    {
        MyPlayer.RpcMark(target);
        InteractionResult result = MyPlayer.InteractWith(target, LotusInteraction.NeutralInteraction.Create(this));
        if (result is InteractionResult.Proceed)
        {
            jailedPlayer = target.PlayerId;
            jailedText?.Delete();
            TextComponent textComponent = new(new LiveString(Translations.JailedText, RoleColor), Game.InGameStates, viewers: MyPlayer);
            jailedText = target.NameModel().GetComponentHolder<TextHolder>().Add(textComponent);
            return true;
        }
        return false;
    }

    [RoleAction(LotusActionType.Vote)]
    private void TryVote(Optional<PlayerControl> voted, MeetingDelegate @delegate, ActionHandle handle)
    {
        if (jailedPlayer == byte.MaxValue) return;
        PlayerControl? jailed = Utils.GetPlayerById(jailedPlayer);
        if (jailed == null) return;

        if (voted.Exists())
        {
            PlayerControl votedPlayer = voted.Get();

            if (votedPlayer.PlayerId == jailedPlayer)
            {
                if (!canExecute || curExecutionTimes <= 0)
                {
                    handle.Cancel();
                    ChatHandler.Of(Translations.CannotVote, RoleColor.Colorize(RoleName)).Send(MyPlayer);
                    return;
                }

                curExecutionTimes--;
                jailedPlayer = byte.MaxValue;
                bool playerIsCrew = jailed.Relationship(FactionInstances.Crewmates) is Relation.FullAllies;
                MyPlayer.InteractWith(jailed, new UnblockedInteraction(new FatalIntent(true, () => new ExecutedDeathEvent(jailed,MyPlayer)), this));
                if (playerIsCrew)
                {
                    canExecute = false;
                    if (suicideOnCrewmateExecuted)
                        MyPlayer.InteractWith(MyPlayer,
                            new UnblockedInteraction(new FatalIntent(true, () => new MisfiredEvent(MyPlayer)), this));
                }
                handle.Cancel();

                if (jailedShownToAll) ChatHandler.Of(Translations.PublicExecuteMessage.Formatted(jailed.name), RoleColor.Colorize(RoleName)).Send();
                return;
            }
            if (jailedPlayerVoteCounts) @delegate.CastVote(jailed, voted);
            return;
        }
        if (!jailedPlayerVoteCounts) return;

        @delegate.CastVote(jailed, voted);
    }

    [RoleAction(LotusActionType.Vote, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath, priority: Priority.First)]
    private void OnOtherTryVote(PlayerControl voter, Optional<PlayerControl> voted, MeetingDelegate @delegate, ActionHandle handle)
    {
        if (jailedPlayer == byte.MaxValue) return;
        if (voter.PlayerId == MyPlayer.PlayerId) return;
        if (voter.PlayerId == jailedPlayer)
        {
            handle.Cancel();
            return;
        }

        if (voted.Exists())
        {
            PlayerControl votedPlayer = voted.Get();
            if (jailedShownToAll && votedPlayer.PlayerId == jailedPlayer)
            {
                handle.Cancel();
                ChatHandler.Of(Translations.CannotVote, RoleColor.Colorize(RoleName)).Send(voter);
            }
        }
    }

    [RoleAction(LotusActionType.VotingComplete, priority: Priority.Last)]
    private void ClearVotedOut(MeetingDelegate meetingDelegate)
    {
        if (meetingDelegate.ExiledPlayer == null) return;
        if (meetingDelegate.ExiledPlayer.PlayerId == jailedPlayer) meetingDelegate.ExiledPlayer = null;
    }

    [RoleAction(LotusActionType.RoundEnd, ActionFlag.WorksAfterDeath)]
    private void OnRoundEnd()
    {
        if (jailedPlayer == byte.MaxValue) return;
        Optional<PlayerControl> jailed = Utils.PlayerById(jailedPlayer);

        List<PlayerControl> allPlayers = jailedShownToAll
            ? Players.GetAllPlayers().ToList()
            : jailed.Transform<List<PlayerControl>>(p => [p, MyPlayer], () => [MyPlayer]);
        if (!jailedText?.IsDeleted() ?? false) jailedText.Get().SetViewerSupplier(() => allPlayers);

        ChatHandler.Of(Translations.JailorMessage, RoleColor.Colorize(RoleName)).Send(MyPlayer);
        jailed.IfPresent(p => ChatHandler.Of(Translations.JailedMessage, RoleColor.Colorize(RoleName)).Send(p));
    }

    [ModRPC((uint)ModCalls.UpdateJailor, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateJailor(bool isRoundOne)
    {
        Jailor? jailor = PlayerControl.LocalPlayer.PrimaryRole<Jailor>();
        if (jailor == null) return;
        jailor.jailCooldown.Start(isRoundOne ? 10 : float.MinValue);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => base.RegisterOptions(optionStream)
        .Color(RoleColor)
        .SubOption(sub => sub
            .KeyName("Jail Cooldown", Translations.Options.JailCooldown)
            .AddFloatRange(1, 15, 1, 9)
            .BindFloat(jailCooldown.SetDuration)
            .Build())
        .SubOption(sub => sub
            .KeyName("Max Execution Times", Translations.Options.MaxExecutionTimes)
            .AddIntRange(1, ModConstants.MaxPlayers, 1)
            .BindInt(i => maxExecutionTimes = i)
            .Build())
        .SubOption(sub => sub
            .KeyName("Jailed Vote Count", Translations.Options.JailedPlayersVoteCounts)
            .AddBoolean()
            .BindBool(b => jailedPlayerVoteCounts = b)
            .Build())
        .SubOption(sub => sub
            .KeyName("Jailor Action Button", Translations.Options.JailorActionButton)
            .Bind(v => useKillButton = (bool)v)
            .Value(v => v.Text(ActionKillButton).Value(true).Color(Color.green).Build())
            .Value(v => v.Text(ActionPetButton).Value(false).Color(Color.cyan).Build())
            .Build())
        .SubOption(sub => sub
            .KeyName("Suicide when Crewmate Executed",
                TranslationUtil.Colorize(Dictator.DictatorTranslations.DictatorOptionTranslations.SuicideIfVoteCrewmate,
                    ModConstants.Palette.CrewmateColor))
            .AddBoolean()
            .BindBool(b => suicideOnCrewmateExecuted = b)
            .Build())
        .SubOption(sub => sub
            .KeyName("Jailed Shown to All", Translations.Options.JailedShowsToAll)
            .AddBoolean()
            .BindBool(b => jailedShownToAll = b)
            .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleColor(new Color(.65f, .65f, .65f))
        .DesyncRole(useKillButton ? RoleTypes.Impostor : RoleTypes.Crewmate)
        .OptionOverride(Override.ImpostorLightMod, () => AUSettings.CrewLightMod(), () => useKillButton)
        .OptionOverride(Override.ImpostorLightMod, () => AUSettings.CrewLightMod() / 5, () => useKillButton && SabotagePatch.CurrentSabotage != null && SabotagePatch.CurrentSabotage.SabotageType() is SabotageType.Lights)
        .IntroSound(RoleTypes.Crewmate)
        .RoleAbilityFlags(!useKillButton ? (RoleAbilityFlags & RoleAbilityFlag.UsesPet) : RoleAbilityFlags, true)
        .OptionOverride(new IndirectKillCooldown(jailCooldown.Duration));

    [Localized(nameof(Jailor))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))] public static string ButtonText = "Jail";

        [Localized(nameof(JailedText))] public static string JailedText = "JAILED";

        [Localized(nameof(JailorMessage))] public static string JailorMessage = "Vote the jailed player to execute them. Depending on settings, you may die if they are crewmate. They may also be forced to vote the same person as you. No one can vote the Jailee, and they cannot be killed unless by you.";
        [Localized(nameof(JailedMessage))] public static string JailedMessage = "You are Jailed. You cannot vote or guess. However, players cannot vote you either. You can hope the Jailor does not execute you.";

        [Localized(nameof(PublicExecuteMessage))] public static string PublicExecuteMessage = "The Jailed Player ({0}) was executed.";

        [Localized(nameof(CannotVote))] public static string CannotVote = "You cannot vote the Jailed Player!";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(JailCooldown))] public static string JailCooldown = "Jail Cooldown";
            [Localized(nameof(JailorActionButton))] public static string JailorActionButton = "Jailor Action Button";
            [Localized(nameof(MaxExecutionTimes))] public static string MaxExecutionTimes = "Max Execution Times";
            [Localized(nameof(JailedPlayersVoteCounts))] public static string JailedPlayersVoteCounts = "Jailed Player's Vote Counts for Jailor";
            [Localized(nameof(JailedShowsToAll))] public static string JailedShowsToAll = "Jailed Player is Shown to All";
        }
    }
}