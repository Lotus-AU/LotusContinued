using System;
using System.Collections.Generic;
using System.Reflection;
using InnerNet;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;

namespace Lotus.GUI;

[RegisterInIl2Cpp]
internal class PersistentAssetLoader : MonoBehaviour
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PersistentAssetLoader));

    private static Dictionary<string, SpriteRenderer> _spriteRenderers = new();
    private static readonly Dictionary<string, (string path, int pixelsPerUnit, Assembly assembly)> SpriteInfo = new();
    private static bool _initialized;

    private static PersistentAssetLoader _instance = null!;
    private List<GameObject> anchors = new();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public PersistentAssetLoader(IntPtr intPtr) : base(intPtr)
    {
        if (_instance != null) return;
        if (!_initialized) LoadAssets();

        _initialized = true;
        _instance = this;
    }

    private void LoadAssets()
    {
        SpriteInfo.ForEach(si => LoadSprite(si.Key, si.Value.path, si.Value.pixelsPerUnit, si.Value.assembly));
    }

    private void LoadSprite(string key, string path, int pixelsPerUnit, Assembly? assembly = null)
    {
        log.Debug($"Loading Persistent Sprite: {key} => {path}", "PersistentAssetLoader");
        GameObject anchor = gameObject.CreateChild("Anchor");
        anchors.Add(anchor);
        SpriteRenderer render = anchor.AddComponent<SpriteRenderer>();
        render.sprite = AssetLoader.LoadSprite(path, pixelsPerUnit, assembly: assembly);
        render.enabled = false;
        _spriteRenderers[key] = render;
    }

    public static Func<Sprite> RegisterSprite(string key, string path, int pixelsPerUnit, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        SpriteInfo.Add(key, (path, pixelsPerUnit, assembly));
        if (_initialized) _instance.LoadSprite(key, path, pixelsPerUnit, assembly);
        return () => _spriteRenderers[key].sprite;
    }

    public static Sprite GetSprite(string key) => _spriteRenderers[key].sprite;

    [QuickPostfix(typeof(InnerNetClient), nameof(InnerNetClient.Start))]
    public static void HookToInnerNetClient(InnerNetClient __instance)
    {
        __instance.gameObject.AddComponent<PersistentAssetLoader>();
    }
}