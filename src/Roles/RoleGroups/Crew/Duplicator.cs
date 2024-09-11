using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Internals.Enums;
using Lotus.GUI;
using Lotus.API.Odyssey;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name.Components;
// using Lotus.RPC.CustomObjects.Builtin;
using System.Collections.Generic;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using Lotus.Extensions;
using Lotus.Options;
using UnityEngine;

namespace Lotus.Roles.RoleGroups.Crew;

public class Duplicator : Crewmate
{
    /*
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Duplicator));
    [UIComponent(GUI.Name.UI.Cooldown)]
    private Cooldown duplicateCooldown = null!;
    private Cooldown duplicateDuration = null!;

    private List<FakePlayer> fakePlayers = null!;
    private RandomSpawn randomSpawn = null!;

    private bool spawnsRandomly;

    protected override void PostSetup()
    {
        if (spawnsRandomly)
        {
            randomSpawn = new();
            randomSpawn.Refresh();
        }
        fakePlayers = new();
        base.PostSetup();
        MyPlayer.NameModel().GetComponentHolder<CooldownHolder>().Add(new CooldownComponent(duplicateDuration, GameState.Roaming, cdText: "Dur: ", viewers: MyPlayer));
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void OnRoundStart(bool gameStart)
    {
        duplicateDuration.Finish();
        duplicateCooldown.Start(gameStart ? 10 : float.MinValue);
    }

    [RoleAction(LotusActionType.OnPet)]
    private void OnAbilityActivation()
    {
        if (duplicateCooldown.NotReady() || duplicateDuration.NotReady()) return;
        log.Debug($"Creating fake player of {MyPlayer.name}");
        FakePlayer fakePlayer = new(MyPlayer.CurrentOutfit, spawnsRandomly ? randomSpawn.GetRandomLocation() : MyPlayer.GetTruePosition(), MyPlayer.PlayerId);
        fakePlayers.Add(fakePlayer);
        duplicateDuration.StartThenRun(() =>
        {
            duplicateCooldown.Start();
            log.Debug($"Removing fake player of {MyPlayer.name}");
            if (fakePlayers.Remove(fakePlayer)) fakePlayer.Despawn();
        });
    }

    [RoleAction(LotusActionType.RoundEnd)]
    [RoleAction(LotusActionType.PlayerDeath)]
    public override void HandleDisconnect()
    {
        log.Debug($"Removing ALL fake players of {MyPlayer.name}");
        fakePlayers.ForEach(p => p.Despawn());
        fakePlayers.Clear();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Duplicate Cooldown", Translations.Options.DuplicateCooldown)
                .BindFloat(duplicateCooldown.SetDuration)
                .AddFloatRange(2.5f, 120, 2.5f, 11, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Duplicate Duration", Translations.Options.DuplicateDuration)
                .BindFloat(duplicateDuration.SetDuration)
                .AddFloatRange(2.5f, 120, 2.5f, 5, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Spawn Location", Translations.Options.SpawnLocation)
                .Value(v => v.Value(false).Text(Translations.Options.CurrentLocation).Color(Color.cyan).Build())
                .Value(v => v.Value(true).Text(Translations.Options.RandomLocation).Color(Color.red).Build())
                .BindBool(v => spawnsRandomly = v)
                .Build());
    */

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.87f, 0.6f, 1f))
            .RoleFlags(RoleFlag.DontRegisterOptions) // still wip
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet);

    [Localized(nameof(Duplicator))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(DuplicateCooldown))] public static string DuplicateCooldown = "Duplicate Cooldown";
            [Localized(nameof(DuplicateDuration))] public static string DuplicateDuration = "Duplicate Duration";
            [Localized(nameof(SpawnLocation))] public static string SpawnLocation = "Spawn Location";
            [Localized(nameof(CurrentLocation))] public static string CurrentLocation = "Current";
            [Localized(nameof(RandomLocation))] public static string RandomLocation = "Random";
        }
    }
}