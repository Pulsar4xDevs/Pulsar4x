using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using SDL3;

namespace Pulsar4X.Client;

/// <summary>
/// Draws a layered portrait with ImGui draw-list AddImage and a multiply tint.
/// Successful textures are cached per file. Misses are not cached, so Reload can retry.
/// Missing art is skipped quietly.
/// </summary>
public sealed class PortraitRenderer
{
    public const float SmallSize = 48f;
    public const float LargeSize = 128f;
    public const float TemplateSize = PortraitAnchors.TemplateSize;

    static readonly object Gate = new();
    static PortraitCatalog? _catalog;
    static bool _catalogLoaded;
    static readonly PortraitAssetCache<IntPtr> Cache = new();
    static IntPtr _rendererTag;

    readonly GlobalUIState _uiState;

    PortraitRenderer(GlobalUIState uiState) => _uiState = uiState;

    public static PortraitRenderer For(GlobalUIState uiState)
    {
        var renderer = new PortraitRenderer(uiState);
        renderer.ForgetCacheIfRendererChanged();
        return renderer;
    }

    public static PortraitCatalog Catalog
    {
        get
        {
            EnsureCatalog();
            return _catalog!;
        }
    }

    /// <summary>Rescan species packs on disk and drop cached textures so new art is picked up.</summary>
    public static void Reload()
    {
        lock (Gate)
        {
            ClearCacheUnlocked();
            _catalogLoaded = false;
            string dir = Path.Combine(PulsarMainWindow.ResourcesPath, "Portraits");
            try
            {
                _catalog = PortraitCatalog.Load(dir);
            }
            catch
            {
                _catalog = new PortraitCatalog(
                    Array.Empty<PortraitLayerSpec>(),
                    new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IReadOnlyList<Vector4>>());
            }
            _catalogLoaded = true;
        }
    }

    public static void ClearCache()
    {
        lock (Gate)
            ClearCacheUnlocked();
    }

    /// <summary>Exposed for tests: the live texture-slot cache (successful loads only).</summary>
    internal static PortraitAssetCache<IntPtr> TextureCache => Cache;

    /// <summary>Draw the portrait, or an empty-chair placeholder when the post is vacant.</summary>
    public void Draw(PortraitRef portrait, float size, PortraitPickOverrides? overrides = null)
        => Draw(portrait.Seed, portrait.Role, PortraitIdentity.FactionColour(_uiState), size, portrait.Species, overrides);

    /// <summary>Draw the portrait for this seed and role at <paramref name="size"/> pixels, reserved in the layout.</summary>
    public void Draw(int seed, string role, Vector4 factionColour, float size, string? species = null, PortraitPickOverrides? overrides = null)
    {
        if (PortraitIdentity.IsEmpty(seed, role))
        {
            DrawEmpty(size);
            return;
        }

        EnsureCatalog();
        ForgetCacheIfRendererChanged();
        var picks = PortraitPicker.Pick(_catalog!, seed, role, factionColour, species, overrides);
        Vector2 origin = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        foreach (var pick in picks)
        {
            IntPtr texture = Load(pick.File);
            if (texture == IntPtr.Zero)
                texture = Load(pick.FallbackFile);
            if (texture == IntPtr.Zero)
                continue;
            Vector2 shift = PortraitAnchors.ScaleOffset(pick.Offset, size);
            Vector2 start = origin + shift;
            Vector2 end = new(start.X + size, start.Y + size);
            uint tint = ImGui.ColorConvertFloat4ToU32(pick.Tint);
            drawList.AddImage(texture.ToTextureRef(), start, end, Vector2.Zero, Vector2.One, tint);
        }
        ImGui.Dummy(new Vector2(size, size));
    }

    public void DrawPerson(CommanderSnapshot person, float size)
        => Draw(PortraitIdentity.Of(person), size);

    public void DrawCandidate(CandidateView candidate, float size)
        => Draw(PortraitIdentity.Of(candidate), size);

    void DrawEmpty(float size)
    {
        Vector2 start = ImGui.GetCursorScreenPos();
        Vector2 end = new(start.X + size, start.Y + size);
        var drawList = ImGui.GetWindowDrawList();
        uint fill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.18f, 0.22f, 0.7f));
        uint border = ImGui.ColorConvertFloat4ToU32(new Vector4(0.45f, 0.45f, 0.5f, 0.55f));
        drawList.AddRectFilled(start, end, fill, 4f);
        drawList.AddRect(start, end, border, 4f);
        ImGui.Dummy(new Vector2(size, size));
    }

    IntPtr Load(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return IntPtr.Zero;

        ForgetCacheIfRendererChanged();
        if (Cache.TryGet(relativePath, out var cached))
            return cached;

        IntPtr loaded = IntPtr.Zero;
        try
        {
            var renderer = _uiState.ViewPort?.Renderer ?? IntPtr.Zero;
            if (renderer != IntPtr.Zero)
            {
                string path = Path.Combine(PulsarMainWindow.ResourcesPath, "Portraits", relativePath);
                if (File.Exists(path))
                    loaded = Image.LoadTexture(renderer, path);
            }
        }
        catch
        {
            loaded = IntPtr.Zero;
        }

        // Misses are not cached, so a later Reload can retry the file.
        if (loaded != IntPtr.Zero)
            Cache.Set(relativePath, loaded);
        return loaded;
    }

    void ForgetCacheIfRendererChanged()
    {
        var renderer = _uiState.ViewPort?.Renderer ?? IntPtr.Zero;
        lock (Gate)
        {
            if (renderer == _rendererTag)
                return;
            ClearCacheUnlocked();
            _rendererTag = renderer;
        }
    }

    static void ClearCacheUnlocked()
    {
        var values = Cache.SnapshotValues(t => t);
        Cache.Clear();
        foreach (var texture in values)
        {
            if (texture == IntPtr.Zero)
                continue;
            try
            {
                SDL.DestroyTexture(texture);
            }
            catch
            {
                // The renderer may already have been torn down.
            }
        }
    }

    static void EnsureCatalog()
    {
        if (_catalogLoaded)
            return;
        lock (Gate)
        {
            if (_catalogLoaded)
                return;
            try
            {
                string dir = Path.Combine(PulsarMainWindow.ResourcesPath, "Portraits");
                _catalog = PortraitCatalog.Load(dir);
            }
            catch
            {
                _catalog = new PortraitCatalog(
                    Array.Empty<PortraitLayerSpec>(),
                    new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IReadOnlyList<Vector4>>());
            }
            _catalogLoaded = true;
        }
    }
}
