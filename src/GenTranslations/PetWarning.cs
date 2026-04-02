using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.API.Player;
using Lotus.Chat;
using UnityEngine;
using VentLib.Utilities.Extensions;
using VentLib.Utilities;
using Lotus.Roles;
using Lotus.Roles.Managers.Interfaces;
using VentLib.Localization.Attributes;

namespace Lotus.GenTranslations;

[Localized("PetWarning")]
public static class PetWarning
{
    [Localized(nameof(WarningToHost))] public static string WarningToHost = "The following players do not have pets: {0}\nThe following enabled roles require pet: {1}";
    [Localized(nameof(WarningToPlayer))] public static string WarningToPlayer = "There are roles enabled that have pets. Please equip one in the cosmetics area.";
    [Localized(nameof(WarningTitle))] public static string WarningTitle = "⚠ WARNING ⚠";

    public static void CheckForPets()
    {
        List<PlayerControl> playersWithoutPets = Players.GetAllPlayers()
            .Where(p => p.cosmetics?.CurrentPet?.Data?.ProductId == "pet_EmptyPet")
            .ToList();
        if (!playersWithoutPets.Any()) return;
        // Some players do not have pets.
        List<CustomRole> rolesWithPets = IRoleManager.Current.AllCustomRoles()
            .Where(r => r.RoleAbilityFlags.HasFlag(RoleAbilityFlag.UsesPet))
            .Where(r => r.Count > 0 && r.Chance > 0)
            .ToList();
        if (!rolesWithPets.Any()) return;
        // We have roles with pets.
        playersWithoutPets.ForEach(p =>
        {
            ChatHandler.Of(WarningToPlayer, Color.yellow.Colorize(WarningTitle)).Send(p);
        });
        ChatHandler.Of(WarningToHost.Formatted(
                playersWithoutPets.Join(p => p.name),
                rolesWithPets.Join(r => r.RoleName)),
            Color.yellow.Colorize(WarningTitle)).Send(PlayerControl.LocalPlayer);
    }
}