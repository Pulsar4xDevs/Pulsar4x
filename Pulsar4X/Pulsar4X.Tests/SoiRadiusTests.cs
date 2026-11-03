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
/// One sphere radius. The star is infinite. A ship's exit is scheduled on that radius.
/// </summary>
[TestFixture]
public class SoiRadiusTests
{
    [Test]
    public void SolBodies_Agree_AndTheStarIsInfinite()
    {
        var (game, sol) = LoadSol();
        var bodies = sol.GetAllEntitiesWithDataBlob<OrbitDB>()
            .Where(body => body.HasDataBlob<MassVolumeDB>())
            .ToList();
        Assert.That(bodies, Has.Count.GreaterThan(5));

        int roots = 0;
        foreach (var body in bodies)
        {
            var orbit = body.GetDataBlob<OrbitDB>();
            double fromEntity = body.GetSOI_m();
            double fromOrbitMath = OrbitMath.GetSOIRadius(orbit);
            double fromProperty = orbit.SOI_m;
            Assert.That(fromOrbitMath, Is.EqualTo(fromEntity), body.GetDefaultName());
            Assert.That(fromProperty, Is.EqualTo(fromEntity), body.GetDefaultName());
            if (!double.IsFinite(fromEntity))
                roots++;
        }

        Assert.That(roots, Is.EqualTo(1), "the star is the one infinite sphere");

        var earth = bodies.Single(body => body.GetDefaultName() == "Earth");
        double earthSoi = earth.GetSOI_m();
        Assert.That(earthSoi, Is.GreaterThan(earth.GetDataBlob<MassVolumeDB>().RadiusInM));
        Assert.That(double.IsFinite(earthSoi), Is.True);

        var ship = ShipOnHyperbola(sol, earth, game);
        var exit = QueuedExit(ship);
        var predicted = OrbitMath.TimeToRadius(ship.GetDataBlob<OrbitDB>(), earthSoi);
        Assert.That(exit, Is.EqualTo(predicted).Within(TimeSpan.FromSeconds(1)));
    }

    static (Game game, StarSystem sol) LoadSol()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        return (game, game.Systems.Single(system => system.ID == "system-sol"));
    }

    static Entity ShipOnHyperbola(StarSystem sol, Entity earth, Game game)
    {
        var epoch = sol.StarSysDateTime;
        double rp = OrbitMath.LowOrbitRadius(earth);
        var ship = Entity.Create();
        var pos = new PositionDB();
        sol.AddEntity(ship, new BaseDataBlob[]
        {
            pos,
            MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            new NameDB("exit-probe"),
        });
        pos.SetParent(earth);
        pos.RelativePosition = new Vector3(rp, 0, 0);
        double mu = OrbitMath.SGP(earth, ship);
        double speed = Math.Sqrt(2.0 * mu / rp) * 1.05;
        ship.SetDataBlob(OrbitDB.FromVelocity(earth, ship, new Vector3(0, speed, 0), epoch));
        Assert.That(ship.GetDataBlob<OrbitDB>().Eccentricity, Is.GreaterThanOrEqualTo(1));
        Assert.That(game, Is.Not.Null);
        return ship;
    }

    static DateTime QueuedExit(Entity ship)
    {
        DateTime? at = null;
        foreach (var item in ship.Manager.ManagerSubpulses.InstanceProcessorsQueue)
        {
            if (item.Item.Item1 != nameof(ChangeSOIProcessor) || item.Item.Item2 != ship)
                continue;
            at = item.Time;
        }
        Assert.That(at, Is.Not.Null, "a hyperbola out of Earth must schedule an exit");
        return at!.Value;
    }
}
