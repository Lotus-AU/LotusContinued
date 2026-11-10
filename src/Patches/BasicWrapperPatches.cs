using Lotus.API.Odyssey;
using Lotus.Logging;
using Lotus.Utilities;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.Patches;

public class BasicWrapperPatches
{
    [QuickPrefix(typeof(PlayerControl), nameof(PlayerControl.RawSetName))]
    public static bool WrapSetNamePatch(PlayerControl __instance, string name)
    {
        __instance.cosmetics.SetName(name);
        __instance.cosmetics.SetNameMask(true);
        if (Game.State is GameState.InLobby) __instance.name = name.RemoveHtmlTags();
        return false;
    }

    [QuickPrefix(typeof(PlayerControl), nameof(PlayerControl.RawSetOutfit))]
    public static bool WrapSetOutfitPatch(PlayerControl __instance, NetworkedPlayerInfo.PlayerOutfit newOutfit, PlayerOutfitType type)
    {
        __instance.RawSetName(newOutfit.PlayerName);
        __instance.RawSetColor(newOutfit.ColorId);
        __instance.RawSetHat(newOutfit.HatId, newOutfit.ColorId);
        __instance.RawSetSkin(newOutfit.SkinId, newOutfit.ColorId);
        __instance.RawSetVisor(newOutfit.VisorId, newOutfit.ColorId);
        __instance.RawSetPet(newOutfit.PetId, newOutfit.ColorId);
        __instance.Visible = __instance.Visible;
        PlayerOutfitType currentOutfitType = __instance.CurrentOutfitType;
        __instance.CurrentOutfitType = type;
        if (currentOutfitType != PlayerOutfitType.Default)
        {
            __instance.Data.Outfits.Remove(currentOutfitType);
        }
        if (type != PlayerOutfitType.Default)
        {
            __instance.Data.SetOutfit(__instance.CurrentOutfitType, newOutfit);
        }
        return false;
    }
}