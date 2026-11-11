using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// A start list that names an id which is not loaded fails with a message naming the mod, file,
/// object, field, and the missing id. Every miss in the same pass is listed, not just the first.
/// </summary>
[TestFixture]
public class ModLoadErrorTests
{
    private const string BaseMod = "Pulsar4x";
    private const string FixtureMod = "Missing Design Mod";
    private const string FixtureNamespace = "Pulsar4x.MissingDesign";
    private const string FixtureFile = "data.json";

    // Nothing loads these in the broken fixture, so nothing can find them. The sound fixture
    // declares the same ids itself, which is what has to make the same start go through.
    private const string DesignA = "missing-design-does-not-exist";
    private const string DesignB = "missing-design-two-does-not-exist";
    private const string ShipDesign = "missing-ship-design-does-not-exist";
    private const string FactionDesign = "missing-faction-design-does-not-exist";

    private static ModsState.ModMetaData Mod(string name)
        => ModsState.AvailableMods.Single(m => m.Mod.ModName == name);

    private static List<string> TickedPaths(string fixtureManifestPath)
        => new () { fixtureManifestPath, Mod(BaseMod).Path };

    private static NewGameRequest StartRequest(List<string> paths)
        => new NewGameRequest(
            ModManifestPaths: paths,
            FactionName: "United Earth Corp",
            FactionAbbreviation: "UEC",
            SpeciesId: "species-human",
            ColonyId: "colony-missing-designs",
            SystemId: "system-sol",
            BodyId: "planet-earth",
            EnabledSystems: new List<string> { "system-sol" },
            MaxSystems: 1,
            MasterSeed: 12345678,
            StartingFunds: 100_000_000,
            EleStart: false,
            SMPassword: "",
            PlayerPassword: "");

    /// <summary>
    /// What the wizard's "Next" page then "Create Game!" do. The game start checks every blueprint
    /// in the store before it builds anything, so a bad reference throws before a Game exists, and
    /// before anything has changed in the store.
    /// </summary>
    private static (ModLoadException? error, Game? game) StartLikeTheMenu(List<string> paths)
    {
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path));

        // A start list on its own only opens what the player colony's StartingItems unlock, so a ship
        // design over those lists can report the armor as missing before the library is opened. When the
        // store can make the faction's designs at all (a dev tool, a mod that unlocks its library), build
        // the faction first so a bad start reference is what this test sees instead.
        foreach (var factionBlueprint in store.Factions.Values)
            TryOpenFactionLibrary(store, factionBlueprint);

        try
        {
            var result = GameLifecycle.CreateGameCore(store, StartRequest(paths));
            return (null, result?.game);
        }
        catch (ModLoadException ex)
        {
            return (ex, null);
        }
    }

    private static string TextFor(IEnumerable<ModLoadError> errors, string missingId)
        => string.Join("\n", errors.Where(e => e.MissingId == missingId).Select(e => e.ToString()));

    /// <summary>
    /// A blueprint faction only opens its design library when it is placed, which needs a game. When the
    /// store can build the faction's designs at all, do that here first, so a start reference that a start
    /// list cannot fix by unlocking is what this test meets. A store that cannot build them is left alone.
    /// Templates, armor, cargo goods, and techs are unlocked so those designs can resolve their formulas.
    /// </summary>
    private static void TryOpenFactionLibrary(ModDataStore store, FactionBlueprint blueprint)
    {
        Game game;
        try
        {
            game = GameFactory.CreateGame(store, new NewGameSettings());
        }
        catch (ModLoadException)
        {
            return;
        }

        var faction = FactionFactory.CreateBasicFaction(game, blueprint.Name + " library", "LIB", 0);
        var info = faction.GetDataBlob<FactionInfoDB>();
        foreach (var id in info.Data.LockedComponentTemplates.Keys.ToList())
            info.Data.Unlock(id);
        foreach (var id in info.Data.LockedArmor.Keys.ToList())
            info.Data.Unlock(id);
        foreach (var id in info.Data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
            info.Data.Unlock(id);
        foreach (var id in info.Data.LockedTechs.Keys.ToList())
            info.Data.Unlock(id);

        try
        {
            ComponentDesigner.StartResearched = true;
            foreach (var id in blueprint.ComponentDesigns ?? new List<string>())
                ComponentDesignFromJson.Create(faction, info.Data, store.ComponentDesigns[id]);
            ComponentDesigner.StartResearched = false;

            foreach (var id in blueprint.ShipDesigns ?? new List<string>())
                ShipDesignFromJson.Create(faction, info.Data, store.ShipDesigns[id]);
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    /// <summary>
    /// A one-off mod in a temp directory: a colony that is the player's start and a faction that is
    /// placed, both naming ids that are not loaded. The colony names two missing component designs so
    /// one pass has to report both.
    /// </summary>
    /// <param name="withTheIds">True to have the mod declare those ids itself, so the same start is sound.</param>
    private static string WriteFixtureMod(bool withTheIds = false)
    {
        var dir = Path.Combine(Path.GetTempPath(), "pulsar4x-mod-load-errors-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        File.WriteAllText(Path.Combine(dir, "modInfo.json"), $$"""
        {
          "Author": "Pulsar4X Tests",
          "ModName": "{{FixtureMod}}",
          "Version": "0.1",
          "Namespace": "{{FixtureNamespace}}",
          "DefaultEnabled": false,
          "Dependencies": [ "{{BaseMod}}" ],
          "DataFiles": [ "{{FixtureFile}}" ]
        }
        """);

        var entries = new List<string>();

        if (withTheIds)
        {
            // A start list only opens what the player colony unlocks, so a sound fixture brings its
            // armor as a blueprint and the colony's StartingItems unlock it for the ship design.
            entries.Add("""
                {
                  "Type": "Armor",
                  "Payload": { "UniqueID": "fixture-plastic-armor", "ResourceID": "plastic", "Density": "1175" }
                }
                """);
            // Basemod's Mars outpost is placed for the player too, and its designs need what the Earth
            // start unlocks. This start is not Earth, so take the outpost out, as earlytechstart does.
            entries.Add("""
                {
                  "Type": "Colony",
                  "Operation": "Remove",
                  "Payload": { "UniqueID": "colony-mars-outpost" }
                }
                """);
            entries.Add(ComponentDesign(DesignA));
            entries.Add(ComponentDesign(DesignB));
            entries.Add(ComponentDesign(FactionDesign));
            entries.Add($$"""
                {
                  "Type": "ShipDesign",
                  "Payload": {
                    "UniqueID": "{{ShipDesign}}",
                    "Name": "Loaded",
                    "Armor": { "Id": "fixture-plastic-armor", "Thickness": 1 },
                    "Components": [ { "Id": "{{DesignA}}", "Amount": 1 } ]
                  }
                }
                """);
        }

        // The start unlocks only what the colony lists, so it names the basemod template its designs
        // use, that template's resources, and the fixture armor.
        entries.Add($$"""
            {
              "Type": "Colony",
              "Payload": {
                "UniqueID": "colony-missing-designs",
                "Name": "Missing Designs Colony",
                "Body": "Earth",
                "StartingPopulation": 1000,
                "StartingItems": [ "cargo-Shuttlebay", "stainless-steel", "aluminium", "titanium", "plastic", "fixture-plastic-armor" ],
                "ComponentDesigns": [ "{{DesignA}}", "{{DesignB}}" ],
                "ShipDesigns": [ "{{ShipDesign}}" ]
              }
            }
            """);

        // The faction is the second start pass, so its miss has to come after the colony's.
        entries.Add($$"""
            {
              "Type": "Faction",
              "Payload": {
                "UniqueID": "faction-missing-designs",
                "Name": "Missing Designs Faction",
                "Abbreviation": "MDF",
                "Species": "species-human",
                "System": "system-sol",
                "Body": "Earth",
                "StartingFunds": 100,
                "ComponentDesigns": [ "{{FactionDesign}}" ]
              }
            }
            """);

        File.WriteAllText(Path.Combine(dir, FixtureFile), "[\n" + string.Join(",\n", entries) + "\n]\n");

        return dir;
    }

    private static string ComponentDesign(string id) => $$"""
        {
          "Type": "ComponentDesign",
          "Payload": { "UniqueId": "{{id}}", "Name": "Loaded", "TemplateId": "cargo-Shuttlebay" }
        }
        """;

    [Test]
    public void MissingStartReferencesNameModFileObjectFieldAndMissingId()
    {
        ModsState.RefreshModsList("Data");
        var fixtureDir = WriteFixtureMod();
        var paths = TickedPaths(Path.Combine(fixtureDir, "modInfo.json"));

        try
        {
            var (error, game) = StartLikeTheMenu(paths);

            Assert.That(game, Is.Null, "a start with missing references must not produce a game");
            Assert.That(error, Is.Not.Null, "the missing references must surface as a ModLoadException");
            var errors = error!.Errors;
            Assert.That(errors, Is.Not.Empty);

            // The player start colony: both of its missing designs, not just the first.
            Assert.That(TextFor(errors, DesignA),
                Does.Contain($"Mod '{FixtureNamespace}'")
                    .And.Contain($"file '{FixtureFile}'")
                    .And.Contain("ColonyBlueprint 'colony-missing-designs'")
                    .And.Contain("field 'ComponentDesigns'")
                    .And.Contain(DesignA));
            Assert.That(TextFor(errors, DesignB), Does.Contain("field 'ComponentDesigns'"));
            Assert.That(TextFor(errors, ShipDesign),
                Does.Contain("field 'ShipDesigns'")
                    .And.Contain("ColonyBlueprint 'colony-missing-designs'"));

            // The placed faction's list, in the same error rather than behind the colony's.
            Assert.That(TextFor(errors, FactionDesign),
                Does.Contain("field 'ComponentDesigns'")
                    .And.Contain("FactionBlueprint 'faction-missing-designs'"));

            // What the new-game banner would show: one string naming every part of each miss.
            var banner = error.DisplayMessage;
            Assert.That(banner, Does.Contain($"Mod '{FixtureNamespace}'"));
            Assert.That(banner, Does.Contain($"file '{FixtureFile}'"));
            Assert.That(banner, Does.Contain("ColonyBlueprint 'colony-missing-designs'")
                .And.Contain("FactionBlueprint 'faction-missing-designs'"));
            Assert.That(banner, Does.Contain(DesignA).And.Contain(DesignB));
            Assert.That(banner, Does.Contain(ShipDesign).And.Contain(FactionDesign));
        }
        finally
        {
            Directory.Delete(fixtureDir, true);
        }
    }

    [Test]
    public void TheSameStartWithTheIdsLoadedCreatesTheGame()
    {
        ModsState.RefreshModsList("Data");
        var fixtureDir = WriteFixtureMod(withTheIds: true);
        var paths = TickedPaths(Path.Combine(fixtureDir, "modInfo.json"));

        try
        {
            var (error, game) = StartLikeTheMenu(paths);

            Assert.That(error, Is.Null, error?.DisplayMessage);
            Assert.That(game, Is.Not.Null, "a start whose references all resolve has to create the game");
        }
        finally
        {
            Directory.Delete(fixtureDir, true);
        }
    }
}
