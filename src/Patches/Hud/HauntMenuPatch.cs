using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Managers.Interfaces;
using Lotus.Roles.RoleGroups.Crew;

namespace Lotus.Patches.Hud;

[HarmonyPatch(typeof(HauntMenuMinigame), nameof(HauntMenuMinigame.SetHauntTarget))]
class HauntMenuPatch
{
    public static bool Prefix(HauntMenuMinigame __instance, PlayerControl target)
    {
        if (!target)
        {
            __instance.HauntTarget = null;
            __instance.NameText.text = "";
            __instance.FilterText.text = "";
            __instance.HauntingText.enabled = false;
        }
        else
        {
            __instance.HauntTarget = target;
            __instance.HauntingText.enabled = true;
            NetworkedPlayerInfo data = target.Data;
            __instance.NameText.text = (data ? data.GetPlayerName(PlayerOutfitType.Default) : null);

            if (GeneralOptions.GameplayOptions.GhostsSeeInfo && !Players.GetAliveRoles().Any(r => r.RoleAbilityFlags.HasFlag(RoleAbilityFlag.CanRevivePlayers)))
                __instance.FilterText.text = target.PrimaryRole().RoleName;
            else if (data)
            {
                var emptyArray = new Il2CppReferenceArray<Object>(0);
                if (data.IsDead)
                    __instance.FilterText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Ghost, emptyArray);
                else if (data.Role.IsImpostor)
                    __instance.FilterText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Impostor, emptyArray);
                else
                    __instance.FilterText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Crewmate, emptyArray);
            }
        }

        return false;
    }
}