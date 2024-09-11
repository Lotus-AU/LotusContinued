using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Vanilla.Meetings;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Holders;
using Lotus.Options;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.API.Vanilla.Sabotages;
using static Lotus.Roles.RoleGroups.Impostors.Mafioso.MafiaTranslations;
using static Lotus.Roles.RoleGroups.Impostors.Mafioso.MafiaTranslations.MafiaOptionTranslations;
using Lotus.Patches.Systems;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Mafioso : Engineer
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Mafioso));
    internal static Color CashColor = new(1f, 0.82f, 0.18f);
    private static Color _gunColor = new(0.55f, 0.28f, 0.16f);
    private static Color _vestColor = new(1f, 0.9f, 0.3f);
    private static Color _bulletColor = new(0.37f, 0.37f, 0.37f);
    private static Color _revealerColor = ModConstants.Palette.GeneralColor5;

    private static string _colorizedCash;
    private bool modifyShopCosts;
    private bool refreshTasks;

    private int cashFromBodies;
    private int gunCost;
    private int vestCost;
    private int revealerCost;
    private int bulletCost;

    [NewOnSetup] private HashSet<byte> killedPlayers = null!;
    private int bulletCount = 1;
    private bool hasGun;
    private bool hasVest;
    private bool hasRevealer;
    private int cashAmount;

    private byte selectedShopItem = byte.MaxValue;
    private bool hasVoted;

    private Cooldown gunCooldown = null!;

    private ShopItem[] shopItems;
    private ShopItem[] currentShopItems;

    public override bool TasksApplyToTotal() => false;

    [UIComponent(UI.Counter, ViewMode.Absolute, GameState.InMeeting)]
    private string DisableTaskCounter() => "";

    [UIComponent(UI.Counter, gameStates: GameState.Roaming)]
    private string BulletCounter()
    {
        string counter = RoleUtils.Counter(bulletCount, color: _bulletColor);
        return hasGun ? counter : $"<s>{counter}</s>";
    }

    [UIComponent(UI.Text, gameStates: GameState.Roaming)]
    private string CashIndicator()
    {
        if (hasRevealer) return RoleColor.Colorize(RevealerReady) + " " + _colorizedCash.Formatted(cashAmount);
        return _colorizedCash.Formatted(cashAmount) + (gunCooldown.IsReady() ? "" : Color.white.Colorize(" (" + gunCooldown + "s)"));
    }

    [UIComponent(UI.Name, ViewMode.Absolute, GameState.InMeeting)]
    private string CustomNameIndicator()
    {
        currentShopItems = shopItems.Where(si => si.Enabled && si.Cost <= cashAmount).ToArray();
        return currentShopItems.Select(si => si.ToDisplay()).Fuse("\n");
    }

    protected override void PostSetup()
    {
        VentCooldown = 0f;
        VentDuration = 120f;
        _colorizedCash = TranslationUtil.Colorize(CashText, CashColor);
        shopItems = [
            new(GunItem, _gunColor, modifyShopCosts ? gunCost : 5, true, () =>
            {
                hasGun = true;
                shopItems[1].Enabled = true;
                shopItems[0].Enabled = false;
            }),
            new(BulletItem, _bulletColor, modifyShopCosts ? bulletCost : 1, false, () => bulletCount++),
            new(VestItem, _vestColor, modifyShopCosts ? vestCost : 3, true, () => hasVest = true),
            new(RevealerItem, _revealerColor, modifyShopCosts ? revealerCost : 9, true, () => hasRevealer = true)
        ];
        if (gunCooldown.Duration <= -1) gunCooldown.Duration = AUSettings.KillCooldown();
        MyPlayer.NameModel().GCH<RoleHolder>().First().GameStates()[2] = GameState.Roaming;
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void RoundStart()
    {
        hasVoted = false;
        selectedShopItem = byte.MaxValue;
        shopItems[0].Enabled = !hasGun;
        shopItems[1].Enabled = hasGun;
    }

    [RoleAction(LotusActionType.RoundEnd)]
    private void RoundEndMessage() => GetChatHandler().Message(ShopMessage).Send();

    [RoleAction(LotusActionType.OnPet)]
    private void KillWithGun()
    {
        if (hasRevealer)
        {
            HandleReveal();
            return;
        }

        if (!hasGun) return;
        if (bulletCount <= 0 || gunCooldown.NotReady()) return;
        PlayerControl? closestPlayer = MyPlayer.GetPlayersInAbilityRangeSorted().FirstOrDefault(p => Relationship(p) is not Relation.FullAllies);
        if (closestPlayer == null) return;
        bulletCount--;
        gunCooldown.Start();
        MyPlayer.InteractWith(closestPlayer, LotusInteraction.FatalInteraction.Create(this));
        killedPlayers.Add(closestPlayer.PlayerId);
    }

    [RoleAction(LotusActionType.ReportBody)]
    private void OnReportBody(Optional<NetworkedPlayerInfo> deadPlayer)
    {
        if (!deadPlayer.Exists()) return;
        if (!killedPlayers.Contains(deadPlayer.Get().PlayerId)) cashAmount += cashFromBodies;
    }

    [RoleAction(LotusActionType.Vote)]
    private void HandleVoting(Optional<PlayerControl> player, MeetingDelegate meetingDelegate, ActionHandle handle)
    {
        player.Handle(p =>
        {
            if (p.PlayerId == MyPlayer.PlayerId) HandleSelfVote(handle);
            else if (!hasVoted)
            {
                hasVoted = true;
                // handle.Cancel();
                // meetingDelegate.CastVote(MyPlayer, player);
            }
        }, () => HandleSkip(handle));
    }

    [RoleAction(LotusActionType.Interaction)]
    private void HandleInteraction(Interaction interaction, ActionHandle handle)
    {
        switch (interaction.Intent)
        {
            case IFatalIntent:
                if (Relationship(interaction.Emitter()) is Relation.FullAllies) handle.Cancel();
                return;
            case IHostileIntent:
                if (Relationship(interaction.Emitter()) is Relation.FullAllies) handle.Cancel();
                return;
        }

        if (!hasVest) return;
        switch (interaction)
        {
            case DelayedInteraction:
            case IndirectInteraction:
            case ManipulatedInteraction:
            case RangedInteraction:
            case Transporter.TransportInteraction:
            case LotusInteraction:
                handle.Cancel();
                hasVest = false;
                break;
        }
    }

    [RoleAction(LotusActionType.SabotageStarted, ActionFlag.GlobalDetector)]
    [RoleAction(LotusActionType.SabotageFixed, ActionFlag.GlobalDetector)]
    private void AdjustSabotageVision(ActionHandle handle)
    {
        log.Trace($"Fixing Player Vision");
        Async.Schedule(SyncOptions, handle.ActionType is LotusActionType.SabotageStarted ? 4f : 0.2f);
    }

    private void HandleReveal()
    {
        PlayerControl? closestPlayer = MyPlayer.GetPlayersInAbilityRangeSorted().FirstOrDefault(p => Relationship(p) is not Relation.FullAllies);
        if (closestPlayer == null) return;
        hasRevealer = false;
        closestPlayer.NameModel().GCH<RoleHolder>().LastOrDefault()?.AddViewer(MyPlayer);
    }

    protected override void OnTaskComplete(Optional<NormalPlayerTask> playerTask)
    {
        cashAmount += playerTask.Map(pt => pt.Length is NormalPlayerTask.TaskLength.Long ? 2 : 1).OrElse(1);
        if (HasAllTasksComplete && refreshTasks) AssignAdditionalTasks();

    }

    private void HandleSelfVote(ActionHandle handle)
    {
        if (hasVoted) return;
        if (currentShopItems.Length == 0) return;
        handle.Cancel();
        if (selectedShopItem == byte.MaxValue) selectedShopItem = 0;
        else selectedShopItem++;
        if (selectedShopItem >= currentShopItems.Length) selectedShopItem = 0;
        ShopItem item = currentShopItems[selectedShopItem];
        GetChatHandler().Message(SelectedItemMessage.Formatted(item.Name, cashAmount - item.Cost)).Send();
    }

    private void HandleSkip(ActionHandle handle)
    {
        if (selectedShopItem == byte.MaxValue) return;
        if (selectedShopItem >= currentShopItems.Length)
        {
            handle.Cancel();
            return;
        }
        ShopItem item = currentShopItems[selectedShopItem];
        cashAmount -= item.Cost;
        // if (item.Color != _gunColor && cashAmount > 0 && !hasVoted) handle.Cancel();
        handle.Cancel();
        GetChatHandler().Message(PurchaseItemMessage.Formatted(item.Name, cashAmount)).Send();
        item.Action();
        currentShopItems = shopItems.Where(si => si.Enabled && si.Cost <= cashAmount).ToArray();
    }

    private ChatHandler GetChatHandler() => ChatHandler.Of(title: RoleColor.Colorize(RoleName)).Player(MyPlayer).LeftAlign();
    public override bool CanVent() => true;
    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Modify Shop Costs", ModifyShopCosts)
                .AddOnOffValues(false)
                .BindBool(b => modifyShopCosts = b)
                .ShowSubOptionPredicate(b => (bool)b)
                .SubOption(sub2 => sub2.KeyName("Gun Cost", GunCost)
                    .AddIntRange(0, 20, 1, 3)
                    .BindInt(i => gunCost = i)
                    .Build())
                .SubOption(sub2 => sub2.KeyName("Bullet Cost", BulletCost)
                    .AddIntRange(0, 20, 1, 0)
                    .BindInt(i => bulletCost = i)
                    .Build())
                .SubOption(sub2 => sub2.KeyName("Vest Cost", VestCost)
                    .AddIntRange(0, 20, 1, 6)
                    .BindInt(i => vestCost = i)
                    .Build())
                .SubOption(sub2 => sub2.KeyName("Revealer Cost", RoleRevealerCost)
                    .AddIntRange(0, 20, 1, 10)
                    .BindInt(i => revealerCost = i)
                    .Build())
                .Build())
            .SubOption(sub => sub.KeyName("Starts Game WIth Gun", StartsGameWithGun)
                .AddOnOffValues(false)
                .BindBool(b => hasGun = b)
                .Build())
            .SubOption(sub => sub.KeyName("Gun Cooldown", GunCooldown)
                .Value(v => v.Text(GeneralOptionTranslations.GlobalText).Color(new Color(1f, 0.61f, 0.33f)).Value(-1f).Build())
                .AddFloatRange(0, 120, 2.5f, 0, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(gunCooldown.SetDuration)
                .Build())
            .SubOption(sub => sub.KeyName("Cash from Reporting Bodies", CashFromReporting)
                .AddIntRange(0, 10, 1, 2)
                .BindInt(i => cashFromBodies = i)
                .Build())
            .SubOption(sub => sub.KeyName("Refresh Tasks When All Complete", RefreshTasks)
                .AddOnOffValues()
                .BindBool(b => refreshTasks = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(Color.red)
            .Faction(FactionInstances.Impostors)
            .RoleAbilityFlags(RoleAbilityFlag.IsAbleToKill | RoleAbilityFlag.UsesPet)
            .OptionOverride(Override.CrewLightMod, () => AUSettings.ImpostorLightMod())
            .OptionOverride(Override.CrewLightMod, () => AUSettings.ImpostorLightMod() * 5, () => SabotagePatch.CurrentSabotage != null && SabotagePatch.CurrentSabotage.SabotageType() is SabotageType.Lights);

    [Localized(nameof(Mafioso))]
    internal static class MafiaTranslations
    {
        [Localized(nameof(CashText))]
        public static string CashText = "Cash::0: {0}";

        [Localized(nameof(GunItem))]
        public static string GunItem = "Tommy Gun";

        [Localized(nameof(VestItem))]
        public static string VestItem = "Bulletproof Vest";

        [Localized(nameof(BulletItem))]
        public static string BulletItem = "Bullet";

        [Localized(nameof(RevealerItem))]
        public static string RevealerItem = "Role Revealer";

        [Localized(nameof(RevealerReady))]
        public static string RevealerReady = "Role Revealer Ready";

        [Localized(nameof(ShopMessage))]
        public static string ShopMessage =
            "You are a member of the Mafia! You can purchase items during meetings. To purchase an item, first vote yourself until that item is selected. Then skip to continue.\nVoting for ANY OTHER player will count as your vote for that player, otherwise you will still remain in shop mode.";

        [Localized(nameof(SelectedItemMessage))]
        public static string SelectedItemMessage = "You have selected to purchase {0}. Purchasing this will leave you with {1} cash. Press the skip vote button to continue your purchase.";

        [Localized(nameof(PurchaseItemMessage))]
        public static string PurchaseItemMessage = "You have purchased: {0}. You now have {1} cash leftover.";

        [Localized(ModConstants.Options)]
        internal static class MafiaOptionTranslations
        {
            [Localized(nameof(RefreshTasks))]
            public static string RefreshTasks = "Refresh Tasks When All Complete";

            [Localized(nameof(StartsGameWithGun))]
            public static string StartsGameWithGun = "Starts Game With Gun";

            [Localized(nameof(ModifyShopCosts))]
            public static string ModifyShopCosts = "Modify Shop Costs";

            [Localized(nameof(GunCost))]
            public static string GunCost = "Gun Cost";

            [Localized(nameof(BulletCost))]
            public static string BulletCost = "Bullet Cost";

            [Localized(nameof(VestCost))]
            public static string VestCost = "Vest Cost";

            [Localized(nameof(RoleRevealerCost))]
            public static string RoleRevealerCost = "Role Revealer Cost";

            [Localized(nameof(GunCooldown))]
            public static string GunCooldown = "Gun Cooldown";

            [Localized(nameof(CashFromReporting))]
            public static string CashFromReporting = "Cash from Reporting Bodies";
        }
    }

    private class ShopItem
    {
        public string Name;
        public Color Color;
        public int Cost;
        public bool Enabled;
        public Action Action;

        public ShopItem(string name, Color color, int cost, bool enabled, Action action)
        {
            Name = name;
            Color = color;
            Cost = cost;
            Enabled = enabled;
            Action = action;
        }

        public string ToDisplay() => $"{Color.Colorize(Name)} ({CashColor.Colorize(Cost.ToString())})";
    }
}