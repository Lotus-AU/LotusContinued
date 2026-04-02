using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Lotus.Utilities;

// code from: https://github.com/NuclearPowered/Reactor/blob/master/Reactor/Patches/Miscellaneous/CustomServersPatch.cs
namespace Lotus.Server.Patches;

[HarmonyPatch]
internal static class AuthDisablePatch
{
    public static bool IsCurrentServerOfficial()
    {
        const string Domain = "among.us";

        return ServerManager.Instance.CurrentRegion?.TryCast<StaticHttpRegionInfo>() is { } regionInfo &&
               regionInfo.PingServer.EndsWith(Domain, StringComparison.Ordinal) &&
               regionInfo.Servers.All(serverInfo => serverInfo.Ip.EndsWith(Domain, StringComparison.Ordinal));
    }
}

[HarmonyPatch]
class AuthManagerCoConnectPatch
{
    public static MethodBase TargetMethod()
    {
        return Utils.GetStateMachineMoveNext<AuthManager>(nameof(AuthManager.CoConnect))!;
    }

    public static bool Prefix(ref bool __result)
    {
        if (AuthDisablePatch.IsCurrentServerOfficial())
        {
            return true;
        }

        __result = false;
        return false;
    }
}
[HarmonyPatch]
class AuthManagerCoWaitForNoncePatch
{
    public static MethodBase TargetMethod()
    {
        return Utils.GetStateMachineMoveNext<AuthManager>(nameof(AuthManager.CoWaitForNonce))!;
    }

    public static bool Prefix(ref bool __result)
    {
        if (AuthDisablePatch.IsCurrentServerOfficial())
        {
            return true;
        }

        __result = false;
        return false;
    }
}
[HarmonyPatch]
class AmongUsClientCoJoinOnlineGamePatch
{
    public static MethodBase TargetMethod()
    {
        return Utils.GetStateMachineMoveNext<AmongUsClient>(nameof(AmongUsClient.CoJoinOnlinePublicGame))!;
    }

    public static void Prefix(Il2CppObjectBase __instance)
    {
        var wrapper = new StateMachineWrapper<AmongUsClient>(__instance);
        if (wrapper.GetState() == 0 && !ServerManager.Instance.IsHttp)
        {
            wrapper.SetParameter("__1__state", 1);
            wrapper.SetParameter("__8__1", new AmongUsClient.__c__DisplayClass49_0
            {
                matchmakerToken = string.Empty,
            });
        }
    }
}