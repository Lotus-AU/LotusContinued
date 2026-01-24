using HarmonyLib;
using Lotus.Factions.Crew;
using Lotus.Extensions;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Logging;
using Lotus.API.Odyssey;
using Lotus.Roles.Managers.Interfaces;
using Lotus.API.Player;
using System.Linq;
using Lotus.Factions;
using VentLib.Utilities.Extensions;

namespace Lotus.Patches.Intro;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
class BeginImpostorPatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        DevLogger.Log("Begin Impostor");
        CustomRole role = PlayerControl.LocalPlayer.PrimaryRole();
        if (role.Faction is not Crewmates || role.GetType() == IRoleManager.Current.FallbackRole().GetType())
        {
            // if (role.IntroSound) PlayerControl.LocalPlayer.Data.Role.IntroSound = role.IntroSound;
            // else PlayerControl.LocalPlayer.Data.Role.IntroSound = BeginCrewmatePatch.GetIntroSound(role.RealRole);
            // if (role.GetType() != IRoleManager.Current.FallbackRole().GetType())
            // {
            //     yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            //     Players.GetPlayers().Where(p => PlayerControl.LocalPlayer.Relationship(p) is Relation.FullAllies).ForEach(yourTeam.Add);
            // }
            if (role.IntroSound != null) PlayerControl.LocalPlayer.Data.Role.IntroSound = role.IntroSound();
            else PlayerControl.LocalPlayer.Data.Role.IntroSound = BeginCrewmatePatch.GetIntroSound(role.RealRole);
            return true;
        }

        yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
        yourTeam.Add(PlayerControl.LocalPlayer);
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (!pc.AmOwner) yourTeam.Add(pc);
        }

        __instance.BeginCrewmate(yourTeam);
        #if PC
        __instance.overlayHandle.color = Palette.CrewmateBlue;
        #endif
        return false;
    }

    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
    }
}