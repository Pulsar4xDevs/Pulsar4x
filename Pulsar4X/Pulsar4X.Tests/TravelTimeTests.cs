using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.JumpPoints;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests;

[TestFixture]
public class TravelTimeTests
{
    static readonly Game Game = Load();

    static Game Load()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 6, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        return game;
    }

    [Test]
    public void NearerColonyWins()
    {
        var scene = Scene.Build();
        var near = scene.Place(scene.Here, scene.Star, new Vector3(1e9, 0, 0));
        var far = scene.Place(scene.Here, scene.Star, new Vector3(1e11, 0, 0));

        Assert.That(TravelTime.TryNearest(scene.Ship, new[] { far, near }, out var nearest, out var hours, out _), Is.True);
        Assert.That(nearest.Id, Is.EqualTo(near.Id));
        Assert.That(hours, Is.LessThan(1));
    }

    [Test]
    public void OneHopLosesToAShortInSystemTrip()
    {
        var scene = Scene.Build(linkThere: true);
        var local = scene.Place(scene.Here, scene.Star, new Vector3(1e6, 0, 0));
        var hopped = scene.Place(scene.There, scene.FarStar, new Vector3(1e6, 0, 0));

        Assert.That(TravelTime.TryNearest(scene.Ship, new[] { hopped, local }, out var nearest, out var hours, out _), Is.True);
        Assert.That(nearest.Id, Is.EqualTo(local.Id));
        Assert.That(hours, Is.LessThan(JumpRoute.HopHours));
    }

    [Test]
    public void NoWarpReturnsNone()
    {
        var scene = Scene.Build();
        scene.Ship.GetDataBlob<WarpAbilityDB>().MaxSpeed = 0;
        var colony = scene.Place(scene.Here, scene.Star, new Vector3(1e9, 0, 0));

        Assert.That(TravelTime.TryNearest(scene.Ship, new[] { colony }, out _, out _, out var reason), Is.False);
        Assert.That(reason, Does.Contain("warp"));
    }

    [Test]
    public void UnreachableOtherSystemIsSkipped()
    {
        var scene = Scene.Build(loose: true);
        var local = scene.Place(scene.Here, scene.Star, new Vector3(2e9, 0, 0));
        var loose = scene.Place(scene.Loose, scene.LooseStar, new Vector3(1e6, 0, 0));

        Assert.That(TravelTime.TryNearest(scene.Ship, new[] { loose, local }, out var nearest, out _, out _), Is.True);
        Assert.That(nearest.Id, Is.EqualTo(local.Id));

        Assert.That(TravelTime.TryNearest(scene.Ship, new[] { loose }, out _, out _, out var reason), Is.False);
        Assert.That(reason, Does.Contain("reachable"));
    }

    [Test]
    public void TieGoesToTheLowerId()
    {
        var scene = Scene.Build();
        var first = scene.Place(scene.Here, scene.Star, new Vector3(5e9, 0, 0));
        var second = scene.Place(scene.Here, scene.Star, new Vector3(5e9, 0, 0));

        Assert.That(TravelTime.TryNearest(scene.Ship, new[] { second, first }, out var nearest, out _, out _), Is.True);
        Assert.That(nearest.Id, Is.EqualTo(Math.Min(first.Id, second.Id)));
    }

    sealed class Scene
    {
        public StarSystem Here = null!;
        public StarSystem There = null!;
        public StarSystem Loose = null!;
        public Entity Star = null!;
        public Entity FarStar = null!;
        public Entity LooseStar = null!;
        public Entity Ship = null!;

        public static Scene Build(bool linkThere = false, bool loose = false)
        {
            var faction = FactionFactory.CreateFaction(Game, "travel-" + Guid.NewGuid().ToString("N"));
            var info = faction.GetDataBlob<FactionInfoDB>();
            var here = SystemWithStar("Here");
            info.KnownSystems.Add(here.System.ID);

            var scene = new Scene
            {
                Here = here.System,
                Star = here.Star,
            };

            var ship = Entity.Create(faction.Id);
            here.System.AddEntity(ship, new List<BaseDataBlob>
            {
                new PositionDB(new Vector3(0, 0, 0), here.Star),
                new WarpAbilityDB { MaxSpeed = 1e9 },
                new NameDB("Courier"),
            });
            ship.FactionOwnerID = faction.Id;
            scene.Ship = ship;

            if (linkThere)
            {
                var there = SystemWithStar("There");
                var gateHere = Gate(here.System, here.Star, new Vector3(3e11, 0, 0));
                var gateThere = Gate(there.System, there.Star, new Vector3(3e11, 0, 0));
                gateHere.SetDataBlob(new JumpPointDB(gateThere));
                gateThere.SetDataBlob(new JumpPointDB(gateHere));
                info.KnownSystems.Add(there.System.ID);
                info.RememberJumpPoint(gateHere);
                info.RememberJumpPoint(gateThere);
                scene.There = there.System;
                scene.FarStar = there.Star;
            }

            if (loose)
            {
                var other = SystemWithStar("Loose");
                info.KnownSystems.Add(other.System.ID);
                scene.Loose = other.System;
                scene.LooseStar = other.Star;
            }

            return scene;
        }

        public Entity Place(StarSystem system, Entity star, Vector3 at)
        {
            var colony = Entity.Create(Ship.FactionOwnerID);
            system.AddEntity(colony, new List<BaseDataBlob>
            {
                new PositionDB(at, star),
                new NameDB("Colony " + colony.Id),
            });
            return colony;
        }

        static (StarSystem System, Entity Star) SystemWithStar(string name)
        {
            var system = new StarSystem();
            system.Initialize(Game, name + Guid.NewGuid().ToString("N"), -1);
            var star = Entity.Create();
            system.AddEntity(star, new List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });
            return (system, star);
        }

        static Entity Gate(StarSystem system, Entity star, Vector3 at)
        {
            var gate = Entity.Create(Game.NeutralFactionId);
            system.AddEntity(gate, new List<BaseDataBlob>
            {
                new PositionDB(at, star),
                new NameDB("Gate"),
            });
            return gate;
        }
    }
}
