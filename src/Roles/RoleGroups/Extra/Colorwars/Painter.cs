using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.GUI;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.NeutralKilling;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.Options;
using Lotus.Extensions;
using Lotus.GameModes.Normal.Colorwars.Factions;
using VentLib.Options.UI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Logging;
using Lotus.Options.Gamemodes;
using Lotus.Roles.Events;
using VentLib.Utilities.Collections;

namespace Lotus.Roles.RoleGroups.Extra.Colorwars;

public class Painter : NeutralKillingBase
{
    public int InitialColor;

    private Cooldown gracePeriod = null!;

    private bool suddenDeathActivated;
    [NewOnSetup] private Dictionary<byte, Remote<IndicatorComponent>> arrowComponents = [];

    [UIComponent(Lotus.GUI.Name.UI.Text)]
    public string GracePeriodText() => gracePeriod.IsReady() ? "" : Color.gray.Colorize(Translations.GracePeriodText).Formatted(gracePeriod + "s");

    protected override void PostSetup()
    {
        InitialColor = MyPlayer.cosmetics.bodyMatProperties.ColorId;
        KillCooldown = ExtraGamemodeOptions.ColorwarsOptions.KillCooldown;
        if (ExtraGamemodeOptions.ColorwarsOptions.ConvertColorMode) KillCooldown *= 2; // Because we rpc mark players. we need to multiply the kill cd by 2.
        base.PostSetup();
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void BeginGracePeriod()
    {
        gracePeriod.Start(ExtraGamemodeOptions.ColorwarsOptions.GracePeriod);
        CheckForSuddenDeath(true);
    }

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (gracePeriod.NotReady()) return false;
        int myColor = MyPlayer.cosmetics.bodyMatProperties.ColorId;
        if (myColor == target.cosmetics.bodyMatProperties.ColorId) return false;

        if (!ExtraGamemodeOptions.ColorwarsOptions.ConvertColorMode || suddenDeathActivated) return base.TryKill(target);

        target.RpcSetColor((byte)myColor);
        target.PrimaryRole().RoleColor = (Color)Palette.PlayerColors[MyPlayer.cosmetics.bodyMatProperties.ColorId];
        MyPlayer.RpcMark(target);
        Game.MatchData.GameHistory.AddEvent(new ConvertEvent(MyPlayer, target, (byte)myColor));
        CheckForSuddenDeath(false);
        return false;
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.WorksAfterDeath | ActionFlag.GlobalDetector)]
    private void OnPlayerKilled(PlayerControl killed)
    {
        if (suddenDeathActivated)
        {
            if (arrowComponents.TryGetValue(killed.PlayerId, out var arrowComponent)) arrowComponent.Delete();
        } else CheckForSuddenDeath(true);
    }

    private void ExitCurrentVent()
    {
        int ventId;

        ISystemType ventilation = ShipStatus.Instance.Systems[SystemTypes.Ventilation];
        if (ventilation.TryCast(out VentilationSystem ventilationSystem))
        {
            if (ventilationSystem.PlayersInsideVents.TryGetValue(MyPlayer.PlayerId, out byte byteId)) ventId = byteId;
            else ventId = Object.FindObjectsOfType<Vent>().ToList().GetRandom().Id;
        }
        else ventId = Object.FindObjectsOfType<Vent>().ToList().GetRandom().Id;

        MyPlayer.MyPhysics.RpcBootFromVent(ventId);
    }

    private void CheckForSuddenDeath(bool wasKillEvent)
    {
        if (suddenDeathActivated || !ExtraGamemodeOptions.ColorwarsOptions.SuddenDeath) return;
        int teamsLeft = Players.GetAlivePlayers().Select(p => p.cosmetics.bodyMatProperties.ColorId).Distinct().Count();
        DevLogger.Log($"{teamsLeft} - {ExtraGamemodeOptions.ColorwarsOptions.RemainingTeams} ({teamsLeft > ExtraGamemodeOptions.ColorwarsOptions.RemainingTeams})");
        if (teamsLeft > ExtraGamemodeOptions.ColorwarsOptions.RemainingTeams) return; // if teams left are greater than # in settings
        if (wasKillEvent) ActivateSuddenDeath();
        else Players.GetAllRoles().ForEach(r =>
        {
            if (r is Painter otherPainter) otherPainter.ActivateSuddenDeath();
        });
    }

    private void ActivateSuddenDeath()
    {
        if (suddenDeathActivated || !ExtraGamemodeOptions.ColorwarsOptions.SuddenDeath) return;
        suddenDeathActivated = true;

        var colorwarsOptions = ExtraGamemodeOptions.ColorwarsOptions;

        if (colorwarsOptions.DisableVents)
        {
            BaseCanVent = false;
            if (MyPlayer.inVent) ExitCurrentVent();
        }

        if (colorwarsOptions.EnableArrows)
            Players.GetAlivePlayers().ForEach(p =>
            {
                if (p.cosmetics.bodyMatProperties.ColorId == MyPlayer.cosmetics.bodyMatProperties.ColorId) return;
                Remote<IndicatorComponent> arrowComponent = MyPlayer.NameModel()
                    .GetComponentHolder<IndicatorHolder>()
                    .Add(new IndicatorComponent(
                        new LiveString(() => RoleUtils.CalculateArrow(MyPlayer, p, p.PrimaryRole().RoleColor)),
                        Game.InGameStates, viewers: MyPlayer));
                arrowComponents[p.PlayerId] = arrowComponent;
            });


        MyPlayer.NameModel().GetComponentHolder<TextHolder>()
            .Add(new TextComponent(new LiveString(CaptureOptions.Translations.SuddenDeath, Color.red), Game.InGameStates,
                viewers: [MyPlayer]));

        if (ExtraGamemodeOptions.ColorwarsOptions.ConvertColorMode)
        {
            KillCooldown /= 2;
            SyncOptions();
        }
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleFlags(RoleFlag.DontRegisterOptions | RoleFlag.Hidden)
        .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage)
        .IntroSound(AmongUs.GameOptions.RoleTypes.Shapeshifter)
        .VanillaRole(AmongUs.GameOptions.RoleTypes.Impostor)
        .Faction(ColorFaction.Instance)
        .RoleColor(Color.white)
        .CanVent(ExtraGamemodeOptions.ColorwarsOptions.CanVent);

    [Localized(nameof(Painter))]
    public static class Translations
    {
        [Localized(nameof(GracePeriodText))]
        public static string GracePeriodText = "No-Kill Grace Period: {0}";
    }

    private class ConvertEvent : TargetedAbilityEvent
    {
        private byte colorId;
        public ConvertEvent(PlayerControl source, PlayerControl target, byte colorId) : base(source, target, true)
        {
            this.colorId = colorId;
        }

        public byte GetNewColor() => colorId;

        public override string Message() =>
            $"{Game.GetName(Player())} converted {Game.GetName(Target())} to Team {((Color)(Palette.PlayerColors[colorId])).Colorize(ModConstants.ColorNames[colorId])}.";
    }
}