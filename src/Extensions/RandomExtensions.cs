namespace Lotus.Extensions;

public static class RandomExtensions
{
    public static float Next(this System.Random random, float min, float max)
    {
        return (float)(random.NextDouble() * (max - min) + min);
    }
}