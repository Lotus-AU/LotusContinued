using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;
using static Lotus.Roles.RoleGroups.NeutralKilling.Pestilence.Translations.Options;
using Lotus.API.Player;
using Lotus.GUI;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;

namespace Lotus.Roles.RoleGroups.NeutralKilling;

public class Pestilence : NeutralKillingBase, IRoleUI
{
    /// <summary>
    /// A list of roles that the Pestilence is immune against, this should only be populated by external addons if they want to add an immunity to pestilence lazily
    /// </summary>
    public static List<Type> ImmuneRoles = new();

    private bool immuneToManipulated;
    private bool immuneToRangedAttacks;
    private bool immuneToDelayedAttacks;
    private bool immuneToArsonist;
    private bool unblockableAttacks;


    public RoleButton KillButton(IRoleButtonEditor killButton) => killButton
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Neut/pestilence_kill.png", 130, true));

    public Pestilence()
    {
        RelatedRoles.Add(typeof(PlagueBearer));
    }

    protected override void PostSetup()
    {
        RoleComponent rc = MyPlayer.NameModel().GetComponentHolder<RoleHolder>()[0];
        Players.GetAllPlayers().Where(p => !p.IsAlive() || Relationship(p) is Relation.FullAllies).ForEach(p => rc.AddViewer(p));
    }

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (!unblockableAttacks) return base.TryKill(target);
        MyPlayer.InteractWith(target, new UnblockedInteraction(new FatalIntent(), this));
        Game.MatchData.GameHistory.AddEvent(new KillEvent(MyPlayer, target));
        return true;
    }

    [RoleAction(LotusActionType.Interaction)]
    private void PestilenceAttacked(PlayerControl actor, Interaction interaction, ActionHandle handle)
    {
        Intent intent = interaction.Intent;
        if (intent is not IFatalIntent || interaction.IsPromised) return;

        bool canceled = false;
        switch (interaction)
        {
            case IUnblockedInteraction: return;
            case IDelayedInteraction when immuneToDelayedAttacks:
            case IRangedInteraction when immuneToRangedAttacks:
                canceled = true;
                break;
            case IIndirectInteraction indirectInteraction:
                if (indirectInteraction.Emitter() is AgiTater) canceled = true;
                if (indirectInteraction.Emitter() is Arsonist && immuneToArsonist) canceled = true;
                break;
            case IManipulatedInteraction when immuneToManipulated:
            default:
                canceled = true;
                TryKill(actor);
                break;
        }

        if (ImmuneRoles.Contains(actor.PrimaryRole().GetType())) canceled = true;
        if (canceled) handle.Cancel();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddKillCooldownOptions(base.RegisterOptions(optionStream))
            .Tab(DefaultTabs.HiddenTab)
            .SubOption(sub2 => sub2
                .KeyName("Unblockable Kill", UnblockableKill)
                .AddOnOffValues(false)
                .BindBool(b => unblockableAttacks = b)
                .Build())
            .SubOption(sub2 => sub2
                .KeyName("Invincibility Settings", InvincibilitySettings)
                .Value(v => v.Text(GeneralOptionTranslations.DefaultText).Value(false).Color(Color.cyan).Build())
                .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(true).Color(new Color(0.45f, 0.31f, 0.72f)).Build())
                .ShowSubOptionPredicate(o => (bool)o)
                .SubOption(sub3 => sub3
                    .KeyName("Immune to Manipulated Attackers", ImmuneManipulatorAttackers)
                    .AddOnOffValues(false)
                    .BindBool(b => immuneToManipulated = b)
                    .Build())
                .SubOption(sub3 => sub3
                    .KeyName("Immune to Ranged Attacks", ImmuneRangedAttacks)
                    .AddOnOffValues(false)
                    .BindBool(b => immuneToRangedAttacks = b)
                    .Build())
                .SubOption(sub3 => sub3
                    .KeyName("Immune to Delayed Attacks", ImmuneDelayedAttacks)
                    .AddOnOffValues(false)
                    .BindBool(b => immuneToDelayedAttacks = b)
                    .Build())
                .SubOption(sub3 => sub3
                    .KeyName("Immune to Arsonist Ignite", ImmuneArsonistIgnite)
                    .AddOnOffValues(false)
                    .BindBool(b => immuneToArsonist = b)
                    .Build())
                .Build());

    public override RoleType GetRoleType() => RoleType.Transformation;

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.22f, 0.22f, 0.22f))
            .RoleFlags(RoleFlag.TransformationRole);

    [Localized(nameof(Pestilence))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))]
        public static string ButtonText = "Kill";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(UnblockableKill))]
            public static string UnblockableKill = "Unblockable Kill";

            [Localized(nameof(InvincibilitySettings))]
            public static string InvincibilitySettings = "Invincibility Settings";

            [Localized(nameof(ImmuneManipulatorAttackers))]
            public static string ImmuneManipulatorAttackers = "Immune to Manipulated Attackers";

            [Localized(nameof(ImmuneRangedAttacks))]
            public static string ImmuneRangedAttacks = "Immune to Ranged Attacks";

            [Localized(nameof(ImmuneDelayedAttacks))]
            public static string ImmuneDelayedAttacks = "Immune to Delayed Attacks";

            [Localized(nameof(ImmuneArsonistIgnite))]
            public static string ImmuneArsonistIgnite = "Immune to Arsonist Ignite";
        }
    }
}