using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Tests;

/// <summary>
/// ScheduleSOIEntry: a radial band, then a step that cannot jump a sphere,
/// then a rescan at the horizon instead of a one-year cap.
/// </summary>
[TestFixture]
public class SoiEntryScanTests
{
    const double Au = 149597870700.0;
    const double Year = 365.25 * 24 * 3600;

    static readonly Game Game = InitializeGame();
    StarSystem _sys;
    DateTime _epoch;
    Entity _sol;

    static Game InitializeGame()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
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
        _sys.Initialize(Game, "SoiEntry", -1);
        _sys.IncrementExternalObserver(priority: true);
        _epoch = _sys.StarSysDateTime;
        _sol = TestingUtilities.BasicSol(_sys);
    }

    [Test]
    public void FastFlyby_ThatTheEqualStepScanMisses_IsScheduled()
    {
        double sunMass = _sol.GetDataBlob<MassVolumeDB>().MassDry;
        // A 50,000 km sphere. Two days at several km/s is millions of km, so the
        // old 180-sample year walks straight past it.
        double mass = sunMass * Math.Pow(5e7 / Au, 2.5);
        var planet = AddBody(_sol, "Rock", mass, 1e6, Au, 0);
        double soi = planet.GetSOI_m();
        Assert.That(soi, Is.LessThan(1e8));

        var ship = AddShip();
        double rp = 0.5 * Au;
        double ra = 1.5 * Au;
        double a = 0.5 * (rp + ra);
        double mu = OrbitMath.SGP(_sol, ship);
        double speed = Math.Sqrt(mu * (2.0 / rp - 1.0 / a));
        Place(ship, _sol, new Vector3(rp, 0, 0), new Vector3(0, speed, 0));
        var shipOrbit = ship.GetDataBlob<OrbitDB>();

        // Meet the ship halfway between two of the old year-long samples. The sphere
        // is crossed in a couple of hours, so a two-day step walks past it.
        double step = Year / 180.0;
        DateTime cross = shipRadiusCrosses(shipOrbit, Au, _epoch);
        double seconds = (cross - _epoch).TotalSeconds;
        double midSeconds = (Math.Floor(seconds / step) + 0.5) * step;
        DateTime mid = _epoch.AddSeconds(midSeconds);
        Vector3 at = shipOrbit.GetPosition(mid);
        double meetR = at.Length();
        double angle = Math.Atan2(at.Y, at.X);
        double muPlanet = OrbitMath.SGP(_sol, planet);
        double n = Math.Sqrt(muPlanet / (meetR * meetR * meetR));
        SetOrbit(planet, meetR, 0, angle - n * midSeconds);
        Place(ship, _sol, new Vector3(rp, 0, 0), new Vector3(0, speed, 0));
        shipOrbit = ship.GetDataBlob<OrbitDB>();

        Assert.That(TryOldEqualStep(shipOrbit, planet, Year, out _), Is.False, "old 180-sample scan still hits; the fixture is not a miss");
        DateTime refined = BruteEntry(shipOrbit, planet, mid.AddDays(-3), mid.AddDays(3), TimeSpan.FromMinutes(1));
        Assert.That(shipOrbit.EntryDueAt, Is.EqualTo(refined).Within(TimeSpan.FromMinutes(1)));
        Assert.That(Queued(ship, nameof(EnterSOIProcessor)), Does.Contain(shipOrbit.EntryDueAt));
    }

    [Test]
    public void EllipseOutToJupiter_IsScheduledPastOneYear()
    {
        double sunMass = _sol.GetDataBlob<MassVolumeDB>().MassDry;
        double jupR = 5.2 * Au;
        double jupMass = 1.898e27;
        var jupiter = AddBody(_sol, "Jupiter", jupMass, 7e7, jupR, 0);
        var ship = AddShip();
        double rp = Au;
        double ra = jupR;
        double a = 0.5 * (rp + ra);
        double mu = OrbitMath.SGP(_sol, ship);
        double speed = Math.Sqrt(mu * (2.0 / rp - 1.0 / a));
        double half = Math.PI * Math.Sqrt(a * a * a / mu);
        Assert.That(half, Is.GreaterThan(Year));

        // Jupiter waits at the apoapsis point, allowing for its own motion during the coast.
        double n = Math.Sqrt(OrbitMath.SGP(_sol, jupiter) / (jupR * jupR * jupR));
        SetOrbit(jupiter, jupR, 0, Math.PI - n * half);
        Place(ship, _sol, new Vector3(rp, 0, 0), new Vector3(0, speed, 0));

        var due = ship.GetDataBlob<OrbitDB>().EntryDueAt;
        Assert.That(due, Is.GreaterThan(_epoch.AddDays(365)));
        DateTime brute = BruteEntry(ship.GetDataBlob<OrbitDB>(), jupiter, due.AddDays(-20), due.AddDays(5), TimeSpan.FromHours(6));
        brute = BruteEntry(ship.GetDataBlob<OrbitDB>(), jupiter, brute.AddHours(-12), brute.AddHours(12), TimeSpan.FromMinutes(1));
        Assert.That(due, Is.EqualTo(brute).Within(TimeSpan.FromMinutes(1)));
    }

    [Test]
    public void BandMiss_SchedulesNothing_AndDoesNotSample()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0);
        AddBody(earth, "Luna", 7.342e22, 1_737_000, 384_400_000, 0);
        var ship = AddShip();
        double r = 7_000_000;
        double mu = OrbitMath.SGP(earth, ship);
        Place(ship, earth, new Vector3(r, 0, 0), new Vector3(0, Math.Sqrt(mu / r), 0));

        Assert.That(OrbitDB.SoiEntryPositionChecks, Is.EqualTo(0));
        Assert.That(Queued(ship, nameof(EnterSOIProcessor)), Is.Empty);
        Assert.That(Queued(ship, nameof(SoiEntryRescanProcessor)), Is.Empty);
    }

    [Test]
    public void RadialOverlapOnTheWrongSide_IsNotAnEntry()
    {
        double sunMass = _sol.GetDataBlob<MassVolumeDB>().MassDry;
        double mass = sunMass * Math.Pow(5e7 / Au, 2.5);
        var planet = AddBody(_sol, "Other", mass, 1e6, Au, Math.PI);
        var ship = AddShip();
        double mu = OrbitMath.SGP(_sol, ship);
        double speed = Math.Sqrt(mu / Au);
        Place(ship, _sol, new Vector3(Au, 0, 0), new Vector3(0, speed, 0));

        Assert.That(OrbitDB.SoiEntryPositionChecks, Is.GreaterThan(0));
        Assert.That(ship.GetDataBlob<OrbitDB>().EntryDueAt, Is.EqualTo(default(DateTime)));
        Assert.That(Queued(ship, nameof(EnterSOIProcessor)), Is.Empty);
        // Same period: the horizon is the ten-year cap, and the miss asks to look again.
        Assert.That(Queued(ship, nameof(SoiEntryRescanProcessor)), Is.Not.Empty);
    }

    [Test]
    public void SiblingThatCrossesTheEllipseLater_MatchesAMinuteScan()
    {
        double sunMass = _sol.GetDataBlob<MassVolumeDB>().MassDry;
        // The body's sphere only reaches the ship's radius near apoapsis, half an orbit later.
        double shipR = 1.5 * Au;
        double bodyPeri = 0.5 * Au;
        double bodyApo = shipR;
        double bodyA = 0.5 * (bodyPeri + bodyApo);
        double bodyE = (bodyApo - bodyPeri) / (bodyApo + bodyPeri);
        double mass = sunMass * Math.Pow(2e7 / bodyA, 2.5);
        var body = AddBody(_sol, "Sweeper", mass, 1e6, bodyA, 0);
        double muBody = OrbitMath.SGP(_sol, body);
        double half = Math.PI * Math.Sqrt(bodyA * bodyA * bodyA / muBody);

        var ship = AddShip();
        double mu = OrbitMath.SGP(_sol, ship);
        double nShip = Math.Sqrt(mu / (shipR * shipR * shipR));
        // Mean anomaly 0 is periapsis now. Apoapsis, half a period later, points at the ship.
        double argpe = nShip * half - Math.PI;
        SetElements(body, _sol, bodyA, bodyE, argpe, 0);
        Assert.That(body.GetSOI_m(), Is.LessThan(shipR * 0.05));

        Place(ship, _sol, new Vector3(shipR, 0, 0), new Vector3(0, Math.Sqrt(mu / shipR), 0));

        DateTime around = _epoch.AddSeconds(half);
        DateTime brute = BruteEntry(ship.GetDataBlob<OrbitDB>(), body, around.AddDays(-20), around.AddDays(5), TimeSpan.FromHours(1));
        brute = BruteEntry(ship.GetDataBlob<OrbitDB>(), body, brute.AddHours(-2), brute.AddHours(2), TimeSpan.FromMinutes(1));
        Assert.That(ship.GetDataBlob<OrbitDB>().EntryDueAt, Is.EqualTo(brute).Within(TimeSpan.FromMinutes(1)));
        Assert.That((brute - _epoch).TotalDays, Is.GreaterThan(150));
    }

    [Test]
    public void EmptyScan_Rescans_AndTheSecondPassFindsTheLaterCrossing()
    {
        double sunMass = _sol.GetDataBlob<MassVolumeDB>().MassDry;
        // Half a 22-year ellipse is about 11 years, past the ten-year horizon.
        double a = Math.Pow(22.0, 2.0 / 3.0) * Au;
        double rp = Au;
        double ra = 2 * a - rp;
        double mass = sunMass * Math.Pow(5e9 / ra, 2.5);
        var planet = AddBody(_sol, "Far", mass, 1e6, ra, 0);
        var ship = AddShip();
        double mu = OrbitMath.SGP(_sol, ship);
        double speed = Math.Sqrt(mu * (2.0 / rp - 1.0 / a));
        double half = Math.PI * Math.Sqrt(a * a * a / mu);
        Assert.That(half / Year, Is.GreaterThan(10));

        double n = Math.Sqrt(OrbitMath.SGP(_sol, planet) / (ra * ra * ra));
        SetOrbit(planet, ra, 0, Math.PI - n * half);
        Place(ship, _sol, new Vector3(rp, 0, 0), new Vector3(0, speed, 0));

        var first = ship.GetDataBlob<OrbitDB>();
        Assert.That(first.EntryDueAt, Is.EqualTo(default(DateTime)));
        Assert.That(first.EntryRescanAt, Is.GreaterThan(_epoch.AddDays(365 * 9)));
        var horizon = Queued(ship, nameof(SoiEntryRescanProcessor));
        Assert.That(horizon, Does.Contain(first.EntryRescanAt));

        Game.ProcessorManager.GetInstanceProcessor(nameof(SoiEntryRescanProcessor))
            .ProcessEntity(ship, first.EntryRescanAt);
        var second = ship.GetDataBlob<OrbitDB>();
        Assert.That(second.EntryDueAt, Is.GreaterThan(first.EntryRescanAt));
        Assert.That((second.EntryDueAt - _epoch).TotalDays / 365.25, Is.EqualTo(half / Year).Within(0.5));
    }

    [Test]
    public void LeoEllipseToLuna_MatchesTheOldScanAndAMinuteSweep()
    {
        var earth = AddBody(_sol, "Earth", 5.972e24, 6_371_000, Au, 0);
        double lunaR = 384_400_000;
        var luna = AddBody(earth, "Luna", 7.342e22, 1_737_000, lunaR, 0);
        var ship = AddShip();
        double rp = 7_008_000;
        double a = 0.5 * (rp + lunaR);
        double mu = OrbitMath.SGP(earth, ship);
        double speed = Math.Sqrt(mu * (2.0 / rp - 1.0 / a));

        // Luna at the apoapsis point when the ship arrives, plus Luna's own motion.
        double half = Math.PI * Math.Sqrt(a * a * a / mu);
        double n = luna.GetDataBlob<OrbitDB>().MeanMotion;
        SetOrbit(luna, lunaR, 0, Math.PI - n * half);
        Place(ship, earth, new Vector3(rp, 0, 0), new Vector3(0, speed, 0));

        var orbit = ship.GetDataBlob<OrbitDB>();
        double scan = Math.Min(orbit.OrbitalPeriod.TotalSeconds, Year);
        Assert.That(TryOldEqualStep(orbit, luna, scan, out DateTime oldHit), Is.True);
        DateTime brute = BruteEntry(orbit, luna, oldHit.AddDays(-2), oldHit.AddDays(1), TimeSpan.FromMinutes(1));
        Assert.That(orbit.EntryDueAt, Is.EqualTo(brute).Within(TimeSpan.FromMinutes(1)));
        Assert.That(orbit.EntryDueAt, Is.EqualTo(oldHit).Within(TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Phobos's Laplace sphere sits inside the moon. A ship that crosses that
    /// geometric sphere stays on Mars: the scan does not schedule entry, an
    /// entry interrupt does not reparent, and a newton step does not either.
    /// </summary>
    [Test]
    public void PhobosSphereInsideTheBody_IsNotEntered()
    {
        var phobosData = LoadedPhobos();
        double radius = phobosData.RadiusInM;
        double soi = phobosData.SoiM;
        TestContext.WriteLine(
            $"Phobos radius_m={radius:G17} soi_m={soi:G17} sma_m={phobosData.SmaM:G17} " +
            $"mass_kg={phobosData.MassKg:G17} marsMass_kg={phobosData.MarsMassKg:G17}");
        Assert.That(radius, Is.EqualTo(Distance.AuToMt(Distance.KmToAU(11.1))),
            "runtime radius is the mars.json Radius (km) through KmToAU / AuToM");
        Assert.That(phobosData.MassKg, Is.EqualTo(1.07e16));
        Assert.That(phobosData.MarsMassKg, Is.EqualTo(0.64174e24));
        Assert.That(phobosData.SmaM, Is.EqualTo(9376.0 * 1000.0));
        Assert.That(soi, Is.EqualTo(OrbitMath.GetSOI(phobosData.SmaM, phobosData.MassKg, phobosData.MarsMassKg)));
        Assert.That(soi, Is.LessThanOrEqualTo(radius));
        Assert.That(phobosData.HasExterior, Is.False);

        var mars = AddBody(_sol, "Mars", phobosData.MarsMassKg, phobosData.MarsRadiusInM, phobosData.MarsSmaM, 0);
        var phobos = AddBody(mars, "Phobos", phobosData.MassKg, radius, phobosData.SmaM, 0);
        Assert.That(phobos.GetSOI_m(), Is.EqualTo(soi));
        Assert.That(phobos.HasExteriorSoi(), Is.False);
        Assert.That(mars.HasExteriorSoi(), Is.True);

        var phobosOrbit = phobos.GetDataBlob<OrbitDB>();
        var phobosPos = phobosOrbit.GetPosition(_epoch);
        var phobosVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(phobosOrbit, _epoch);
        var outDir = Vector3.Normalise(new Vector3(-phobosPos.Y, phobosPos.X, 0));

        // 15 km out is outside the body and outside the sphere. 50 m/s inward
        // crosses the sphere in a few minutes, slowly enough that the entry
        // scan's 60 s step cannot step over it.
        const double startGap = 15_000;
        const double closeSpeed = 50;
        var ship = AddShip();
        Place(ship, mars, phobosPos + outDir * startGap, phobosVel - outDir * closeSpeed);

        var shipOrbit = ship.GetDataBlob<OrbitDB>();
        double startDist = (shipOrbit.GetPosition(_epoch) - phobosPos).Length();
        Assert.That(startDist, Is.GreaterThan(radius));
        Assert.That(shipOrbit.EntryDueAt, Is.EqualTo(default(DateTime)));
        Assert.That(Queued(ship, nameof(EnterSOIProcessor)), Is.Empty);
        Assert.That(Queued(ship, nameof(SoiEntryRescanProcessor)), Is.Empty);

        double min = double.MaxValue;
        DateTime insideAt = default;
        for (int s = 0; s <= 2000; s += 10)
        {
            var t = _epoch.AddSeconds(s);
            double dist = (shipOrbit.GetPosition(t) - phobosOrbit.GetPosition(t)).Length();
            if (dist < min)
                min = dist;
            if (insideAt == default && dist < soi)
                insideAt = t;
        }
        Assert.That(min, Is.LessThan(soi),
            "the coast does pass through the geometric sphere; the scan must be what refused it");
        Assert.That(min, Is.LessThan(radius), "that pass is inside the body");
        Assert.That(ship.GetDataBlob<PositionDB>().Parent, Is.SameAs(mars));
        Assert.That(shipOrbit.Parent, Is.SameAs(mars));

        shipOrbit.EntryDueAt = insideAt;
        Game.ProcessorManager.GetInstanceProcessor(nameof(EnterSOIProcessor))
            .ProcessEntity(ship, insideAt);
        Assert.That(ship.GetDataBlob<PositionDB>().Parent, Is.SameAs(mars),
            "an entry interrupt while inside the buried sphere must not reparent");
        Assert.That(ship.GetDataBlob<OrbitDB>().Parent, Is.SameAs(mars));
        Assert.That(ship.GetDataBlob<OrbitDB>().GravitationalParameter_m3S2,
            Is.EqualTo(OrbitMath.SGP(mars, ship)).Within(1e-6));

        // Same geometry under thrust integration: already inside the sphere, coast one second.
        var newton = AddShip();
        newton.FactionOwnerID = FactionFactory.CreateFaction(Game, "phobos-soi").Id;
        var insidePos = phobosPos + outDir * (soi * 0.5);
        Place(newton, mars, insidePos, phobosVel);
        Assert.That((newton.GetDataBlob<PositionDB>().RelativePosition - phobosPos).Length(), Is.LessThan(soi));
        var thrust = new NewtonThrustAbilityDB("methalox");
        thrust.ExhaustVelocity = 3000;
        thrust.FuelBurnRate = 0;
        thrust.ThrustInNewtons = 1;
        newton.SetDataBlob(thrust);
        var move = new NewtonMoveDB(mars, phobosVel);
        newton.SetDataBlob(move);
        NewtonionMovementProcessor.NewtonMove(move, _epoch.AddSeconds(1));
        Assert.That(newton.GetDataBlob<PositionDB>().Parent, Is.SameAs(mars));
        Assert.That(newton.HasDataBlob<OrbitDB>()
            ? newton.GetDataBlob<OrbitDB>().Parent
            : newton.GetDataBlob<NewtonMoveDB>().SOIParent, Is.SameAs(mars));
    }

    struct PhobosNumbers
    {
        public double RadiusInM;
        public double SoiM;
        public double SmaM;
        public double MassKg;
        public double MarsMassKg;
        public double MarsRadiusInM;
        public double MarsSmaM;
        public bool HasExterior;
    }

    static PhobosNumbers LoadedPhobos()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        var sol = game.Systems.Single(system => system.ID == "system-sol");
        var phobos = sol.GetAllEntitiesWithDataBlob<OrbitDB>()
            .Single(body => body.GetDefaultName() == "Phobos");
        var mars = phobos.GetSOIParentEntity();
        var phobosOrbit = phobos.GetDataBlob<OrbitDB>();
        var marsOrbit = mars.GetDataBlob<OrbitDB>();
        return new PhobosNumbers
        {
            RadiusInM = phobos.GetDataBlob<MassVolumeDB>().RadiusInM,
            SoiM = phobos.GetSOI_m(),
            SmaM = phobosOrbit.SemiMajorAxis,
            MassKg = phobos.GetDataBlob<MassVolumeDB>().MassDry,
            MarsMassKg = mars.GetDataBlob<MassVolumeDB>().MassDry,
            MarsRadiusInM = mars.GetDataBlob<MassVolumeDB>().RadiusInM,
            MarsSmaM = marsOrbit.SemiMajorAxis,
            HasExterior = phobos.HasExteriorSoi(),
        };
    }

    /// <summary>The previous 180-equal-sample scan, kept here so a regression can see the miss.</summary>
    static bool TryOldEqualStep(OrbitDB ship, Entity child, double scanSeconds, out DateTime inside)
    {
        inside = default;
        var childOrbit = child.GetDataBlob<OrbitDB>();
        double soi = child.GetSOI_m();
        int samples = 180;
        double step = scanSeconds / samples;
        DateTime epoch = ship.Epoch;
        bool wasOutside = (ship.GetPosition(epoch) - childOrbit.GetPosition(epoch)).Length() >= soi;
        for (int i = 1; i <= samples; i++)
        {
            var t = epoch.AddSeconds(i * step);
            double dist = (ship.GetPosition(t) - childOrbit.GetPosition(t)).Length();
            if (dist < soi)
            {
                if (!wasOutside)
                    return false;
                var outsideAt = epoch.AddSeconds((i - 1) * step);
                inside = BisectOld(ship, childOrbit, soi, outsideAt, t);
                return true;
            }
            wasOutside = true;
        }
        return false;
    }

    /// <summary>The old scheduler's 20-step bisection. It schedules the inside edge.</summary>
    static DateTime BisectOld(OrbitDB ship, OrbitDB childOrbit, double soi, DateTime lo, DateTime hi)
    {
        for (int iter = 0; iter < 20; iter++)
        {
            var mid = new DateTime(lo.Ticks / 2 + hi.Ticks / 2);
            double dist = (ship.GetPosition(mid) - childOrbit.GetPosition(mid)).Length();
            if (dist < soi)
                hi = mid;
            else
                lo = mid;
        }
        return hi;
    }

    static DateTime BruteEntry(OrbitDB ship, Entity child, DateTime from, DateTime to, TimeSpan step)
    {
        var childOrbit = child.GetDataBlob<OrbitDB>();
        double soi = child.GetSOI_m();
        bool outside = (ship.GetPosition(from) - childOrbit.GetPosition(from)).Length() >= soi;
        for (var t = from + step; t <= to; t += step)
        {
            double dist = (ship.GetPosition(t) - childOrbit.GetPosition(t)).Length();
            if (outside && dist < soi)
                return t;
            outside = dist >= soi;
        }
        Assert.Fail($"no entry between {from:u} and {to:u}, step {step}");
        return default;
    }

    static DateTime shipRadiusCrosses(OrbitDB ship, double radius, DateTime notBefore)
    {
        var t = ship.Epoch > notBefore ? ship.Epoch : notBefore;
        double prev = ship.GetPosition(t).Length();
        for (int i = 0; i < 24 * 400; i++)
        {
            t = t.AddHours(1);
            double r = ship.GetPosition(t).Length();
            if (prev < radius && r >= radius)
                return t;
            prev = r;
        }
        Assert.Fail("ship never reached the radius");
        return default;
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
        SetElements(body, parent, sma, 0, 0, meanAnomaly);
        return body;
    }

    void SetOrbit(Entity body, double sma, double eccentricity, double meanAnomaly)
    {
        SetElements(body, ParentOf(body) ?? _sol, sma, eccentricity, 0, meanAnomaly);
    }

    void SetElements(Entity body, Entity parent, double sma, double eccentricity, double argumentOfPeriapsis, double meanAnomaly)
    {
        double parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
        var orbit = OrbitDB.FromAsteroidFormat_r(
            parent, parentMass, body.GetDataBlob<MassVolumeDB>().MassDry,
            sma, eccentricity, 0, 0, argumentOfPeriapsis, meanAnomaly, _epoch);
        body.SetDataBlob(orbit);
        body.GetDataBlob<PositionDB>().SetParent(parent);
        body.GetDataBlob<PositionDB>().RelativePosition = orbit.GetPosition(_epoch);
    }

    Entity ParentOf(Entity body)
    {
        if (body.TryGetDataBlob<OrbitDB>(out var orbit) && orbit.Parent != null)
            return orbit.Parent;
        if (body.TryGetDataBlob<PositionDB>(out var pos) && pos.Parent != null)
            return pos.Parent;
        return null;
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

    void Place(Entity ship, Entity parent, Vector3 relative, Vector3 velocity)
    {
        var pos = ship.GetDataBlob<PositionDB>();
        pos.SetParent(parent);
        pos.RelativePosition = relative;
        var parentAbs = parent.TryGetDataBlob<OrbitDB>(out _)
            ? (Vector3)Pulsar4X.Movement.MoveMath.GetAbsoluteFuturePosition(parent, _epoch)
            : parent.GetDataBlob<PositionDB>().AbsolutePosition;
        pos.AbsolutePosition = parentAbs + relative;
        ship.SetDataBlob(OrbitDB.FromVelocity(parent, ship, velocity, _epoch));
    }

    static List<DateTime> Queued(Entity ship, string processor)
    {
        var times = new List<DateTime>();
        foreach (var item in ship.Manager.ManagerSubpulses.InstanceProcessorsQueue)
        {
            if (item.Item.Item1 == processor && item.Item.Item2 == ship)
                times.Add(item.Time);
        }
        return times;
    }
}
