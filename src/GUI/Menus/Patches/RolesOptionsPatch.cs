using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace Lotus.GUI.Menus.Patches;

[HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.InitialSetup))]
public static class RolesSettingsMenuPatch
{
    public static void Postfix(RolesSettingsMenu __instance)
    {
        foreach (var ob in __instance.advancedSettingChildren.ToArray())
        {
            switch (ob.Title)
            {
                case StringNames.EngineerCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.ShapeshifterCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
            }
        }
    }
}
[HarmonyPatch(typeof(NormalGameOptionsV12), nameof(NormalGameOptionsV12.SetRecommendations), typeof(int), typeof(bool), typeof(RulesPresets))]
public static class SetRecommendationsPatch
{
    public static bool Prefix(NormalGameOptionsV12 __instance, int numPlayers, bool isOnline, RulesPresets rulesPresets)
    {
        numPlayers = Mathf.Clamp(numPlayers, 4, ModConstants.MaxPlayers);
        __instance.PlayerSpeedMod = __instance.MapId == 4 ? 1.25f : 1f; //AirShipなら1.25、それ以外は1
        __instance.CrewLightMod = 0.5f;
        __instance.ImpostorLightMod = 1.75f;
        __instance.KillCooldown = 25f;
        __instance.NumCommonTasks = 2;
        __instance.NumLongTasks = 4;
        __instance.NumShortTasks = 6;
        __instance.NumEmergencyMeetings = 1;
        if (!isOnline)
            __instance.NumImpostors = NormalGameOptionsV12.RecommendedImpostors[numPlayers];
        __instance.KillDistance = 0;
        __instance.DiscussionTime = 0;
        __instance.VotingTime = 150;
        __instance.IsDefaults = true;
        __instance.ConfirmImpostor = false;
        __instance.VisualTasks = false;

        __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Shapeshifter);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Scientist);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.GuardianAngel);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Engineer);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Tracker);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Phantom);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Noisemaker);

        /*if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek) //HideAndSeek
        {
            __instance.PlayerSpeedMod = 1.75f;
            __instance.CrewLightMod = 5f;
            __instance.ImpostorLightMod = 0.25f;
            __instance.NumImpostors = 1;
            __instance.NumCommonTasks = 0;
            __instance.NumLongTasks = 0;
            __instance.NumShortTasks = 10;
            __instance.KillCooldown = 10f;
        }
        if (OldOptions.IsStandardHAS) //StandardHAS
        {
            __instance.PlayerSpeedMod = 1.75f;
            __instance.CrewLightMod = 5f;
            __instance.ImpostorLightMod = 0.25f;
            __instance.NumImpostors = 1;
            __instance.NumCommonTasks = 0;
            __instance.NumLongTasks = 0;
            __instance.NumShortTasks = 10;
            __instance.KillCooldown = 10f;
        }*/
        return false;
    }
}

[HarmonyPatch(typeof(NormalGameOptionsV12), nameof(NormalGameOptionsV12.SetRecommendations), typeof(int), typeof(bool))]
class NoRulesRecommendationPatch
{
    public static bool Prefix(NormalGameOptionsV12 __instance, int numPlayers, bool isOnline)
    {
        return SetRecommendationsPatch.Prefix(__instance, numPlayers, isOnline, RulesPresets.Standard);
    }
}