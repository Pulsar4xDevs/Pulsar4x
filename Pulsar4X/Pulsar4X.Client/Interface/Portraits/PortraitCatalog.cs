using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pulsar4X.Client;

/// <summary>A layer in <c>portrait-layers.json</c>, drawn back to front.</summary>
public sealed record PortraitLayerSpec(string Id, string Folder, string Tint, float EmptyChance, bool ByRole);

/// <summary>Parsed portrait-layers.json plus the PNG files found in each layer folder.</summary>
public sealed class PortraitCatalog
{
    public static readonly IReadOnlyList<string> KnownRoles = new[]
    {
        "captain", "administrator", "industry-head", "scientist",
        "lab-technician", "civilian", "candidate",
    };

    public IReadOnlyList<PortraitLayerSpec> Layers { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<Vector4>> Palettes { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FilesByFolder { get; }
    public IReadOnlyList<string> Roles { get; }

    public PortraitCatalog(
        IReadOnlyList<PortraitLayerSpec> layers,
        IReadOnlyDictionary<string, IReadOnlyList<Vector4>> palettes,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? filesByFolder = null,
        IReadOnlyList<string>? roles = null)
    {
        Layers = layers ?? Array.Empty<PortraitLayerSpec>();
        Palettes = palettes ?? new Dictionary<string, IReadOnlyList<Vector4>>();
        FilesByFolder = filesByFolder ?? new Dictionary<string, IReadOnlyList<string>>();
        Roles = roles is { Count: > 0 } ? roles : KnownRoles;
    }

    public static PortraitCatalog Parse(string json, IReadOnlyDictionary<string, IReadOnlyList<string>>? filesByFolder = null)
    {
        var root = JObject.Parse(json);
        var layers = new List<PortraitLayerSpec>();
        foreach (var token in root["layers"] as JArray ?? new JArray())
        {
            if (token is not JObject obj)
                continue;
            string id = obj.Value<string>("id") ?? "";
            if (string.IsNullOrEmpty(id))
                continue;
            string folder = obj.Value<string>("folder") ?? id;
            string tint = (obj.Value<string>("tint") ?? "none").Trim().ToLowerInvariant();
            float empty = obj.Value<float?>("emptyChance") ?? 0f;
            if (empty < 0f) empty = 0f;
            if (empty > 1f) empty = 1f;
            bool byRole = obj.Value<bool?>("byRole") ?? false;
            layers.Add(new PortraitLayerSpec(id, folder, tint, empty, byRole));
        }

        var palettes = new Dictionary<string, IReadOnlyList<Vector4>>(StringComparer.OrdinalIgnoreCase);
        if (root["palettes"] is JObject pal)
        {
            foreach (var prop in pal.Properties())
            {
                var colours = new List<Vector4>();
                if (prop.Value is JArray arr)
                {
                    foreach (var item in arr)
                    {
                        if (TryParseColour(item.Value<string>(), out var colour))
                            colours.Add(colour);
                    }
                }
                palettes[prop.Name] = colours;
            }
        }

        var roles = new List<string>();
        if (root["roles"] is JArray roleArr)
        {
            foreach (var item in roleArr)
            {
                var role = item.Value<string>();
                if (!string.IsNullOrWhiteSpace(role))
                    roles.Add(role.Trim().ToLowerInvariant());
            }
        }

        return new PortraitCatalog(layers, palettes, filesByFolder, roles);
    }

    public static PortraitCatalog Load(string directory)
    {
        string jsonPath = Path.Combine(directory, "portrait-layers.json");
        if (!File.Exists(jsonPath))
            return new PortraitCatalog(Array.Empty<PortraitLayerSpec>(), new Dictionary<string, IReadOnlyList<Vector4>>());

        var files = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(directory))
        {
            foreach (var folder in Directory.GetDirectories(directory))
            {
                string name = Path.GetFileName(folder);
                var pngs = Directory.GetFiles(folder, "*.png")
                    .Select(p => Path.GetFileName(p)!)
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .Select(n => name + "/" + n)
                    .ToList();
                files[name] = pngs;
            }
        }

        return Parse(File.ReadAllText(jsonPath), files);
    }

    public static bool TryParseColour(string? text, out Vector4 colour)
    {
        colour = Vector4.One;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var hex = text.Trim();
        if (hex.StartsWith("#", StringComparison.Ordinal))
            hex = hex[1..];
        if (hex.Length != 6 && hex.Length != 8)
            return false;
        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packed))
            return false;
        if (hex.Length == 6)
        {
            colour = new Vector4(
                ((packed >> 16) & 0xFF) / 255f,
                ((packed >> 8) & 0xFF) / 255f,
                (packed & 0xFF) / 255f,
                1f);
        }
        else
        {
            colour = new Vector4(
                ((packed >> 24) & 0xFF) / 255f,
                ((packed >> 16) & 0xFF) / 255f,
                ((packed >> 8) & 0xFF) / 255f,
                (packed & 0xFF) / 255f);
        }
        return true;
    }
}
