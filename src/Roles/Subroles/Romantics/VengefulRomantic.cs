using System.Linq;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Factions.Interfaces;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;

namespace Lotus.Roles.Subroles.Romantics;

public class VengefulRomantic : Subrole
{
    private bool canKillBystander;
    private bool suicideAfterRevenge;
    private bool arrowToKiller;
    private byte killerId;

    private IFaction faction = null!;
    private IRemote? remote;

    public override string Identifier() => "♥";

    protected override void PostSetup()
    {
        MyPlayer.PrimaryRole().RoleFlags |= RoleFlag.CannotWinAlone;
        RoleHolder roleHolder = MyPlayer.NameModel().GetComponentHolder<RoleHolder>();
        remote = roleHolder.Insert(0, new RoleComponent(new LiveString(Translations.Adjective, RoleColor), Game.InGameStates, ViewMode.Additive, MyPlayer));

        SubroleHolder subroleHolder = MyPlayer.NameModel().GetComponentHolder<SubroleHolder>();
        if (subroleHolder.Count > 0) subroleHolder.RemoveAt(subroleHolder.Count - 1);
        MyPlayer.NameModel().Render(force: true);
    }

    [RoleAction(LotusActionType.OnPet, priority: Priority.VeryHigh)]
    public void EnablePetKill(ActionHandle handle)
    {
        PlayerControl? target = MyPlayer.GetPlayersInAbilityRangeSorted().FirstOrDefault();
        if (target == null) return;

        handle.Cancel();

        if (target.PlayerId == killerId || canKillBystander)
        {
            MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this));
            return;
        }

        MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(), this));
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    public void CheckPlayerDeaths(PlayerControl dead, PlayerControl killer)
    {
        if (dead.PlayerId != killerId && killer.PlayerId == MyPlayer.PlayerId && dead.PlayerId != MyPlayer.PlayerId)
            MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(), this));
        else if (dead.PlayerId == killerId)
        {
            killerId = byte.MaxValue;
            if (killer.PlayerId == MyPlayer.PlayerId && suicideAfterRevenge && MyPlayer.IsAlive())
                MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(), this));
            remote?.Delete();
            Async.Schedule(() => MyPlayer.GetSubroles().Remove(this), 0.001f);
            MyPlayer.PrimaryRole().Faction = faction;
        }
    }

    public void SetupVengeful(PlayerControl killer, IFaction oldFaction)
    {
        killerId = killer.PlayerId;
        this.faction = oldFaction;
        if (!arrowToKiller) return;

        LiveString liveString = new(() => RoleUtils.CalculateArrow(MyPlayer, killer, RoleColor));
        MyPlayer.NameModel().GCH<IndicatorHolder>().Add(new IndicatorComponent(liveString, GameState.Roaming, ViewMode.Absolute, MyPlayer));
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Can Kill Bystanders", Translations.Options.CanKillBystanders)
                .AddOnOffValues(false)
                .BindBool(b => canKillBystander = b)
                .Build())
            .SubOption(sub => sub.KeyName("Arrow to Killer", Translations.Options.ArrowToKiller)
                .AddOnOffValues(false)
                .BindBool(b => arrowToKiller = b)
                .Build())
            .SubOption(sub => sub.KeyName("Suicide After Revenge", Translations.Options.SuicideAfterRevenge)
                .AddOnOffValues(false)
                .BindBool(b => suicideAfterRevenge = b)
                .Build());

    public override RoleType GetRoleType() => RoleType.Transformation;

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleFlags(RoleFlag.TransformationRole)
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet)
            .RoleColor(new Color(0.71f, 0.23f, 0.35f));

    [Localized(nameof(VengefulRomantic))]
    private static class Translations
    {
        [Localized(nameof(Adjective))]
        public static string Adjective = "Vengeful";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(CanKillBystanders))]
            public static string CanKillBystanders = "Can Kill Bystanders";

            [Localized(nameof(ArrowToKiller))]
            public static string ArrowToKiller = "Arrow to Killer";

            [Localized(nameof(SuicideAfterRevenge))]
            public static string SuicideAfterRevenge = "Suicide after Revenge";
        }
    }
}