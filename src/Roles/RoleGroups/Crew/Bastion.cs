using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Extensions;
using Lotus.Options;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Options.IO;
using VentLib.Utilities.Collections;
using static Lotus.Roles.RoleGroups.Crew.Bastion.BastionTranslations.BastionOptionTranslations;

namespace Lotus.Roles.RoleGroups.Crew;

public class Bastion : Engineer
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Bastion));
    private int bombsPerRounds;
    // Here we can use the vent button as cooldown
    [NewOnSetup] private HashSet<int> bombedVents;

    private int currentBombs;
    private Remote<CounterComponent>? counterRemote;


    protected override void PostSetup()
    {
        if (bombsPerRounds == -1) return;
        CounterHolder counterHolder = MyPlayer.NameModel().GetComponentHolder<CounterHolder>();
        LiveString ls = new(() => RoleUtils.Counter(currentBombs, bombsPerRounds, ModConstants.Palette.GeneralColor2));
        counterRemote = counterHolder.Add(new CounterComponent(ls, new[] { GameState.Roaming }, ViewMode.Additive, MyPlayer));
    }

    [RoleAction(LotusActionType.VentEntered, ActionFlag.GlobalDetector)]
    private void EnterVent(PlayerControl player, Vent vent, ActionHandle handle)
    {
        bool isBombed = bombedVents.Remove(vent.Id);
        log.Trace($"Bombed Vent Check: (player={player.name}, isBombed={isBombed})", "BastionAbility");
        if (isBombed) MyPlayer.InteractWith(player, CreateInteraction(player));
        else if (player.PlayerId == MyPlayer.PlayerId)
        {
            handle.Cancel();
            if (currentBombs == 0) return;
            currentBombs--;
            bombedVents.Add(vent.Id);
        }
    }

    [RoleAction(LotusActionType.RoundStart, ActionFlag.WorksAfterDeath)]
    private void RefreshBastion()
    {
        currentBombs = bombsPerRounds;
        bombedVents.Clear();
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    private void ClearCounter() => counterRemote?.Delete();

    private IndirectInteraction CreateInteraction(PlayerControl deadPlayer)
    {
        return new IndirectInteraction(new FatalIntent(true, () => new BombedEvent(deadPlayer, MyPlayer)), this);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Plant Bomb Cooldown", PlantBombCooldown)
                .BindFloat(v => VentCooldown = v)
                .Value(1f)
                .IOSettings(io => io.UnknownValueAction = ADEAnswer.Allow)
                .AddFloatRange(2.5f, 120, 2.5f, 8, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Bombs per Round", BombsPerRound)
                .Value(v => v.Text(ModConstants.Infinity).Color(ModConstants.Palette.InfinityColor).Value(-1).Build())
                .AddIntRange(1, 20, 1, 0)
                .BindInt(i => bombsPerRounds = i)
                .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
        .RoleColor("#524f4d");

    [Localized(nameof(Bastion))]
    internal static class BastionTranslations
    {
        [Localized(ModConstants.Options)]
        public static class BastionOptionTranslations
        {
            [Localized(nameof(PlantBombCooldown))]
            public static string PlantBombCooldown = "Plant Bomb Cooldown";

            [Localized(nameof(BombsPerRound))]
            public static string BombsPerRound = "Bombs per Round";
        }
    }
}