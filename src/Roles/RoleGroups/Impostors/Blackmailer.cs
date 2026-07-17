using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.Chat;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Optionals;
using static Lotus.Roles.RoleGroups.Impostors.Blackmailer.BlackmailerTranslations;
using Lotus.API.Player;
using Lotus.GUI;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Commands;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Blackmailer : Shapeshifter, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Blackmailer));
    private Remote<TextComponent>? blackmailingText;
    private Optional<PlayerControl> blackmailedPlayer = Optional<PlayerControl>.Null();
    private bool inBlackmailingMode = true;

    private bool showBlackmailedToAll;
    private bool usesShapeshifter;

    private int warnsUntilKick;
    private int currentWarnings;

    [UIComponent(UI.Text)]
    private string ModeDisplay() => usesShapeshifter ? "" : Color.red.Colorize(inBlackmailingMode ? BlackmailingMode : Witch.Translations.KillingModeText);

    public RoleButton AbilityButton(IRoleButtonEditor editor) => !usesShapeshifter
        ? editor.Default(true)
        : editor.SetText(BlackmailButtonText).SetSprite(() => LotusAssets.LoadSprite("Buttons/Imp/blackmailer_blackmail.png", 130, true));

    public RoleButton KillButton(IRoleButtonEditor editor) => usesShapeshifter || !inBlackmailingMode
        ? editor.Default(true)
        : editor.SetText(BlackmailButtonText).SetSprite(() => LotusAssets.LoadSprite("Buttons/Imp/blackmailer_blackmail.png", 130, true));

    public RoleButton PetButton(IRoleButtonEditor petButton) => usesShapeshifter
        ? petButton.Default(true)
        : petButton.SetText(RoleTranslations.Switch)
            .SetSprite(() => LotusAssets.LoadSprite("Buttons/generic_switch_ability.png", 130, true));

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (usesShapeshifter | !inBlackmailingMode) return base.TryKill(target);

        InteractionResult result = MyPlayer.InteractWith(target, LotusInteraction.HostileInteraction.Create(this));
        if (result is InteractionResult.Halt) return false;
        TryBlackmail(target);
        return false;
    }

    [RoleAction(LotusActionType.Shapeshift)]
    public void Blackmail(PlayerControl target, ActionHandle handle)
    {
        handle.Cancel();
        if (!usesShapeshifter) return;
        TryBlackmail(target);
    }

    private void TryBlackmail(PlayerControl target)
    {
        blackmailingText?.Delete();
        blackmailedPlayer = Optional<PlayerControl>.NonNull(target);
        TextComponent textComponent = new(new LiveString(BlackmailedText, Color.red), Game.InGameStates, viewers: MyPlayer);
        blackmailingText = target.NameModel().GetComponentHolder<TextHolder>().Add(textComponent);
    }

    [RoleAction(LotusActionType.RoundStart, ActionFlag.WorksAfterDeath)]
    public void ClearBlackmail()
    {
        blackmailedPlayer = Optional<PlayerControl>.Null();
        currentWarnings = 0;
        blackmailingText?.Delete();
    }

    [RoleAction(LotusActionType.RoundEnd, ActionFlag.WorksAfterDeath)]
    public void NotifyBlackmailed()
    {
        if (!blackmailedPlayer.Exists()) return;
        List<PlayerControl> allPlayers = showBlackmailedToAll
            ? Players.GetAllPlayers().ToList()
            : blackmailedPlayer.Transform<List<PlayerControl>>(p => [p, MyPlayer], () => [MyPlayer]);
        if (!blackmailingText?.IsDeleted() ?? false) blackmailingText?.Get().SetViewerSupplier(() => allPlayers);
        blackmailedPlayer.IfPresent(p =>
        {
            string message = $"{RoleColor.Colorize(MyPlayer.name)} blackmailed {p.GetRoleColor().Colorize(p.name)}.";
            Game.MatchData.GameHistory.AddEvent(new GenericTargetedEvent(MyPlayer, p, message));
            ChatHandler.Of(BlackmailedMessage, RoleColor.Colorize(RoleName)).Send(p);
        });
    }

    [RoleAction(LotusActionType.Exiled)]
    [RoleAction(LotusActionType.PlayerDeath)]
    private void BlackmailerDies()
    {
        blackmailedPlayer = Optional<PlayerControl>.Null();
        blackmailingText?.Delete();
    }

    [RoleAction(LotusActionType.OnPet)]
    private void TrySwitchMode()
    {
        if (usesShapeshifter) return;
        inBlackmailingMode = !inBlackmailingMode;
        if (MyPlayer.AmOwner) UpdateKillButton(inBlackmailingMode);
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateBlackmailer)?.Send([MyPlayer.OwnerId], inBlackmailingMode);
    }

    [RoleAction(LotusActionType.Chat, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath)]
    public void InterceptChat(PlayerControl speaker, string message, GameState state, bool isAlive)
    {
        if (!isAlive || state is not GameState.InMeeting) return;
        if (!blackmailedPlayer.Exists() || speaker.PlayerId != blackmailedPlayer.Get().PlayerId) return;
        if (message.StartsWith(CommandRunner.Prefix)) return;
        if (currentWarnings++ < warnsUntilKick)
        {
            ChatHandler.Of(WarningMessage, RoleColor.Colorize(RoleName)).Send(speaker);
            return;
        }

        log.Trace($"Blackmailer Killing Player: {speaker.name}");
        MyPlayer.InteractWith(speaker, new UnblockedInteraction(new FatalIntent(), this));
    }

    [RoleAction(LotusActionType.Disconnect, ActionFlag.GlobalDetector)]
    private void CheckForDisconnect(PlayerControl disconnecter)
    {
        if (!blackmailedPlayer.Exists() || disconnecter.PlayerId != blackmailedPlayer.Get().PlayerId) return;
        ClearBlackmail();
    }

    [ModRPC((uint)ModCalls.UpdateBlackmailer, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateBlackmailer(bool inBlackmailMode)
    {
        Blackmailer? blackmailer = PlayerControl.LocalPlayer.PrimaryRole<Blackmailer>();
        if (blackmailer == null) return;
        blackmailer.UpdateKillButton(inBlackmailMode);
    }

    public override void HandleDisconnect() => ClearBlackmail();

    public void UpdateKillButton(bool inBlackmailMode)
    {
        inBlackmailingMode = inBlackmailMode;
        RoleButton killButton = UIManager.KillButton;
        if (inBlackmailingMode)
            killButton.SetText(BlackmailButtonText).SetSprite(() =>
                LotusAssets.LoadSprite("Buttons/Imp/blackmailer_blackmail.png", 130, true));
        else killButton.RevertSprite().SetText(Witch.Translations.KillButtonText);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Warnings Until Death", BlackmailerTranslations.Options.WarningsUntilDeath)
                .AddIntRange(0, 5, 1)
                .BindInt(i => warnsUntilKick = i)
                .Build())
            .SubOption(sub => sub.KeyName("Show Blackmailed to All", BlackmailerTranslations.Options.ShowBlackmailedToAll)
                .AddBoolean()
                .BindBool(b => showBlackmailedToAll = b)
                .Build())
            .SubOption(sub => sub.KeyName("Uses Shapeshifter", BlackmailerTranslations.Options.BlackmailerIsSSBased)
                .AddBoolean(false)
                .BindBool(b => usesShapeshifter = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .VanillaRole(usesShapeshifter ? RoleTypes.Shapeshifter : RoleTypes.Impostor)
        .RoleAbilityFlags(usesShapeshifter ? RoleAbilityFlags : RoleAbilityFlags & RoleAbilityFlag.UsesPet);

    [Localized(nameof(Blackmailer))]
    public static class BlackmailerTranslations
    {
        [Localized(nameof(BlackmailedMessage))] public static string BlackmailedMessage = "You have been blackmailed! Sending a chat message will kill you.";
        [Localized(nameof(WarningMessage))] public static string WarningMessage = "You are not allowed to speak! If you speak again you may be killed.";
        [Localized(nameof(BlackmailedText))] public static string BlackmailedText = "BLACKMAILED";

        [Localized(nameof(BlackmailButtonText))] public static string BlackmailButtonText = "Blackmail";
        [Localized(nameof(BlackmailingMode))] public static string BlackmailingMode = "Blackmailing";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(WarningsUntilDeath))] public static string WarningsUntilDeath = "Warnings Until Death";
            [Localized(nameof(ShowBlackmailedToAll))] public static string ShowBlackmailedToAll = "Show Blackmailed to All";
            [Localized(nameof(BlackmailerIsSSBased))] public static string BlackmailerIsSSBased = "Blackmailer Uses Shapeshifter";
        }
    }
}