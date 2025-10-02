// ==================================================================================
// Runtime Image Loader (with Caching & Cleanup)
//
// Utility for dynamically loading Sprites from disk at runtime. 
// Provides caching, automatic cleanup, and configurable texture import options.
//
// Key Features:
// - Sprite Caching:
//      • Caches loaded sprites/textures by absolute file path for reuse.
//      • Memorizes "misses" to avoid repeated failed disk reads.
// - Customizable Load Options:
//      • Configure pixels-per-unit, pivot, mipmaps, color space, wrap/filter mode, 
//        anisotropy, and hide flags.
//      • Includes a pre-defined `UI` profile optimized for UI elements.
// - Lifecycle Management:
//      • Call `Initialize()` once to automatically clear cached assets when scenes 
//        unload or when the application quits.
//      • Manual cache clearing is also supported via `ClearCache()`.
// - Safe Loading:
//      • Wraps file I/O in try/catch with graceful error handling and logging.
//      • Ensures destroyed textures and sprites are cleaned from memory.
//
// Usage:
// Call `RuntimeImageLoader.LoadSprite(path, name, options)` to load sprites from disk.  
// Useful for mods or dynamic content where assets must be loaded outside of Unity’s 
// built-in asset pipeline.
// ==================================================================================

using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight runtime image loader with caching and cleanup for Sprites (and their Textures).
/// Call Initialize() once if you want automatic cleanup on scene unload / app quit.
/// </summary>
public static class RuntimeImageLoader
{
    private static bool _initialized;

    // Cache: absolute path -> entry (null means "we tried; not found/failed")
    private static readonly Dictionary<string, AssetEntry> _cache = new Dictionary<string, AssetEntry>(StringComparer.OrdinalIgnoreCase);
    private static readonly AssetEntry _miss = default; // default(AssetEntry) used as "null" sentinel

    private struct AssetEntry
    {
        public Sprite Sprite;
        public Texture2D Texture; // keep texture alive; Sprite alone won’t free it
        public bool IsValid => Sprite != null && Texture != null;
    }

    /// <summary>Options for sprite creation / texture import.</summary>
    public struct SpriteLoadOptions
    {
        public float PixelsPerUnit;        // default 100
        public Vector2 Pivot;              // default (0.5, 0.5)
        public bool UseMipMaps;            // default false for UI
        public bool LinearColorSpace;      // default false
        public TextureWrapMode WrapMode;   // default Clamp for UI
        public FilterMode FilterMode;      // default Bilinear
        public int AnisoLevel;             // default 1
        public HideFlags HideFlags;        // default DontUnloadUnusedAsset

        public static SpriteLoadOptions UI => new SpriteLoadOptions
        {
            PixelsPerUnit = 100f,
            Pivot = new Vector2(0.5f, 0.5f),
            UseMipMaps = false,
            LinearColorSpace = false,
            WrapMode = TextureWrapMode.Clamp,
            FilterMode = FilterMode.Bilinear,
            AnisoLevel = 1,
            HideFlags = HideFlags.DontUnloadUnusedAsset
        };
    }

    /// <summary>
    /// Call once (e.g., your mod's Initialize) to auto-clean cached assets on scene unload / app quit.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        _initialized = true;

        SceneManager.sceneUnloaded += _ => ClearCache();
        Application.quitting += ClearCache;
    }

    /// <summary>
    /// Loads a Sprite from disk with caching. Returns null on failure (and memoizes the miss).
    /// </summary>
    /// <param name="p_absolutePath">Absolute file path to an image (e.g., PNG).</param>
    /// <param name="p_spriteName">Optional name for the sprite/texture (for debugging).</param>
    /// <param name="p_options">Sprite creation / texture import options (use SpriteLoadOptions.UI for UI).</param>
    public static Sprite LoadSprite(string p_absolutePath, string p_spriteName = null, SpriteLoadOptions? p_options = null)
    {
        if (string.IsNullOrEmpty(p_absolutePath))
            return null;

        // Normalize
        p_absolutePath = Path.GetFullPath(p_absolutePath);

        // Cache hit/miss?
        if (_cache.TryGetValue(p_absolutePath, out AssetEntry o_cached))
            return o_cached.IsValid ? o_cached.Sprite : null; // null => known miss

        if (!File.Exists(p_absolutePath))
        {
            _cache[p_absolutePath] = _miss; // remember the miss

            return null;
        }

        SpriteLoadOptions opt = p_options ?? SpriteLoadOptions.UI;

        try
        {
            byte[] bytes = File.ReadAllBytes(p_absolutePath);

            // Minimal temp size; will resize on LoadImage
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, opt.UseMipMaps, opt.LinearColorSpace)
            {
                name = p_spriteName ?? Path.GetFileNameWithoutExtension(p_absolutePath),
                wrapMode = opt.WrapMode,
                filterMode = opt.FilterMode,
                anisoLevel = opt.AnisoLevel,
                hideFlags = opt.HideFlags
            };

            // markNonReadable=true to free system memory after upload
            if (!tex.LoadImage(bytes, true))
            {
                UnityEngine.Object.Destroy(tex);

                _cache[p_absolutePath] = _miss;

                return null;
            }

            Rect rect = new Rect(0, 0, tex.width, tex.height);
            Vector2 pivot = opt.Pivot;

            Sprite sprite = Sprite.Create(tex, rect, pivot, opt.PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            sprite.hideFlags = opt.HideFlags;

            AssetEntry entry = new AssetEntry { Sprite = sprite, Texture = tex };

            _cache[p_absolutePath] = entry;

            return sprite;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RuntimeImageLoader] Failed to load sprite at \"{p_absolutePath}\": {e}");

            _cache[p_absolutePath] = _miss;

            return null;
        }
    }

    /// <summary>
    /// Clears and destroys all cached Sprites/Textures and cache entries to free up RAM.
    /// </summary>
    public static void ClearCache()
    {
        foreach (var kvp in _cache)
        {
            AssetEntry entry = kvp.Value;

            if (entry.Sprite != null)
                UnityEngine.Object.Destroy(entry.Sprite);

            if (entry.Texture != null)
                UnityEngine.Object.Destroy(entry.Texture);
        }

        _cache.Clear();
    }
}