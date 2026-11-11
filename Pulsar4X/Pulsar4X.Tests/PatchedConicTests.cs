using System;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Tests;

/// <summary>
/// Patched-conic frames and the one-level planner. A chain of parent-relative states
/// is the absolute state, a sphere exit is the sum ChangeSOIProcessor publishes, and
/// Earth to Mars is the Hohmann hand sum.
/// </summary>
[TestFixture]
public class PatchedConicTests
{
    const double Au = 149597870700.0;

    static readonly Game Game = InitializeGame();
    StarSystem _sys;
    DateTime _epoch;
    Entity _sol;

    static Game InitializeGame()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings(), store);
        game.Settings.EnforceSingleThread = true;
        return game;
    }

    [SetUp]
    public void SetUp()
    {
        foreach (var sys in Game.Systems)
            sys.SetActivityState(SystemActivityState.Stasis);
        _sys = new StarSystem();
        _sys.Initialize(Game, "PatchedConic", -1);
        _sys.IncrementExternalObserver(priority: true);
        _epoch = _sys.StarSysDateTime;
        _sol = TestingUtilities.BasicSol(_sys);
    }

    [Test]
    public void EarthToSun_MatchesAbsolutePosition_WithinAMetre()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0.4);
        var ship = AddShip();
        double r = 7_008_000;
        double mu = OrbitMath.SGP(earth, ship);
        Place(ship, earth, new Vector3(r, 0, 0), new Vector3(0, Math.Sqrt(mu / r), 0));

        var when = _epoch.AddDays(10);
        var state = PatchedConic.StateRelativeToAncestor(ship, _sol, when);
        var absolute = (Vector3)MoveMath.GetAbsoluteFuturePosition(ship, when);
        Assert.That((state.pos - absolute).Length(), Is.LessThan(1));
    }

    [Test]
    public void EarthLeoToMars_MatchesTheHohmannHandSum()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0);
        var mars = AddBody(_sol, "Mars", 6.39e23, 3_390_000, 1.524 * Au, 0.8);
        var ship = ParkCircular(earth, 7_008_000);

        var plan = PatchedConicPlanner.TryPlan(ship, mars, _epoch, 20_000);
        double days = (plan.ArriveAt - plan.DepartAt).TotalDays;
        Assert.That(plan.Feasible, Is.True, plan.Reason);
        Assert.That(plan.TotalDeltaV, Is.EqualTo(5600).Within(5600 * 0.03),
            $"Δv {plan.TotalDeltaV:N0} over {days:N0} days, vInf {plan.VInf.Length():N0}");
        Assert.That(days, Is.InRange(240, 280));

        var earthAtExit = PatchedConic.StateRelativeToAncestor(earth, _sol, plan.ExitAt);
        Assert.That(Vector3.Dot(plan.VInf, earthAtExit.vel), Is.GreaterThan(0));
    }

    [Test]
    public void MarsToEarth_ConvergesRetrograde()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0.4);
        var mars = AddBody(_sol, "Mars", 6.39e23, 3_390_000, 1.524 * Au, 1.2);
        var ship = ParkCircular(mars, 3_729_000);

        var plan = PatchedConicPlanner.TryPlan(ship, earth, _epoch, 20_000);
        Assert.That(plan.Feasible, Is.True, plan.Reason);
        double days = (plan.ArriveAt - plan.DepartAt).TotalDays;
        Assert.That(days, Is.InRange(240, 280), plan.Reason);

        var marsAtExit = PatchedConic.StateRelativeToAncestor(mars, _sol, plan.ExitAt);
        Assert.That(Vector3.Dot(plan.VInf, marsAtExit.vel), Is.LessThan(0));
    }

    [Test]
    public void EarthToVenus_EscapeIsRetrograde()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0.2);
        var venus = AddBody(_sol, "Venus", 4.867e24, 6_051_800, 0.723 * Au, 2.0);
        var ship = ParkCircular(earth, 7_008_000);

        var plan = PatchedConicPlanner.TryPlan(ship, venus, _epoch, 20_000);
        Assert.That(plan.Feasible, Is.True, plan.Reason);

        var earthAtExit = PatchedConic.StateRelativeToAncestor(earth, _sol, plan.ExitAt);
        Assert.That(Vector3.Dot(plan.VInf, earthAtExit.vel), Is.LessThan(0));
    }

    [Test]
    public void ShortBudget_RefusesWithNeedsAndHave()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0);
        var mars = AddBody(_sol, "Mars", 6.39e23, 3_390_000, 1.524 * Au, 0.8);
        var ship = ParkCircular(earth, 7_008_000);

        var plan = PatchedConicPlanner.TryPlan(ship, mars, _epoch, 1000);
        Assert.That(plan.Feasible, Is.False);
        Assert.That(plan.Reason, Does.Contain("needs").And.Contain("have").And.Contain("m/s"));
    }

    [Test]
    public void MoonToPlanet_PlanFromWindow_StaysADifferentParentRefusal()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0);
        var mars = AddBody(_sol, "Mars", 6.39e23, 3_390_000, 1.524 * Au, 0.8);
        var luna = AddBody(earth, "Luna", 7.342e22, 1_737_000, 384_400_000, 0.4);
        var probe = ParkCircular(luna, 2_000_000);
        GiveThrust(probe);
        var window = new TransferWindow(
            _epoch.AddDays(10), _epoch.AddDays(260),
            new Vector3(3000, 0, 0), new Vector3(-2500, 0, 0),
            5500, 3000, 2500);

        Assert.That(
            MovePlanner.PlanFromWindow(probe, mars, window, _epoch, out _, out string reason),
            Is.False);
        Assert.That(reason, Does.Contain("different SOI parent"));
    }

    [Test]
    public void HintedWindow_PlansEarthToMars_AndQueuesOneAction()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0);
        var mars = AddBody(_sol, "Mars", 6.39e23, 3_390_000, 1.524 * Au, 0.8);
        var ship = ParkCircular(earth, 7_008_000);
        GiveThrust(ship);

        double mu = OrbitMath.SGP(_sol, ship);
        double muPlanet = OrbitMath.SGP(earth, ship);
        double muMars = OrbitMath.SGP(mars, ship);
        double rp = OrbitMath.LowOrbitRadius(mars);
        var band = TransferWindows.HohmannTofBand(mu, Au, 1.524 * Au);
        TransferGrid grid = TransferWindows.Scan(
            mu,
            t => PatchedConic.StateRelativeToAncestor(earth, _sol, t),
            t => PatchedConic.StateRelativeToAncestor(mars, _sol, t),
            _epoch, _epoch.AddDays(780), 15,
            band.tofMin, band.tofMax, 11,
            (v, _) =>
            {
                double s = v.Length();
                return s > 1 ? OrbitalMath.EscapeBurn(muPlanet, 7_008_000, s) : double.NaN;
            },
            (v, _) =>
            {
                double s = v.Length();
                return s > 1 ? OrbitalMath.CaptureBurn(muMars, s, rp) : double.NaN;
            },
            true);
        Assert.That(grid.HasBest, Is.True);

        var hinted = PatchedConicPlanner.TryPlanFromWindow(ship, mars, _epoch, 30_000, grid.Best);
        Assert.That(hinted.Feasible, Is.True, hinted.Reason);
        Assert.That(hinted.TotalDeltaV, Is.InRange(4000, 9000), hinted.Reason);
        Assert.That((hinted.ArriveAt - hinted.DepartAt).TotalDays, Is.InRange(120, 420));

        Assert.That(
            MovePlanner.PlanFromWindow(ship, mars, grid.Best, _epoch, out var actions, out string reason),
            Is.True, reason);
        Assert.That(actions, Has.Count.EqualTo(1));
        Assert.That(actions[0], Is.InstanceOf<SoiTransferAction>());
        Assert.That(reason, Does.Contain("Transfer to Mars").And.Contain("depart "));
    }

    [Test]
    public void RandomDates_ConvergeOrFailWithAReason()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0);
        var mars = AddBody(_sol, "Mars", 6.39e23, 3_390_000, 1.524 * Au, 0.3);
        var ship = ParkCircular(earth, 7_008_000);
        var rng = new Random(1);

        for (int i = 0; i < 100; i++)
        {
            var now = _epoch.AddDays(rng.NextDouble() * 800);
            var plan = PatchedConicPlanner.TryPlan(ship, mars, now, 50_000);
            Assert.That(plan.Feasible || !string.IsNullOrEmpty(plan.Reason), Is.True, $"date {i}");
        }
    }

    [Test]
    public void LunaChain_SumsToTheSunFrame_WithinAMetre()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0.2);
        var luna = AddBody(earth, "Luna", 7.342e22, 1_737_000, 384_400_000, 1.1);
        var when = _epoch.AddDays(3);

        var lunaSun = PatchedConic.StateRelativeToAncestor(luna, _sol, when);
        var lunaEarth = PatchedConic.StateRelativeToAncestor(luna, earth, when);
        var earthSun = PatchedConic.StateRelativeToAncestor(earth, _sol, when);
        Assert.That((lunaEarth.pos + earthSun.pos - lunaSun.pos).Length(), Is.LessThan(1));
        Assert.That((lunaEarth.vel + earthSun.vel - lunaSun.vel).Length(), Is.LessThan(1e-6));

        luna.GetDataBlob<PositionDB>().MoveType = PositionDB.MoveTypes.Orbit;
        var absolute = (Vector3)MoveMath.GetAbsoluteFuturePosition(luna, when);
        Assert.That((lunaSun.pos - absolute).Length(), Is.LessThan(1));
    }

    [Test]
    public void CommonAncestor_IsOneLevelForPlanets_AndNotForAMoon()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0);
        var mars = AddBody(_sol, "Mars", 6.39e23, 3_390_000, 1.524 * Au, 0.8);
        var luna = AddBody(earth, "Luna", 7.342e22, 1_737_000, 384_400_000, 0);
        var ship = AddShip();
        double r = 7_008_000;
        double mu = OrbitMath.SGP(earth, ship);
        Place(ship, earth, new Vector3(r, 0, 0), new Vector3(0, Math.Sqrt(mu / r), 0));

        Assert.That(PatchedConic.FindCommonAncestor(ship, mars), Is.EqualTo(_sol));
        Assert.That(PatchedConic.IsOneLevelTransfer(ship, mars), Is.True);
        Assert.That(PatchedConic.FindCommonAncestor(ship, luna), Is.EqualTo(earth));
        Assert.That(PatchedConic.IsOneLevelTransfer(ship, luna), Is.False);

        var probe = AddShip();
        double lr = 2_000_000;
        double lmu = OrbitMath.SGP(luna, probe);
        Place(probe, luna, new Vector3(lr, 0, 0), new Vector3(0, Math.Sqrt(lmu / lr), 0));
        Assert.That(PatchedConic.FindCommonAncestor(probe, mars), Is.EqualTo(_sol));
        Assert.That(PatchedConic.IsOneLevelTransfer(probe, mars), Is.False);
    }

    [Test]
    public void ExitState_MatchesTheProcessorSum_AtTheSphere()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 1.2);
        var ship = AddShip();
        double rp = 7_008_000;
        double vInf = 3_000;
        double mu = OrbitMath.SGP(earth, ship);
        var (e, a, _, _) = OrbitalMath.HyperbolaForPeriapsis(mu, vInf, rp);
        var escape = new KeplerElements
        {
            SemiMajorAxis = a,
            Eccentricity = e,
            Epoch = _epoch,
            MeanAnomalyAtEpoch = 0,
            TrueAnomalyAtEpoch = 0,
            StandardGravParameter = mu,
        };
        double shipMass = ship.GetDataBlob<MassVolumeDB>().MassDry;

        Assert.That(PatchedConic.ExitStateAtSphere(escape, earth, shipMass, out DateTime at, out Vector3 pos, out Vector3 vel), Is.True);

        var hyperbola = OrbitDB.FromKeplerElements(earth, shipMass, escape, _epoch);
        var parentOrbit = earth.GetDataBlob<OrbitDB>();
        Vector3 expectPos = parentOrbit.GetPosition(at) + hyperbola.GetPosition(at);
        Vector3 expectVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(parentOrbit, at)
                            + OrbitMath.InstantaneousOrbitalVelocityVector_m(hyperbola, at);
        Assert.That((pos - expectPos).Length(), Is.LessThan(1));
        Assert.That((vel - expectVel).Length(), Is.LessThan(1e-4));
        Assert.That((pos - parentOrbit.GetPosition(at)).Length(), Is.EqualTo(earth.GetSOI_m()).Within(1));
        Assert.That(at, Is.GreaterThan(_epoch));
    }

    Entity AddBody(Entity parent, string name, double mass, double radius, double sma, double meanAnomaly)
    {
        var body = Entity.Create();
        _sys.AddEntity(body, new BaseDataBlob[]
        {
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(mass, radius),
            new SystemBodyInfoDB(),
            new NameDB(name),
        });
        double parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
        var orbit = OrbitDB.FromAsteroidFormat_r(
            parent, parentMass, mass, sma, 0, 0, 0, 0, meanAnomaly, _epoch);
        body.SetDataBlob(orbit);
        var pos = body.GetDataBlob<PositionDB>();
        pos.SetParent(parent);
        pos.RelativePosition = orbit.GetPosition(_epoch);
        pos.MoveType = PositionDB.MoveTypes.Orbit;
        return body;
    }

    Entity AddShip()
    {
        var ship = Entity.Create();
        _sys.AddEntity(ship, new BaseDataBlob[]
        {
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            new NameDB("probe"),
        });
        return ship;
    }

    Entity ParkCircular(Entity parent, double radius)
    {
        var ship = AddShip();
        double mu = OrbitMath.SGP(parent, ship);
        Place(ship, parent, new Vector3(radius, 0, 0), new Vector3(0, Math.Sqrt(mu / radius), 0));
        return ship;
    }

    void GiveThrust(Entity ship)
    {
        var thrust = new NewtonThrustAbilityDB("test-fuel")
        {
            ThrustInNewtons = 1_000_000,
            ExhaustVelocity = 10_000,
            FuelBurnRate = 100,
        };
        double dry = ship.GetDataBlob<MassVolumeDB>().MassDry;
        double wet = dry * Math.Exp(2.5);
        thrust.SetFuel(wet - dry, wet);
        ship.SetDataBlob(thrust);
    }

    void Place(Entity ship, Entity parent, Vector3 relative, Vector3 velocity)
    {
        var pos = ship.GetDataBlob<PositionDB>();
        pos.SetParent(parent);
        pos.RelativePosition = relative;
        var parentAbs = parent.TryGetDataBlob<OrbitDB>(out _)
            ? (Vector3)MoveMath.GetAbsoluteFuturePosition(parent, _epoch)
            : parent.GetDataBlob<PositionDB>().AbsolutePosition;
        pos.AbsolutePosition = parentAbs + relative;
        pos.MoveType = PositionDB.MoveTypes.Orbit;
        ship.SetDataBlob(OrbitDB.FromVelocity(parent, ship, velocity, _epoch));
    }
}
