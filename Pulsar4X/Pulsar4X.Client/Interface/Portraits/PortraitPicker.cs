using System;
using System.Collections.Generic;
using System.Numerics;

namespace Pulsar4X.Client;

/// <summary>One layer after a seeded pick. <see cref="File"/> is null when the layer is empty.</summary>
public sealed record PortraitLayerPick(
    string? File,
    string? FallbackFile,
    Vector4 Tint,
    Vector2 Offset = default,
    string? Anchor = null,
    int FileIndex = -1,
    int FileCount = 0);

/// <summary>
/// Optional pins for the debug portrait builder. Overriding a layer or palette
/// pins that pick; everything else still comes from the seed.
/// A layer value of <c>null</c> means None (the layer is empty).
/// </summary>
public sealed class PortraitPickOverrides
{
    public Dictionary<string, int?> Layers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Palettes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasLayer(string id) => Layers.ContainsKey(id);
    public bool HasPalette(string name) => Palettes.ContainsKey(name);

    public void Clear()
    {
        Layers.Clear();
        Palettes.Clear();
    }
}

/// <summary>
/// Seeded layer and palette picks for a layered portrait. Pure: no ImGui, no I/O.
/// Uses a fixed SplitMix32 PRNG, not System.Random, so results do not depend on the runtime.
/// </summary>
public static class PortraitPicker
{
    public const string CivilianRole = "civilian";

    public static string NormalizeRole(string? role, IReadOnlyList<string>? knownRoles = null)
    {
        var known = knownRoles is { Count: > 0 } ? knownRoles : PortraitCatalog.KnownRoles;
        if (string.IsNullOrWhiteSpace(role))
            return CivilianRole;
        var trimmed = role.Trim().ToLowerInvariant();
        foreach (var item in known)
        {
            if (string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase))
                return item.ToLowerInvariant();
        }
        return CivilianRole;
    }

    public static IReadOnlyList<PortraitLayerPick> Pick(
        PortraitCatalog catalog,
        int seed,
        string? role,
        Vector4 factionColour)
        => Pick(catalog, seed, role, factionColour, species: null, overrides: null);

    public static IReadOnlyList<PortraitLayerPick> Pick(
        PortraitCatalog catalog,
        int seed,
        string? role,
        Vector4 factionColour,
        string? species,
        PortraitPickOverrides? overrides = null)
    {
        var pack = catalog.ResolveSpecies(species);
        var rng = new PortraitRng(seed);
        string resolved = NormalizeRole(role, catalog.Roles);

        // Pick one colour per palette in species.json key order, then pick the layers.
        // That matches compose_preview.py and keeps layers that share a palette in sync.
        var palettePicks = new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase);
        var paletteNames = pack.PaletteOrder.Count > 0 ? pack.PaletteOrder : pack.Palettes.Keys;
        foreach (var name in paletteNames)
        {
            if (!pack.Palettes.TryGetValue(name, out var colours))
                continue;
            palettePicks[name] = NextPalette(rng, colours);
        }

        if (overrides != null)
        {
            foreach (var pin in overrides.Palettes)
            {
                if (!pack.Palettes.TryGetValue(pin.Key, out var colours) || colours.Count == 0)
                    continue;
                int index = pin.Value;
                if (index < 0) index = 0;
                palettePicks[pin.Key] = colours[index % colours.Count];
            }
        }

        var seeded = new List<PortraitLayerPick>(pack.Layers.Count);
        foreach (var layer in pack.Layers)
        {
            bool empty = layer.EmptyChance > 0f && rng.NextSingle() < layer.EmptyChance;
            var files = FilesOf(pack, layer);
            string? file;
            string? fallback = null;
            int fileIndex = -1;
            if (empty)
            {
                file = null;
            }
            else if (layer.ByRole)
            {
                file = PortraitCatalog.LayerPath(layer, resolved + ".png");
                fallback = PortraitCatalog.LayerPath(layer, CivilianRole + ".png");
                if (string.Equals(file, fallback, StringComparison.OrdinalIgnoreCase))
                    fallback = null;
                fileIndex = IndexOf(files, file);
            }
            else
            {
                file = files.Count == 0 ? null : files[rng.Next(files.Count)];
                fileIndex = IndexOf(files, file);
            }

            var tint = ResolveTint(layer.Tint, palettePicks, factionColour);
            seeded.Add(new PortraitLayerPick(file, fallback, tint, Vector2.Zero, layer.Anchor, fileIndex, files.Count));
        }

        if (overrides != null)
        {
            for (int i = 0; i < pack.Layers.Count; i++)
            {
                var layer = pack.Layers[i];
                if (!overrides.Layers.TryGetValue(layer.Id, out var pin))
                    continue;
                var files = FilesOf(pack, layer);
                string? file;
                string? fallback = null;
                int fileIndex = -1;
                if (pin is null || files.Count == 0)
                {
                    file = null;
                }
                else if (layer.ByRole)
                {
                    int index = pin.Value;
                    if (index < 0) index = 0;
                    file = files[index % files.Count];
                    fileIndex = index % files.Count;
                    fallback = PortraitCatalog.LayerPath(layer, CivilianRole + ".png");
                    if (string.Equals(file, fallback, StringComparison.OrdinalIgnoreCase))
                        fallback = null;
                }
                else
                {
                    int index = pin.Value;
                    if (index < 0) index = 0;
                    file = files[index % files.Count];
                    fileIndex = index % files.Count;
                }
                var current = seeded[i];
                seeded[i] = current with { File = file, FallbackFile = fallback, FileIndex = fileIndex, FileCount = files.Count };
            }
        }

        string? headFile = FindHeadFile(pack, seeded);
        for (int i = 0; i < seeded.Count; i++)
        {
            var pick = seeded[i];
            string? anchor = pack.Layers[i].Anchor;
            if (string.IsNullOrEmpty(anchor))
                continue;
            var offset = OffsetFor(pack, headFile, anchor);
            seeded[i] = pick with { Offset = offset, Anchor = anchor };
        }

        return seeded;
    }

    public static IReadOnlyList<string> FilesOf(PortraitSpeciesPack pack, PortraitLayerSpec layer)
    {
        if (pack.FilesByFolder.TryGetValue(layer.Folder, out var files))
            return files;
        return Array.Empty<string>();
    }

    static Vector4 NextPalette(PortraitRng rng, IReadOnlyList<Vector4> colours)
    {
        if (colours.Count == 0)
            return Vector4.One;
        return colours[rng.Next(colours.Count)];
    }

    static Vector4 ResolveTint(string tint, IReadOnlyDictionary<string, Vector4> palettes, Vector4 factionColour)
    {
        if (string.IsNullOrWhiteSpace(tint) || string.Equals(tint, "none", StringComparison.OrdinalIgnoreCase))
            return Vector4.One;
        if (string.Equals(tint, "faction", StringComparison.OrdinalIgnoreCase))
            return factionColour;
        if (palettes.TryGetValue(tint, out var colour))
            return colour;
        return Vector4.One;
    }

    static int IndexOf(IReadOnlyList<string> files, string? file)
    {
        if (string.IsNullOrEmpty(file))
            return -1;
        for (int i = 0; i < files.Count; i++)
        {
            if (string.Equals(files[i], file, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    static string? FindHeadFile(PortraitSpeciesPack pack, IReadOnlyList<PortraitLayerPick> picks)
    {
        for (int i = 0; i < pack.Layers.Count; i++)
        {
            var layer = pack.Layers[i];
            if (string.Equals(layer.Id, "head", StringComparison.OrdinalIgnoreCase)
                || string.Equals(layer.Folder, "head", StringComparison.OrdinalIgnoreCase))
                return picks[i].File ?? picks[i].FallbackFile;
        }
        return null;
    }

    static Vector2 OffsetFor(PortraitSpeciesPack pack, string? headFile, string anchor)
    {
        if (string.IsNullOrEmpty(headFile))
            return Vector2.Zero;
        if (pack.HeadAnchors.TryGetValue(headFile, out var anchors)
            && anchors.TryGetValue(anchor, out var offset))
            return offset;
        return Vector2.Zero;
    }
}

/// <summary>
/// Deterministic 32-bit SplitMix-style generator. The algorithm is fixed here so picks
/// stay stable across .NET versions (unlike <see cref="Random"/>).
/// </summary>
public struct PortraitRng
{
    uint _state;

    public PortraitRng(int seed)
    {
        // Mix the seed so nearby entity ids diverge. Zero is a valid seed.
        _state = Mix((uint)seed);
    }

    public uint NextUInt()
    {
        _state += 0x9E3779B9u;
        uint z = _state;
        z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
        z = (z ^ (z >> 13)) * 0xC2B2AE35u;
        return z ^ (z >> 16);
    }

    /// <summary>Uniform in [0, max) using rejection sampling to avoid modulo bias.</summary>
    public int Next(int max)
    {
        if (max <= 1)
            return 0;
        uint umax = (uint)max;
        uint threshold = (uint.MaxValue / umax) * umax;
        uint value;
        do
        {
            value = NextUInt();
        } while (value >= threshold);
        return (int)(value % umax);
    }

    /// <summary>Uniform in [0, 1).</summary>
    public float NextSingle()
        => (NextUInt() >> 8) * (1f / 16777216f);

    static uint Mix(uint x)
    {
        x += 0x9E3779B9u;
        x = (x ^ (x >> 16)) * 0x85EBCA6Bu;
        x = (x ^ (x >> 13)) * 0xC2B2AE35u;
        return x ^ (x >> 16);
    }
}
