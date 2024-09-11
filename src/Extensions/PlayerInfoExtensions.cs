namespace Lotus.Extensions;

public static class PlayerInfoExtensions
{
    public static string ColoredName(this NetworkedPlayerInfo playerInfo)
    {
        return playerInfo == null! ? "Unknown" : playerInfo.ColorName.Trim('(', ')');
    }
}

public static class PlayerOutfitExtensions
{
    public static NetworkedPlayerInfo.PlayerOutfit DeepCopy(this NetworkedPlayerInfo.PlayerOutfit outfit) => new()
    {
        PlayerName = outfit.PlayerName,
        ColorId = outfit.ColorId,
        HatId = outfit.HatId,
        PetId = outfit.PetId,
        SkinId = outfit.SkinId,
        VisorId = outfit.VisorId,
        NamePlateId = outfit.NamePlateId,
        HatSequenceId = outfit.HatSequenceId,
        PetSequenceId = outfit.PetSequenceId,
        SkinSequenceId = outfit.SkinSequenceId,
        VisorSequenceId = outfit.VisorSequenceId,
        NamePlateSequenceId = outfit.NamePlateSequenceId
    };
}