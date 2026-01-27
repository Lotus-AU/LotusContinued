using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Factions.Undead;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name.Impl;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Victory;
using Lotus.Extensions;
using Lotus.Managers;
using Lotus.Options;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.API.Player;
using Lotus.GameModes.Standard;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using VentLib.Localization.Attributes;

namespace Lotus.Roles.RoleGroups.Undead.Roles;

public class Necromancer : UndeadRole, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Necromancer));
    private static Deathknight _deathknight = new Deathknight();

    private bool isFirstConvert = true;
    private bool immuneToPartialConverted;

    private Deathknight? myDeathknight;
    private CustomRole deathknightOriginal = null!;
    private bool disableWinCheck;

    public RoleButton KillButton(IRoleButtonEditor petButton) => petButton
        .SetText(Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Neut/necromancer_convert.png", 130, true));

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        Game.GetWinDelegate().AddSubscriber(DenyWinConditions);
    }

    [RoleAction(LotusActionType.Attack)]
    private bool NecromancerConvert(PlayerControl target)
    {
        if (MyPlayer.InteractWith(target, LotusInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt) return false;
        MyPlayer.RpcMark(target);
        log.Debug($"Is first convert? {isFirstConvert} - {target.GetNameWithRole()}");
        if (isFirstConvert) return ConvertToDeathknight(target);
        FinishConversionToUndead(target);
        return false;
    }

    [RoleAction(LotusActionType.Interaction)]
    private void NecromancerImmunity(PlayerControl actor, Interaction interaction, ActionHandle handle)
    {
        if (interaction.Intent is not (IHostileIntent or IFatalIntent)) return;
        if (IsConvertedUndead(actor)) handle.Cancel();
        else if (immuneToPartialConverted && IsUnconvertedUndead(actor)) handle.Cancel();
    }

    [RoleAction(LotusActionType.Exiled)]
    [RoleAction(LotusActionType.Disconnect)]
    [RoleAction(LotusActionType.PlayerDeath)]
    private void NecromancerDeath()
    {
        if (myDeathknight == null || !myDeathknight.MyPlayer.IsAlive() || !myDeathknight.CanBecomeNecromancer) return;
        PlayerControl player = myDeathknight.MyPlayer;
        player.GetSubroles().Remove(deathknightOriginal);
        myDeathknight = null;

        player.PrimaryRole().ChangeRoleTo(this, false);
        Necromancer necromancer = player.PrimaryRole<Necromancer>()!;
        necromancer.isFirstConvert = false;
        disableWinCheck = true;
    }

    private bool ConvertToDeathknight(PlayerControl target)
    {
        isFirstConvert = false;

        InitiateConvertToUndead(target);
        FinishConversionToUndead(target);

        deathknightOriginal = target.PrimaryRole();
        _deathknight.VirtualRole = Deathknight.ReplacedRole = deathknightOriginal.RealRole;
        deathknightOriginal.ChangeRoleTo(_deathknight, false, false);
        Game.MatchData.Roles.AddSubrole(target.PlayerId, deathknightOriginal);
        myDeathknight = target.PrimaryRole<Deathknight>();
        target.NameModel().GetComponentHolder<RoleHolder>()[^1]
            .SetViewerSupplier(() => Players.GetAllPlayers().Where(p => p.PlayerId == target.PlayerId || p.Relationship(target) is Relation.FullAllies).ToList());

        log.Fatal($"Indicator count 22: {target.NameModel().GetComponentHolder<IndicatorHolder>().Count}");
        Game.MatchData.GameHistory.AddEvent(new RoleChangeEvent(target, _deathknight));
        return false;
    }

    private void DenyWinConditions(WinDelegate winDelegate)
    {
        if (disableWinCheck) return;
        List<PlayerControl> winners = winDelegate.GetWinners();
        if (winners.Any(p => p.PlayerId == MyPlayer.PlayerId)) return;
        List<PlayerControl> undeadWinners = winners.Where(p => p.PrimaryRole().Faction is TheUndead).ToList();

        if (undeadWinners.Count(IsConvertedUndead) == winners.Count) winDelegate.CancelGameWin();
        else if (undeadWinners.Count == winners.Count && MyPlayer.IsAlive()) winDelegate.CancelGameWin();
        else undeadWinners.Where(tc => IsConvertedUndead(tc) || MyPlayer.IsAlive() && IsUnconvertedUndead(tc)).ForEach(uw => winners.Remove(uw));
    }

    protected override string ForceRoleImageDirectory() => "RoleOutfits/Neutral/necromancer.yaml";

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddKillCooldownOptions(base.RegisterOptions(optionStream), "Convert Cooldown", Translations.Options.ConvertCooldown)
            .SubOption(sub => sub
                .KeyName("Immune to Partially Converted", Translations.Options.PartialConvertImmunity)
                .AddBoolean()
                .BindBool(b => immuneToPartialConverted = b)
                .Build());

    public override List<CustomRole> LinkedRoles() => base.LinkedRoles().Concat([_deathknight]).ToList();

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.61f, 0.53f, 0.67f))
            .CanVent(false)
            .OptionOverride(new IndirectKillCooldown(KillCooldown))
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet);

    [Localized(nameof(Necromancer))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))] public static string ButtonText = "Convert";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(ConvertCooldown))]
            public static string ConvertCooldown = "Convert Cooldown";

            [Localized(nameof(PartialConvertImmunity))]
            public static string PartialConvertImmunity = "Immune to Partially Converted";
        }
    }
}