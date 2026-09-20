using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests
{
    /// <summary>The personnel read surface (the CommanderSnapshot roster) and its push triggers.</summary>
    [TestFixture]
    public class ApiCommanderTests : ApiTestBase
    {
        private Entity MakeCommander(int factionId, CommanderTypes type, int rank = 1)
        {
            var commanderDB = new CommanderDB($"Test {type} {rank}", rank, type)
            {
                CommissionedOn = _game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365.25 * 10),
                RankedOn = _game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365.25),
            };
            return CommanderFactory.Create(_game.Systems[0], factionId, commanderDB);
        }

        [Test]
        public void ProjectCommanders_returns_the_factions_people_with_rank_and_dates()
        {
            var session = Connect();
            var navy = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 6);
            var scientist = MakeCommander(session.FactionId, CommanderTypes.Scientist);

            // Another faction's commander must never appear in this faction's roster.
            int otherFactionId = _game.Factions.Keys.First(id => id != session.FactionId);
            MakeCommander(otherFactionId, CommanderTypes.Navy);

            var commanders = _projector.ProjectCommanders(session.FactionId);

            Assert.That(commanders, Is.Not.Null);
            Assert.That(commanders!.Select(c => c.Id), Is.EquivalentTo(new[] { navy.Id, scientist.Id }));

            var navySnapshot = commanders.Single(c => c.Id == navy.Id);
            Assert.That(navySnapshot.Kind, Is.EqualTo(CommanderKind.Navy));
            Assert.That(navySnapshot.Rank, Is.EqualTo(6));
            Assert.That(navySnapshot.RankName, Is.EqualTo("Captain"), "expected the theme's title for navy rank 6");
            Assert.That(navySnapshot.RankedOn, Is.EqualTo(navy.GetDataBlob<CommanderDB>().RankedOn));
            Assert.That(navySnapshot.CommissionedOn, Is.EqualTo(navy.GetDataBlob<CommanderDB>().CommissionedOn));
            Assert.That(navySnapshot.IsAssigned, Is.False);
            Assert.That(navySnapshot.AssignmentName, Is.Null);

            var scientistSnapshot = commanders.Single(c => c.Id == scientist.Id);
            Assert.That(scientistSnapshot.Kind, Is.EqualTo(CommanderKind.Scientist));
            Assert.That(scientistSnapshot.RankName, Is.Null, "only the navy track has theme rank titles");
        }

        [Test]
        public void ProjectCommanders_resolves_a_post_assignment_to_its_name()
        {
            var session = Connect();
            var admin = MakeCommander(session.FactionId, CommanderTypes.Civilian);

            // Point the commander at a named entity the way the lab/admin assignment orders do.
            var post = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(post, new List<BaseDataBlob>
            {
                new NameDB("Mars Administration", session.FactionId, "Mars Administration"),
            });
            admin.GetDataBlob<CommanderDB>().AssignedTo = post.Id;

            var snapshot = _projector.ProjectCommanders(session.FactionId)!.Single(c => c.Id == admin.Id);

            Assert.That(snapshot.IsAssigned, Is.True);
            Assert.That(snapshot.AssignmentName, Is.EqualTo("Mars Administration"));
        }

        [Test]
        public void ProjectCommanders_resolves_ship_command_from_the_fleet_tree()
        {
            var session = Connect();
            var captain = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 6);

            // Ship command is recorded on the ship (ShipInfoDB.CommanderID), not the commander, so
            // the projector reverse-maps it from the faction's fleet tree.
            var shipInfo = new ShipInfoDB { CommanderID = captain.Id };
            var ship = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(ship, new List<BaseDataBlob>
            {
                shipInfo,
                new NameDB("ISS Test", session.FactionId, "ISS Test"),
            });
            _game.Factions[session.FactionId].GetDataBlob<FleetDB>().AddChild(ship);

            var snapshot = _projector.ProjectCommanders(session.FactionId)!.Single(c => c.Id == captain.Id);

            Assert.That(snapshot.IsAssigned, Is.True);
            Assert.That(snapshot.AssignmentName, Is.EqualTo("ISS Test"));
        }

        [Test]
        public void Commanders_are_pushed_on_connect_and_on_clock_advance()
        {
            var session = Connect();
            var commander = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 3);

            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);

            var initial = received.LastOrDefault(e => e.Type == GameEventType.CommandersChanged);
            Assert.That(initial, Is.Not.Null, "expected the roster in the initial connect push");
            Assert.That(initial!.Commanders!.Select(c => c.Id), Does.Contain(commander.Id));

            received.Clear();
            _server.SetTimeControl(session,
                new TimeControlRequest(TimeControlAction.StepOnce, StepLength: TimeSpan.FromDays(1)));

            Assert.That(received.Any(e => e.Type == GameEventType.CommandersChanged && e.Commanders != null),
                Is.True, "expected a roster re-push on clock advance");
        }
    }
}
