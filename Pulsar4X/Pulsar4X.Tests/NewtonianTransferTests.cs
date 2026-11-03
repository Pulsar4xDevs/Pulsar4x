using System;
using System.Collections.Generic;
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
/// Slice 1 of movement-newtonian-soi-capture: fly phasing and Hohmann, do not just call them.
/// Arrival tolerance matches MatchOrbitAction: 1% of the target radius, or three low-orbit radii.
/// </summary>
[TestFixture]
[NonParallelizable]
public class NewtonianTransferTests
{
    const string BaseMod = "Pulsar4x";
    const string EarlyMod = "Early Tech Start";

    Game _game = null!;
    Entity _faction = null!;
    Entity _earth = null!;
    Entity _starship = null!;
    ShipDesign _design = null!;
    ICargoable _methalox = null!;
    double _fuelKg;
    DateTime _now;

    [OneTimeSetUp]
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
        _faction = result.Value.faction;
        _starship = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>()).Single();
        _earth = _starship.GetSOIParentEntity();
        _now = _starship.StarSysDateTime;
        _design = _faction.GetDataBlob<FactionInfoDB>().ShipDesigns["early-ship-design-starship"];
        _methalox = _faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
        _fuelKg = _starship.GetDataBlob<CargoStorageDB>().GetMassStored(_methalox, false);
        Assert.That(_fuelKg, Is.GreaterThan(0));
        Assert.That(_earth, Is.Not.Null);
    }

    [TestCase(30)]
    [TestCase(-30)]
    [TestCase(170)]
    public void Phasing_ProgradeBuddy_EndsAlongside(double phaseDeg)
    {
        var (ship, buddy) = PairOnSameRadius(phaseDeg, OrbitSense.Prograde);
        FlyAndAssertAlongside(ship, buddy, $"prograde {phaseDeg}°");
    }

    [TestCase(30)]
    [TestCase(-30)]
    [TestCase(170)]
    public void Phasing_RetrogradeBuddy_EndsAlongside(double phaseDeg)
    {
        var (ship, buddy) = PairOnSameRadius(phaseDeg, OrbitSense.Retrograde);
        FlyAndAssertAlongside(ship, buddy, $"retrograde {phaseDeg}°");
    }

    [TestCase(30)]
    [TestCase(-30)]
    [TestCase(170)]
    public void Phasing_InclinedBuddy_EndsAlongside(double phaseDeg)
    {
        var (ship, buddy) = PairOnSameRadius(phaseDeg, OrbitSense.Inclined5);
        FlyAndAssertAlongside(ship, buddy, $"inclined {phaseDeg}°");
    }

    [Test]
    public void Hohmann_EveryPhase_WaitsWithinASynodicPeriodAndArrives()
    {
        double shipR = _earth.GetDataBlob<MassVolumeDB>().RadiusInM * 2;
        double targetR = _earth.GetDataBlob<MassVolumeDB>().RadiusInM * 6;
        double sgp = GeneralMath.StandardGravitationalParameter(
            _earth.GetDataBlob<MassVolumeDB>().MassDry + _design.MassPerUnit);
        double synodic = 2 * Math.PI / Math.Abs(
            Math.Sqrt(sgp / Math.Pow(shipR, 3)) - Math.Sqrt(sgp / Math.Pow(targetR, 3)));

        var phases = new List<double>();
        for (double deg = 0.1; deg < 360; deg += 7)
            phases.Add(deg);
        phases.Add(359.9);

        foreach (double deg in phases)
        {
            var ship = FuelledShip(CircularPosition(shipR, 0), $"h-ship-{deg:0.0}");
            var buddy = FuelledShip(CircularPosition(targetR, deg * Math.PI / 180), $"h-buddy-{deg:0.0}");
            Assert.That(
                MatchOrbitAction.TryPlanBurns(ship, buddy, _now, out var burns, out string reason),
                Is.True, $"phase {deg}°: {reason}");
            Assert.That(burns, Has.Count.EqualTo(2), $"phase {deg}°");

            double tWait = (burns[0].at - _now).TotalSeconds;
            Assert.That(tWait, Is.GreaterThanOrEqualTo(0), $"phase {deg}° tWait {tWait}");
            Assert.That(tWait, Is.LessThanOrEqualTo(synodic + 1),
                $"phase {deg}° tWait {tWait} s, synodic {synodic} s");

            Fly(ship, burns);
            double at = (burns[^1].at - _now).TotalSeconds;
            double separation = (RelativeAt(ship, burns[^1].at) - RelativeAt(buddy, burns[^1].at)).Length();
            double tolerance = ArrivalTolerance(buddy);
            Assert.That(separation, Is.LessThanOrEqualTo(tolerance),
                $"phase {deg}° separation {separation / 1000:F1} km, tolerance {tolerance / 1000:F1} km, arrival +{at / 3600:F1} h");
        }
    }

    enum OrbitSense { Prograde, Retrograde, Inclined5 }

    (Entity ship, Entity buddy) PairOnSameRadius(double phaseDeg, OrbitSense sense)
    {
        double radius = _earth.GetDataBlob<MassVolumeDB>().RadiusInM * 2;
        double phase = phaseDeg * Math.PI / 180.0;
        var ship = FuelledShip(CircularPosition(radius, 0), $"{sense}-ship-{phaseDeg}");
        var buddy = FuelledShip(CircularPosition(radius, phase), $"{sense}-buddy-{phaseDeg}");
        if (sense == OrbitSense.Retrograde)
        {
            FlipRetrograde(ship);
            FlipRetrograde(buddy);
        }
        else if (sense == OrbitSense.Inclined5)
        {
            SetInclination(ship, radius, 0, 5 * Math.PI / 180);
            SetInclination(buddy, radius, phase, 5 * Math.PI / 180);
        }

        Assert.That(ship.GetDataBlob<OrbitDB>().Eccentricity, Is.LessThan(0.01), sense.ToString());
        Assert.That(buddy.GetDataBlob<OrbitDB>().Eccentricity, Is.LessThan(0.01), sense.ToString());
        return (ship, buddy);
    }

    void FlyAndAssertAlongside(Entity ship, Entity buddy, string label)
    {
        Assert.That(
            MatchOrbitAction.TryPlanBurns(ship, buddy, _now, out var burns, out string reason),
            Is.True, $"{label}: {reason}");
        Assert.That(burns, Is.Not.Empty, label);
        Fly(ship, burns);
        var at = burns[^1].at;
        double separation = (RelativeAt(ship, at) - RelativeAt(buddy, at)).Length();
        double tolerance = ArrivalTolerance(buddy);
        Assert.That(separation, Is.LessThanOrEqualTo(tolerance),
            $"{label}: separation {separation / 1000:F1} km, tolerance {tolerance / 1000:F1} km");
    }

    Entity FuelledShip(Vector3 position, string name)
    {
        var ship = ShipFactory.CreateShip(_design, _faction, position, _earth, name);
        double added = CargoTransferProcessor.AddRemoveCargoMass(ship, _methalox, _fuelKg);
        Assert.That(added, Is.EqualTo(_fuelKg).Within(1), name);
        Assert.That(ship.GetDataBlob<NewtonThrustAbilityDB>().DeltaV, Is.GreaterThan(1000), name);
        return ship;
    }

    static Vector3 CircularPosition(double radius, double angle)
        => new Vector3(radius * Math.Cos(angle), radius * Math.Sin(angle), 0);

    void FlipRetrograde(Entity ship)
    {
        var orbit = ship.GetDataBlob<OrbitDB>();
        var state = OrbitalMath.GetStateVectors(orbit.GetElements(), _now);
        double myMass = ship.GetDataBlob<MassVolumeDB>().MassDry;
        double parentMass = _earth.GetDataBlob<MassVolumeDB>().MassDry;
        ship.SetDataBlob(OrbitDB.FromVector(
            _earth, myMass, parentMass,
            state.position, -((Vector3)state.velocity), _now));
    }

    void SetInclination(Entity ship, double radius, double angle, double inclination)
    {
        double myMass = ship.GetDataBlob<MassVolumeDB>().MassDry;
        double parentMass = _earth.GetDataBlob<MassVolumeDB>().MassDry;
        double sgp = GeneralMath.StandardGravitationalParameter(myMass + parentMass);
        double speed = Math.Sqrt(sgp / radius);
        var position = new Vector3(
            radius * Math.Cos(angle),
            radius * Math.Sin(angle) * Math.Cos(inclination),
            radius * Math.Sin(angle) * Math.Sin(inclination));
        var velocity = speed * new Vector3(
            -Math.Sin(angle),
            Math.Cos(angle) * Math.Cos(inclination),
            Math.Cos(angle) * Math.Sin(inclination));
        ship.SetDataBlob(OrbitDB.FromVector(_earth, myMass, parentMass, position, velocity, _now));
    }

    static void Fly(Entity ship, IReadOnlyList<(KeplerElements start, KeplerElements end, DateTime at)> burns)
    {
        foreach (var burn in burns)
        {
            var parent = ship.GetSOIParentEntity();
            Assert.That(parent, Is.Not.Null);
            var db = new NewtonSimpleMoveDB(parent!, burn.start, burn.end, burn.at);
            ship.SetDataBlob(db);

            var thrust = ship.GetDataBlob<NewtonThrustAbilityDB>();
            double mass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var start = OrbitalMath.GetStateVectors(burn.start, burn.at);
            var end = OrbitalMath.GetStateVectors(burn.end, burn.at);
            double dv = ((Vector3)end.velocity - (Vector3)start.velocity).Length();
            double fuel = dv < 0.01 ? 0 : OrbitMath.TsiolkovskyFuelUse(mass, thrust.ExhaustVelocity, dv);
            double seconds = thrust.FuelBurnRate > 0 ? fuel / thrust.FuelBurnRate + 2 : 2;
            NewtonSimpleProcessor.ProcessEntity(ship, burn.at + TimeSpan.FromSeconds(seconds));
            Assert.That(db.IsFailed, Is.False, $"burn failed, dv {dv:F0} m/s");
            Assert.That(db.IsComplete, Is.True, $"burn still running after {seconds:F0}s, dv {dv:F0} m/s");
        }
    }

    /// <summary>Same threshold MatchOrbitAction uses for "alongside".</summary>
    static double ArrivalTolerance(Entity target)
    {
        double radius = target.GetDataBlob<OrbitDB>().SemiMajorAxis;
        double close = OrbitMath.LowOrbitRadius(target) * 3.0;
        if (!double.IsFinite(radius) || radius <= 0)
            return close;
        return Math.Max(close, radius * 0.01);
    }

    static Vector3 RelativeAt(Entity entity, DateTime at)
        => OrbitMath.GetPosition(entity.GetDataBlob<OrbitDB>(), at);

    [Test]
    public void CircularCoplanar_StaysOnHohmann()
    {
        double earthR = _earth.GetDataBlob<MassVolumeDB>().RadiusInM;
        var ship = FuelledShip(CircularPosition(earthR * 2, 0), "lambert-circ-ship");
        var buddy = FuelledShip(CircularPosition(earthR * 6, 0.7), "lambert-circ-buddy");

        MoveOption newt = Newtonian(ship, buddy);
        Assert.That(newt.Feasible, Is.True, newt.Reason);
        Assert.That(newt.HasWindow, Is.False);
        Assert.That(newt.Reason, Does.Contain("Hohmann"));
    }

    [Test]
    public void EccentricTarget_UsesLambertWindow()
    {
        double earthR = _earth.GetDataBlob<MassVolumeDB>().RadiusInM;
        var ship = FuelledShip(CircularPosition(earthR * 2, 0), "lambert-ecc-ship");
        var buddy = FuelledShip(CircularPosition(earthR * 6, 0), "lambert-ecc-buddy");
        PlaceEllipse(buddy, earthR * 6, 0.3, Math.PI / 2);

        MoveOption newt = Newtonian(ship, buddy);
        Assert.That(newt.Feasible, Is.True, newt.Reason);
        Assert.That(newt.HasWindow, Is.True, newt.Reason);
        Assert.That(buddy.GetDataBlob<OrbitDB>().Eccentricity, Is.EqualTo(0.3).Within(0.02));
    }

    [Test]
    public void PlanFromWindow_BurnMagnitudesMatchTheCell()
    {
        double earthR = _earth.GetDataBlob<MassVolumeDB>().RadiusInM;
        double shipR = earthR * 2;
        double targetR = earthR * 6;
        var ship = FuelledShip(CircularPosition(shipR, 0), "lambert-plan-ship");
        var buddy = FuelledShip(CircularPosition(targetR, 1.2), "lambert-plan-buddy");
        double mu = OrbitMath.SGP(_earth, ship);
        var (tofMin, tofMax) = TransferWindows.HohmannTofBand(mu, shipR, targetR);

        TransferGrid grid = TransferPlanner.Scan(
            ship, buddy, mu, _now, _now.AddDays(2), 12, tofMin, tofMax, 8, true);
        Assert.That(grid.HasBest, Is.True);

        Assert.That(
            MovePlanner.PlanFromWindow(ship, buddy, grid.Best, _now, out var actions, out string reason),
            Is.True, reason);

        double planned = 0;
        foreach (NewtonThrustAction burn in actions)
            planned += burn.OrbitrelativeDeltaV.Length();
        Assert.That(planned, Is.EqualTo(grid.Best.TotalDeltaV).Within(grid.Best.TotalDeltaV * 0.01));

        var queue = ship.GetDataBlob<ActionQueueDB>();
        foreach (EntityAction action in actions)
            queue.Enqueue(action);
        Assert.That(queue.ActionList.OfType<NewtonThrustAction>().Count(), Is.GreaterThanOrEqualTo(2));
    }

    MoveOption Newtonian(Entity ship, Entity target)
        => MovePlanner.Evaluate(ship, target, _now).Single(option => option.Mode == MoveMode.NewtonianTransfer);

    void PlaceEllipse(Entity body, double sma, double eccentricity, double trueAnomaly)
    {
        double myMass = body.GetDataBlob<MassVolumeDB>().MassDry;
        double parentMass = _earth.GetDataBlob<MassVolumeDB>().MassDry;
        double mu = GeneralMath.StandardGravitationalParameter(myMass + parentMass);
        double p = sma * (1 - eccentricity * eccentricity);
        double radius = p / (1 + eccentricity * Math.Cos(trueAnomaly));
        double h = Math.Sqrt(mu * p);
        double vr = mu / h * eccentricity * Math.Sin(trueAnomaly);
        double vt = h / radius;
        var radial = new Vector3(Math.Cos(trueAnomaly), Math.Sin(trueAnomaly), 0);
        var tangent = new Vector3(-Math.Sin(trueAnomaly), Math.Cos(trueAnomaly), 0);
        body.SetDataBlob(OrbitDB.FromVector(
            _earth, myMass, parentMass, radial * radius, radial * vr + tangent * vt, _now));
    }
}
