using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// The Early Tech Start mod, driven through the same host calls as the New Game wizard:
/// scan the mods folder, load the ticked mods, take the catalog defaults, create the game.
/// </summary>
[TestFixture]
public class EarlyTechStartTests
{
    private const string BaseMod = "Pulsar4x";
    private const string EarlyMod = "Early Tech Start";
    private const string EarlyColony = "colony-early-earth";

    private static ModsState.ModMetaData Mod(string name)
        => ModsState.AvailableMods.Single(m => m.Mod.ModName == name);

    private static List<string> TickedPaths(params string[] names) => names.Select(n => Mod(n).Path).ToList();

    private static (Game game, Entity faction) StartGameLikeTheMenu(params string[] tickedMods)
    {
        ModsState.RefreshModsList("Data");
        var paths = TickedPaths(tickedMods);

        // "Next" on the mods page.
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path));
        var catalog = GameLifecycle.BuildCatalog(store);

        // The wizard's defaults: first species, first colony, every startable system, first body.
        var systems = catalog.Systems.Where(s => s.StartingBodies.Count > 0).ToList();
        var request = new NewGameRequest(
            ModManifestPaths: paths,
            FactionName: "United Earth Corp",
            FactionAbbreviation: "UEC",
            SpeciesId: catalog.Species.First().Id,
            ColonyId: catalog.Colonies.First().Id,
            SystemId: systems.First().Id,
            BodyId: systems.First().StartingBodies.First().Id,
            EnabledSystems: systems.Select(s => s.Id).ToList(),
            MaxSystems: 10,
            MasterSeed: 12345678,
            StartingFunds: 100_000_000,
            EleStart: true,
            SMPassword: "",
            PlayerPassword: "");

        // "Create Game!" on the details page.
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null, "CreateGameCore returned null");
        return (result!.Value.game, result.Value.faction);
    }

    [Test]
    public void ManifestIsDiscovered_DependsOnTheBaseMod_AndIsNotEnabledByDefault()
    {
        ModsState.RefreshModsList("Data");

        var names = ModsState.AvailableMods.Select(m => m.Mod.ModName).ToList();
        Assert.That(names, Does.Contain(EarlyMod));
        Assert.That(ModsState.IsModEnabled[EarlyMod], Is.False);
        Assert.That(Mod(EarlyMod).Mod.Dependencies, Is.EquivalentTo(new[] { "Pulsar4x" }));
    }

    [Test]
    public void EarlyModAlone_PullsInTheBaseModFirst()
    {
        ModsState.RefreshModsList("Data");

        var store = GameLifecycle.LoadModStore(TickedPaths(EarlyMod), ModsState.AvailableMods.Select(m => m.Path));

        Assert.That(store.ModManifests.Select(m => m.Namespace),
            Is.EqualTo(new[] { "Pulsar4x", "Pulsar4x.EarlyTechStart" }));
        Assert.That(store.ComponentTemplates, Does.ContainKey("mine"));
    }

    [Test]
    public void LoadOrderFollowsDependenciesNotTheOrderPathsAreGiven()
    {
        ModsState.RefreshModsList("Data");
        var all = ModsState.AvailableMods.Select(m => m.Path).ToList();

        var store = GameLifecycle.LoadModStore(TickedPaths(EarlyMod, BaseMod), all);

        Assert.That(store.ModManifests.Select(m => m.Namespace).ToList(),
            Is.EqualTo(new[] { "Pulsar4x", "Pulsar4x.EarlyTechStart" }));
    }

    [Test]
    public void LoadingAModDirectlyWithoutItsDependency_Throws()
    {
        var loader = new ModLoader();
        var store = new ModDataStore();

        var ex = Assert.Throws<System.InvalidOperationException>(
            () => loader.LoadModManifest("Data/earlytechstart/modInfo.json", store));

        Assert.That(ex!.Message, Does.Contain("Pulsar4x"));
        Assert.That(store.Colonies, Is.Empty);
    }

    [Test]
    public void MissingDependency_ReportsWhichModIsMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), "p4x-mod-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            var manifest = Path.Combine(dir, "modInfo.json");
            File.WriteAllText(manifest,
                "{\"ModName\":\"Orphan\",\"Namespace\":\"orphan\",\"Dependencies\":[\"nowhere\"],\"DataFiles\":[]}");

            var ex = Assert.Throws<FileNotFoundException>(
                () => GameLifecycle.LoadModStore(new[] { manifest }, new[] { manifest }));

            Assert.That(ex!.Message, Does.Contain("orphan").And.Contain("nowhere"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void RemoveOfAnIdThatIsNotLoaded_DoesNotAddAStubBlueprint()
    {
        var dir = Path.Combine(Path.GetTempPath(), "p4x-mod-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "data.json"),
                "[{\"Type\":\"Colony\",\"Operation\":\"Remove\",\"Payload\":{\"UniqueID\":\"colony-ghost\"}}]");
            var manifest = Path.Combine(dir, "modInfo.json");
            File.WriteAllText(manifest,
                "{\"ModName\":\"Remover\",\"Namespace\":\"remover\",\"DataFiles\":[\"data.json\"]}");

            var store = GameLifecycle.LoadModStore(new[] { manifest }, new[] { manifest });

            Assert.That(store.Colonies, Does.Not.ContainKey("colony-ghost"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void StartingConfigurationList_OffersOnlyTheEarlyEarthStart()
    {
        ModsState.RefreshModsList("Data");

        var store = GameLifecycle.LoadModStore(TickedPaths(BaseMod, EarlyMod), ModsState.AvailableMods.Select(m => m.Path));
        var catalog = GameLifecycle.BuildCatalog(store);

        Assert.That(catalog.Colonies.Select(c => c.Id), Is.EqualTo(new[] { EarlyColony }));
        Assert.That(store.Factions, Is.Empty);
    }

    [Test]
    public void EarlyMod_ReusesTheBaseComponentDesignsAndTemplates()
    {
        ModsState.RefreshModsList("Data");
        var store = GameLifecycle.LoadModStore(TickedPaths(EarlyMod), ModsState.AvailableMods.Select(m => m.Path));

        // The mod ships no facility or component data of its own.
        var early = Mod(EarlyMod).Mod;
        Assert.That(early.DataFiles, Is.EquivalentTo(new[] { "TemplateFiles/techs.json", "ScenarioFiles/colonies.json" }));

        var colony = store.Colonies[EarlyColony];
        foreach (var id in colony.ComponentDesigns!)
            Assert.That(store.ComponentDesigns, Does.ContainKey(id), id);
        foreach (var installation in colony.Installations!)
            Assert.That(store.ComponentDesigns, Does.ContainKey(installation.Id), installation.Id);
    }

    [Test]
    public void NewGameWithEarlyMod_HasEarthAsTheOnlyColony()
    {
        var (game, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);

        var colonies = game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .ToList();

        Assert.That(colonies, Has.Count.EqualTo(1));
        var colony = colonies[0];
        Assert.That(colony.FactionOwnerID, Is.EqualTo(faction.Id));
        Assert.That(colony.GetDataBlob<NameDB>().GetName(faction.Id), Does.Contain("Earth"));

        // No scenario factions were placed, only the player's (and the game master's).
        var withColonies = game.Factions.Values.Where(f => f.Id != faction.Id
            && colonies.Any(c => c.FactionOwnerID == f.Id));
        Assert.That(withColonies, Is.Empty);
    }

    [Test]
    public void NewGameWithEarlyMod_BuildsTheFacilitiesAndStocksTheWarehouse()
    {
        var (game, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var colony = game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Single();

        var installations = colony.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign.Keys.ToList();
        foreach (var id in new[] { "default-design-mine", "default-design-refinery", "default-design-factory",
                     "default-design-shipyard", "default-design-research-lab", "default-design-city-hall" })
            Assert.That(installations, Does.Contain(id), id);
        Assert.That(installations, Does.Not.Contain("default-design-auto-mine"));

        var designs = faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Keys;
        Assert.That(designs, Does.Not.Contain("default-design-alcubierre-2k"));
    }

    [Test]
    public void NewGameWithEarlyMod_RunsAMonthOfGameTimeAndSurvivesASaveAndLoad()
    {
        var (game, _) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        game.Settings.EnforceSingleThread = true;

        for (int day = 0; day < 30; day++)
            game.TimePulse.TimeStep(game.TimePulse.GameGlobalDateTime + System.TimeSpan.FromDays(1));

        var json = Game.Save(game);
        var loaded = Game.Load(json);

        var colonies = loaded.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .ToList();
        Assert.That(colonies, Has.Count.EqualTo(1));
    }

    [Test]
    public void NewGameWithEarlyModOnly_StartsTheSameWayAsWithBothTicked()
    {
        var (game, faction) = StartGameLikeTheMenu(EarlyMod);

        var colonies = game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .ToList();
        Assert.That(colonies, Has.Count.EqualTo(1));
        Assert.That(colonies[0].FactionOwnerID, Is.EqualTo(faction.Id));
    }

    [Test]
    public void NewGameWithEarlyMod_StartsWithEarlyTechnology()
    {
        var (_, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;

        // Basic industry and rockets are known.
        foreach (var known in new[] { "mine", "refinery", "factory", "shipyard", "conventional-engine", "solarArray", "ship-command" })
            Assert.That(data.ComponentTemplates.ContainsKey(known), Is.True, known);

        // Advanced systems are not, but can be researched toward.
        foreach (var locked in new[] { "alcubierre-warp-drive", "reactor", "laser-weapon", "automine", "scntr-engine" })
        {
            Assert.That(data.ComponentTemplates.ContainsKey(locked), Is.False, locked);
            Assert.That(data.LockedComponentTemplates.ContainsKey(locked), Is.True, locked);
        }

        Assert.That(data.Techs["tech-early-technology"].Level, Is.EqualTo(1));
        Assert.That(data.IsResearchable("tech-early-nuclear-power"), Is.True);
        Assert.That(data.Techs.ContainsKey("tech-early-warp-drive"), Is.False);
        Assert.That(data.Techs.ContainsKey("tech-early-directed-energy"), Is.False);
        Assert.That(data.IsResearchable("tech-geo-surveyor"), Is.True);
    }

    [Test]
    public void ResearchingNuclearPower_UnlocksTheNextTechsAndTemplates()
    {
        var (_, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;

        data.IncrementTechLevel("tech-early-nuclear-power");

        Assert.That(data.ComponentTemplates.ContainsKey("reactor"), Is.True);
        Assert.That(data.IsResearchable("tech-early-warp-drive"), Is.True);
        Assert.That(data.IsResearchable("tech-early-directed-energy"), Is.True);

        data.IncrementTechLevel("tech-early-warp-drive");
        Assert.That(data.ComponentTemplates.ContainsKey("alcubierre-warp-drive"), Is.True);
    }

    [Test]
    public void DefaultBaseModStart_IsUnchangedByTheEarlyModBeingInstalled()
    {
        var (game, _) = StartGameLikeTheMenu(BaseMod);

        var colonies = game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .ToList();
        Assert.That(colonies.Count, Is.GreaterThan(1));
    }
}
