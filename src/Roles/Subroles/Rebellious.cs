using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Interfaces;
using Lotus.Factions.Neutrals;
using Lotus.GameModes.Normal.Standard;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers;
using Lotus.Roles.Internals.Enums;
using Lotus.Utilities;
using MonoMod.Utils;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.Subroles;

public class Rebellious: Subrole
{
    private static ColorGradient _rebelliousGradient = new(ModConstants.Palette.MadmateColor, Color.red);
    public static Dictionary<Type, int> FactionMaxDictionary = new();

    private bool canAssignToNK;
    private bool canAssignToNP;

    public override string Identifier() => "";

    public Rebellious()
    {
        NormalStandardRoles.Callbacks.Add(AddFactionSettings);
    }

    protected override void PostSetup()
    {
        CustomRole role = MyPlayer.PrimaryRole();
        RoleHolder roleHolder = MyPlayer.NameModel().GetComponentHolder<RoleHolder>();
        string newRoleName = _rebelliousGradient.Apply(role.RoleName);
        role.RoleColorGradient = _rebelliousGradient;
        roleHolder.Add(new RoleComponent(new LiveString(newRoleName), Game.InGameStates, ViewMode.Replace, MyPlayer));
        new RoleModifier(role)
            .SpecialType(SpecialType.Madmate)
            .Faction(FactionInstances.Madmates);
    }

    private int GetAmountOfPeopleOnFaction(Type faction) => Players.GetAlivePlayers().Count(p =>
        p.PrimaryRole().Faction.GetType() == faction && p.GetSubroles().Any(s => s is Rebellious));

    public override bool IsAssignableTo(PlayerControl player)
    {
        SpecialType specialType = player.PrimaryRole().SpecialType;
        if (specialType == SpecialType.Neutral && !canAssignToNP) return false;
        if (specialType == SpecialType.NeutralKilling && !canAssignToNK) return false;

        IFaction playerFaction = player.PrimaryRole().Faction;
        if (playerFaction is INeutralFaction) playerFaction = FactionInstances.Neutral;
        Type myFaction = playerFaction.GetType();

        // Check if their faction already has the max amount of allowed players.
        // If they are maxed out, we don't even call base and just immediately exit.
        return GetAmountOfPeopleOnFaction(myFaction) < FactionMaxDictionary.GetValueOrDefault(myFaction, 0) && base.IsAssignableTo(player);
    }

    private void AddFactionSettings()
    {
        Dictionary<Type, IFaction> allFactions = new() {
            {FactionInstances.Crewmates.GetType(), FactionInstances.Crewmates},
            {FactionInstances.Neutral.GetType(), FactionInstances.Neutral},
            {FactionInstances.TheUndead.GetType(), FactionInstances.TheUndead}
        };
        allFactions.AddRange(FactionInstances.AddonFactions);
        allFactions.ForEach(kvp =>
        {
            string keyName = Translations.Options.FactionMaxRogues.Formatted(kvp.Value.Name());
            Option option = new GameOptionBuilder()
                .KeyName(TranslationUtil.Remove(keyName), TranslationUtil.Colorize(keyName, kvp.Value.Color))
                .AddIntRange(0, ModConstants.MaxPlayers, 1, 1)
                .BindInt(i => FactionMaxDictionary[kvp.Key] = i)
                .Build();
            RoleOptions.AddChild(option);
            GlobalRoleManager.RoleOptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        });
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => base
        .RegisterOptions(optionStream)
        .SubOption(sub => sub
            .KeyName("Assign to NK", Translations.Options.AssignToNK)
            .BindBool(b => canAssignToNK = b)
            .AddBoolean()
            .Build())
        .SubOption(sub => sub
            .KeyName("Assign to NP", Translations.Options.AssignToNP)
            .BindBool(b => canAssignToNP = b)
            .AddBoolean()
            .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .RoleColor(ModConstants.Palette.MadmateColor);

    [Localized(nameof(Rebellious))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(AssignToNK))] public static string AssignToNK = "Assign to NK";
            [Localized(nameof(AssignToNP))] public static string AssignToNP = "Assign to NP";
            [Localized(nameof(FactionMaxRogues))] public static string FactionMaxRogues = "{0}::0 Faction Max Rebellious";
        }
    }
}