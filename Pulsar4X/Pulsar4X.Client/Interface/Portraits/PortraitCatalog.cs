using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>A layer in a species pack (or the legacy portrait-layers.json), drawn back to front.</summary>
public sealed record PortraitLayerSpec(
    string Id,
    string Folder,
    string Tint,
    float EmptyChance,
    bool ByRole,
    bool Shared = false,
    string? Anchor = null,
    string PathPrefix = "");

/// <summary>One species pack under <c>Portraits/species/&lt;id&gt;/</c>, or a legacy fallback pack.</summary>
public sealed class PortraitSpeciesPack
{
    public string Id { get; }
    public string Name { get; }
    public string Author { get; }
    public string Version { get; }
    public string License { get; }
    public IReadOnlyList<PortraitLayerSpec> Layers { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<Vector4>> Palettes { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FilesByFolder { get; }
    /// <summary>Head PNG path (relative to Portraits/) → anchor name → offset in 256-px template pixels.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, Vector2>> HeadAnchors { get; }

    public PortraitSpeciesPack(
        string id,
        string? name,
        IReadOnlyList<PortraitLayerSpec> layers,
        IReadOnlyDictionary<string, IReadOnlyList<Vector4>> palettes,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? filesByFolder = null,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, Vector2>>? headAnchors = null,
        string? author = null,
        string? version = null,
        string? license = null)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
        Author = author ?? "";
        Version = version ?? "";
        License = license ?? "";
        Layers = layers ?? Array.Empty<PortraitLayerSpec>();
        Palettes = palettes ?? new Dictionary<string, IReadOnlyList<Vector4>>();
        FilesByFolder = filesByFolder ?? new Dictionary<string, IReadOnlyList<string>>();
        HeadAnchors = headAnchors ?? new Dictionary<string, IReadOnlyDictionary<string, Vector2>>();
    }
}

/// <summary>Parsed portrait packs plus the PNG files found for each layer folder.</summary>
public sealed class PortraitCatalog
{
    public static readonly IReadOnlyList<string> KnownRoles = PortraitRoles.All;

    readonly string? _directory;
    readonly Dictionary<string, PortraitSpeciesPack> _packs;
    readonly PortraitSpeciesPack _primary;

    public IReadOnlyList<PortraitSpeciesPack> Packs { get; }
    public IReadOnlyList<PortraitLayerSpec> Layers => _primary.Layers;
    public IReadOnlyDictionary<string, IReadOnlyList<Vector4>> Palettes => _primary.Palettes;
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FilesByFolder => _primary.FilesByFolder;
    public IReadOnlyList<string> Roles { get; }
    public string DirectoryPath => _directory ?? "";

    public PortraitCatalog(
        IReadOnlyList<PortraitLayerSpec> layers,
        IReadOnlyDictionary<string, IReadOnlyList<Vector4>> palettes,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? filesByFolder = null,
        IReadOnlyList<string>? roles = null)
        : this(
            new[]
            {
                new PortraitSpeciesPack(
                    "human",
                    "Human",
                    layers,
                    palettes,
                    filesByFolder)
            },
            roles,
            directory: null)
    {
    }

    public PortraitCatalog(
        IReadOnlyList<PortraitSpeciesPack> packs,
        IReadOnlyList<string>? roles = null,
        string? directory = null)
    {
        var list = packs is { Count: > 0 } ? packs.ToList() : new List<PortraitSpeciesPack>
        {
            new("human", "Human", Array.Empty<PortraitLayerSpec>(), new Dictionary<string, IReadOnlyList<Vector4>>())
        };
        _packs = new Dictionary<string, PortraitSpeciesPack>(StringComparer.OrdinalIgnoreCase);
        foreach (var pack in list)
        {
            if (!_packs.ContainsKey(pack.Id))
                _packs[pack.Id] = pack;
        }
        Packs = _packs.Values.ToList();
        _primary = ResolvePrimary();
        Roles = roles is { Count: > 0 } ? roles : KnownRoles;
        _directory = directory;
    }

    /// <summary>The named pack, or human, or the first loaded pack when human is missing.</summary>
    public PortraitSpeciesPack ResolveSpecies(string? species)
    {
        if (!string.IsNullOrWhiteSpace(species) && _packs.TryGetValue(species.Trim(), out var exact))
            return exact;
        if (_packs.TryGetValue("human", out var human))
            return human;
        return _primary;
    }

    public static PortraitCatalog Parse(string json, IReadOnlyDictionary<string, IReadOnlyList<string>>? filesByFolder = null)
    {
        var parsed = ParseDocument(json);
        var pack = new PortraitSpeciesPack(
            "human",
            "Human",
            parsed.Layers,
            parsed.Palettes,
            filesByFolder);
        return new PortraitCatalog(new[] { pack }, parsed.Roles, directory: null);
    }

    /// <summary>Scan <c>species/*/species.json</c>, or fall back to legacy <c>portrait-layers.json</c>.</summary>
    public static PortraitCatalog Load(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return new PortraitCatalog(Array.Empty<PortraitLayerSpec>(), new Dictionary<string, IReadOnlyList<Vector4>>());

        var packs = new List<PortraitSpeciesPack>();
        string speciesRoot = Path.Combine(directory, "species");
        if (Directory.Exists(speciesRoot))
        {
            foreach (var folder in Directory.GetDirectories(speciesRoot).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var pack = TryLoadPack(directory, folder);
                if (pack != null && packs.All(p => !string.Equals(p.Id, pack.Id, StringComparison.OrdinalIgnoreCase)))
                    packs.Add(pack);
            }
        }

        if (packs.Count == 0)
        {
            var legacy = TryLoadLegacy(directory);
            if (legacy != null)
                packs.Add(legacy);
        }

        return new PortraitCatalog(packs, KnownRoles, directory);
    }

    /// <summary>Rescan the same folder. Returns a new catalog; broken packs are skipped quietly.</summary>
    public PortraitCatalog Reload()
        => Load(string.IsNullOrEmpty(_directory) ? "" : _directory);

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

    internal static string LayerPath(PortraitLayerSpec layer, string fileName)
    {
        string relative = layer.Folder + "/" + fileName;
        return string.IsNullOrEmpty(layer.PathPrefix) ? relative : layer.PathPrefix + "/" + relative;
    }

    PortraitSpeciesPack ResolvePrimary()
        => _packs.TryGetValue("human", out var human) ? human : Packs[0];

    static PortraitSpeciesPack? TryLoadPack(string portraitsRoot, string packFolder)
    {
        try
        {
            string jsonPath = Path.Combine(packFolder, "species.json");
            if (!File.Exists(jsonPath))
                return null;
            var parsed = ParseDocument(File.ReadAllText(jsonPath));
            string id = parsed.Id;
            if (string.IsNullOrWhiteSpace(id))
                id = Path.GetFileName(packFolder);
            if (string.IsNullOrWhiteSpace(id))
                return null;

            string packPrefix = "species/" + Path.GetFileName(packFolder);
            var layers = new List<PortraitLayerSpec>(parsed.Layers.Count);
            foreach (var layer in parsed.Layers)
            {
                string prefix = layer.Shared ? "shared" : packPrefix;
                string? anchor = layer.Anchor ?? PortraitAnchors.DefaultAnchor(layer.Id);
                layers.Add(layer with { PathPrefix = prefix, Anchor = anchor });
            }

            var files = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var anchors = new Dictionary<string, IReadOnlyDictionary<string, Vector2>>(StringComparer.OrdinalIgnoreCase);
            foreach (var layer in layers)
            {
                if (files.ContainsKey(layer.Folder))
                    continue;
                string abs = Path.Combine(portraitsRoot, layer.PathPrefix, layer.Folder);
                if (!Directory.Exists(abs))
                    continue;
                files[layer.Folder] = ListPngs(abs, layer.PathPrefix, layer.Folder);
                if (string.Equals(layer.Folder, "head", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(layer.Id, "head", StringComparison.OrdinalIgnoreCase))
                    LoadSidecars(abs, layer.PathPrefix, layer.Folder, anchors);
            }

            return new PortraitSpeciesPack(
                id,
                parsed.Name,
                layers,
                parsed.Palettes,
                files,
                anchors,
                parsed.Author,
                parsed.Version,
                parsed.License);
        }
        catch
        {
            return null;
        }
    }

    static PortraitSpeciesPack? TryLoadLegacy(string directory)
    {
        try
        {
            string jsonPath = Path.Combine(directory, "portrait-layers.json");
            if (!File.Exists(jsonPath))
                return null;
            var parsed = ParseDocument(File.ReadAllText(jsonPath));
            var files = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var anchors = new Dictionary<string, IReadOnlyDictionary<string, Vector2>>(StringComparer.OrdinalIgnoreCase);
            var layers = new List<PortraitLayerSpec>(parsed.Layers.Count);
            foreach (var layer in parsed.Layers)
            {
                string? anchor = layer.Anchor ?? PortraitAnchors.DefaultAnchor(layer.Id);
                layers.Add(layer with { PathPrefix = "", Anchor = anchor });
                if (!files.ContainsKey(layer.Folder))
                {
                    string abs = Path.Combine(directory, layer.Folder);
                    if (Directory.Exists(abs))
                    {
                        files[layer.Folder] = ListPngs(abs, "", layer.Folder);
                        if (string.Equals(layer.Folder, "head", StringComparison.OrdinalIgnoreCase))
                            LoadSidecars(abs, "", layer.Folder, anchors);
                    }
                }
            }

            // Also scan remaining subfolders so a legacy tree still lists every PNG.
            foreach (var folder in Directory.GetDirectories(directory))
            {
                string name = Path.GetFileName(folder);
                if (name is "species" or "shared")
                    continue;
                if (!files.ContainsKey(name))
                    files[name] = ListPngs(folder, "", name);
            }

            return new PortraitSpeciesPack(
                "human",
                "Human",
                layers,
                parsed.Palettes,
                files,
                anchors);
        }
        catch
        {
            return null;
        }
    }

    static IReadOnlyList<string> ListPngs(string absoluteFolder, string pathPrefix, string folderName)
    {
        if (!Directory.Exists(absoluteFolder))
            return Array.Empty<string>();
        return Directory.GetFiles(absoluteFolder, "*.png")
            .Select(p => Path.GetFileName(p)!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .Select(n => string.IsNullOrEmpty(pathPrefix) ? folderName + "/" + n : pathPrefix + "/" + folderName + "/" + n)
            .ToList();
    }

    static void LoadSidecars(
        string absoluteFolder,
        string pathPrefix,
        string folderName,
        Dictionary<string, IReadOnlyDictionary<string, Vector2>> into)
    {
        if (!Directory.Exists(absoluteFolder))
            return;
        foreach (var path in Directory.GetFiles(absoluteFolder, "*.json"))
        {
            try
            {
                var parsed = PortraitAnchors.ParseSidecar(File.ReadAllText(path));
                string stem = Path.GetFileNameWithoutExtension(path);
                string png = string.IsNullOrEmpty(pathPrefix)
                    ? folderName + "/" + stem + ".png"
                    : pathPrefix + "/" + folderName + "/" + stem + ".png";
                into[png] = parsed;
            }
            catch
            {
                // Skip a broken sidecar quietly.
            }
        }
    }

    sealed class ParsedDocument
    {
        public string Id = "";
        public string? Name;
        public string? Author;
        public string? Version;
        public string? License;
        public IReadOnlyList<PortraitLayerSpec> Layers = Array.Empty<PortraitLayerSpec>();
        public IReadOnlyDictionary<string, IReadOnlyList<Vector4>> Palettes =
            new Dictionary<string, IReadOnlyList<Vector4>>();
        public IReadOnlyList<string> Roles = Array.Empty<string>();
    }

    static ParsedDocument ParseDocument(string json)
    {
        var result = new ParsedDocument();
        var root = JObject.Parse(json);
        result.Id = (root.Value<string>("id") ?? "").Trim();
        result.Name = root.Value<string>("name");
        result.Author = root.Value<string>("author");
        result.Version = root.Value<string>("version");
        result.License = root.Value<string>("license");

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
            bool shared = obj.Value<bool?>("shared") ?? false;
            string? anchor = obj["anchor"] != null ? obj.Value<string>("anchor") : null;
            if (anchor != null)
                anchor = string.IsNullOrWhiteSpace(anchor) ? "" : anchor.Trim();
            layers.Add(new PortraitLayerSpec(id, folder, tint, empty, byRole, shared, anchor));
        }
        result.Layers = layers;

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
        result.Palettes = palettes;

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
        result.Roles = roles;
        return result;
    }
}
