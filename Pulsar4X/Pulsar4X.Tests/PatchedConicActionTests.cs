using System;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Earth to Mars on the Early Tech start ship. One day per step, with a save
/// after the escape, mid-cruise, and on the approach.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PatchedConicActionTests
{
    const string BaseMod = "Pulsar4x";
    const string EarlyMod = "Early Tech Start";

    Game _game = null!;
    Entity _faction = null!;
    Entity _starship = null!;
    ICargoable _methalox = null!;

    [SetUp]
    public void StartEarlyTech()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { BaseMod, EarlyMod }
            .Select(name => mods.Single(m => m.Mod.ModName == name).Path)
            .ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path));
        var catalog = GameLifecycle.BuildCatalog(store);
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
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);
        _game = result!.Value.game;
        _game.Settings.EnforceSingleThread = true;
        _faction = result.Value.faction;
        _starship = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>()).Single();
        _methalox = _faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
        Assert.That(_starship.GetName(_faction.Id), Is.EqualTo("Starship I"));
        Assert.That(_starship.GetDataBlob<NewtonThrustAbilityDB>().DeltaV, Is.GreaterThan(10_000));
    }

    [Test]
    public void EarthToMars_CapturesWithin450Days_AcrossThreeSaves()
    {
        var mars = Body("Mars");
        var earth = _starship.GetSOIParentEntity();
        int earthId = earth.Id;
        int marsId = mars.Id;
        int shipId = _starship.Id;
        int factionId = _faction.Id;
        string managerId = _starship.Manager.ManagerID;

        string planText = MovePlanner.Evaluate(_starship, mars, _starship.StarSysDateTime)
            .Single(o => o.Mode == MoveMode.NewtonianTransfer).Reason;
        double planned = PlannedDeltaV(_starship, mars);
        double fuelBefore = Fuel(_starship);
        double wet = _starship.GetDataBlob<MassVolumeDB>().MassTotal;
        double ve = _starship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
        double low = OrbitMath.LowOrbitRadius(mars);

        var goal = new Goal(GoalType.MoveTo) { TargetEntityID = mars.Id };
        AgentProcessor.AssignGoal(_starship, goal);
        Assert.That(goal.Status, Is.Not.EqualTo(GoalStatus.Failed), goal.Message);

        bool savedEscape = false;
        bool savedCruise = false;
        bool savedApproach = false;
        DateTime leftEarth = default;
        int cargoIds = Game.EntityIDCounter;
        int nextId = EntityIDGenerator.NextId;

        try
        {
            // The cheapest window can sit most of a synodic period ahead of day 0.
            // 450 days is the flight itself; the wait is on top of that.
            for (int day = 0; day < 1100; day++)
            {
                StepDay();
                var ship = Ship(shipId, managerId);
                var action = Transfer(ship);
                if (action == null || action.Status == ActionStatus.Failed)
                    Assert.Fail(Describe(ship, action) + " day " + day);

                var parent = ship.GetSOIParentEntity();
                if (!savedEscape && parent != null && parent.Id != earthId && parent.Id != marsId
                    && action.PhaseText == "Cruise")
                {
                    leftEarth = _game.TimePulse.GameGlobalDateTime;
                    ship = Reload(shipId, managerId, factionId, ref cargoIds, ref nextId);
                    savedEscape = true;
                    action = Transfer(ship);
                    Assert.That(action, Is.Not.Null, Describe(ship, action));
                    Assert.That(action.PhaseText, Is.EqualTo("Cruise"), Describe(ship, action));
                }

                if (savedEscape && action.PhaseText == "Cruise" && leftEarth != default
                    && _game.TimePulse.GameGlobalDateTime > leftEarth.AddDays(220))
                    Assert.Fail(planText + " " + Describe(ship, action));

                if (savedEscape && !savedCruise && action.PhaseText == "Cruise"
                    && leftEarth != default
                    && _game.TimePulse.GameGlobalDateTime > leftEarth.AddDays(40))
                {
                    ship = Reload(shipId, managerId, factionId, ref cargoIds, ref nextId);
                    savedCruise = true;
                    action = Transfer(ship);
                    Assert.That(action.PhaseText, Is.EqualTo("Cruise").Or.EqualTo("Correct").Or.EqualTo("Approach"),
                        Describe(ship, action));
                }

                if (!savedApproach && action.PhaseText == "Approach")
                {
                    ship = Reload(shipId, managerId, factionId, ref cargoIds, ref nextId);
                    savedApproach = true;
                    action = Transfer(ship);
                    Assert.That(action.PhaseText, Is.EqualTo("Approach").Or.EqualTo("Capture"), Describe(ship, action));
                }

                ship = Ship(shipId, managerId);
                if (ship.GetSOIParentEntity()?.Id == marsId && Captured(ship))
                    break;
            }

            var done = Ship(shipId, managerId);
            var finished = Transfer(done);
            string where = planText + " " + Describe(done, finished);
            Assert.That(savedEscape, Is.True, where);
            Assert.That(savedCruise, Is.True, where);
            Assert.That(savedApproach, Is.True, where);
            Assert.That(done.GetSOIParentEntity().Id, Is.EqualTo(marsId), where);

            var orbit = done.GetDataBlob<OrbitDB>();
            bool ellipse = LogText(done).Contains("Elliptical orbit");
            if (!ellipse)
                Assert.That(orbit.Eccentricity, Is.LessThan(0.05), Describe(done, finished));
            double peri = orbit.Periapsis;
            Assert.That(peri, Is.EqualTo(low).Within(low * 0.25), Describe(done, finished));

            string events = EventText();
            int exitAt = events.IndexOf("Exited SOI of Earth", StringComparison.Ordinal);
            int enterAt = events.IndexOf("Entered SOI of Mars", StringComparison.Ordinal);
            Assert.That(exitAt, Is.GreaterThanOrEqualTo(0), events);
            Assert.That(enterAt, Is.GreaterThan(exitAt), events);

            double correction = finished?.CorrectionDeltaV ?? 0;
            double used = fuelBefore - Fuel(done);
            double expected = OrbitMath.TsiolkovskyFuelUse(wet, ve, planned + correction);
            Assert.That(used, Is.EqualTo(expected).Within(Math.Max(1, expected * 0.05)),
                $"used {used:F0} kg, tsiolkovsky {expected:F0} kg for {planned:F0}+{correction:F0} m/s. {Describe(done, finished)}");
        }
        finally
        {
            if (Game.EntityIDCounter < cargoIds)
                Game.EntityIDCounter = cargoIds;
            if (EntityIDGenerator.NextId < nextId)
                EntityIDGenerator.NextId = nextId;
        }
    }

    [Test]
    public void ShortTank_RefusesMarsBeforeTheFirstBurn()
    {
        var mars = Body("Mars");
        double ve = _starship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
        double dry = _starship.GetDataBlob<MassVolumeDB>().MassDry;
        double want = dry * (Math.Exp(4000.0 / ve) - 1.0);
        double before = Fuel(_starship);
        double moved = CargoTransferProcessor.AddRemoveCargoMass(_starship, _methalox, want - before);
        Assert.That(moved, Is.EqualTo(want - before).Within(1));
        double fuel = Fuel(_starship);
        Assert.That(_starship.GetDataBlob<NewtonThrustAbilityDB>().DeltaV, Is.InRange(3600, 4400));

        var now = _starship.StarSysDateTime;
        var transfer = MovePlanner.Evaluate(_starship, mars, now).Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(transfer.Feasible, Is.False, transfer.Reason);
        Assert.That(transfer.Reason, Does.Contain("Δv"));
        Assert.That(MovePlanner.TryBuildMoveActions(_starship, mars, out var actions, out string why, now), Is.False, why);
        Assert.That(why, Does.Contain("Δv"));
        Assert.That(actions, Is.Empty);
        Assert.That(Fuel(_starship), Is.EqualTo(fuel).Within(0.001));
    }

    Entity Body(string name)
    {
        return _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<OrbitDB>())
            .First(body => body.GetName(_faction.Id) == name);
    }

    static double PlannedDeltaV(Entity ship, Entity target)
    {
        var option = MovePlanner.Evaluate(ship, target, ship.StarSysDateTime)
            .Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(option.Feasible, Is.True, option.Reason);
        return option.DeltaV;
    }

    void StepDay()
    {
        _game.TimePulse.TimeStep(_game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(1));
        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
    }

    Entity Ship(int shipId, string managerId)
    {
        var system = _game.Systems.Single(s => s.ManagerID == managerId);
        Assert.That(system.TryGetEntityById(shipId, out var ship), Is.True);
        return ship;
    }

    Entity Reload(int shipId, string managerId, int factionId, ref int cargoIds, ref int nextId)
    {
        nextId = EntityIDGenerator.NextId;
        cargoIds = Game.EntityIDCounter;
        string json = Game.Save(_game);
        EntityIDGenerator.NextId = 0;
        Game.EntityIDCounter = 0;
        var loaded = Game.Load(json);
        loaded.Settings.EnforceSingleThread = true;
        _game = loaded;
        Assert.That(loaded.GlobalManager.TryGetGlobalEntityById(factionId, out var faction), Is.True);
        _faction = faction;
        return Ship(shipId, managerId);
    }

    static SoiTransferAction Transfer(Entity ship)
    {
        if (!ship.TryGetDataBlob<ActionQueueDB>(out var queue))
            return null;
        return queue.ActionList.OfType<SoiTransferAction>().SingleOrDefault();
    }

    static bool Captured(Entity ship)
    {
        if (!ship.TryGetDataBlob<OrbitDB>(out var orbit))
            return false;
        if (orbit.Eccentricity < 0.05)
            return true;
        return LogText(ship).Contains("Elliptical orbit") || LogText(ship).Contains("Arrived at");
    }

    double Fuel(Entity ship) => ship.GetDataBlob<CargoStorageDB>().GetMassStored(_methalox, false);

    string EventText()
    {
        var log = _faction.GetDataBlob<FactionInfoDB>().EventLog;
        return string.Join("\n", log.GetEvents().Select(e => e.Message));
    }

    static string LogText(Entity ship)
    {
        if (!ship.TryGetDataBlob<ShipLogDB>(out var book))
            return "";
        return string.Join(" | ", book.Entries.Select(entry => entry.Text));
    }

    string Describe(Entity ship, SoiTransferAction action)
    {
        string parent = ship.GetSOIParentEntity()?.GetName(_faction.Id) ?? "none";
        string orbit = ship.TryGetDataBlob<OrbitDB>(out var o)
            ? $"e {o.Eccentricity:G4} peri {o.Periapsis:G6}"
            : "no orbit";
        string phase = action == null ? "no action" : action.PhaseText + " " + action.Status + " " + action.Details;
        return $"{parent} {orbit} {phase} log [{LogText(ship)}]";
    }
}
