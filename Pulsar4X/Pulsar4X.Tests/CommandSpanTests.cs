using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
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
using Pulsar4X.People;
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
                "default-design-bridge-well", "Planetary Command Bridge", span: 1, consoles: 8));
            var system = ComponentDesignFromJson.Create(faction, data, BridgeBlueprint(
                "default-design-bridge-system", "System Command Bridge", span: 2, consoles: 12));

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
            Assert.AreEqual(mars.Id, system[0].Id);
            Assert.That(system.Select(e => e.Id), Does.Contain(phobos.Id));
            Assert.That(system.Select(e => e.Id), Does.Not.Contain(sol.Id));
        }

        [Test]
        public void Expand_System_Moon_ExcludesParentAndSiblings()
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var mercury = AddBody(sol, "Mercury", 0.330e24, 2_440_000, 0.387);
            var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, 1.0);
            var luna = AddMoon(earth, "Luna", 7.346e22, 1_737_000, 384_400_000);
            bool yes(Entity _) => true;

            var system = CommandSpan.Expand(luna, CommandSpanKind.System, yes);
            Assert.AreEqual(1, system.Count);
            Assert.AreEqual(luna.Id, system[0].Id);
            Assert.That(system.Select(e => e.Id), Does.Not.Contain(mercury.Id));
            Assert.That(system.Select(e => e.Id), Does.Not.Contain(earth.Id));
            Assert.That(system.Select(e => e.Id), Does.Not.Contain(sol.Id));
        }

        [Test]
        public void Expand_System_Star_IncludesNestedMoons_WellDoesNot()
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, 1.0);
            var luna = AddMoon(earth, "Luna", 7.346e22, 1_737_000, 384_400_000);
            bool yes(Entity _) => true;

            var well = CommandSpan.Expand(sol, CommandSpanKind.Well, yes);
            Assert.That(well.Select(e => e.Id), Does.Contain(earth.Id));
            Assert.That(well.Select(e => e.Id), Does.Not.Contain(luna.Id));

            var system = CommandSpan.Expand(sol, CommandSpanKind.System, yes);
            Assert.AreEqual(sol.Id, system[0].Id);
            Assert.That(system.Select(e => e.Id), Does.Contain(earth.Id));
            Assert.That(system.Select(e => e.Id), Does.Contain(luna.Id));
        }

        static string Title(int rank) => rank switch { 4 => "Lieutenant Commander", 6 => "Captain", _ => null };

        [Test]
        public void Fix_BridgeLimited_SaysFitABridge()
        {
            var well = CommandSpan.Fix(CommandSpanKind.Body, seated: true, rank: 6, CommandSpanKind.Well, Title);
            Assert.That(well, Does.Contain("The bridge limits reach."));
            Assert.That(well, Does.Contain("Fit a Planetary Command Bridge"));
            Assert.That(well, Does.Not.Contain("Seat"));

            var system = CommandSpan.Fix(CommandSpanKind.Well, seated: true, rank: 6, CommandSpanKind.System, Title);
            Assert.That(system, Does.Contain("Fit a System Command Bridge"));
        }

        [Test]
        public void Fix_RankLimited_SaysWhichRankToSeat()
        {
            var well = CommandSpan.Fix(CommandSpanKind.Well, seated: true, rank: 2, CommandSpanKind.Well, Title);
            Assert.That(well, Does.Contain("The captain's rank limits reach."));
            Assert.That(well, Does.Contain("rank 4+ (Lieutenant Commander)"));
            Assert.That(well, Does.Not.Contain("Fit a"));

            var system = CommandSpan.Fix(CommandSpanKind.System, seated: true, rank: 4, CommandSpanKind.System, Title);
            Assert.That(system, Does.Contain("rank 6+ (Captain)"));
        }

        [Test]
        public void Fix_EmptyChair_SaysSeatAnOfficer()
        {
            var fix = CommandSpan.Fix(CommandSpanKind.Well, seated: false, rank: 0, CommandSpanKind.Well, Title);
            Assert.That(fix, Does.StartWith("The chair is empty."));
            Assert.That(fix, Does.Contain("Seat an officer of rank 4+ (Lieutenant Commander)"));
            Assert.That(fix, Does.Not.Contain("Fit a"));
        }

        [Test]
        public void Fix_BothLimit_NamesBoth_AndNothingWhenReachIsEnough()
        {
            var both = CommandSpan.Fix(CommandSpanKind.Body, seated: true, rank: 1, CommandSpanKind.Well, Title);
            Assert.That(both, Does.Contain("Fit a Planetary Command Bridge"));
            Assert.That(both, Does.Contain("rank 4+"));
            var emptyBoth = CommandSpan.Fix(CommandSpanKind.Body, seated: false, rank: 0, CommandSpanKind.Well, null);
            Assert.That(emptyBoth, Does.Contain("empty chair"));
            Assert.That(emptyBoth, Does.Contain("Seat an officer of rank 4+."));

            Assert.That(CommandSpan.Fix(CommandSpanKind.Well, true, 4, CommandSpanKind.Well, Title), Is.Null);
            Assert.That(CommandSpan.Fix(CommandSpanKind.System, true, 6, CommandSpanKind.System, Title), Is.Null);
            Assert.That(CommandSpan.Fix(CommandSpanKind.Body, false, 0, CommandSpanKind.Body, Title), Is.Null);
        }

        [Test]
        public void LimiterOf_NamesTheCappingPart()
        {
            Assert.That(CommandSpan.LimiterOf(CommandSpanKind.System, true, 6), Is.EqualTo(CommandSpan.SpanLimiter.None));
            Assert.That(CommandSpan.LimiterOf(CommandSpanKind.Well, true, 6), Is.EqualTo(CommandSpan.SpanLimiter.Bridge));
            Assert.That(CommandSpan.LimiterOf(CommandSpanKind.System, true, 4), Is.EqualTo(CommandSpan.SpanLimiter.Rank));
            Assert.That(CommandSpan.LimiterOf(CommandSpanKind.Well, true, 4), Is.EqualTo(CommandSpan.SpanLimiter.Bridge | CommandSpan.SpanLimiter.Rank));
            Assert.That(CommandSpan.LimiterOf(CommandSpanKind.System, false, 0), Is.EqualTo(CommandSpan.SpanLimiter.EmptyChair));
            Assert.That(CommandSpan.LimiterOf(CommandSpanKind.Body, false, 0),
                Is.EqualTo(CommandSpan.SpanLimiter.Bridge | CommandSpan.SpanLimiter.EmptyChair));
            Assert.That(CommandSpan.LimitClause(CommandSpan.SpanLimiter.Bridge), Is.EqualTo("The bridge limits reach."));
            Assert.That(CommandSpan.LimitClause(CommandSpan.SpanLimiter.Rank), Is.EqualTo("The captain's rank limits reach."));
            Assert.That(CommandSpan.LimitClause(CommandSpan.SpanLimiter.EmptyChair), Is.EqualTo("The empty chair limits reach."));
            Assert.That(CommandSpan.LimitClause(CommandSpan.SpanLimiter.None), Is.Empty);
        }

        [Test]
        public void Describe_NamesTheLimitingPart()
        {
            var ship = BareShip();
            AttachBridge(ship, AdminLevel.Planet);
            // The empty chair holds a well bridge back to this ship.
            Assert.That(CommandSpan.Describe(ship), Does.EndWith("The empty chair limits reach."));
            Assert.That(CommandSpan.Describe(ship), Does.StartWith("Body. This bridge can cover a well. No officer is seated."));
            Assert.That(CommandSpan.Fix(ship, CommandSpanKind.Well), Does.Contain("Seat an officer of rank 4+"));

            var plain = BareShip();
            Assert.That(CommandSpan.Describe(plain), Does.Contain("The bridge and the empty chair limit reach."));
            Assert.That(CommandSpan.Fix(plain, CommandSpanKind.Well), Does.Contain("Fit a Planetary Command Bridge"));
        }

        [Test]
        public void Effective_ShipInFleet_IsCappedByTheFlagship()
        {
            var faction = SpanFaction();
            var flag = SeatedShip(faction, AdminLevel.Planet, rank: 6);
            var wide = SeatedShip(faction, AdminLevel.System, rank: 6);
            Assert.AreEqual(CommandSpanKind.System, CommandSpan.Effective(wide), "alone, nothing caps it");

            var fleet = FleetFactory.Create(_sys, faction.Id, "flotilla");
            var fdb = fleet.GetDataBlob<FleetDB>();
            fdb.AddChild(flag);
            fdb.AddChild(wide);
            fdb.FlagShipID = flag.Id;

            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(wide));
            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(flag));
        }

        [Test]
        public void Effective_ParentCeiling_CapsFlagshipAndSubFleet()
        {
            var faction = SpanFaction();
            var parentFlag = SeatedShip(faction, AdminLevel.Planet, rank: 6);
            var parent = FleetFactory.Create(_sys, faction.Id, "parent");
            parent.GetDataBlob<FleetDB>().AddChild(parentFlag);
            parent.GetDataBlob<FleetDB>().FlagShipID = parentFlag.Id;

            var childFlag = SeatedShip(faction, AdminLevel.System, rank: 6);
            var child = FleetFactory.Create(_sys, faction.Id, "child");
            child.GetDataBlob<FleetDB>().AddChild(childFlag);
            child.GetDataBlob<FleetDB>().FlagShipID = childFlag.Id;

            Assert.AreEqual(CommandSpanKind.System, CommandSpan.Effective(child));
            Assert.AreEqual(CommandSpanKind.System, CommandSpan.Effective(childFlag));

            child.GetDataBlob<FleetDB>().SetParent(parent);

            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(child), "the sub-fleet is capped");
            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(childFlag), "so is its flagship");
            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(parent));
        }

        [Test]
        public void Effective_ParentCeiling_LowRankOfTheParentCaptainCaps()
        {
            var faction = SpanFaction();
            var parentFlag = SeatedShip(faction, AdminLevel.System, rank: 1);
            var parent = FleetFactory.Create(_sys, faction.Id, "parent");
            parent.GetDataBlob<FleetDB>().AddChild(parentFlag);
            parent.GetDataBlob<FleetDB>().FlagShipID = parentFlag.Id;

            var ship = SeatedShip(faction, AdminLevel.System, rank: 6);
            parent.GetDataBlob<FleetDB>().AddChild(ship);

            Assert.AreEqual(CommandSpanKind.Body, CommandSpan.Effective(ship));
            Assert.That(CommandSpan.Describe(ship), Does.Contain("The parent fleet limits reach."));
            Assert.That(CommandSpan.Fix(ship, CommandSpanKind.Well), Does.Contain("The parent fleet limits reach."));
        }

        [Test]
        public void Effective_FlagshipLessFleet_ImposesNoCeiling_ButItsAncestorsStillCap()
        {
            var faction = SpanFaction();
            var topFlag = SeatedShip(faction, AdminLevel.Planet, rank: 6);
            var top = FleetFactory.Create(_sys, faction.Id, "top");
            top.GetDataBlob<FleetDB>().AddChild(topFlag);
            top.GetDataBlob<FleetDB>().FlagShipID = topFlag.Id;

            var middle = FleetFactory.Create(_sys, faction.Id, "middle");
            var leafFlag = SeatedShip(faction, AdminLevel.System, rank: 6);
            var leaf = FleetFactory.Create(_sys, faction.Id, "leaf");
            leaf.GetDataBlob<FleetDB>().AddChild(leafFlag);
            leaf.GetDataBlob<FleetDB>().FlagShipID = leafFlag.Id;

            leaf.GetDataBlob<FleetDB>().SetParent(middle);
            Assert.AreEqual(CommandSpanKind.System, CommandSpan.Effective(leaf), "a flagship-less parent alone caps nothing");
            Assert.AreEqual(CommandSpanKind.System, CommandSpan.Effective(leafFlag));

            middle.GetDataBlob<FleetDB>().SetParent(top);
            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(leaf), "the grandparent still caps");
            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(leafFlag));
        }

        [Test]
        public void Effective_ShipDirectlyInAFlagshipLessFleet_IsNotCapped()
        {
            var faction = SpanFaction();
            var ship = SeatedShip(faction, AdminLevel.System, rank: 6);
            var fleet = FleetFactory.Create(_sys, faction.Id, "empty-chair");
            fleet.GetDataBlob<FleetDB>().AddChild(ship);

            Assert.AreEqual(CommandSpanKind.System, CommandSpan.Effective(ship));
        }

        [Test]
        public void Effective_CycleInTheFleetTree_Terminates()
        {
            var faction = SpanFaction();
            var aFlag = SeatedShip(faction, AdminLevel.Planet, rank: 6);
            var a = FleetFactory.Create(_sys, faction.Id, "a");
            a.GetDataBlob<FleetDB>().AddChild(aFlag);
            a.GetDataBlob<FleetDB>().FlagShipID = aFlag.Id;
            var bFlag = SeatedShip(faction, AdminLevel.System, rank: 6);
            var b = FleetFactory.Create(_sys, faction.Id, "b");
            b.GetDataBlob<FleetDB>().AddChild(bFlag);
            b.GetDataBlob<FleetDB>().FlagShipID = bFlag.Id;

            a.GetDataBlob<FleetDB>().SetParent(b);
            b.GetDataBlob<FleetDB>().SetParent(a);

            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(a));
            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(bFlag));
        }

        [Test]
        public void Effective_Colony_IsNotCappedByAFleet()
        {
            var colony = Entity.Create();
            _sys.AddEntity(colony, new BaseDataBlob[] { new NameDB("c") });
            AttachBridge(colony, AdminLevel.Planet);

            Assert.AreEqual(CommandSpanKind.Well, CommandSpan.Effective(colony));
            Assert.AreEqual(CommandSpanKind.System, CommandSpan.ParentCeiling(colony));
        }

        Entity SpanFaction() => FactionFactory.CreateFaction(Game, "span-" + Guid.NewGuid().ToString("N"));

        Entity SeatedShip(Entity faction, AdminLevel bridge, int rank)
        {
            var ship = BareShip();
            ship.FactionOwnerID = faction.Id;
            AttachBridge(ship, bridge);
            var officer = CommanderFactory.Create(_sys, faction.Id, new CommanderDB("O", rank, CommanderTypes.Navy));
            ship.GetDataBlob<ShipInfoDB>().CommanderID = officer.Id;
            return ship;
        }

        static ComponentDesignBlueprint BridgeBlueprint(string id, string name, int span, int consoles)
        {
            return new ComponentDesignBlueprint
            {
                UniqueID = id,
                Name = name,
                TemplateId = "ship-command",
                Properties = new List<ComponentDesignBlueprint.Property>
                {
                    new() { Key = "Command Span", Value = new JValue(span) },
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
