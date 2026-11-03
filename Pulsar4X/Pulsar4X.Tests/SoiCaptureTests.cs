using System;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
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
/// Slice 4 of movement-newtonian-soi-capture. One level in and one level out.
/// A fresh Early Tech game per test, so a thirty-day pulse does not move the
/// phasing fixture's clock. The ship is placed in the moon's plane and both
/// the ship and Luna are snapped to the departure, so the wait is minutes.
/// </summary>
[TestFixture]
[NonParallelizable]
public class SoiCaptureTests
{
    const string BaseMod = "Pulsar4x";
    const string EarlyMod = "Early Tech Start";

    Game _game = null!;
    Entity _faction = null!;
    Entity _earth = null!;
    Entity _luna = null!;
    Entity _starship = null!;
    ShipDesign _design = null!;
    ICargoable _methalox = null!;
    double _fuelKg;

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
        _earth = _starship.GetSOIParentEntity();
        _luna = _earth.GetDataBlob<OrbitDB>().Children.First(c => c.GetName(_faction.Id) == "Luna");
        _design = _faction.GetDataBlob<FactionInfoDB>().ShipDesigns["early-ship-design-starship"];
        _methalox = _faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
        _fuelKg = _starship.GetDataBlob<CargoStorageDB>().GetMassStored(_methalox, false);
        Assert.That(_fuelKg, Is.GreaterThan(0));
        Assert.That(_earth, Is.Not.Null);
        Assert.That(_luna, Is.Not.Null);
    }

    [Test]
    public void EarthToLuna_PlansATransferAndNoWarp()
    {
        var ship = ShipInLunasPlane("Planner");
        SnapToWindow(ship, _luna, _earth);
        var now = ship.StarSysDateTime;

        var transfer = MovePlanner.Evaluate(ship, _luna, now).Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(transfer.Feasible, Is.True, transfer.Reason);
        Assert.That(transfer.SoiTransfer, Is.True, transfer.Reason);
        Assert.That(transfer.Interplanetary, Is.False, transfer.Reason);
        Assert.That(MovePlanner.Evaluate(ship, _luna, now).Single(o => o.Mode == MoveMode.Warp).Feasible, Is.False);

        Assert.That(MovePlanner.TryBuildMoveActions(ship, _luna, out var actions, out string why, now), Is.True, why);
        Assert.That(actions.OfType<WarpMoveAction>(), Is.Empty);
        Assert.That(actions.OfType<SoiTransferAction>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void EarthToLuna_CapturesWithinThirtyDays()
    {
        var ship = ShipInLunasPlane("Outbound");
        SnapToWindow(ship, _luna, _earth);
        double planned = PlannedDeltaV(ship, _luna);
        double fuelBefore = Fuel(ship);
        double wet = ship.GetDataBlob<MassVolumeDB>().MassTotal;
        double ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;

        var goal = OrderMove(ship, _luna);
        Pulse(30);

        AssertCaptured(ship, _luna, goal, fuelBefore, wet, ve, planned);
        Assert.That(LogText(ship), Does.Contain("Arrived at Luna"));
        Assert.That(LogText(ship), Does.Contain("Departure burn"));
        Assert.That(LogText(ship), Does.Contain("Capture burn"));
    }

    [Test]
    public void LunaToEarth_CapturesWithinThirtyDays()
    {
        var ship = ShipInLowLunaOrbit("Return");
        double planned = PlannedDeltaV(ship, _earth);
        double fuelBefore = Fuel(ship);
        double wet = ship.GetDataBlob<MassVolumeDB>().MassTotal;
        double ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;

        var goal = OrderMove(ship, _earth);
        Pulse(30);

        AssertCaptured(ship, _earth, goal, fuelBefore, wet, ve, planned);
        Assert.That(LogText(ship), Does.Contain("Arrived at Earth"));
        Assert.That(LogText(ship), Does.Contain("Departure burn"));
        Assert.That(LogText(ship), Does.Contain("Capture burn"));
    }

    [Test]
    public void ShortTank_RefusesBeforeBurning()
    {
        var ship = ShipInLunasPlane("Short");
        SnapToWindow(ship, _luna, _earth);
        double ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
        double dry = ship.GetDataBlob<MassVolumeDB>().MassDry;
        double want = dry * (Math.Exp(2000.0 / ve) - 1.0);
        double before = Fuel(ship);
        double moved = CargoTransferProcessor.AddRemoveCargoMass(ship, _methalox, want - before);
        Assert.That(moved, Is.EqualTo(want - before).Within(1));
        double fuel = Fuel(ship);
        Assert.That(fuel, Is.EqualTo(want).Within(1));
        Assert.That(ship.GetDataBlob<NewtonThrustAbilityDB>().DeltaV, Is.InRange(1800, 2200));

        var now = ship.StarSysDateTime;
        var transfer = MovePlanner.Evaluate(ship, _luna, now).Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(transfer.Feasible, Is.False, transfer.Reason);
        Assert.That(transfer.Reason, Does.Contain("Δv"));
        Assert.That(MovePlanner.TryBuildMoveActions(ship, _luna, out var actions, out string why, now), Is.False, why);
        Assert.That(why, Does.Contain("Δv"));
        Assert.That(actions, Is.Empty);
        Assert.That(Fuel(ship), Is.EqualTo(fuel).Within(0.001));
    }

    [Test]
    public void ShortTank_PreviewListsOrbitsInsideTheTank()
    {
        var ship = ShipInLunasPlane("Preview");
        var full = MovePlanner.Preview(ship, _luna, ship.StarSysDateTime);
        Assert.That(full.Feasible, Is.True, DescribePreview(full));
        Assert.That(full.RefusedForDeltaV, Is.False);
        Assert.That(full.Alternatives, Is.Empty);
        Assert.That(full.DeltaV, Is.GreaterThan(3000), DescribePreview(full));

        // Departure from low Earth orbit is about 3.03 km/s, so 3 km/s cannot
        // finish any lunar arrival. The offer list is the band under the low
        // circular trip and over that departure burn.
        DrainTo(ship, 3000);
        var poor = MovePlanner.Preview(ship, _luna, ship.StarSysDateTime);
        Assert.That(poor.Feasible, Is.False, DescribePreview(poor));
        Assert.That(poor.Reason, Does.Contain("Δv"));
        Assert.That(poor.RefusedForDeltaV, Is.True);
        Assert.That(poor.Alternatives, Is.Empty, DescribePreview(poor));

        DrainTo(ship, 3500);
        var mid = MovePlanner.Preview(ship, _luna, ship.StarSysDateTime);
        Assert.That(mid.Feasible, Is.False,
            $"low circular {full.DeltaV:F0} m/s. {DescribePreview(mid)}");
        Assert.That(mid.Reason, Does.Contain("Δv"));
        Assert.That(mid.RefusedForDeltaV, Is.True);
        Assert.That(mid.Alternatives, Is.Not.Empty,
            $"low circular {full.DeltaV:F0} m/s. {DescribePreview(mid)}");
        Assert.That(mid.Alternatives.Count, Is.LessThanOrEqualTo(3));
        double previous = 0;
        foreach (var alt in mid.Alternatives)
        {
            Assert.That(alt.DeltaV, Is.LessThanOrEqualTo(mid.Have + 1), DescribePreview(mid));
            Assert.That(alt.DeltaV, Is.GreaterThanOrEqualTo(previous));
            Assert.That(alt.Periapsis, Is.GreaterThan(0));
            previous = alt.DeltaV;
        }

        var offer = mid.Alternatives[0];
        Assert.That(MovePlanner.TryBuildMoveActions(
            ship, _luna, out var actions, out string why, ship.StarSysDateTime,
            null, offer.Periapsis, offer.Apoapsis), Is.True, why + " " + DescribePreview(mid));
        var action = actions.OfType<SoiTransferAction>().Single();
        Assert.That(action.ArrivalPeriapsis, Is.EqualTo(offer.Periapsis).Within(1));
        Assert.That(action.ArrivalApoapsis, Is.EqualTo(offer.Apoapsis).Within(1));

        var mars = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<OrbitDB>())
            .First(body => body.GetName(_faction.Id) == "Mars");
        var far = MovePlanner.Preview(ship, mars, ship.StarSysDateTime);
        Assert.That(far.Feasible, Is.False, DescribePreview(far));
        Assert.That(far.Reason, Does.Contain("Δv"), DescribePreview(far));
        Assert.That(far.RefusedForDeltaV, Is.True, DescribePreview(far));
        Assert.That(far.Alternatives, Is.Empty, DescribePreview(far));
    }

    [Test]
    public void CoastingTransfer_ResumesAfterSave()
    {
        var ship = ShipInLunasPlane("Coaster");
        SnapToWindow(ship, _luna, _earth);
        double planned = PlannedDeltaV(ship, _luna);
        double fuelBefore = Fuel(ship);
        double wet = ship.GetDataBlob<MassVolumeDB>().MassTotal;
        double ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
        OrderMove(ship, _luna);
        Pulse(0.5);

        Assert.That(ship.GetSOIParentEntity(), Is.EqualTo(_earth), Describe(ship, null));
        var coasting = ship.GetDataBlob<ActionQueueDB>().ActionList.OfType<SoiTransferAction>().Single();
        Assert.That(coasting.Status, Is.EqualTo(ActionStatus.Running), coasting.DetailText);
        Assert.That(ship.GetDataBlob<OrbitDB>().Eccentricity, Is.GreaterThan(0.1), Describe(ship, null));

        int cargoIds = Game.EntityIDCounter;
        int nextId = EntityIDGenerator.NextId;
        int shipId = ship.Id;
        var managerId = ship.Manager.ManagerID;
        try
        {
            nextId = EntityIDGenerator.NextId;
            cargoIds = Game.EntityIDCounter;
            string json = Game.Save(_game);
            EntityIDGenerator.NextId = 0;
            Game.EntityIDCounter = 0;
            var loaded = Game.Load(json);
            loaded.Settings.EnforceSingleThread = true;
            var loadedSys = loaded.Systems.Single(system => system.ManagerID == managerId);
            Assert.That(loadedSys.TryGetEntityById(shipId, out var loadedShip), Is.True);
            var resumed = loadedShip.GetDataBlob<ActionQueueDB>().ActionList.OfType<SoiTransferAction>().Single();
            Assert.That(resumed.Status, Is.EqualTo(ActionStatus.Running), resumed.DetailText);

            loaded.TimePulse.TimeStep(loaded.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(30));
            Assert.That(loaded.TimePulse.LastFault, Is.Null, loaded.TimePulse.LastFault?.Report);
            Assert.That(loadedShip.GetSOIParentEntity().Id, Is.EqualTo(_luna.Id), Describe(loadedShip, null));
            var orbit = loadedShip.GetDataBlob<OrbitDB>();
            var done = loadedShip.GetDataBlob<ShipLogDB>().Entries.Any(entry => entry.Text.Contains("Arrived at Luna"))
                || loadedShip.GetDataBlob<ActionQueueDB>().ActionList.OfType<SoiTransferAction>()
                    .Any(action => action.Details.StartsWith("Elliptical orbit"));
            if (!done)
                Assert.That(orbit.Eccentricity, Is.LessThan(0.05), Describe(loadedShip, null));
            double used = fuelBefore - loadedShip.GetDataBlob<CargoStorageDB>().GetMassStored(_methalox, false);
            double expected = OrbitMath.TsiolkovskyFuelUse(wet, ve, planned);
            Assert.That(used, Is.EqualTo(expected).Within(Math.Max(1, expected * 0.05)), Describe(loadedShip, null));
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
    public void EarthToMars_PlansAPatchedConicTransfer()
    {
        var mars = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<OrbitDB>())
            .First(body => body.GetName(_faction.Id) == "Mars");
        var now = _starship.StarSysDateTime;
        var options = MovePlanner.Evaluate(_starship, mars, now);
        var transfer = options.Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(transfer.Feasible, Is.True, transfer.Reason);
        Assert.That(transfer.SoiTransfer, Is.True, transfer.Reason);
        Assert.That(transfer.Interplanetary, Is.True);
        Assert.That(transfer.Reason, Does.Contain("Transfer to Mars, depart "));
        Assert.That(transfer.Reason, Does.Contain("arrive "));
        int departAt = transfer.Reason.IndexOf("depart ", StringComparison.Ordinal);
        var departStamp = transfer.Reason.Substring(departAt + "depart ".Length, 19);
        var departWhen = DateTime.ParseExact(departStamp, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        Assert.That(transfer.DepartSeconds, Is.EqualTo(Math.Max(0, (departWhen - now).TotalSeconds)).Within(1.5));
        var preview = MovePlanner.Preview(_starship, mars, now);
        Assert.That(preview.Feasible, Is.True, preview.Reason);
        Assert.That(preview.DepartInSeconds, Is.EqualTo(transfer.DepartSeconds).Within(1.5));
        Assert.That(preview.EtaSeconds, Is.GreaterThan(preview.DepartInSeconds));
        Assert.That(options.Single(o => o.Mode == MoveMode.Warp).Feasible, Is.False);
        Assert.That(options.Single(o => o.Mode == MoveMode.Warp).Reason, Does.Contain("no warp drive"));

        Assert.That(MovePlanner.TryBuildMoveActions(_starship, mars, out var actions, out string why, now), Is.True, why);
        Assert.That(actions.OfType<SoiTransferAction>().Count(), Is.EqualTo(1));
        Assert.That(actions.OfType<WarpMoveAction>(), Is.Empty);

        var goal = new Goal(GoalType.MoveTo) { TargetEntityID = mars.Id };
        AgentProcessor.AssignGoal(_starship, goal);
        Assert.That(goal.Status, Is.Not.EqualTo(GoalStatus.Failed), goal.Message);
        Assert.That(_starship.GetDataBlob<ActionQueueDB>().ActionList.OfType<SoiTransferAction>().Count(), Is.EqualTo(1));
    }

    Entity ShipInLunasPlane(string name)
    {
        var ship = FuelledShip(name, _earth);
        var moon = MoonState(_luna, ship.StarSysDateTime);
        Vector3 normal = Vector3.Normalise(Vector3.Cross(moon.pos, moon.vel));
        PlaceCircular(ship, _earth, normal, moon.pos, OrbitMath.LowOrbitRadius(_earth));
        return ship;
    }

    Entity ShipInLowLunaOrbit(string name)
    {
        var ship = FuelledShip(name, _luna);
        var moon = MoonState(_luna, ship.StarSysDateTime);
        Vector3 normal = Vector3.Normalise(Vector3.Cross(moon.pos, moon.vel));
        PlaceCircular(ship, _luna, normal, moon.pos, OrbitMath.LowOrbitRadius(_luna));
        return ship;
    }

    Entity FuelledShip(string name, Entity parent)
    {
        var ship = ShipFactory.CreateShip(_design, _faction, parent, name);
        double added = CargoTransferProcessor.AddRemoveCargoMass(ship, _methalox, _fuelKg);
        Assert.That(added, Is.EqualTo(_fuelKg).Within(1), name);
        Assert.That(ship.GetDataBlob<NewtonThrustAbilityDB>().DeltaV, Is.GreaterThan(10_000), name);
        return ship;
    }

    static void PlaceCircular(Entity ship, Entity parent, Vector3 normal, Vector3 radialHint, double radius)
    {
        var now = ship.StarSysDateTime;
        Vector3 n = Vector3.Normalise(normal);
        Vector3 radial = radialHint - n * Vector3.Dot(radialHint, n);
        if (radial.Length() < 1)
            radial = Vector3.Cross(n, new Vector3(0, 0, 1));
        radial = Vector3.Normalise(radial);
        var pos = radial * radius;
        double myMass = ship.GetDataBlob<MassVolumeDB>().MassDry;
        double parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
        double sgp = GeneralMath.StandardGravitationalParameter(myMass + parentMass);
        double speed = Math.Sqrt(sgp / radius);
        var vel = Vector3.Normalise(Vector3.Cross(n, pos)) * speed;
        ship.SetDataBlob(OrbitDB.FromVector(parent, myMass, parentMass, pos, vel, now));
        Assert.That(ship.GetDataBlob<OrbitDB>().Eccentricity, Is.LessThan(0.01));
    }

    /// <summary>
    /// Advance the ship and the moon to the planned departure and install that
    /// state at the current time. Relative phase is unchanged, so the next
    /// window is now rather than a synodic month away.
    /// </summary>
    void SnapToWindow(Entity ship, Entity moon, Entity earth)
    {
        var now = ship.StarSysDateTime;
        double budget = ship.GetDataBlob<NewtonThrustAbilityDB>().DeltaV;
        var first = SoiTransferMath.Evaluate(
            ship, moon, earth, ship.GetDataBlob<OrbitDB>(), moon.GetDataBlob<OrbitDB>(), now, budget);
        Assert.That(first.Kind, Is.EqualTo(SoiTransferMath.Kind.Ready), first.Reason);

        var shipAt = OrbitalMath.GetStateVectors(ship.GetDataBlob<OrbitDB>().GetElements(), first.DepartAt);
        var moonAt = OrbitalMath.GetStateVectors(moon.GetDataBlob<OrbitDB>().GetElements(), first.DepartAt);
        double shipMass = ship.GetDataBlob<MassVolumeDB>().MassDry;
        double moonMass = moon.GetDataBlob<MassVolumeDB>().MassDry;
        double earthMass = earth.GetDataBlob<MassVolumeDB>().MassDry;
        ship.SetDataBlob(OrbitDB.FromVector(
            earth, shipMass, earthMass, shipAt.position, (Vector3)shipAt.velocity, now));
        moon.SetDataBlob(OrbitDB.FromVector(
            earth, moonMass, earthMass, moonAt.position, (Vector3)moonAt.velocity, now));

        var again = SoiTransferMath.Evaluate(
            ship, moon, earth, ship.GetDataBlob<OrbitDB>(), moon.GetDataBlob<OrbitDB>(), now, budget);
        Assert.That(again.Kind, Is.EqualTo(SoiTransferMath.Kind.Ready), again.Reason);
        double waitMin = (again.DepartAt - now).TotalMinutes;
        Assert.That(waitMin, Is.GreaterThanOrEqualTo(0).And.LessThan(180),
            $"departure wait {waitMin:F1} min. {again.Reason}");
    }

    static double PlannedDeltaV(Entity ship, Entity target)
    {
        var now = ship.StarSysDateTime;
        var option = MovePlanner.Evaluate(ship, target, now).Single(o => o.Mode == MoveMode.NewtonianTransfer);
        Assert.That(option.Feasible, Is.True, option.Reason);
        Assert.That(option.SoiTransfer, Is.True, option.Reason);
        Assert.That(option.DeltaV, Is.GreaterThan(1000), option.Reason);
        return option.DeltaV;
    }

    Goal OrderMove(Entity ship, Entity target)
    {
        var goal = new Goal(GoalType.MoveTo) { TargetEntityID = target.Id };
        AgentProcessor.AssignGoal(ship, goal);
        Assert.That(goal.Status, Is.Not.EqualTo(GoalStatus.Failed), goal.Message + " " + Describe(ship, goal));
        return goal;
    }

    void Pulse(double days)
    {
        _game.TimePulse.TimeStep(_game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(days));
        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);
    }

    void AssertCaptured(Entity ship, Entity target, Goal goal, double fuelBefore, double wet, double ve, double plannedDv)
    {
        Assert.That(ship.GetSOIParentEntity(), Is.EqualTo(target), Describe(ship, goal));
        var orbit = ship.GetDataBlob<OrbitDB>();
        bool ellipse = LogText(ship).Contains("Elliptical orbit")
            || ship.GetDataBlob<ActionQueueDB>().ActionList.Any(action => action.Details.StartsWith("Elliptical orbit"));
        if (!ellipse)
            Assert.That(orbit.Eccentricity, Is.LessThan(0.05), Describe(ship, goal));
        double surface = target.GetDataBlob<MassVolumeDB>().RadiusInM;
        Assert.That(orbit.Periapsis, Is.GreaterThan(surface), Describe(ship, goal));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Completed), Describe(ship, goal));
        double used = fuelBefore - Fuel(ship);
        double expected = OrbitMath.TsiolkovskyFuelUse(wet, ve, plannedDv);
        Assert.That(used, Is.EqualTo(expected).Within(Math.Max(1, expected * 0.05)),
            $"used {used:F0} kg, tsiolkovsky {expected:F0} kg for {plannedDv:F0} m/s. {Describe(ship, goal)}");
    }

    double Fuel(Entity ship) => ship.GetDataBlob<CargoStorageDB>().GetMassStored(_methalox, false);

    void DrainTo(Entity ship, double dv)
    {
        double ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
        double dry = ship.GetDataBlob<MassVolumeDB>().MassDry;
        double want = dry * (Math.Exp(dv / ve) - 1.0);
        double before = Fuel(ship);
        double moved = CargoTransferProcessor.AddRemoveCargoMass(ship, _methalox, want - before);
        Assert.That(moved, Is.EqualTo(want - before).Within(1));
        Assert.That(Fuel(ship), Is.EqualTo(want).Within(1));
        Assert.That(ship.GetDataBlob<NewtonThrustAbilityDB>().DeltaV, Is.EqualTo(dv).Within(dv * 0.1));
    }

    static string DescribePreview(TripPreview preview)
    {
        string alts = preview.Alternatives.Count == 0
            ? "none"
            : string.Join("; ", preview.Alternatives.Select(alt =>
                $"{alt.Label} {alt.DeltaV:F0} m/s peri {alt.Periapsis:F0} apo {alt.Apoapsis:F0}"));
        return $"feasible {preview.Feasible} dvRefuse {preview.RefusedForDeltaV} "
            + $"need {preview.DeltaV:F0} have {preview.Have:F0} '{preview.Reason}' alts [{alts}]";
    }

    static string LogText(Entity ship)
    {
        if (!ship.TryGetDataBlob<ShipLogDB>(out var book))
            return "";
        return string.Join(" | ", book.Entries.Select(entry => entry.Text));
    }

    string Describe(Entity ship, Goal? goal)
    {
        string parent = ship.GetSOIParentEntity()?.GetName(_faction.Id) ?? "none";
        string orbit = ship.TryGetDataBlob<OrbitDB>(out var body)
            ? $"e={body.Eccentricity:F4} a={body.SemiMajorAxis / 1000:F0} km"
            : "no orbit";
        string actions = ship.TryGetDataBlob<ActionQueueDB>(out var queue)
            ? string.Join("; ", queue.ActionList.Select(action => $"{action.Name} {action.Status} {action.DetailText}"))
            : "no queue";
        return $"parent={parent} {orbit} goal={goal?.Status} '{goal?.Message}' actions=[{actions}] log=[{LogText(ship)}]";
    }

    static (Vector3 pos, Vector3 vel) MoonState(Entity moon, DateTime at)
    {
        var state = OrbitalMath.GetStateVectors(moon.GetDataBlob<OrbitDB>().GetElements(), at);
        return (state.position, (Vector3)state.velocity);
    }
}
