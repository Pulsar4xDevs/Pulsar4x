using System;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.DataStructures;
using Pulsar4X.GeoSurveys;
using Pulsar4X.JumpPoints;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests
{
    public class CommanderSkillsTests
    {
        private static readonly Game Game = InitializeGame();
        private StarSystem _sys;

        static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);
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
        }

        [Test]
        public void RelayDelay_NoCommander_IsDefault()
        {
            var ship = BareShip();
            Assert.AreEqual(CommanderSkills.DefaultRelayDelay, CommanderSkills.RelayDelay(ship));
            Assert.AreEqual(CommanderSkills.DefaultRecheckInterval, CommanderSkills.RecheckInterval(ship));
        }

        [Test]
        public void RelayDelay_HighCommandSkill_IsShorter()
        {
            var (ship, db) = ShipWithCommander();
            db.ExperienceCap = 100;
            CommanderSkills.Grant(db, SkillDomain.Command, 100);

            var delay = CommanderSkills.RelayDelay(ship);
            Assert.Less(delay, CommanderSkills.DefaultRelayDelay);
            Assert.AreEqual(TimeSpan.FromTicks(CommanderSkills.DefaultRelayDelay.Ticks / 2), delay);

            var recheck = CommanderSkills.RecheckInterval(ship);
            Assert.Greater(recheck, CommanderSkills.DefaultRecheckInterval);
        }

        [Test]
        public void GrantForCompleted_MoveTo_IsNav_Survey_IsSurvey_Fleet_IsCommand()
        {
            var (ship, db) = ShipWithCommander();
            var fleet = FleetFactory.Create(_sys, ship.FactionOwnerID, "flotilla");
            fleet.GetDataBlob<FleetDB>().FlagShipID = ship.Id;

            var move = new Goal(GoalType.MoveTo) { Status = GoalStatus.Completed };
            CommanderSkills.GrantForCompleted(ship, move);
            Assert.AreEqual(1, CommanderSkills.Get(db, SkillDomain.Nav));
            Assert.AreEqual(0, CommanderSkills.Get(db, SkillDomain.Survey));
            Assert.AreEqual(0, CommanderSkills.Get(db, SkillDomain.Command));

            var survey = new Goal(GoalType.ServeyBodies) { Status = GoalStatus.Completed };
            CommanderSkills.GrantForCompleted(ship, survey);
            Assert.AreEqual(1, CommanderSkills.Get(db, SkillDomain.Survey));
            Assert.AreEqual(1, CommanderSkills.Get(db, SkillDomain.Nav));

            var fleetGoal = new Goal(GoalType.ServeyBodies) { Status = GoalStatus.Completed };
            CommanderSkills.GrantForCompleted(fleet, fleetGoal);
            Assert.AreEqual(1, CommanderSkills.Get(db, SkillDomain.Command));
            Assert.AreEqual(1, CommanderSkills.Get(db, SkillDomain.Survey));
        }

        [Test]
        public void SurveyRate_NoCommander_IsKitSpeed_CapSurvey_Doubles()
        {
            var ship = BareShip();
            Assert.AreEqual(100u, CommanderSkills.SurveyRate(100, ship));

            var (skilled, db) = ShipWithCommander();
            db.ExperienceCap = 100;
            CommanderSkills.Grant(db, SkillDomain.Survey, 100);
            Assert.AreEqual(200u, CommanderSkills.SurveyRate(100, skilled));
        }

        [Test]
        public void GeoSurveyTick_SurveySkill_ScalesPoints()
        {
            var (ship, db) = ShipWithCommander();
            db.ExperienceCap = 100;
            ship.SetDataBlob(new GeoSurveyAbilityDB { Speed = 100 });
            var body = SurveyBody(250);
            new GeoSurveyProcessor(ship, body).ProcessEntity(ship, _sys.StarSysDateTime);
            Assert.AreEqual(150u, body.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[ship.FactionOwnerID]);

            CommanderSkills.Grant(db, SkillDomain.Survey, 100);
            var body2 = SurveyBody(250);
            new GeoSurveyProcessor(ship, body2).ProcessEntity(ship, _sys.StarSysDateTime);
            Assert.AreEqual(50u, body2.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[ship.FactionOwnerID]);
        }

        [Test]
        public void GravSurveyTick_SurveySkill_ScalesPoints()
        {
            var (ship, db) = ShipWithCommander();
            db.ExperienceCap = 100;
            ParkTogether(ship, out var anomaly);
            ship.SetDataBlob(new JPSurveyAbilityDB { Speed = 100 });
            ship.SetDataBlob(new JPSurveyDB { TargetId = anomaly.Id });

            new JPSurveyProcessor().ProcessEntity(ship, 3600);
            Assert.AreEqual(150u, anomaly.GetDataBlob<JPSurveyableDB>().SurveyPointsRemaining[ship.FactionOwnerID]);

            CommanderSkills.Grant(db, SkillDomain.Survey, 100);
            anomaly.GetDataBlob<JPSurveyableDB>().SurveyPointsRemaining[ship.FactionOwnerID] = 250;
            new JPSurveyProcessor().ProcessEntity(ship, 3600);
            Assert.AreEqual(50u, anomaly.GetDataBlob<JPSurveyableDB>().SurveyPointsRemaining[ship.FactionOwnerID]);
        }

        Entity SurveyBody(uint points)
        {
            var ent = Entity.Create();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                new PositionDB(),
                new NameDB("body"),
                new GeoSurveyableDB { PointsRequired = points },
            });
            return ent;
        }

        void ParkTogether(Entity ship, out Entity anomaly)
        {
            anomaly = Entity.Create();
            var apos = new PositionDB();
            _sys.AddEntity(anomaly, new BaseDataBlob[]
            {
                apos,
                new NameDB("anomaly"),
                new JPSurveyableDB(250, new SafeDictionary<int, uint>(), 10_000_000),
            });
            if (!ship.TryGetDataBlob<PositionDB>(out var spos))
            {
                spos = new PositionDB();
                ship.SetDataBlob(spos);
            }
            spos.SetParent(anomaly);
            spos.RelativePosition = Vector3.Zero;
        }

        (Entity ship, CommanderDB db) ShipWithCommander()
        {
            var faction = FactionFactory.CreateFaction(Game, "cmd-" + Guid.NewGuid().ToString("N"));
            var db = CommanderFactory.CreateShipCaptain(Game);
            var commander = CommanderFactory.Create(_sys, faction.Id, db);
            var ship = BareShip(faction.Id);
            ship.GetDataBlob<ShipInfoDB>().CommanderID = commander.Id;
            return (ship, db);
        }

        Entity BareShip(int factionId = -1)
        {
            var ship = Entity.Create();
            _sys.AddEntity(ship, new BaseDataBlob[]
            {
                new ShipInfoDB(),
                new NameDB("skiff"),
            });
            if (factionId >= 0)
                ship.FactionOwnerID = factionId;
            return ship;
        }
    }
}
