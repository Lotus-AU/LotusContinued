using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Utilities;
using UnityEngine;

namespace Lotus.GUI;

public class LotusAssets
{
    private static AssetBundle? _assetBundle;
    private static bool _loaded = false;

    public static AssetBundle Bundle
    {
        get
        {
            if (_loaded) return _assetBundle!;
            _loaded = true;
            #if ANDROID
            _assetBundle = AssetLoader.LoadAssetBundle("Lotus.assets.projectlotus_bundle-android");
            #else
            _assetBundle = AssetLoader.LoadAssetBundle("Lotus.assets.projectlotus_bundle-win");
            #endif
            return _assetBundle;
        }
    }

    public static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        return Bundle.LoadAsset<T>(path.ToLower())!;
    }


    public static bool TryLoadAsset<T>(string path, [MaybeNullWhen(false)] out T asset) where T : UnityEngine.Object
    {
        T? targetAsset = asset = Bundle.LoadAsset<T>(path);
        return targetAsset != null;
    }

    public static Sprite LoadSprite(string path, float pixelsPerUnit = 100f, bool linear = false, int mipMapLevel = 0, Vector4 borderInPixels = default(Vector4))
    {
        Sprite? originalSprite = Bundle.LoadAsset<Sprite>(path);
        if (originalSprite == null) return null!;

        Texture2D originalTexture = originalSprite.texture;
        Rect rect = originalSprite.rect;

        RenderTexture rt = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0, RenderTextureFormat.ARGB32, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
        Graphics.Blit(originalTexture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D texture = new((int)rect.width, (int)rect.height, TextureFormat.ARGB32, true, linear);
        texture.ReadPixels(new Rect(0, 0, rect.width, rect.height), 0, 0);
        texture.Apply(true);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        Sprite sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            borderInPixels);
        sprite.texture.requestedMipmapLevel = mipMapLevel;

        return sprite;
    }

    public static bool TryLoadSprite(string path, [MaybeNullWhen(false)] out Sprite sprite, float pixelsPerUnit = 100f, bool linear = false, int mipMapLevel = 0, Vector4 borderInPixels = default(Vector4))
    {
        Sprite outputSprite = sprite = LoadSprite(path, pixelsPerUnit, linear, mipMapLevel, borderInPixels);
        return outputSprite != null!;
    }
}