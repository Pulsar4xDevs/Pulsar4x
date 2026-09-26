using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests
{
    public class CommandSpanTests
    {
        private static readonly Game Game = InitializeGame();
        private StarSystem _sys;
        private DateTime _epoch;

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
            _sys.Initialize(Game, "Sol", -1);
            _epoch = _sys.StarSysDateTime;
        }

        [Test]
        public void Of_NoAdmin_IsBody()
        {
            var ship = BareShip();
            Assert.AreEqual(CommandSpanKind.Body, CommandSpan.Of(ship));
            var faction = FactionFactory.CreateFaction(Game, "span-" + Guid.NewGuid().ToString("N"));
            var fleet = FleetFactory.Create(_sys, faction.Id, "empty");
            Assert.AreEqual(CommandSpanKind.Body, CommandSpan.Of(fleet));
        }

        [Test]
        public void Of_PlanetSeat_IsWell_SystemSeat_IsSystem()
        {
            var ship = BareShip();
            AttachBridge(ship, AdminLevel.Planet);
            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Of(ship));
            AttachBridge(ship, AdminLevel.System);
            Assert.AreEqual(CommandSpanKind.System, CommandSpan.Of(ship));
        }

        [Test]
        public void Of_Fleet_UsesFlagshipNotOtherChild()
        {
            var flag = BareShip();
            var other = BareShip();
            AttachBridge(flag, AdminLevel.Planet);
            AttachBridge(other, AdminLevel.System);
            var faction = FactionFactory.CreateFaction(Game, "span-" + Guid.NewGuid().ToString("N"));
            var fleet = FleetFactory.Create(_sys, faction.Id, "flotilla");
            var fdb = fleet.GetDataBlob<FleetDB>();
            fdb.AddChild(flag);
            fdb.AddChild(other);
            fdb.FlagShipID = flag.Id;
            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Of(fleet));
        }

        [Test]
        public void CreateShip_FromBridgeDesign_SetsSpan()
        {
            var faction = FactionFactory.CreateFaction(Game, "span-bp-" + Guid.NewGuid().ToString("N"));
            var info = faction.GetDataBlob<FactionInfoDB>();
            var data = info.Data;
            if (data.LockedComponentTemplates.ContainsKey("ship-command"))
                data.Unlock("ship-command");
            if (data.LockedArmor.ContainsKey("plastic-armor"))
                data.Unlock("plastic-armor");
            foreach (var uniqueId in data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
                data.Unlock(uniqueId);
            foreach (var id in data.LockedTechs.Keys.ToList())
                data.Unlock(id);

            var well = ComponentDesignFromJson.Create(faction, data, BridgeBlueprint(
                "default-design-bridge-well", "Planetary Command Bridge", adminLevel: 6, consoles: 8));
            var system = ComponentDesignFromJson.Create(faction, data, BridgeBlueprint(
                "default-design-bridge-system", "System Command Bridge", adminLevel: 8, consoles: 12));

            var sol = TestingUtilities.BasicSol(_sys);
            var wellShip = SpawnWithBridge(faction, info, well, sol, "well-flag");
            var systemShip = SpawnWithBridge(faction, info, system, sol, "sys-flag");

            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Of(wellShip));
            Assert.AreEqual(CommandSpanKind.System, CommandSpan.Of(systemShip));
        }

        [Test]
        public void Expand_BodyWellSystem_OnPlanetTree()
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var mars = AddBody(sol, "Mars", 0.64174e24, 3_396_200, 1.524);
            var phobos = AddMoon(mars, "Phobos", 1.07e16, 11_100, 9_376_000);
            bool yes(Entity _) => true;

            var body = CommandSpan.Expand(mars, CommandSpanKind.Body, yes);
            Assert.AreEqual(1, body.Count);
            Assert.AreEqual(mars.Id, body[0].Id);

            var well = CommandSpan.Expand(mars, CommandSpanKind.Well, yes);
            Assert.That(well.Select(e => e.Id), Does.Contain(mars.Id));
            Assert.That(well.Select(e => e.Id), Does.Contain(phobos.Id));
            Assert.That(well.Select(e => e.Id), Does.Not.Contain(sol.Id));

            var system = CommandSpan.Expand(mars, CommandSpanKind.System, yes);
            Assert.That(system.Count, Is.GreaterThan(well.Count));
            Assert.That(system.Select(e => e.Id), Does.Contain(sol.Id));
        }

        static ComponentDesignBlueprint BridgeBlueprint(string id, string name, int adminLevel, int consoles)
        {
            return new ComponentDesignBlueprint
            {
                UniqueID = id,
                Name = name,
                TemplateId = "ship-command",
                Properties = new List<ComponentDesignBlueprint.Property>
                {
                    new() { Key = "Admin Level", Value = new JValue(adminLevel) },
                    new() { Key = "Console Space", Value = new JValue(consoles) },
                },
            };
        }

        Entity SpawnWithBridge(Entity faction, FactionInfoDB info, Pulsar4X.Components.ComponentDesign bridge, Entity parent, string name)
        {
            var armor = info.Data.Armor["plastic-armor"];
            var design = new ShipDesign(info, name, new List<(Pulsar4X.Components.ComponentDesign, int)> { (bridge, 1) }, (armor, 1f));
            design.Initialise(info);
            return ShipFactory.CreateShip(design, faction, parent, name);
        }

        static void AttachBridge(Entity ship, AdminLevel level)
        {
            var admin = new AdminSpaceDB();
            admin.CommanderSeats.Add(new AdminSpaceAbilityState(level, "command-bridge"));
            ship.SetDataBlob(admin);
        }

        Entity BareShip()
        {
            var ship = Entity.Create();
            _sys.AddEntity(ship, new BaseDataBlob[] { new ShipInfoDB(), new NameDB("s") });
            return ship;
        }

        Entity AddBody(Entity parent, string name, double mass, double radius_m, double smaAu)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, 0, 0, 0, 0, 0, _epoch);
            var ent = Entity.Create();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                new PositionDB(),
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB(name),
            });
            OrbitProcessor.ProcessEntity(ent, _epoch);
            return ent;
        }

        Entity AddMoon(Entity parent, string name, double mass, double radius_m, double sma_m)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, Distance.MToAU(sma_m), 0, 0, 0, 0, 0, _epoch);
            var ent = Entity.Create();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                new PositionDB(),
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB(name),
            });
            OrbitProcessor.ProcessEntity(ent, _epoch);
            return ent;
        }
    }
}
