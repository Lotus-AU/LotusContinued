extern alias JBAnnotations;
using System.Collections.Generic;
using JBAnnotations::JetBrains.Annotations;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Roles.Internals.Enums;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Statuses;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;

namespace Lotus.Roles.RoleGroups.NeutralKilling;

public class Occultist : NeutralKillingBase, IRoleUI
{
    private bool freelySwitchModes;
    private bool switchModesAfterAttack;

    [NewOnSetup] private Dictionary<byte, Remote<IndicatorComponent>> indicators;
    [NewOnSetup] private Dictionary<byte, Remote<IStatus>?> cursedPlayers;

    private bool isCursingMode = true;
    private bool killButtonMode;

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        killButtonMode = !isCursingMode;
    }

    [UIComponent(UI.Text)]
    private string ModeDisplay() => (freelySwitchModes || switchModesAfterAttack) ? (isCursingMode ? RoleColor.Colorize(Translations.CursingModeText) : Color.red.Colorize(Translations.KillingModeText)) : "";

    public RoleButton PetButton(IRoleButtonEditor petButton) => !freelySwitchModes
        ? petButton.Default(true)
        : petButton.SetText(RoleTranslations.Switch)
            .SetSprite(() => LotusAssets.LoadSprite("Buttons/generic_switch_ability.png", 130, true));

    public RoleButton KillButton(IRoleButtonEditor killButton) => killButton
        .SetText(Witch.Translations.CursingButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Neut/occultist_hex.png", 130, true));

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (!isCursingMode)
        {
            if (switchModesAfterAttack)
            {
                isCursingMode = !isCursingMode;
                if (MyPlayer.AmOwner) UpdateKillButton();
                else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateOccultist)?.Send([MyPlayer.OwnerId], isCursingMode);
            }
            return base.TryKill(target);
        }

        MyPlayer.RpcMark(target);
        if (switchModesAfterAttack)
        {
            isCursingMode = !isCursingMode;

            if (MyPlayer.AmOwner) UpdateKillButton();
            else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateOccultist)?.Send([MyPlayer.OwnerId], isCursingMode);
        }
        if (MyPlayer.InteractWith(target, LotusInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt) return false;
        if (cursedPlayers.ContainsKey(target.PlayerId)) return false;

        CustomStatus status = CustomStatus.Of(Translations.HexedCauseOfDeath).Description(Translations.CursedStatusDescription).Color(Color.red).Build();
        cursedPlayers.Add(target.PlayerId, Game.MatchData.AddStatus(target, status, MyPlayer));
        indicators.GetValueOrDefault(target.PlayerId)?.Delete();
        indicators[target.PlayerId] = target.NameModel().GCH<IndicatorHolder>().Add(new SimpleIndicatorComponent("†", Color.red, GameState.InMeeting));

        string eventMessage = TranslationUtil.Colorize(Translations.CursedMessage.Formatted(MyPlayer.name, target.name), RoleColor, target.PrimaryRole().RoleColor);
        Game.MatchData.GameHistory.AddEvent(new GenericTargetedEvent(MyPlayer, target, eventMessage));
        return false;
    }

    [RoleAction(LotusActionType.OnPet)]
    public void SwitchWitchMode()
    {
        if (freelySwitchModes)
        {
            isCursingMode = !isCursingMode;
            if (MyPlayer.AmOwner) UpdateKillButton();
            else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateOccultist)?.Send([MyPlayer.OwnerId], isCursingMode);
        }
    }

    [RoleAction(LotusActionType.MeetingEnd)]
    public void KillCursedPlayers(Optional<NetworkedPlayerInfo> exiledPlayer)
    {
        if (exiledPlayer.Compare(p => p.PlayerId == MyPlayer.PlayerId)) return;
        cursedPlayers.Keys.Filter(Players.PlayerById).ForEach(p =>
        {
            IDeathEvent cod = new CustomDeathEvent(MyPlayer, p, Translations.HexedCauseOfDeath);
            MyPlayer.InteractWith(p, new UnblockedInteraction(new FatalIntent(false, () => cod), this));
            cursedPlayers[p.PlayerId]?.Delete();
        });
    }

    [RoleAction(LotusActionType.RoundStart, ActionFlag.WorksAfterDeath)]
    [RoleAction(LotusActionType.PlayerDeath)]
    private void ClearCursedPlayers()
    {
        cursedPlayers.ForEach(c => c.Value?.Delete());
        indicators.ForEach(i => i.Value.Delete());
        cursedPlayers.Clear();
        indicators.Clear();
    }

    public override void HandleDisconnect() => ClearCursedPlayers();

    [UsedImplicitly]
    [ModRPC((uint)ModCalls.UpdateOccultist, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateKillButton(bool curseMode)
    {
        Occultist? occultist = PlayerControl.LocalPlayer.PrimaryRole<Occultist>();
        if (occultist == null) return;
        occultist.isCursingMode = curseMode;
        occultist.UpdateKillButton();
    }

    private void UpdateKillButton()
    {
        if (killButtonMode == !isCursingMode) return;
        killButtonMode = !isCursingMode;

        if (killButtonMode) UIManager.KillButton
            .RevertSprite()
            .SetText(Witch.Translations.KillButtonText);
        else UIManager.KillButton
            .SetText(Witch.Translations.CursingButtonText)
            .SetSprite(() => LotusAssets.LoadSprite("Buttons/Neut/occultist_hex.png", 130, true));
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Freely Switch Modes", Translations.Options.FreelySwitchModes)
                .AddBoolean()
                .BindBool(b => freelySwitchModes = b)
                .Build())
            .SubOption(sub => sub.KeyName("Switch Modes After Attack", Translations.Options.SwitchModesAfterAttack)
                .AddBoolean()
                .BindBool(b => switchModesAfterAttack = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.39f, 0.47f, 0.87f))
            .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.UsesPet)
            .OptionOverride(new IndirectKillCooldown(KillCooldown, () => isCursingMode));


    [Localized(nameof(Occultist))]
    public static class Translations
    {
        [Localized(nameof(CursedStatusDescription))]
        public static string CursedStatusDescription = "You have been hexed. Hexed players will die after the meeting unless the source of the hex is voted out.";

        [Localized(nameof(CursedMessage))]
        public static string CursedMessage = "{0}::0 cursed {1}::1 to die at the end of next meeting.";

        [Localized(nameof(CursingModeText))]
        public static string CursingModeText = "Cursing";

        [Localized(nameof(KillingModeText))]
        public static string KillingModeText = "Killing";

        [Localized(nameof(HexedCauseOfDeath))]
        public static string HexedCauseOfDeath = "Hexed";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(FreelySwitchModes))]
            public static string FreelySwitchModes = "Freely Switch Modes";

            [Localized(nameof(SwitchModesAfterAttack))]
            public static string SwitchModesAfterAttack = "Switch Modes After Attack";
        }
    }

}