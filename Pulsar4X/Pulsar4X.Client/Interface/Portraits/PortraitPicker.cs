using System;
using System.Collections.Generic;
using System.Numerics;

namespace Pulsar4X.Client;

/// <summary>One layer after a seeded pick. <see cref="File"/> is null when the layer is empty.</summary>
public sealed record PortraitLayerPick(string? File, string? FallbackFile, Vector4 Tint);

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
    {
        var rng = new PortraitRng(seed);
        string resolved = NormalizeRole(role, catalog.Roles);
        var skin = PickPalette(rng, catalog, "skin");
        var hair = PickPalette(rng, catalog, "hair");
        var eye = PickPalette(rng, catalog, "eye");
        var uniform = PickPalette(rng, catalog, "uniform");

        var picks = new List<PortraitLayerPick>(catalog.Layers.Count);
        foreach (var layer in catalog.Layers)
        {
            bool empty = layer.EmptyChance > 0f && rng.NextSingle() < layer.EmptyChance;
            if (empty)
            {
                picks.Add(new PortraitLayerPick(null, null, Vector4.One));
                continue;
            }

            string? file;
            string? fallback = null;
            if (layer.ByRole)
            {
                file = layer.Folder + "/" + resolved + ".png";
                fallback = layer.Folder + "/" + CivilianRole + ".png";
                if (string.Equals(file, fallback, StringComparison.OrdinalIgnoreCase))
                    fallback = null;
            }
            else
            {
                file = PickFile(rng, catalog, layer.Folder);
            }

            var tint = layer.Tint switch
            {
                "skin" => skin,
                "hair" => hair,
                "eye" => eye,
                "uniform" => uniform,
                "faction" => factionColour,
                _ => Vector4.One,
            };
            picks.Add(new PortraitLayerPick(file, fallback, tint));
        }
        return picks;
    }

    static Vector4 PickPalette(PortraitRng rng, PortraitCatalog catalog, string name)
    {
        if (!catalog.Palettes.TryGetValue(name, out var colours) || colours.Count == 0)
            return Vector4.One;
        return colours[rng.Next(colours.Count)];
    }

    static string? PickFile(PortraitRng rng, PortraitCatalog catalog, string folder)
    {
        if (!catalog.FilesByFolder.TryGetValue(folder, out var files) || files.Count == 0)
            return null;
        return files[rng.Next(files.Count)];
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
