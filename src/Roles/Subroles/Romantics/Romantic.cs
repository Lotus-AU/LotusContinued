using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Options;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Utilities;
using Lotus.Victory;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using System.Collections.Generic;
using Lotus.API.Vanilla.Meetings;
using Lotus.GameModes.Normal.Standard;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interfaces;

namespace Lotus.Roles.Subroles.Romantics;

public class Romantic : Subrole, IInfoResender
{
    public static Color RomanticColor = new(1f, 0.28f, 0.47f);
    private static VengefulRomantic _vengefulRomantic = new();
    private static RuthlessRomantic _ruthlessRomantic = new();

    private bool targetKnowsRomantic;
    private Cooldown protectionCooldown = null!;
    private Cooldown protectionDuration = null!;
    private bool parterCanWin;
    private bool cancelGameWin;

    private bool partnerLockedIn;
    private byte partner = byte.MaxValue;
    private IRemote? winDelegateRemote;
    private RomanticFaction myFaction = null!;
    private IFaction originalFaction = null!;

    public override string Identifier() => "♥";

    protected override void PostSetup()
    {
        DisplayOrder = 100;
        winDelegateRemote = Game.GetWinDelegate().AddSubscriber(InterceptWinCondition);
        CustomRole myRole = MyPlayer.PrimaryRole();
        originalFaction = myRole.Faction;
        myRole.Faction = myFaction = new RomanticFaction(originalFaction);
    }

    public void ResendMessages() => RHandler(Translations.RomanticMessage).Send(MyPlayer);

    [RoleAction(LotusActionType.OnPet, priority: Priority.VeryHigh)]
    public void HandlePet(ActionHandle handle)
    {
        PlayerControl? love = Players.FindPlayerById(partner);
        if (love == null || !love.IsAlive()) return;
        if (protectionCooldown.NotReady() || protectionDuration.NotReady()) return;
        handle.Cancel();
        protectionDuration.StartThenRun(() => protectionCooldown.Start());
    }

    [RoleAction(LotusActionType.RoundEnd)]
    public void SendMeetingMessage()
    {
        if (partner != byte.MaxValue) return;
        ResendMessages();
    }

    [RoleAction(LotusActionType.Interaction, ActionFlag.GlobalDetector)]
    public void InterceptActions(PlayerControl target, PlayerControl _, Interaction interaction, ActionHandle handle)
    {
        if (protectionDuration.IsReady()) return;
        if (interaction.Intent is not IFatalIntent) return;
        if (target.PlayerId == partner) handle.Cancel();
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void CheckPartnerDeath(PlayerControl dead, PlayerControl killer)
    {
        if (dead.PlayerId != partner) return;
        MyPlayer.GetSubroles().Remove(this);
        winDelegateRemote?.Delete();
        if (Random.RandomRange(0, 100) < _ruthlessRomantic.Chance) Game.AssignSubRole(MyPlayer, _ruthlessRomantic);
        else
        {
            Game.AssignSubRole(MyPlayer, _vengefulRomantic);
            MyPlayer.PrimaryRole().Faction = FactionInstances.Neutral;
            MyPlayer.GetSubrole<VengefulRomantic>()?.SetupVengeful(killer, originalFaction);
        }
    }

    [RoleAction(LotusActionType.Vote, priority: Priority.High)]
    public void SetPartner(Optional<PlayerControl> votedPlayer, MeetingDelegate _, ActionHandle handle)
    {
        if (!partnerLockedIn) handle.Cancel();
        else return;
        if (!votedPlayer.Exists())
        {
            RHandler(Translations.NoSkipMessage).Send(MyPlayer);
            return;
        }

        PlayerControl player = votedPlayer.Get();

        byte votedId = player.PlayerId;

        if (votedId == MyPlayer.PlayerId) return;

        if (votedId != partner)
        {
            partner = votedId;
            RHandler(Translations.RomanticSelectMessage.Formatted(player.name)).Send(MyPlayer);
            return;
        }

        partnerLockedIn = true;
        myFaction.Partner = votedId;
        RHandler(Translations.ConfirmedPartnerMessage.Formatted(player.name)).Send(MyPlayer);

        string partnerText = TranslationUtil.Colorize(Translations.PartnerIndicator.Formatted(player.name), RoleColor);
        NameComponent nameComponent = new(new LiveString(partnerText), Game.InGameStates, ViewMode.Replace, MyPlayer);
        player.NameModel().GCH<NameHolder>().Add(nameComponent);
        LiveString protectionIndicator = new(() => protectionDuration.NotReady() ? RoleColor.Colorize(Identifier()) : "");
        player.NameModel().GCH<IndicatorHolder>().Add(new IndicatorComponent(protectionIndicator, GameState.Roaming, ViewMode.Additive, MyPlayer));

        if (!targetKnowsRomantic) return;

        string myText = TranslationUtil.Colorize(Translations.PartnerIndicator.Formatted(MyPlayer.name), RoleColor);
        nameComponent = new NameComponent(new LiveString(myText), Game.InGameStates, ViewMode.Replace, player);
        MyPlayer.NameModel().GCH<NameHolder>().Add(nameComponent);
        RHandler(Translations.NotifyPartnerMessage.Formatted(MyPlayer.name)).Send(player);
    }

    [RoleAction(LotusActionType.MeetingEnd)]
    public void KillIfUndecided(bool _, bool isForceEnd)
    {
        if (!partnerLockedIn && !isForceEnd)
        {
            ProtectedRpc.CheckMurder(MyPlayer, MyPlayer);
            Game.MatchData.GameHistory.SetCauseOfDeath(MyPlayer.PlayerId, new SuicideEvent(MyPlayer));
        }
    }

    private void InterceptWinCondition(WinDelegate winDelegate)
    {
        Optional<PlayerControl> partnerControl = Players.PlayerById(partner);
        if (!partnerControl.Exists()) return; // if we win round1, do nothing
        if (!partnerControl.Get().IsAlive()) return; // if they died, do nothing.
        NormalStandardGameMode.Instance.Assign(partnerControl.Get(), new Partner(), false);
        bool hasRomantic = winDelegate.GetWinners().Any(w => w.PlayerId == MyPlayer.PlayerId);
        bool hasPartner = winDelegate.GetWinners().Any(w => w.PlayerId == partner);
        if ((!hasRomantic && !hasPartner) || (hasRomantic && hasPartner)) return; // includes neither or both us so we skip.
        if (parterCanWin)
        {
            if (hasRomantic && !hasPartner) winDelegate.AddAdditionalWinner(partnerControl.Get()); // add partner if we win
            else if (hasPartner && !hasRomantic) winDelegate.AddAdditionalWinner(MyPlayer); // add us if partner wins
            return;
        }
        if (hasPartner) // add us without questions if partner wins
        {
            winDelegate.AddAdditionalWinner(MyPlayer);
            return;
        }

        if (winDelegate.GetWinners().Count != 1) winDelegate.RemoveWinner(MyPlayer);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddRestrictToCrew(base.RegisterOptions(optionStream))
            .SubOption(sub => sub.KeyName("Notify Target of Romance", Translations.Options.TargetKnowsRomantic)
                .AddBoolean()
                .BindBool(b => targetKnowsRomantic = b)
                .Build())
            .SubOption(sub => sub.KeyName("Protection Cooldown", Translations.Options.ProtectionCooldown)
                .AddFloatRange(0, 120, 2.5f, 12, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(protectionCooldown.SetDuration)
                .Build())
            .SubOption(sub => sub.KeyName("Protection Duration", Translations.Options.ProtectionDuration)
                .AddFloatRange(0, 120, 2.5f, 2, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(protectionDuration.SetDuration)
                .Build())
            .SubOption(sub => sub.KeyName("Partner can Win with Romantic", Translations.Options.PartnerCanWin)
                .AddBoolean()
                .BindBool(b => parterCanWin = b)
                .ShowSubOptionPredicate(v => !(bool)v)
                .Build());

    public override List<CustomRole> LinkedRoles() => base.LinkedRoles().Concat(new List<CustomRole>() { _vengefulRomantic, _ruthlessRomantic }).ToList();
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(RomanticColor)
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet);

    private ChatHandler RHandler(string message) => new ChatHandler().Title(t => t.PrefixSuffix(Identifier()).Color(RoleColor).Text(RoleName).Build()).LeftAlign().Message(message);

    [Localized(nameof(Romantic))]
    private static class Translations
    {
        [Localized(nameof(PartnerText))] public static string PartnerText = "Partner ♥";

        public static string PartnerIndicator = "{0} ♥::0";

        [Localized(nameof(RomanticMessage))] public static string RomanticMessage = "You are a Romantic. You must select a partner by the end of this meeting or die! To select a partner, vote them twice! Afterwards, you may vote normally.";

        [Localized(nameof(RomanticSelectMessage))] public static string RomanticSelectMessage = "You have selected {0} to be your partner. To confirm, vote them again, otherwise vote a different player.";

        [Localized(nameof(ConfirmedPartnerMessage))] public static string ConfirmedPartnerMessage = "You have confirmed {0} to be your partner. You may now vote normally";

        [Localized(nameof(NotifyPartnerMessage))] public static string NotifyPartnerMessage = "You have been selected by {0} to be their romantic partner! Congrats!";

        [Localized(nameof(NoSkipMessage))] public static string NoSkipMessage = "You may not skip until you have chosen a partner!";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(TargetKnowsRomantic))] public static string TargetKnowsRomantic = "Notify Target of Romance";

            [Localized(nameof(ProtectionCooldown))] public static string ProtectionCooldown = "Protection Cooldown";

            [Localized(nameof(ProtectionDuration))] public static string ProtectionDuration = "Protection Duration";

            [Localized(nameof(PartnerCanWin))] public static string PartnerCanWin = "Partner can win with Romantic";

            [Localized(nameof(CancelGameWinIfSolo))] public static string CancelGameWinIfSolo = "Cancel Game Win if Solo Winner";
        }
    }
}