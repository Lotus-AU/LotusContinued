using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Factions.Crew;
using Lotus.Factions.Impostors;
using Lotus.GameModes.Normal.Standard;
using Lotus.Logging;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Managers.Interfaces;
using UnityEngine;
using VentLib.Localization;
using VentLib.Utilities;

namespace Lotus.Patches.Intro;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(BeginCrewmatePatch));

    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        //Team Display Change
        DevLogger.Log("Begin Crewmate");
        CustomRole role = PlayerControl.LocalPlayer.PrimaryRole();
        if (role.GetType() == IRoleManager.Current.FallbackRole().GetType()) return;

        if (role.IntroSound != null) PlayerControl.LocalPlayer.Data.Role.IntroSound = role.IntroSound();
        else PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(role.RealRole);

        switch (role.Metadata.GetOrDefault(LotusKeys.AuxiliaryRoleType, SpecialType.None))
        {
            case SpecialType.NeutralKilling:
            case SpecialType.Undead:
            case SpecialType.Neutral:
                __instance.TeamTitle.text = Factions.FactionInstances.Neutral.Name();
                __instance.TeamTitle.color = Color.white;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "";
                __instance.BackgroundBar.material.color = role.RoleColor;
                break;
            case SpecialType.Madmate:
                __instance.TeamTitle.text = NormalStandardRoles.Instance.Static.Madmate.RoleName;
                __instance.TeamTitle.color = ModConstants.Palette.MadmateColor;
                __instance.ImpostorText.text = "";
                StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                break;
        }

        // if (role.RoleDefinition is not GameMaster) return;

        __instance.TeamTitle.text = role.RoleName;
        __instance.TeamTitle.color = role.RoleColor;
        __instance.BackgroundBar.material.color = role.RoleColor;
        if (role.Faction is ImpostorFaction) __instance.ImpostorText.gameObject.SetActive(false);
        Async.Schedule(() => ShowRolePatch.Postfix(__instance), 4f);
    }

    public static AudioClip? GetIntroSound(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.ToArray().FirstOrDefault(role => role.Role == roleType)?.IntroSound;
    }

    private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
    {
        await System.Threading.Tasks.Task.Delay(1000);
        int milliseconds = 0;
        while (true)
        {
            DevLogger.Log("???");
            await System.Threading.Tasks.Task.Delay(20);
            milliseconds += 20;
            float time = milliseconds / (float)500;
            Color lerpingColor = Color.Lerp(start, end, time);
            if (__instance == null || milliseconds > 500)
            {
                log.Trace("Exit The Loop (GTranslated)", "StartFadeIntro");
                break;
            }
            __instance.BackgroundBar.material.color = lerpingColor;
        }
    }
}