using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests;

/// <summary>
/// The effective DefaultStart: scenario fields merge along the RequiredScenarios chain,
/// the catalog and the new-game request carry that start, and the wizard no longer asks
/// for the values the scenario already set.
/// </summary>
[TestFixture]
public class DefaultStartTests
{
    private const string BaseMod = "Pulsar4x";
    private const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    private const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";

    private static ModsState.ModMetaData Mod(string name)
        => ModsState.AvailableMods.Single(m => m.Mod.ModName == name);

    private static List<string> TickedPaths(params string[] names)
        => names.Select(n => Mod(n).Path).ToList();

    private static (ModDataStore store, NewGameCatalog catalog, List<string> paths) Load(
        string scenarioId, params string[] modNames)
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();
        var paths = TickedPaths(modNames);
        var store = GameLifecycle.LoadModStore(paths, available, scenarioId);
        return (store, GameLifecycle.BuildCatalog(store), paths);
    }

    private static NewGameRequest RequestFrom(NewGameCatalog catalog, IReadOnlyList<string> paths, int seed = 1)
    {
        var start = catalog.DefaultStart;
        return NewGameRequests.FromDefaultStart(
            catalog,
            paths,
            start?.FactionName ?? "",
            start?.FactionAbbreviation ?? "",
            seed);
    }

    private static ScenarioManifest.ScenarioStart SolStart()
        => new ScenarioManifest.ScenarioStart
        {
            Species = "species-human",
            Colony = "colony-earth",
            System = "system-sol",
            Body = "planet-earth",
            Theme = "default-theme",
            StartingFunds = 100000000,
            MaxSystems = 10,
            EleStart = true,
            FactionName = "United Earth Corp",
            FactionAbbreviation = "UEC",
        };

    private static ScenarioManifest.ScenarioStart EarlyStart()
        => new ScenarioManifest.ScenarioStart
        {
            Species = "species-human",
            Colony = "colony-early-earth",
            System = "system-sol",
            Body = "planet-earth",
        };

    [Test]
    public void ScenarioManifest_RoundTrip_KeepsNewFields_AndOmitsOnesLeftOut()
    {
        var manifest = new ScenarioManifest
        {
            Namespace = "round-trip",
            ScenarioName = "Round Trip",
            DefaultStart = SolStart(),
        };

        var json = JsonConvert.SerializeObject(manifest, ScenarioManifest.SerializerSettings);
        var round = JsonConvert.DeserializeObject<ScenarioManifest>(json, ScenarioManifest.SerializerSettings);
        Assert.That(round, Is.Not.Null);
        Assert.That(round!.DefaultStart.Species, Is.EqualTo("species-human"));
        Assert.That(round.DefaultStart.Colony, Is.EqualTo("colony-earth"));
        Assert.That(round.DefaultStart.System, Is.EqualTo("system-sol"));
        Assert.That(round.DefaultStart.Body, Is.EqualTo("planet-earth"));
        Assert.That(round.DefaultStart.Theme, Is.EqualTo("default-theme"));
        Assert.That(round.DefaultStart.StartingFunds, Is.EqualTo(100000000));
        Assert.That(round.DefaultStart.MaxSystems, Is.EqualTo(10));
        Assert.That(round.DefaultStart.EleStart, Is.True);
        Assert.That(round.DefaultStart.FactionName, Is.EqualTo("United Earth Corp"));
        Assert.That(round.DefaultStart.FactionAbbreviation, Is.EqualTo("UEC"));

        const string earlyLike = """
            {
              "Namespace": "early-like",
              "ScenarioName": "Early Like",
              "DefaultStart": {
                "Species": "species-human",
                "Colony": "colony-early-earth",
                "System": "system-sol",
                "Body": "planet-earth"
              }
            }
            """;
        var omitted = JsonConvert.DeserializeObject<ScenarioManifest>(earlyLike, ScenarioManifest.SerializerSettings);
        Assert.That(omitted, Is.Not.Null);
        Assert.That(omitted!.DefaultStart.Species, Is.EqualTo("species-human"));
        Assert.That(omitted.DefaultStart.Colony, Is.EqualTo("colony-early-earth"));
        Assert.That(omitted.DefaultStart.Theme, Is.Null);
        Assert.That(omitted.DefaultStart.StartingFunds, Is.Null);
        Assert.That(omitted.DefaultStart.MaxSystems, Is.Null);
        Assert.That(omitted.DefaultStart.EleStart, Is.Null);
        Assert.That(omitted.DefaultStart.FactionName, Is.Null);
        Assert.That(omitted.DefaultStart.FactionAbbreviation, Is.Null);

        var again = JsonConvert.SerializeObject(omitted, ScenarioManifest.SerializerSettings);
        Assert.That(again, Does.Not.Contain("\"Theme\""));
        Assert.That(again, Does.Not.Contain("\"StartingFunds\""));
        Assert.That(again, Does.Not.Contain("\"MaxSystems\""));
        Assert.That(again, Does.Not.Contain("\"EleStart\""));
        Assert.That(again, Does.Not.Contain("\"FactionName\""));
        Assert.That(again, Does.Not.Contain("\"FactionAbbreviation\""));
        Assert.That(again, Does.Contain("colony-early-earth"));
    }

    [Test]
    public void Merge_LaterScenarioOverwritesOnlyTheFieldsItSets()
    {
        var merged = ScenarioManifest.ScenarioStart.Merge(new ScenarioManifest.ScenarioStart?[]
        {
            SolStart(),
            EarlyStart(),
        });

        Assert.That(merged, Is.Not.Null);
        Assert.That(merged!.Species, Is.EqualTo("species-human"));
        Assert.That(merged.Colony, Is.EqualTo("colony-early-earth"));
        Assert.That(merged.System, Is.EqualTo("system-sol"));
        Assert.That(merged.Body, Is.EqualTo("planet-earth"));
        Assert.That(merged.Theme, Is.EqualTo("default-theme"));
        Assert.That(merged.StartingFunds, Is.EqualTo(100000000));
        Assert.That(merged.MaxSystems, Is.EqualTo(10));
        Assert.That(merged.EleStart, Is.True);
        Assert.That(merged.FactionName, Is.EqualTo("United Earth Corp"));
        Assert.That(merged.FactionAbbreviation, Is.EqualTo("UEC"));

        var overridden = ScenarioManifest.ScenarioStart.Merge(new ScenarioManifest.ScenarioStart?[]
        {
            SolStart(),
            EarlyStart(),
            new ScenarioManifest.ScenarioStart
            {
                EleStart = false,
                StartingFunds = 5,
                Colony = "",
            },
        });
        Assert.That(overridden!.EleStart, Is.False);
        Assert.That(overridden.StartingFunds, Is.EqualTo(5));
        Assert.That(overridden.Colony, Is.EqualTo("colony-early-earth"));
        Assert.That(overridden.Theme, Is.EqualTo("default-theme"));
        Assert.That(overridden.MaxSystems, Is.EqualTo(10));
        Assert.That(overridden.FactionAbbreviation, Is.EqualTo("UEC"));

        Assert.That(
            ScenarioManifest.ScenarioStart.Merge(new ScenarioManifest.ScenarioStart?[] { null, null }),
            Is.Null);
    }

    [Test]
    public void Catalog_WarpTechStartAndEarlyTech_CarryTheMergedStartAndIncludedSystems()
    {
        var (solStore, solCatalog, _) = Load(WarpTechStart, BaseMod);
        AssertEffective(solCatalog, solStore, "colony-earth");

        var (earlyStore, earlyCatalog, _) = Load(EarlyTech, BaseMod);
        AssertEffective(earlyCatalog, earlyStore, "colony-early-earth");
    }

    private static void AssertEffective(NewGameCatalog catalog, ModDataStore store, string colonyId)
    {
        var start = catalog.DefaultStart;
        Assert.That(start, Is.Not.Null);
        Assert.That(start!.SpeciesId, Is.EqualTo("species-human"));
        Assert.That(start.ColonyId, Is.EqualTo(colonyId));
        Assert.That(start.SystemId, Is.EqualTo("system-sol"));
        Assert.That(start.BodyId, Is.EqualTo("planet-earth"));
        Assert.That(start.ThemeId, Is.EqualTo("default-theme"));
        Assert.That(start.StartingFunds, Is.EqualTo(100000000));
        Assert.That(start.MaxSystems, Is.EqualTo(10));
        Assert.That(start.EleStart, Is.True);
        Assert.That(start.FactionName, Is.EqualTo("United Earth Corp"));
        Assert.That(start.FactionAbbreviation, Is.EqualTo("UEC"));

        var included = new List<string>();
        foreach (var pair in store.Systems)
        {
            if (store.LoadedSystems.Contains(pair.Value.ModNamespace))
                included.Add(pair.Key);
        }

        Assert.That(catalog.IncludedSystemIds, Is.EqualTo(included));
        Assert.That(catalog.IncludedSystemIds, Does.Contain("system-sol"));
        string alphaId = store.Systems.Single(pair => pair.Value.Name == "Alpha Centauri").Key;
        Assert.That(catalog.IncludedSystemIds, Does.Contain(alphaId));
    }

    [Test]
    public void FromDefaultStart_UsesTheCatalog_AndRejectsAMissingStartingFunds()
    {
        var (_, catalog, paths) = Load(WarpTechStart, BaseMod);
        var request = RequestFrom(catalog, paths);

        Assert.That(request.SpeciesId, Is.EqualTo("species-human"));
        Assert.That(request.ColonyId, Is.EqualTo("colony-earth"));
        Assert.That(request.SystemId, Is.EqualTo("system-sol"));
        Assert.That(request.BodyId, Is.EqualTo("planet-earth"));
        Assert.That(request.ThemeId, Is.EqualTo("default-theme"));
        Assert.That(request.StartingFunds, Is.EqualTo(100000000));
        Assert.That(request.MaxSystems, Is.EqualTo(10));
        Assert.That(request.EleStart, Is.True);
        Assert.That(request.FactionName, Is.EqualTo("United Earth Corp"));
        Assert.That(request.FactionAbbreviation, Is.EqualTo("UEC"));
        Assert.That(request.EnabledSystems, Is.EqualTo(catalog.IncludedSystemIds));
        Assert.That(request.ScenarioId, Is.EqualTo(catalog.ScenarioId));
        Assert.That(request.SMPassword, Is.EqualTo(""));
        Assert.That(request.PlayerPassword, Is.EqualTo(""));

        var broken = catalog with
        {
            DefaultStart = catalog.DefaultStart! with { StartingFunds = null },
        };
        Assert.Throws<InvalidOperationException>(() => RequestFrom(broken, paths));
    }

    [Test]
    public void CreateGameCore_AppliesThemeId()
    {
        var (store, catalog, paths) = Load(WarpTechStart, BaseMod);
        var source = store.Themes["default-theme"];
        var theme = new ThemeBlueprint
        {
            UniqueID = "test-theme",
            Name = source.Name,
            SystemNames = source.SystemNames,
            FleetNames = source.FleetNames,
            ShipNames = source.ShipNames,
            FirstNames = source.FirstNames,
            LastNames = source.LastNames,
            NavyRanks = source.NavyRanks,
            NavyRanksAbbreviations = source.NavyRanksAbbreviations,
        };
        theme.SetFullIdentifier(source.ModNamespace);
        store.Themes[theme.UniqueID] = theme;

        var solRequest = RequestFrom(catalog, paths);
        Assert.That(solRequest.ThemeId, Is.EqualTo("default-theme"));

        // Stay on the included packs so this check does not generate systems.
        int included = catalog.IncludedSystemIds.Count;
        var plain = solRequest with { EleStart = false, MaxSystems = included };
        var themed = plain with { ThemeId = theme.UniqueID };

        var themedGame = GameLifecycle.CreateGameCore(store, themed);
        Assert.That(themedGame, Is.Not.Null);
        Assert.That(themedGame!.Value.game.Settings.CurrentTheme, Is.EqualTo(theme.UniqueID));

        var plainGame = GameLifecycle.CreateGameCore(store, plain);
        Assert.That(plainGame, Is.Not.Null);
        Assert.That(plainGame!.Value.game.Settings.CurrentTheme, Is.EqualTo("default-theme"));
    }

    [Test]
    public void CreateGameCore_GeneratesOnlyTheSystemsPastTheIncludedPacks()
    {
        var (store, catalog, paths) = Load(WarpTechStart, BaseMod);
        int included = catalog.IncludedSystemIds.Count;
        Assert.That(included, Is.GreaterThan(0));

        var request = RequestFrom(catalog, paths) with
        {
            EleStart = false,
            MaxSystems = included + 2,
        };
        var grown = GameLifecycle.CreateGameCore(store, request);
        Assert.That(grown, Is.Not.Null);
        Assert.That(grown!.Value.game.Systems, Has.Count.EqualTo(included + 2));

        var shrunkRequest = request with { MaxSystems = included - 1 };
        var shrunk = GameLifecycle.CreateGameCore(store, shrunkRequest);
        Assert.That(shrunk, Is.Not.Null);
        Assert.That(shrunk!.Value.game.Systems, Has.Count.EqualTo(included));
    }

    [Test]
    public void BuildQuickstartRequest_UsesDefaultStartValues()
    {
        var (_, catalog, paths) = Load(WarpTechStart, BaseMod);
        var request = GameLifecycle.BuildQuickstartRequest(paths, catalog, 7);
        var start = catalog.DefaultStart;

        Assert.That(start, Is.Not.Null);
        Assert.That(request.StartingFunds, Is.EqualTo(start!.StartingFunds));
        Assert.That(request.MaxSystems, Is.EqualTo(start.MaxSystems));
        Assert.That(request.EleStart, Is.EqualTo(start.EleStart));
        Assert.That(request.FactionName, Is.EqualTo(start.FactionName));
        Assert.That(request.FactionAbbreviation, Is.EqualTo(start.FactionAbbreviation));
        Assert.That(request.ThemeId, Is.EqualTo(start.ThemeId));
        Assert.That(request.SpeciesId, Is.EqualTo(start.SpeciesId));
        Assert.That(request.ColonyId, Is.EqualTo(start.ColonyId));
        Assert.That(request.SystemId, Is.EqualTo(start.SystemId));
        Assert.That(request.BodyId, Is.EqualTo(start.BodyId));
        Assert.That(request.EnabledSystems, Is.EqualTo(catalog.IncludedSystemIds));
        Assert.That(request.StartingFunds, Is.EqualTo(100000000));
        Assert.That(request.MaxSystems, Is.EqualTo(10));
        Assert.That(request.EleStart, Is.True);
        Assert.That(request.FactionName, Is.EqualTo("United Earth Corp"));
        Assert.That(request.FactionAbbreviation, Is.EqualTo("UEC"));
        Assert.That(request.ThemeId, Is.EqualTo("default-theme"));
    }

    [Test]
    public void Wizard_HasTwoPages_AndKeepsOnlyNameAbbreviationAndSeed()
    {
        var page = typeof(NewGameMenu).Assembly.GetType("Pulsar4X.Client.Page");
        Assert.That(page, Is.Null);

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var fields = typeof(NewGameMenu).GetFields(flags);
        Assert.That(fields.Select(field => field.Name), Does.Not.Contain("_currentPage"));
        Assert.That(typeof(NewGameMenu).GetMethods(flags).Select(method => method.Name),
            Does.Not.Contain("DisplayDetailsPage"));

        string[] forbidden =
        {
            "password", "species", "theme", "colony", "funds", "maxsystems", "enabledsystems",
        };
        foreach (var field in fields)
        {
            string name = field.Name.ToLowerInvariant();
            foreach (var word in forbidden)
                Assert.That(name, Does.Not.Contain(word), field.Name);
        }

        var names = fields.Select(field => field.Name).ToList();
        Assert.That(names, Does.Contain("_corporationNameBuffer"));
        Assert.That(names, Does.Contain("_corporationAbbreviationBuffer"));
        Assert.That(names, Does.Contain("_masterSeed"));
    }
}
