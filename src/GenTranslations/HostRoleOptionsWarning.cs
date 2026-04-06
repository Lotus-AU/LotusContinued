using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Factions;
using Lotus.Options;
using Lotus.Options.General;
using Lotus.Options.Roles;
using UnityEngine;
using VentLib.Utilities.Extensions;
using VentLib.Utilities;
using Lotus.Roles;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Managers.Interfaces;
using Lotus.Utilities;
using VentLib.Localization.Attributes;

namespace Lotus.GenTranslations;

[Localized("RoleOptionsWarning")]
public static class HostRoleOptionsWarning
{
    [Localized(nameof(WarningTitle))] public static string WarningTitle = "⚠ WARNING ⚠";
    [Localized(nameof(WarningToHost))] public static string WarningToHost =
        "Automatically set {0} to {1} due to {2} being enabled.\nTo manually set your own amounts, change them in the Lotus Settings.";

    public static List<(Func<(string optionName, string factionName)> optionInfo, Func<bool> shouldChange, Action changeValue, object defaultValue)> OptionsToCheck =
    [
        (() => (TranslationUtil.Remove(NeutralOptions.NeutralOptionTranslations.MaximumNeutralKillingRoles), FactionTranslations.NeutralKillers.Name),
            () => RoleOptions.LoadNeutralOptions().MaximumNeutralKillingRoles == 0
                  && Game.CurrentGameMode.RoleManager.AllCustomRoles().Any(r => r.SpecialType is SpecialType.NeutralKilling && r.Count > 0 && r.Chance > 0),
            () =>
            {
                RoleOptions.LoadNeutralOptions().MaximumNeutralKillingRoles = 1;
            }, 1),
        (() => (TranslationUtil.Remove(NeutralOptions.NeutralOptionTranslations.MaximumNeutralPassiveRoles), FactionTranslations.NeutralKillers.Name),
            () => RoleOptions.LoadNeutralOptions().MaximumNeutralPassiveRoles == 0
                  && Game.CurrentGameMode.RoleManager.AllCustomRoles().Any(r => r.SpecialType is SpecialType.Neutral && r.Count > 0 && r.Chance > 0),
            () =>
            {
                RoleOptions.LoadNeutralOptions().MaximumNeutralPassiveRoles = 1;
            }, 1),
        (() => (TranslationUtil.Remove(SubroleOptions.SubroleOptionTranslations.ModifierMaximumText), FactionTranslations.Modifiers.Name),
            () => RoleOptions.LoadSubroleOptions().ModifierLimits == 0
                  && Game.CurrentGameMode.RoleManager.AllCustomRoles().Any(r => r.RoleFlags.HasFlag(RoleFlag.IsSubrole) && r.Count > 0 && r.Chance > 0),
            () =>
            {
                RoleOptions.LoadSubroleOptions().ModifierLimits = 1;
            }, 1)
    ];

    public static void CheckForOptions()
    {
        OptionsToCheck.ForEach(otc =>
        {
            if (!otc.shouldChange()) return;
            (string optionName, string factionName) = otc.optionInfo();

            otc.changeValue();

            ChatHandler.Of(WarningToHost.Formatted(optionName, otc.defaultValue, factionName),
                Color.yellow.Colorize(WarningTitle)).Send(PlayerControl.LocalPlayer);
        });
    }
}