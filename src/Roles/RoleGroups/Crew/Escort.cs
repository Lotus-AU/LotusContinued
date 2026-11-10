using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name.Impl;
using Lotus.Logging;
using Lotus.Roles.Events;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using Lotus.API;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Extensions;
using Lotus.Options;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.RoleGroups.Impostors;
using UnityEngine;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Localization.Attributes;

namespace Lotus.Roles.RoleGroups.Crew;

public class Escort : Crewmate, IRoleUI
{
    private float roleblockDuration;
    [NewOnSetup]
    private Dictionary<byte, BlockDelegate> blockedPlayers;

    [UIComponent(UI.Cooldown)]
    private Cooldown roleblockCooldown;

    public RoleButton PetButton(IRoleButtonEditor editor) => editor
        .BindCooldown(roleblockCooldown)
        .SetText(Consort.Translations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/escort_roleblock.png", 130, true));

    [RoleAction(LotusActionType.OnPet)]
    private void ChangeToBlockMode()
    {
        if (roleblockCooldown.NotReady()) return;
        List<PlayerControl> candidates = MyPlayer.GetPlayersInAbilityRangeSorted().Where(p => !blockedPlayers.ContainsKey(p.PlayerId)).ToList();
        if (candidates.Count == 0) return;
        roleblockCooldown.Start();

        PlayerControl target = candidates[0];

        blockedPlayers[target.PlayerId] = BlockDelegate.Block(target, MyPlayer, roleblockDuration);
        MyPlayer.RpcMark(target);
        Game.MatchData.GameHistory.AddEvent(new GenericTargetedEvent(MyPlayer, target, $"{RoleColor.Colorize(MyPlayer.name)} role blocked {target.GetRoleColor().Colorize(target.name)}."));

        if (roleblockDuration > 0) Async.Schedule(() => blockedPlayers.Remove(target.PlayerId), roleblockDuration);
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    [RoleAction(LotusActionType.RoundStart)]
    [RoleAction(LotusActionType.RoundEnd)]
    private void UnblockPlayers()
    {
        blockedPlayers.ToArray().ForEach(k =>
        {
            blockedPlayers.Remove(k.Key);
            k.Value.Delete();
        });
    }

    private void Block(PlayerControl source, ActionHandle handle)
    {
        BlockDelegate? blockDelegate = blockedPlayers.GetValueOrDefault(source.PlayerId);
        if (blockDelegate == null) return;

        handle.Cancel();
        blockDelegate.UpdateDelegate();
    }

    [RoleAction(LotusActionType.PlayerAction, ActionFlag.GlobalDetector)]
    private void BlockAction(PlayerControl source, ActionHandle handle, RoleAction action)
    {
        if (action.Blockable) Block(source, handle);
    }

    [RoleAction(LotusActionType.VentEntered, ActionFlag.GlobalDetector)]
    private void BlockVent(PlayerControl source, ActionHandle handle) => Block(source, handle);

    [RoleAction(LotusActionType.SabotageStarted,ActionFlag.GlobalDetector)]
    private void BlockSabotage(PlayerControl caller, ActionHandle handle)  => Block(caller, handle);

    [RoleAction(LotusActionType.ReportBody, ActionFlag.GlobalDetector)]
    private void BlockReport(PlayerControl reporter, ActionHandle handle) => Block(reporter, handle);

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Role-Block Cooldown", Translations.Options.RoleblockCooldown)
                .BindFloat(roleblockCooldown.SetDuration)
                .AddFloatRange(0, 120, 2.5f, 18, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Roleblock Duration", Translations.Options.RoleblockDuration)
                .BindFloat(v => roleblockDuration = v)
                .Value(v => v.Text("Until Meeting").Value(-1f).Build())
                .AddFloatRange(5, 120, 5, suffix: GeneralOptionTranslations.SecondsSuffix)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(1f, 0.73f, 0.92f)).RoleAbilityFlags(RoleAbilityFlag.UsesPet);


    public class BlockDelegate
    {
        public Remote<IndicatorComponent> BlockedIndicator;
        public Remote<IndicatorComponent>? BlockedCounter;
        public PlayerControl Blocker;
        public byte Player;
        public bool HasUsedAction;
        public Cooldown? BlockDuration;

        private BlockDelegate(PlayerControl target, PlayerControl blocker, float duration)
        {
            Player = target.PlayerId;
            Blocker = blocker;

            IndicatorHolder indicatorHolder = target.NameModel().GetComponentHolder<IndicatorHolder>();

            var indicator = new SimpleIndicatorComponent("(RB)", new Color(1f, 0.89f, 0.24f), GameState.Roaming, blocker);
            BlockedIndicator = indicatorHolder.Add(indicator);

            if (duration <= 0) return;

            BlockDuration = new Cooldown(duration, true);
            BlockDuration.StartThenRun(Delete);
        }

        public static BlockDelegate Block(PlayerControl target, PlayerControl blocker, float duration)
        {
            return new BlockDelegate(target, blocker, duration);
        }

        public void UpdateDelegate(bool usedAction = true)
        {
            HasUsedAction = usedAction;
            if (!HasUsedAction) return;

            PlayerControl? thisPlayer = Utils.GetPlayerById(Player);
            if (thisPlayer == null) return;
            Async.Schedule(() =>
            {
                TextComponent component = new(new LiveString("BLOCKED!", Color.red), GameState.Roaming, ViewMode.Absolute, thisPlayer);
                Remote<TextComponent> text = thisPlayer.NameModel().GetComponentHolder<TextHolder>().Add(component);
                Async.Schedule(() => text.Delete(), 1f);

                if (BlockedCounter != null) return;
                LiveString liveString = new(_ => RelRbIndicator(BlockDuration?.TimeRemaining() ?? 60));
                BlockedCounter = thisPlayer.NameModel().GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(liveString, GameState.Roaming, viewers: thisPlayer));
            }, 0.05f, false, true);
    }

        private string RelRbIndicator(float timeRemaining)
        {
            if (timeRemaining > 35) return Color.green.Colorize("RB'd");
            if (timeRemaining > 18) return Color.yellow.Colorize("RB'd");
            return Color.red.Colorize("RB'd");
        }

        public void Delete()
        {
            BlockedIndicator.Delete();
            BlockedCounter?.Delete();
        }
    }

    [Localized(nameof(Escort))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(RoleblockCooldown))]
            public static string RoleblockCooldown = "Role-Block Cooldown";

            [Localized(nameof(RoleblockDuration))]
            public static string RoleblockDuration = "Role-Block Duration";
        }
    }
}