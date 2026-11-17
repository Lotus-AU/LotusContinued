using System;

namespace Lotus.Extensions;

public static class OperatingSystemExtensions
{
    extension(OperatingSystem _)
    {
        public static bool IsMobile() => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
    }
}