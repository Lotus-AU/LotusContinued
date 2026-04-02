using System;
using System.Linq;
using HarmonyLib;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.GUI;
using Lotus.GUI.Menus.OptionsMenu;
using Lotus.Logging;
using Lotus.RPC;
using Lotus.Utilities;
using TMPro;
using UnityEngine;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using VentLib.Version;
using VentLib.Version.Git;
using Version = VentLib.Version.Version;

namespace Lotus.API;

[LoadStatic]
public static class ModVersion
{
    public static VersionControl VersionControl = null!;
    public static Version Version => VersionControl.Version!;

    private static (bool isCached, bool allModded) _moddedStatus = (false, false);

    static ModVersion()
    {
        const string clientsModdedStatusHookKey = nameof(clientsModdedStatusHookKey);
        Hooks.PlayerHooks.PlayerJoinHook.Bind(clientsModdedStatusHookKey, _ => _moddedStatus.isCached = false);
        const string clientsModdedStatusHookKey2 = nameof(clientsModdedStatusHookKey2);
        Hooks.PlayerHooks.PlayerDisconnectHook.Bind(clientsModdedStatusHookKey2, _ => _moddedStatus.isCached = false);
    }

    public static bool AllClientsModded()
    {
        if (_moddedStatus.isCached) return _moddedStatus.allModded;
        _moddedStatus.isCached = true;
        return _moddedStatus.allModded = PlayerControl.AllPlayerControls.ToArray().Where(p => !p.Data.Disconnected && !p.Data.IsIncomplete)
            .All(p =>
            {
                if (p == null || p.IsHost()) return true;
                return Version.Equals(VersionControl.GetPlayerVersion(p.PlayerId));
            });
    }

    [ModRPC((uint)ModCalls.ShowPlayerVersion, RpcActors.Host, RpcActors.NonHosts, MethodInvocation.ExecuteBefore)]
    public static void AddVersionShowerToPlayer(PlayerControl target, Version version)
    {
        GameObject backgroundHolder = new("Background");
        backgroundHolder.transform.SetParent(target.gameObject.FindChild<Transform>("Names"));
        // -1.7109
        backgroundHolder.transform.localPosition = new Vector3(0, -1.7f, 0);
        backgroundHolder.transform.localScale = Vector3.one;

        var renderer = backgroundHolder.AddComponent<SpriteRenderer>();
        renderer.sprite = LotusAssets.LoadSprite("VersionBackground.png", 300);

        GameObject textHolder = new("Text");
        textHolder.transform.SetParent(backgroundHolder.transform);
        textHolder.transform.localScale = new Vector3(.05f, .05f);
        // textHolder.transform.localPosition = new Vector3(.3f, .03f, 0f);
        textHolder.transform.localPosition = new Vector3(.1f, -.05f, 0f);

        string extension = " Beta";
        if (version.ToSimpleName().StartsWith("2")) extension = String.Empty; // not in beta
        else if (version is GitVersion gitVersion && gitVersion.Branch == "develop") extension = " Dev"; // dev branch

        var tmpText = textHolder.AddComponent<TextMeshPro>();
        tmpText.text = $"<font=\"OCRAEXT SDF\">v{version.ToSimpleName()}{extension}</font>".Trim();
        tmpText.fontSize = 24;

        // Change color based on version
        if (Version.Equals(version)) renderer.color = Color.green;
        else if (version is not GitVersion) renderer.color = Color.yellow;
        else
        {
            System.Version myVersion = new(Version.ToSimpleName());
            System.Version targetVersion = new(version.ToSimpleName());

            int comparison = myVersion.CompareTo(targetVersion);
            if (comparison < 0)
                // our version is older
                renderer.color = Color.white;
            else if (comparison > 0)
                // our version is newer
                renderer.color = Color.red;
            else
                // idk how you would get here but gl.
                renderer.color = Color.cyan;
        }
    }

    public static void DestroyPlayerVersions()
    {
        Players.GetPlayers().Do(p => p.FindChildOrEmpty<SpriteRenderer>("Background").IfPresent(s => s.gameObject.Destroy()));
    }
}