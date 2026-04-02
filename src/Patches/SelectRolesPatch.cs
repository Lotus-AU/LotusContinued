using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.GameModes;
using Lotus.GameModes.Normal.Standard;
using Lotus.GUI.Name.Holders;
using Lotus.Logging;
using Lotus.Managers;
using Lotus.Options;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name;
using VentLib.Utilities.Collections;

namespace Lotus.Patches;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
class SelectRolesPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(SelectRolesPatch));
    internal static Dictionary<byte, Remote<TextComponent>> desyncedIntroText = new();
    private static bool encounteredError;
    public static bool Prefix()
    {
        encounteredError = false;
        if (!AmongUsClient.Instance.AmHost) return true;
        try
        {
            List<PlayerControl> unassignedPlayers = Players.GetPlayers().ToList();
            if (GeneralOptions.AdminOptions.HostGM)
            {
                Game.AssignRole(PlayerControl.LocalPlayer, NormalStandardRoles.Instance.Special.GM, true);
                unassignedPlayers.RemoveAll(p => p.PlayerId == PlayerControl.LocalPlayer.PlayerId);
            }

            Game.CurrentGameMode.AssignRoles(unassignedPlayers);
        }
        catch (Exception exception)
        {
            encounteredError = true;
            FatalErrorHandler.ForceEnd(exception, "Assignment Phase");
        }
        return !ProjectLotus.AdvancedRoleAssignment;
    }

    public static void Postfix()
    {
        if (encounteredError) return;
        desyncedIntroText = new();

        TextTable textTable = new("ID", "Color", "Player", "Role", "SubRoles");
        Players.GetPlayers().Where(p => p != null).ForEach(p =>
        {
            var primaryRole = p.PrimaryRole();
            primaryRole.SyncOptions();
            textTable.AddEntry((object)p.PlayerId, ModConstants.ColorNames[p.cosmetics.ColorId], p.name, primaryRole.RoleName, p.SecondaryRoles().Fuse());
            desyncedIntroText.Add(p.PlayerId, p.NameModel().GetComponentHolder<TextHolder>().Add(new TextComponent(new LiveString(primaryRole.GetRoleIntroString()), [
                GameState.InIntro, GameState.InLobby], ViewMode.Additive, p)));
        });
        log.Debug($"RoleManager::SelectRoles~Postfix - Role Assignments\n{textTable}");
        if (!AmongUsClient.Instance.AmHost || encounteredError) return;
        Players.GetPlayers().ForEach(p => p.NameModel().RenderFor(p, GameState.InIntro, force: true));
    }
}