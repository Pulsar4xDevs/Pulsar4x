using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Seeded portrait layer picks and catalog parse. No ImGui and no game.</summary>
[TestFixture]
public class PortraitPickerTests
{
    const string SampleJson = """
        {
          "roles": ["captain", "civilian"],
          "layers": [
            { "id": "background", "folder": "background", "tint": "none", "emptyChance": 0, "byRole": false },
            { "id": "hair", "folder": "hair", "tint": "hair", "emptyChance": 0.5, "byRole": false },
            { "id": "uniform", "folder": "uniform", "tint": "uniform", "emptyChance": 0, "byRole": true },
            { "id": "badge", "folder": "badge", "tint": "none", "emptyChance": 1, "byRole": false }
          ],
          "palettes": {
            "hair": ["#1C1C1C", "#D4A017"],
            "uniform": ["#2C3E50"]
          }
        }
        """;

    static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Files = new Dictionary<string, IReadOnlyList<string>>
    {
        ["background"] = new[] { "background/01.png", "background/02.png" },
        ["hair"] = new[] { "hair/01.png", "hair/02.png" },
        ["uniform"] = new[] { "uniform/captain.png", "uniform/civilian.png" },
        ["badge"] = new[] { "badge/01.png" },
    };

    static PortraitCatalog Catalog() => PortraitCatalog.Parse(SampleJson, Files);

    [Test]
    public void SameSeed_GivesTheSamePicks()
    {
        var catalog = Catalog();
        var faction = new Vector4(0.2f, 0.4f, 0.8f, 1f);
        var a = PortraitPicker.Pick(catalog, 12345, "captain", faction);
        var b = PortraitPicker.Pick(catalog, 12345, "captain", faction);
        Assert.That(a.Count, Is.EqualTo(b.Count));
        for (int i = 0; i < a.Count; i++)
        {
            Assert.That(a[i].File, Is.EqualTo(b[i].File));
            Assert.That(a[i].FallbackFile, Is.EqualTo(b[i].FallbackFile));
            Assert.That(a[i].Tint, Is.EqualTo(b[i].Tint));
        }

        var other = PortraitPicker.Pick(catalog, 12346, "captain", faction);
        bool differed = a.Zip(other, (l, r) => l.File != r.File || l.Tint != r.Tint).Any(x => x);
        Assert.That(differed, Is.True, "a nearby seed should be able to produce a different portrait");
    }

    [Test]
    public void EmptyChance_IsRespected()
    {
        var catalog = Catalog();
        var faction = Vector4.One;

        // emptyChance 1: badge is always empty, and does not consume a file pick.
        for (int seed = 0; seed < 40; seed++)
        {
            var picks = PortraitPicker.Pick(catalog, seed, "civilian", faction);
            Assert.That(picks[3].File, Is.Null, "emptyChance 1 must always skip the layer");
            Assert.That(picks[0].File, Is.Not.Null, "emptyChance 0 must never skip the layer");
            Assert.That(picks[2].File, Is.Not.Null);
        }

        // emptyChance 0.5: some seeds skip hair, some do not, and a given seed is stable.
        int empty = 0, present = 0;
        for (int seed = 0; seed < 80; seed++)
        {
            var picks = PortraitPicker.Pick(catalog, seed, "civilian", faction);
            if (picks[1].File == null) empty++;
            else present++;
            var again = PortraitPicker.Pick(catalog, seed, "civilian", faction);
            Assert.That(again[1].File, Is.EqualTo(picks[1].File));
        }
        Assert.That(empty, Is.GreaterThan(0), "emptyChance 0.5 should skip for some seeds");
        Assert.That(present, Is.GreaterThan(0), "emptyChance 0.5 should keep the layer for some seeds");
    }

    [Test]
    public void UnknownRoles_FallBackToCivilian()
    {
        Assert.That(PortraitPicker.NormalizeRole(null), Is.EqualTo("civilian"));
        Assert.That(PortraitPicker.NormalizeRole(""), Is.EqualTo("civilian"));
        Assert.That(PortraitPicker.NormalizeRole("wizard"), Is.EqualTo("civilian"));
        Assert.That(PortraitPicker.NormalizeRole("Captain"), Is.EqualTo("captain"));
        Assert.That(PortraitPicker.NormalizeRole(" industry-head "), Is.EqualTo("industry-head"));

        var catalog = Catalog();
        var captain = PortraitPicker.Pick(catalog, 7, "captain", Vector4.One);
        var unknown = PortraitPicker.Pick(catalog, 7, "wizard", Vector4.One);
        var civilian = PortraitPicker.Pick(catalog, 7, "civilian", Vector4.One);
        Assert.That(unknown[2].File, Is.EqualTo("uniform/civilian.png"));
        Assert.That(civilian[2].File, Is.EqualTo("uniform/civilian.png"));
        Assert.That(captain[2].File, Is.EqualTo("uniform/captain.png"));
        Assert.That(captain[2].FallbackFile, Is.EqualTo("uniform/civilian.png"));
    }

    [Test]
    public void Parse_ReadsLayersPalettesAndEmptyChance()
    {
        var catalog = Catalog();
        Assert.That(catalog.Layers.Select(l => l.Id), Is.EqualTo(new[] { "background", "hair", "uniform", "badge" }));
        Assert.That(catalog.Layers[1].Tint, Is.EqualTo("hair"));
        Assert.That(catalog.Layers[1].EmptyChance, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(catalog.Layers[2].ByRole, Is.True);
        Assert.That(catalog.Palettes["hair"].Count, Is.EqualTo(2));
        Assert.That(catalog.Palettes["hair"][0], Is.EqualTo(new Vector4(0x1C / 255f, 0x1C / 255f, 0x1C / 255f, 1f)));
        Assert.That(catalog.Roles, Is.EqualTo(new[] { "captain", "civilian" }));

        Assert.That(PortraitCatalog.TryParseColour("#224466", out var c), Is.True);
        Assert.That(c, Is.EqualTo(new Vector4(0x22 / 255f, 0x44 / 255f, 0x66 / 255f, 1f)));
    }

    [Test]
    public void Parse_ShippedPortraitLayersJson()
    {
        string? path = FindShippedJson();
        Assert.That(path, Is.Not.Null, "portrait-layers.json should be findable from the test run or the client project");
        var catalog = PortraitCatalog.Parse(File.ReadAllText(path!));
        Assert.That(catalog.Layers.Select(l => l.Id), Is.EqualTo(new[]
        {
            "background", "back-hair", "neck", "uniform", "head", "eyes",
            "nose-mouth", "hair", "extras", "insignia", "badge",
        }));
        Assert.That(catalog.Layers.Single(l => l.Id == "uniform").ByRole, Is.True);
        Assert.That(catalog.Palettes.ContainsKey("skin"), Is.True);
        Assert.That(catalog.Palettes.ContainsKey("hair"), Is.True);
        Assert.That(catalog.Palettes.ContainsKey("eye"), Is.True);
        Assert.That(catalog.Palettes.ContainsKey("uniform"), Is.True);
        Assert.That(catalog.Roles, Does.Contain("industry-head"));
        Assert.That(catalog.Roles, Does.Contain("candidate"));
    }

    static string? FindShippedJson()
    {
        var starts = new[]
        {
            TestContext.CurrentContext.TestDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..")),
        };
        foreach (var start in starts)
        {
            var candidates = new[]
            {
                Path.Combine(start, "Resources", "Portraits", "portrait-layers.json"),
                Path.Combine(start, "Pulsar4X.Client", "Resources", "Portraits", "portrait-layers.json"),
            };
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }
        }
        return null;
    }
}
