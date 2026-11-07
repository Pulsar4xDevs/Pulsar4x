using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Engine.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.JumpPoints;
using Pulsar4X.Orbits;
using Pulsar4X.Components;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

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

        // The mod ships no facility or component data of its own, only techs, the colony and one ship design.
        var early = Mod(EarlyMod).Mod;
        Assert.That(early.DataFiles, Is.EquivalentTo(new[] { "TemplateFiles/techs.json", "ScenarioFiles/colonies.json", "ScenarioFiles/ships.json" }));

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

    [TestCase(false)]
    [TestCase(true)]
    public void NewGameWithEarlyMod_BuildsTheFacilitiesAndStocksTheWarehouse(bool earlyModOnly)
    {
        var (game, faction) = StartGameLikeTheMenu(earlyModOnly ? new[] { EarlyMod } : new[] { BaseMod, EarlyMod });
        var colony = game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Single();

        var installations = colony.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign.Keys.ToList();
        foreach (var id in new[] { "default-design-mine", "default-design-refinery", "default-design-factory",
                     "default-design-shipyard", "default-design-research-lab", "default-design-city-hall" })
            Assert.That(installations, Does.Contain(id), id);
        Assert.That(installations, Does.Not.Contain("default-design-auto-mine"));
        var byDesign = colony.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign;
        Assert.That(byDesign["default-design-factory"], Has.Count.EqualTo(1), "one installed factory in a fresh Earth start");
        Assert.That(byDesign["default-design-refinery"], Has.Count.EqualTo(1), "one installed refinery in a fresh Earth start");

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
        foreach (var locked in new[] { "alcubierre-warp-drive", "reactor", "laser-weapon", "automine", "scntr-engine", "gravitational-surveyor" })
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

    // --- Newtonian-only start: no warp, no jump -------------------------------------------

    [Test]
    public void NewGameWithEarlyMod_KnowsNoWarpOrJumpTech()
    {
        var (_, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;

        // Not known and not even researchable: the whole line sits behind tech-early-warp-drive.
        foreach (var id in data.Techs.Keys)
        {
            Assert.That(id, Does.Not.Contain("warp"), id);
            Assert.That(id, Does.Not.Contain("alcubierre"), id);
            Assert.That(id, Does.Not.Contain("jump"), id);
            Assert.That(id, Does.Not.Contain("gravitational"), id);
        }
        foreach (var id in new[] { "tech-early-warp-drive", "tech-gravitational-surveyor", "tech-modern-technology" })
            Assert.That(data.Techs.ContainsKey(id), Is.False, id);

        foreach (var id in new[] { "alcubierre-warp-drive", "gravitational-surveyor" })
        {
            Assert.That(data.ComponentTemplates.ContainsKey(id), Is.False, id);
            Assert.That(data.LockedComponentTemplates.ContainsKey(id), Is.True, id);
        }
    }

    [Test]
    public void NewGameWithEarlyMod_HasNoWarpOrJumpComponentsOrKnownJumpPoints()
    {
        var (game, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var info = faction.GetDataBlob<FactionInfoDB>();

        foreach (var design in info.ComponentDesigns.Values)
        {
            Assert.That(design.HasAttribute<WarpDriveAtb>(), Is.False, design.UniqueID);
            Assert.That(design.HasAttribute<GravSurveyAtb>(), Is.False, design.UniqueID);
        }
        Assert.That(info.KnownJumpPoints, Is.Empty);

        var engines = info.ComponentDesigns.Values.Where(d => d.HasAttribute<NewtonionThrustAtb>()).ToList();
        Assert.That(engines, Is.Not.Empty, "there must be reaction engines to fly with");

        // Nothing that was built or launched can warp or survey.
        var all = game.Systems.SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Concat(system.GetAllEntitiesWithDataBlob<ShipInfoDB>())).ToList();
        foreach (var entity in all)
        {
            Assert.That(entity.HasDataBlob<WarpAbilityDB>(), Is.False, entity.GetName(faction.Id));
            Assert.That(entity.HasDataBlob<JPSurveyAbilityDB>(), Is.False, entity.GetName(faction.Id));
        }
    }

    [Test]
    public void NewGameWithEarlyMod_StartingShipsFlyOnReactionDrivesOnly()
    {
        var (game, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var info = faction.GetDataBlob<FactionInfoDB>();

        Assert.That(info.ShipDesigns.Keys, Is.EquivalentTo(new[] { "early-ship-design-starship", "early-ship-design-newton-tug" }));
        var ships = game.Systems.SelectMany(system => system.GetAllEntitiesWithDataBlob<ShipInfoDB>()).ToList();
        Assert.That(ships, Has.Count.EqualTo(1));

        var ship = ships[0];
        Assert.That(ship.HasDataBlob<WarpAbilityDB>(), Is.False);
        Assert.That(ship.GetDataBlob<NewtonThrustAbilityDB>().ThrustInNewtons, Is.GreaterThan(0));
        Assert.That(MovePlanner.CanMove(ship, out string reason), Is.True, reason);
    }

    [Test]
    public void EarlyTechShip_CannotJump_AndABaseModWarpShipCan()
    {
        var (early, earlyFaction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        AssertJumpMatchesWarp(early, earlyFaction);
        var earlyShips = ProjectedShips(early, earlyFaction.Id);
        Assert.That(earlyShips, Is.Not.Empty);
        Assert.That(earlyShips.All(s => !s.CanJump), Is.True);

        var (stock, stockFaction) = StartGameLikeTheMenu(BaseMod);
        AssertJumpMatchesWarp(stock, stockFaction);
        Assert.That(ProjectedShips(stock, stockFaction.Id).Any(s => s.CanJump), Is.True);
    }

    static void AssertJumpMatchesWarp(Game game, Entity faction)
    {
        var hierarchy = new GameProjector(game).ProjectFleetHierarchy(faction.Id);
        var byId = game.Systems.SelectMany(system => system.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            .Where(ship => ship.FactionOwnerID == faction.Id)
            .ToDictionary(ship => ship.Id);
        foreach (var snap in EveryShip(hierarchy.Fleets).Concat(hierarchy.UnattachedShips))
        {
            Assert.That(byId.ContainsKey(snap.Id), Is.True, snap.Name);
            Assert.That(snap.CanJump, Is.EqualTo(byId[snap.Id].HasDataBlob<WarpAbilityDB>()), snap.Name);
        }
        foreach (var fleet in hierarchy.Fleets)
            AssertFleetJump(fleet);
    }

    static void AssertFleetJump(FleetSnapshot fleet)
    {
        Assert.That(fleet.CanJump, Is.EqualTo(AnyChildCanJump(fleet)), fleet.Name);
        foreach (var sub in fleet.SubFleets)
            AssertFleetJump(sub);
    }

    static bool AnyChildCanJump(FleetSnapshot fleet)
        => fleet.Ships.Any(ship => ship.CanJump) || fleet.SubFleets.Any(AnyChildCanJump);

    static List<ShipSnapshot> ProjectedShips(Game game, int factionId)
    {
        var hierarchy = new GameProjector(game).ProjectFleetHierarchy(factionId);
        return EveryShip(hierarchy.Fleets).Concat(hierarchy.UnattachedShips).ToList();
    }

    static IEnumerable<ShipSnapshot> EveryShip(IEnumerable<FleetSnapshot> fleets)
    {
        foreach (var fleet in fleets)
        {
            foreach (var ship in fleet.Ships)
                yield return ship;
            foreach (var ship in EveryShip(fleet.SubFleets))
                yield return ship;
        }
    }

    [Test]
    public void StartingShip_IsStarshipLike_AndHasTheDeltaVForEarthToMars()
    {
        var (game, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var ship = game.Systems.SelectMany(system => system.GetAllEntitiesWithDataBlob<ShipInfoDB>()).Single();
        var thrust = ship.GetDataBlob<NewtonThrustAbilityDB>();

        Assert.That(ship.GetName(faction.Id), Is.EqualTo("Starship I"));
        // ~1200 t of methalox through Raptor-class engines (ve = 3.6 km/s, Isp ~ 370 s).
        Assert.That(thrust.TotalFuel_kg, Is.InRange(1.1e6, 1.3e6));
        Assert.That(thrust.ExhaustVelocity, Is.InRange(3500, 3700));
        Assert.That(thrust.ThrustInNewtons, Is.GreaterThan(5e6));

        // An Earth parking orbit to Mars transfer is about 5.6 km/s. Demand a clear margin.
        const double earthToMarsDv = 5600;
        Assert.That(thrust.DeltaV, Is.GreaterThan(earthToMarsDv * 1.5));
    }

    [Test]
    public void NewtonianOnlyShip_IsOfferedNoWarpMove_AndCanHohmannWithinTheWell()
    {
        var (game, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var ship = game.Systems.SelectMany(system => system.GetAllEntitiesWithDataBlob<ShipInfoDB>()).Single();
        var now = ship.StarSysDateTime;
        var system = game.Systems.Single(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>().Contains(ship));
        var earth = ship.GetSOIParentEntity();
        var luna = earth.GetDataBlob<OrbitDB>().Children
            .First(c => c.GetName(faction.Id) == "Luna");
        var mars = system.GetAllEntitiesWithDataBlob<OrbitDB>()
            .First(c => c.GetName(faction.Id) == "Mars");

        // Interplanetary Newtonian is the patched conic. There is still no warp drive.
        var toMars = MovePlanner.Evaluate(ship, mars, now);
        Assert.That(toMars.Single(o => o.Mode == MoveMode.Warp).Feasible, Is.False);
        var marsTransfer = toMars.Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(marsTransfer.Feasible, Is.True, marsTransfer.Reason);
        Assert.That(marsTransfer.SoiTransfer, Is.True, marsTransfer.Reason);
        Assert.That(marsTransfer.Interplanetary, Is.True);
        Assert.That(marsTransfer.Reason, Does.Contain("Transfer to Mars"));
        Assert.That(MovePlanner.TryBuildMoveActions(ship, mars, out var marsActions, out string marsWhy, now), Is.True, marsWhy);
        Assert.That(marsActions.OfType<WarpMoveAction>(), Is.Empty);
        Assert.That(marsActions.OfType<SoiTransferAction>().Count(), Is.EqualTo(1));

        // Luna is in Earth's well but has its own SOI. Whatever the planner decides, it must not warp.
        var toLuna = MovePlanner.Evaluate(ship, luna, now);
        Assert.That(toLuna.Single(o => o.Mode == MoveMode.Warp).Feasible, Is.False);
        MovePlanner.TryBuildMoveActions(ship, luna, out var lunaActions, out _, now);
        Assert.That(lunaActions.OfType<WarpMoveAction>(), Is.Empty);

        // Rendezvous with another ship in a higher circular orbit: a Hohmann transfer, no warp.
        var design = faction.GetDataBlob<FactionInfoDB>().ShipDesigns["early-ship-design-starship"];
        double r = earth.GetDataBlob<MassVolumeDB>().RadiusInM * 6;
        var buddy = ShipFactory.CreateShip(design, faction, new Pulsar4X.Orbital.Vector3(r, 0, 0), earth, "Buddy");
        var methalox = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
        double shipFuel = ship.GetDataBlob<CargoStorageDB>().GetMassStored(methalox, false);
        Assert.That(shipFuel, Is.GreaterThan(0), "the starting ship is fuelled");
        CargoTransferProcessor.AddRemoveCargoMass(buddy.GetDataBlob<CargoStorageDB>(), methalox, shipFuel);
        var toBuddy = MovePlanner.Evaluate(ship, buddy, now);
        var transfer = toBuddy.Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(transfer.Feasible, Is.True, transfer.Reason);
        Assert.That(toBuddy.Single(o => o.Mode == MoveMode.Warp).Feasible, Is.False);
        Assert.That(MovePlanner.TryBuildMoveActions(ship, buddy, out var buddyActions, out string why, now), Is.True, why);
        Assert.That(buddyActions.OfType<WarpMoveAction>(), Is.Empty);
        Assert.That(buddyActions, Is.Not.Empty);

        // Coming down from the higher orbit to Earth's parking altitude is a Hohmann too.
        var toEarth = MovePlanner.Evaluate(buddy, earth, now).Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(toEarth.Feasible, Is.True, toEarth.Reason);
        Assert.That(toEarth.DeltaV, Is.GreaterThan(0));
        Assert.That(MovePlanner.TryBuildMoveActions(buddy, earth, out var earthActions, out why, now), Is.True, why);
        Assert.That(earthActions, Is.Not.Empty);
        Assert.That(earthActions.OfType<WarpMoveAction>(), Is.Empty);
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
    public void NuclearThermalEngine_IsDesignableOnlyAfterFirstNuclearPowerResearch()
    {
        var (_, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var data = info.Data;
        Assert.That(data.ComponentTemplates.ContainsKey("scntr-engine"), Is.False);
        Assert.That(data.LockedComponentTemplates.ContainsKey("scntr-engine"), Is.True);
        Assert.That(info.ComponentDesigns.ContainsKey("default-design-NTR1.8"), Is.False,
            "the stock NERVA must not bypass the nuclear gate");
        Assert.That(info.ComponentDesigns.Values.Any(d => d.TemplateID == "scntr-engine"), Is.False);
        Assert.That(data.CargoGoods.GetAny("ntp"), Is.Null);
        Assert.That(data.ComponentTemplates.ContainsKey("conventional-engine"), Is.True);

        data.IncrementTechLevel("tech-early-nuclear-power");

        Assert.That(data.ComponentTemplates.ContainsKey("scntr-engine"), Is.True);
        Assert.That(data.CargoGoods.GetAny("ntp"), Is.Not.Null,
            "the template's default nuclear propellant must be available too");
        Assert.DoesNotThrow(() => new ComponentDesigner(data.ComponentTemplates["scntr-engine"], data,
            faction.GetDataBlob<FactionTechDB>()));
        Assert.That(data.ComponentTemplates.ContainsKey("conventional-engine"), Is.True);
    }

    [TestCase("steam-turbine-reactor")]
    [TestCase("rtg")]
    [TestCase("reactor")]
    public void ResearchingNuclearPower_MakesItsTemplatesDesignableWithTheirFuel(string templateId)
    {
        var (_, faction) = StartGameLikeTheMenu(BaseMod, EarlyMod);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        Assert.That(data.CargoGoods.GetAny("fissile-fuels"), Is.Null,
            "nuclear fuel is not available before nuclear research");

        data.IncrementTechLevel("tech-early-nuclear-power");

        Assert.DoesNotThrow(() => new ComponentDesigner(data.ComponentTemplates[templateId], data,
            faction.GetDataBlob<FactionTechDB>()), "selecting the newly unlocked template must not crash");
        Assert.That(data.CargoGoods.GetAny("fissile-fuels"), Is.Not.Null,
            "nuclear research must also make its processed fuel available");
        Assert.That(data.LockedCargoGoods.Contains("fissile-fuels"), Is.False);
    }

    [Test]
    public void DefaultBaseModStart_IsUnchangedByTheEarlyModBeingInstalled()
    {
        var (game, faction) = StartGameLikeTheMenu(BaseMod);
        var info = faction.GetDataBlob<FactionInfoDB>();
        Assert.That(info.Data.ComponentTemplates.ContainsKey("scntr-engine"), Is.True);
        Assert.That(info.ComponentDesigns.ContainsKey("default-design-NTR1.8"), Is.True);

        var colonies = game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .ToList();
        Assert.That(colonies.Count, Is.GreaterThan(1));
    }
}
