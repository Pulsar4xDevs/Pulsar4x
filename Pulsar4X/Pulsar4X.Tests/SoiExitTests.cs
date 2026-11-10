using System;
using System.Linq;
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
/// A bound ellipse that crosses its parent's sphere changes frame at that crossing.
/// One that stays inside, including a capture ellipse, does not.
/// </summary>
[TestFixture]
public class SoiExitTests
{
    static readonly Game Game = InitializeGame();
    StarSystem _sys;
    DateTime _epoch;
    Entity _sol;
    Entity _planet;

    static Game InitializeGame()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/sol-default/scenarioInfo.json", store);
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
        _sys.Initialize(Game, "SoiExit", -1);
        _sys.IncrementExternalObserver(priority: true);
        _epoch = _sys.StarSysDateTime;
        _sol = TestingUtilities.BasicSol(_sys);
        _planet = AddPlanet(_sol, "Earth", 5.972e24, 6_371_000, 1.0);
    }

    [TestCase(0)]
    [TestCase(5)]
    [TestCase(180)]
    public void EllipseThatCrosses_ChangesParentAtTheSphere(double inclinationDeg)
    {
        double soi = _planet.GetSOI_m();
        var ship = AddShip();
        SetEllipse(ship, 1.2 * soi, inclinationDeg * Math.PI / 180.0);

        var orbit = ship.GetDataBlob<OrbitDB>();
        Assert.That(orbit.Eccentricity, Is.LessThan(1));
        Assert.That(orbit.Apoapsis, Is.GreaterThan(soi));

        var exit = QueuedExit(ship);
        var predicted = OrbitMath.TimeToRadius(orbit, soi);
        Assert.That(exit, Is.EqualTo(predicted).Within(TimeSpan.FromSeconds(1)));
        double atExit = orbit.GetPosition(exit).Length();
        Assert.That(atExit, Is.EqualTo(soi).Within(soi * 1e-4));

        var processor = Game.ProcessorManager.GetInstanceProcessor(nameof(ChangeSOIProcessor));
        processor.ProcessEntity(ship, exit - TimeSpan.FromHours(1));
        Assert.That(ship.GetSOIParentEntity(), Is.EqualTo(_planet), "still inside an hour early");

        processor.ProcessEntity(ship, exit);
        Assert.That(ship.GetSOIParentEntity(), Is.EqualTo(_sol));
        Assert.That(ship.HasDataBlob<OrbitDB>(), Is.True);
    }

    [Test]
    public void EllipseInsideNinetyFivePercent_DoesNotScheduleAnExit()
    {
        double soi = _planet.GetSOI_m();
        var ship = AddShip();
        SetEllipse(ship, 0.95 * soi, 0);
        Assert.That(ship.GetDataBlob<OrbitDB>().Apoapsis, Is.LessThan(soi));
        Assert.That(QueuedExitOrNull(ship), Is.Null);
        Assert.That(ship.GetSOIParentEntity(), Is.EqualTo(_planet));
    }

    [Test]
    public void CaptureEllipse_StaysInFrameForAYear()
    {
        double soi = _planet.GetSOI_m();
        var ship = AddShip();
        SetEllipse(ship, OrbitMath.SoiCaptureFraction * soi, 0);
        Assert.That(ship.GetDataBlob<OrbitDB>().Apoapsis, Is.LessThan(soi));
        Assert.That(QueuedExitOrNull(ship), Is.Null);

        var processor = Game.ProcessorManager.GetInstanceProcessor(nameof(ChangeSOIProcessor));
        processor.ProcessEntity(ship, _epoch + TimeSpan.FromDays(365));
        Assert.That(ship.GetSOIParentEntity(), Is.EqualTo(_planet));
        Assert.That(ship.GetDataBlob<OrbitDB>().Eccentricity, Is.LessThan(1));
    }

    [Test]
    public void Hyperbola_StillExits()
    {
        var ship = AddShip();
        double rp = OrbitMath.LowOrbitRadius(_planet);
        double mu = OrbitMath.SGP(_planet, ship);
        Place(ship, rp, Math.Sqrt(2.0 * mu / rp) * 1.05, 0);
        var orbit = ship.GetDataBlob<OrbitDB>();
        Assert.That(orbit.Eccentricity, Is.GreaterThanOrEqualTo(1));

        var exit = QueuedExit(ship);
        Assert.That(exit, Is.EqualTo(OrbitMath.TimeToRadius(orbit, _planet.GetSOI_m())).Within(TimeSpan.FromSeconds(1)));

        Game.ProcessorManager.GetInstanceProcessor(nameof(ChangeSOIProcessor)).ProcessEntity(ship, exit);
        Assert.That(ship.GetSOIParentEntity(), Is.EqualTo(_sol));
    }

    [Test]
    public void ReplacedOrbit_LeavesTheOldExit_AndTheGuardIgnoresIt()
    {
        double soi = _planet.GetSOI_m();
        var ship = AddShip();
        SetEllipse(ship, 1.2 * soi, 0);
        var stale = QueuedExit(ship);

        double rp = OrbitMath.LowOrbitRadius(_planet);
        double mu = OrbitMath.SGP(_planet, ship);
        Place(ship, rp, Math.Sqrt(mu / rp), 0);
        Assert.That(ship.GetDataBlob<OrbitDB>().Apoapsis, Is.LessThan(soi));

        Assert.That(QueuedExitOrNull(ship), Is.EqualTo(stale), "a new orbit does not cancel the old interrupt");

        Game.ProcessorManager.GetInstanceProcessor(nameof(ChangeSOIProcessor)).ProcessEntity(ship, stale);
        Assert.That(ship.GetSOIParentEntity(), Is.EqualTo(_planet));
        Assert.That(ship.GetDataBlob<OrbitDB>().Eccentricity, Is.LessThan(0.01));
    }

    Entity AddPlanet(Entity sol, string name, double mass, double radius, double smaAu)
    {
        var orbit = OrbitDB.FromAsteroidFormat(sol, sol.GetDataBlob<MassVolumeDB>().MassDry,
            mass, smaAu, 0, 0, 0, 0, 0, _epoch);
        var body = Entity.Create();
        _sys.AddEntity(body, new BaseDataBlob[]
        {
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(mass, radius),
            orbit,
            new NameDB(name),
        });
        OrbitProcessor.ProcessEntity(body, _epoch);
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

    void SetEllipse(Entity ship, double apoapsis, double inclinationRad)
    {
        double rp = OrbitMath.LowOrbitRadius(_planet);
        double a = (rp + apoapsis) / 2.0;
        double mu = OrbitMath.SGP(_planet, ship);
        double speed = Math.Sqrt(mu * (2.0 / rp - 1.0 / a));
        Place(ship, rp, speed, inclinationRad);
        Assert.That(ship.GetDataBlob<OrbitDB>().Apoapsis, Is.EqualTo(apoapsis).Within(apoapsis * 1e-6));
    }

    void Place(Entity ship, double radius, double speed, double inclinationRad)
    {
        var planetAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(_planet, _epoch);
        var pos = ship.GetDataBlob<PositionDB>();
        pos.SetParent(_planet);
        pos.RelativePosition = new Vector3(radius, 0, 0);
        pos.AbsolutePosition = planetAbs + pos.RelativePosition;
        double ci = Math.Cos(inclinationRad);
        double si = Math.Sin(inclinationRad);
        ship.SetDataBlob(OrbitDB.FromVelocity(_planet, ship, new Vector3(0, speed * ci, speed * si), _epoch));
    }

    DateTime QueuedExit(Entity ship)
    {
        var at = QueuedExitOrNull(ship);
        Assert.That(at, Is.Not.Null, ship.GetDefaultName());
        return at!.Value;
    }

    DateTime? QueuedExitOrNull(Entity ship)
    {
        DateTime? at = null;
        foreach (var item in ship.Manager.ManagerSubpulses.InstanceProcessorsQueue)
        {
            if (item.Item.Item1 == nameof(ChangeSOIProcessor) && item.Item.Item2 == ship)
                at = item.Time;
        }
        return at;
    }
}
