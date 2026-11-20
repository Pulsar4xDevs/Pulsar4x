using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Species packs, shared layers, anchors, overrides, and cache retry. No ImGui and no game.</summary>
[TestFixture]
public class PortraitPackTests
{
    // 1x1 transparent PNG.
    static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    string _root = "";

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "portrait-packs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (!string.IsNullOrEmpty(_root) && Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Test]
    public void Load_ReadsSpeciesPacksAndFiles()
    {
        WriteHumanPack();
        var catalog = PortraitCatalog.Load(_root);
        Assert.That(catalog.Packs.Select(p => p.Id), Is.EquivalentTo(new[] { "human" }));
        var human = catalog.ResolveSpecies("human");
        Assert.That(human.Name, Is.EqualTo("Human"));
        Assert.That(human.Author, Is.EqualTo("Ada"));
        Assert.That(human.Layers.Select(l => l.Id), Is.EqualTo(new[] { "background", "head", "eyes", "uniform" }));
        Assert.That(human.Layers.Single(l => l.Id == "uniform").ByRole, Is.True);
        Assert.That(human.Palettes["skin"].Count, Is.EqualTo(2));
        Assert.That(human.FilesByFolder["head"], Does.Contain("species/human/head/01.png"));
        Assert.That(human.FilesByFolder["head"], Does.Contain("species/human/head/02.png"));
        Assert.That(human.FilesByFolder["uniform"], Does.Contain("species/human/uniform/captain.png"));
        Assert.That(catalog.Roles, Does.Contain("industry-head"));
        Assert.That(catalog.Roles, Does.Contain("candidate"));
    }

    [Test]
    public void Load_SkipsABrokenPack_AndUnknownSpeciesFallsBackToHuman()
    {
        WriteHumanPack();
        string broken = Path.Combine(_root, "species", "broken");
        Directory.CreateDirectory(broken);
        File.WriteAllText(Path.Combine(broken, "species.json"), "{ this is not json");
        // A pack with no id and no recoverable folder would also be skipped; an empty file is broken.
        Directory.CreateDirectory(Path.Combine(_root, "species", "empty"));
        File.WriteAllText(Path.Combine(_root, "species", "empty", "species.json"), "[]");

        var catalog = PortraitCatalog.Load(_root);
        Assert.That(catalog.Packs.Select(p => p.Id), Is.EquivalentTo(new[] { "human" }));
        Assert.That(catalog.ResolveSpecies("broken").Id, Is.EqualTo("human"));
        Assert.That(catalog.ResolveSpecies("wizard").Id, Is.EqualTo("human"));
        Assert.That(catalog.ResolveSpecies(null).Id, Is.EqualTo("human"));
        Assert.That(() => PortraitPicker.Pick(catalog, 7, "captain", Vector4.One, "wizard"), Throws.Nothing);
    }

    [Test]
    public void Load_SharedLayers_ComeFromTheSharedFolder()
    {
        WriteHumanPack(includeSharedBackground: true);
        var catalog = PortraitCatalog.Load(_root);
        var human = catalog.ResolveSpecies("human");
        var background = human.Layers.Single(l => l.Id == "background");
        Assert.That(background.Shared, Is.True);
        Assert.That(background.PathPrefix, Is.EqualTo("shared"));
        Assert.That(human.FilesByFolder["background"], Is.EqualTo(new[] { "shared/background/01.png" }));
        Assert.That(File.Exists(Path.Combine(_root, "shared", "background", "01.png")), Is.True);

        var picks = PortraitPicker.Pick(catalog, 3, "civilian", Vector4.One, "human");
        Assert.That(picks[0].File, Is.EqualTo("shared/background/01.png"));
    }

    [Test]
    public void Anchors_ShiftByThePickedHeadsOffset_ScaledToDrawSize()
    {
        WriteHumanPack();
        WriteFile(Path.Combine(_root, "species", "human", "head", "01.json"),
            """{"anchors":{"eyes":[10,20],"nose":[1,2],"mouth":[3,4],"hairline":[5,6]},"weight":2,"roles":["captain"]}""");
        WriteFile(Path.Combine(_root, "species", "human", "head", "02.json"),
            """{"anchors":{"eyes":[40,8]}}""");

        var catalog = PortraitCatalog.Load(_root);
        var human = catalog.ResolveSpecies("human");
        Assert.That(human.Layers.Single(l => l.Id == "eyes").Anchor, Is.EqualTo("eyes"),
            "eyes default to the eyes anchor when species.json omits anchor");

        // Pin head to 01.png so the offset is known.
        var overrides = new PortraitPickOverrides();
        overrides.Layers["head"] = 0;
        var picks = PortraitPicker.Pick(catalog, 11, "civilian", Vector4.One, "human", overrides);
        var eyes = picks.Where((_, i) => human.Layers[i].Id == "eyes").Single();
        var head = picks.Where((_, i) => human.Layers[i].Id == "head").Single();
        Assert.That(head.File, Is.EqualTo("species/human/head/01.png"));
        Assert.That(eyes.Offset, Is.EqualTo(new Vector2(10, 20)));
        Assert.That(PortraitAnchors.ScaleOffset(eyes.Offset, 128f), Is.EqualTo(new Vector2(5, 10)));
        Assert.That(PortraitAnchors.ScaleOffset(eyes.Offset, 256f), Is.EqualTo(new Vector2(10, 20)));
        Assert.That(PortraitAnchors.ScaleOffset(eyes.Offset, 48f), Is.EqualTo(new Vector2(10, 20) * (48f / 256f)));

        overrides.Layers["head"] = 1;
        var other = PortraitPicker.Pick(catalog, 11, "civilian", Vector4.One, "human", overrides);
        var otherEyes = other.Where((_, i) => human.Layers[i].Id == "eyes").Single();
        Assert.That(otherEyes.Offset, Is.EqualTo(new Vector2(40, 8)));
    }

    [Test]
    public void Overrides_PinALayerOrPalette_AndLeaveTheRestToTheSeed()
    {
        WriteHumanPack();
        var catalog = PortraitCatalog.Load(_root);
        const int seed = 21;
        var baseline = PortraitPicker.Pick(catalog, seed, "captain", Vector4.One, "human");
        Assert.That(baseline[1].File, Is.Not.Null, "head");

        var overrides = new PortraitPickOverrides();
        overrides.Layers["head"] = null; // None
        var emptied = PortraitPicker.Pick(catalog, seed, "captain", Vector4.One, "human", overrides);
        Assert.That(emptied[1].File, Is.Null);
        Assert.That(emptied[0].File, Is.EqualTo(baseline[0].File), "unpinned layers stay on the seed pick");
        Assert.That(emptied[2].File, Is.EqualTo(baseline[2].File));
        Assert.That(emptied[3].File, Is.EqualTo(baseline[3].File));
        Assert.That(emptied[3].File, Is.EqualTo("species/human/uniform/captain.png"));

        overrides.Clear();
        var headFiles = catalog.ResolveSpecies("human").FilesByFolder["head"];
        int otherIndex = headFiles[0] == baseline[1].File ? 1 : 0;
        overrides.Layers["head"] = otherIndex;
        var swapped = PortraitPicker.Pick(catalog, seed, "captain", Vector4.One, "human", overrides);
        Assert.That(swapped[1].File, Is.EqualTo(headFiles[otherIndex]));
        Assert.That(swapped[1].File, Is.Not.EqualTo(baseline[1].File));
        Assert.That(swapped[0].File, Is.EqualTo(baseline[0].File));

        overrides.Clear();
        overrides.Palettes["skin"] = 0;
        var pinnedTint = PortraitPicker.Pick(catalog, seed, "captain", Vector4.One, "human", overrides);
        var skin0 = catalog.ResolveSpecies("human").Palettes["skin"][0];
        Assert.That(pinnedTint[1].Tint, Is.EqualTo(skin0));
        Assert.That(pinnedTint[1].File, Is.EqualTo(baseline[1].File), "a palette pin does not change the file pick");
    }

    [Test]
    public void AssetCache_DoesNotRememberMisses_AndClearsForARetry()
    {
        var cache = new PortraitAssetCache<string>();
        Assert.That(cache.TryGet("missing.png", out _), Is.False);
        Assert.That(cache.Contains("missing.png"), Is.False);
        Assert.That(cache.Count, Is.EqualTo(0), "a miss must not be stored");

        cache.Set("head/01.png", "tex-1");
        Assert.That(cache.TryGet("head/01.png", out var hit), Is.True);
        Assert.That(hit, Is.EqualTo("tex-1"));
        Assert.That(cache.Count, Is.EqualTo(1));

        cache.Clear();
        Assert.That(cache.Count, Is.EqualTo(0));
        Assert.That(cache.TryGet("head/01.png", out _), Is.False, "after Clear, a later Reload can retry the file");

        cache.Set("head/01.png", "tex-2");
        Assert.That(cache.TryGet("head/01.png", out var retried), Is.True);
        Assert.That(retried, Is.EqualTo("tex-2"));
    }

    [Test]
    public void Reload_RescansPacksAfterAChange()
    {
        WriteHumanPack();
        var catalog = PortraitCatalog.Load(_root);
        Assert.That(catalog.ResolveSpecies("human").FilesByFolder["head"].Count, Is.EqualTo(2));

        WritePng(Path.Combine(_root, "species", "human", "head", "03.png"));
        var reloaded = catalog.Reload();
        Assert.That(reloaded.ResolveSpecies("human").FilesByFolder["head"], Does.Contain("species/human/head/03.png"));
        Assert.That(reloaded.ResolveSpecies("human").FilesByFolder["head"].Count, Is.EqualTo(3));
    }

    [Test]
    public void Load_IgnoresLeftoverLegacyJson_WhenThereAreNoSpeciesPacks()
    {
        WriteFile(Path.Combine(_root, "portrait-layers.json"), """
            {
              "roles": ["captain", "civilian"],
              "layers": [
                { "id": "head", "folder": "head", "tint": "skin", "emptyChance": 0, "byRole": false }
              ],
              "palettes": { "skin": ["#FFFFFF"] }
            }
            """);
        WritePng(Path.Combine(_root, "head", "01.png"));

        var catalog = PortraitCatalog.Load(_root);
        Assert.That(catalog.ResolveSpecies("human").Layers, Is.Empty,
            "portrait-layers.json is no longer a load path");
        Assert.That(catalog.FilesByFolder.ContainsKey("head"), Is.False);
    }

    [Test]
    public void Load_ShippedHumanAndKzariPacks()
    {
        string? dir = PortraitPickerTests.FindShippedPortraitsDir();
        Assert.That(dir, Is.Not.Null, "the shipped Portraits folder should be findable");
        var catalog = PortraitCatalog.Load(dir!);
        Assert.That(catalog.Packs.Select(p => p.Id), Is.EquivalentTo(new[] { "human", "kzari" }));

        var human = catalog.ResolveSpecies("human");
        Assert.That(human.Name, Is.EqualTo("Human"));
        Assert.That(human.Layers.Select(l => l.Id), Does.Contain("nose"));
        Assert.That(human.Layers.Select(l => l.Id), Does.Contain("mouth"));
        Assert.That(human.Layers.Select(l => l.Id), Does.Contain("hair"));
        Assert.That(human.PaletteOrder, Is.EqualTo(new[] { "skin", "hair", "eye", "uniform" }));
        Assert.That(human.Layers.Single(l => l.Id == "background").Shared, Is.True);
        Assert.That(human.FilesByFolder["background"][0], Does.StartWith("shared/"));
        Assert.That(human.HeadAnchors.ContainsKey("species/human/head/07.png"), Is.True);
        Assert.That(human.HeadAnchors["species/human/head/07.png"]["eyes"], Is.EqualTo(new Vector2(0, -8)));

        var kzari = catalog.ResolveSpecies("kzari");
        Assert.That(kzari.Name, Is.EqualTo("Kzari"));
        Assert.That(kzari.Layers.Select(l => l.Id), Does.Not.Contain("nose"));
        Assert.That(kzari.Layers.Select(l => l.Id), Does.Not.Contain("mouth"));
        Assert.That(kzari.Layers.Select(l => l.Id), Does.Not.Contain("hair"));
        Assert.That(kzari.Layers.Select(l => l.Id), Does.Contain("mandibles"));
        Assert.That(kzari.Layers.Select(l => l.Id), Does.Contain("crest"));
        Assert.That(kzari.PaletteOrder, Is.EqualTo(new[] { "chitin", "crest", "eye", "uniform" }));
        Assert.That(kzari.Palettes.ContainsKey("chitin"), Is.True);
        Assert.That(kzari.Layers.Single(l => l.Id == "mandibles").Anchor, Is.EqualTo("mouth"));
        Assert.That(kzari.Layers.Single(l => l.Id == "crest").Anchor, Is.EqualTo("hairline"));
        Assert.That(kzari.Layers.Single(l => l.Id == "crest-back").Anchor, Is.EqualTo("hairline"));

        Assert.That(() => PortraitPicker.Pick(catalog, 7, "captain", Vector4.One, "human"), Throws.Nothing);
        Assert.That(() => PortraitPicker.Pick(catalog, 7, "captain", Vector4.One, "kzari"), Throws.Nothing);
        var kzariPick = PortraitPicker.Pick(catalog, 7, "captain", Vector4.One, "kzari");
        Assert.That(kzariPick.Count, Is.EqualTo(kzari.Layers.Count));
        Assert.That(catalog.ResolveSpecies("unknown-pack").Id, Is.EqualTo("human"));
    }

    void WriteHumanPack(bool includeSharedBackground = false)
    {
        string pack = Path.Combine(_root, "species", "human");
        Directory.CreateDirectory(Path.Combine(pack, "head"));
        Directory.CreateDirectory(Path.Combine(pack, "eyes"));
        Directory.CreateDirectory(Path.Combine(pack, "uniform"));
        WritePng(Path.Combine(pack, "head", "01.png"));
        WritePng(Path.Combine(pack, "head", "02.png"));
        WritePng(Path.Combine(pack, "eyes", "01.png"));
        WritePng(Path.Combine(pack, "uniform", "captain.png"));
        WritePng(Path.Combine(pack, "uniform", "civilian.png"));

        string backgroundLayer = includeSharedBackground
            ? """{ "id": "background", "folder": "background", "tint": "none", "emptyChance": 0, "byRole": false, "shared": true }"""
            : """{ "id": "background", "folder": "background", "tint": "none", "emptyChance": 0, "byRole": false }""";

        if (includeSharedBackground)
        {
            Directory.CreateDirectory(Path.Combine(_root, "shared", "background"));
            WritePng(Path.Combine(_root, "shared", "background", "01.png"));
        }
        else
        {
            Directory.CreateDirectory(Path.Combine(pack, "background"));
            WritePng(Path.Combine(pack, "background", "01.png"));
        }

        WriteFile(Path.Combine(pack, "species.json"), $$"""
            {
              "id": "human",
              "name": "Human",
              "author": "Ada",
              "version": "1",
              "license": "test",
              "layers": [
                {{backgroundLayer}},
                { "id": "head", "folder": "head", "tint": "skin", "emptyChance": 0, "byRole": false },
                { "id": "eyes", "folder": "eyes", "tint": "none", "emptyChance": 0, "byRole": false },
                { "id": "uniform", "folder": "uniform", "tint": "uniform", "emptyChance": 0, "byRole": true }
              ],
              "palettes": {
                "skin": ["#F6D3B8", "#4A2A18"],
                "uniform": ["#FFFFFF"]
              },
              "uniformByRole": { "captain": ["#1B2A4E"] }
            }
            """);
    }

    static void WritePng(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, TinyPng);
    }

    static void WriteFile(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }
}
