#nullable enable
using System;
using AmongUs.GameOptions;

namespace Lotus.Extensions;

public static class GameOptionsExtensions
{
    [Obsolete("Looking into new TOH code to figure out a better way to do this")]
    public static NormalGameOptionsV12? AsNormalOptions(this IGameOptions options)
    {
        return options.Cast<NormalGameOptionsV12>();
    }

    public static Byte[] ToBytes(this IGameOptions gameOptions)
    {
        return GameOptionsManager.Instance.gameOptionsFactory.ToBytes(gameOptions, false);
    }

    public static IGameOptions DeepCopy(this IGameOptions opt)
    {
        return GameOptionsManager.Instance.gameOptionsFactory.FromBytes(opt.ToBytes());
    }
}