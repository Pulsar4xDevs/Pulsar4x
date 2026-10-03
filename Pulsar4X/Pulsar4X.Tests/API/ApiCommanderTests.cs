using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
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

        private Entity MakeShip(int factionId, string name, int commanderId = -1)
        {
            var ship = Entity.Create(factionId);
            _game.Systems[0].AddEntity(ship, new List<BaseDataBlob>
            {
                new ShipInfoDB { CommanderID = commanderId },
                new NameDB(name, factionId, name),
            });
            _game.Factions[factionId].GetDataBlob<FleetDB>().AddChild(ship);
            return ship;
        }

        [Test]
        public void AssignCaptain_seats_a_navy_officer_and_refreshes_while_paused()
        {
            var session = Connect();
            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.Pause));
            var officer = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 6);
            var ship = MakeShip(session.FactionId, "ISS Test");

            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);
            received.Clear();

            var result = _server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, officer.Id));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(ship.GetDataBlob<ShipInfoDB>().CommanderID, Is.EqualTo(officer.Id));
            Assert.That(officer.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(ship.Id));

            var roster = _projector.ProjectCommanders(session.FactionId)!.Single(c => c.Id == officer.Id);
            Assert.That(roster.IsAssigned, Is.True);
            Assert.That(roster.AssignmentName, Is.EqualTo("ISS Test"));

            var (_, unattached) = _projector.ProjectFleetHierarchy(session.FactionId);
            var projected = unattached.Single(s => s.Id == ship.Id);
            Assert.That(projected.CommanderId, Is.EqualTo(officer.Id));
            Assert.That(projected.CommanderName, Is.EqualTo(officer.GetName(session.FactionId)));

            Assert.That(received.Any(e => e.Type == GameEventType.FleetsChanged
                && e.UnattachedShips != null
                && e.UnattachedShips.Any(s => s.Id == ship.Id && s.CommanderId == officer.Id)),
                Is.True, "expected the fleet tree to refresh while paused");
            Assert.That(received.Any(e => e.Type == GameEventType.CommandersChanged
                && e.Commanders != null
                && e.Commanders.Any(c => c.Id == officer.Id && c.AssignmentName == "ISS Test")),
                Is.True, "expected the roster to refresh while paused");
        }

        [Test]
        public void AssignCaptain_moves_an_officer_off_the_ship_they_already_command()
        {
            var session = Connect();
            var officer = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 6);
            // Factory captains record the person on the ship only. AssignedTo stays empty.
            var left = MakeShip(session.FactionId, "ISS Left", officer.Id);
            var right = MakeShip(session.FactionId, "ISS Right");
            Assert.That(officer.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));

            var result = _server.SubmitCommand(session, new AssignCaptainCommand(right.Id, officer.Id));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(left.GetDataBlob<ShipInfoDB>().CommanderID, Is.EqualTo(-1));
            Assert.That(right.GetDataBlob<ShipInfoDB>().CommanderID, Is.EqualTo(officer.Id));
            Assert.That(officer.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(right.Id));

            var roster = _projector.ProjectCommanders(session.FactionId)!.Single(c => c.Id == officer.Id);
            Assert.That(roster.AssignmentName, Is.EqualTo("ISS Right"));

            var (_, unattached) = _projector.ProjectFleetHierarchy(session.FactionId);
            Assert.That(unattached.Single(s => s.Id == left.Id).CommanderId, Is.Null);
            Assert.That(unattached.Single(s => s.Id == right.Id).CommanderId, Is.EqualTo(officer.Id));
        }

        [Test]
        public void AssignCaptain_none_clears_the_chair_and_a_goal_is_rejected()
        {
            var session = Connect();
            var officer = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 6);
            var ship = MakeShip(session.FactionId, "ISS Test");
            var seated = _server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, officer.Id));
            Assert.That(seated.Accepted, Is.True, seated.RejectionReason);

            var cleared = _server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, -1));

            Assert.That(cleared.Accepted, Is.True, cleared.RejectionReason);
            Assert.That(ship.GetDataBlob<ShipInfoDB>().CommanderID, Is.EqualTo(-1));
            Assert.That(officer.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
            Assert.That(_projector.ProjectCommanders(session.FactionId)!.Single(c => c.Id == officer.Id).IsAssigned, Is.False);

            var move = _server.SubmitCommand(session, new MoveToBodyCommand(ship.Id, 1));
            Assert.That(move.Accepted, Is.False);
            Assert.That(move.RejectionReason, Does.Contain("captain"));
        }

        [Test]
        public void AssignCaptain_rejects_anyone_who_is_not_a_free_navy_officer()
        {
            var session = Connect();
            var ship = MakeShip(session.FactionId, "ISS Test");
            var scientist = MakeCommander(session.FactionId, CommanderTypes.Scientist);
            var civilian = MakeCommander(session.FactionId, CommanderTypes.Civilian);
            var ground = MakeCommander(session.FactionId, CommanderTypes.Ground);
            int otherFactionId = _game.Factions.Keys.First(id => id != session.FactionId);
            var outsider = MakeCommander(otherFactionId, CommanderTypes.Navy, rank: 6);

            var posted = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 3);
            var post = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(post, new List<BaseDataBlob>
            {
                new NameDB("City Hall", session.FactionId, "City Hall"),
            });
            posted.GetDataBlob<CommanderDB>().AssignedTo = post.Id;

            Assert.That(_server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, scientist.Id)).RejectionReason,
                Does.Contain("navy"));
            Assert.That(_server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, civilian.Id)).RejectionReason,
                Does.Contain("navy"));
            Assert.That(_server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, ground.Id)).RejectionReason,
                Does.Contain("navy"));
            Assert.That(_server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, outsider.Id)).RejectionReason,
                Does.Contain("not found"));
            Assert.That(_server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, posted.Id)).RejectionReason,
                Does.Contain("post"));

            Assert.That(ship.GetDataBlob<ShipInfoDB>().CommanderID, Is.EqualTo(-1));
            Assert.That(posted.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(post.Id));
            Assert.That(scientist.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
        }

        [Test]
        public void AssignCaptain_rejects_a_fleet()
        {
            var session = Connect();
            var officer = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 6);
            var created = _server.SubmitCommand(session, new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            Assert.That(created.Accepted, Is.True, created.RejectionReason);
            var fleet = _projector.ProjectFleetHierarchy(session.FactionId).Fleets[0];

            var result = _server.SubmitCommand(session, new AssignCaptainCommand(fleet.Id, officer.Id));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("ship"));
            Assert.That(officer.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
        }

        [Test]
        public void Fleet_commander_is_the_flagship_captain()
        {
            var session = Connect();
            var officer = MakeCommander(session.FactionId, CommanderTypes.Navy, rank: 6);
            var ship = MakeShip(session.FactionId, "ISS Flag");
            var created = _server.SubmitCommand(session, new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            Assert.That(created.Accepted, Is.True, created.RejectionReason);
            int fleetId = _projector.ProjectFleetHierarchy(session.FactionId).Fleets[0].Id;

            var moved = _server.SubmitCommand(session, new ReassignShipCommand(ship.Id, fleetId));
            Assert.That(moved.Accepted, Is.True, moved.RejectionReason);
            var seated = _server.SubmitCommand(session, new AssignCaptainCommand(ship.Id, officer.Id));
            Assert.That(seated.Accepted, Is.True, seated.RejectionReason);

            var fleet = _projector.ProjectFleetHierarchy(session.FactionId).Fleets.Single(f => f.Id == fleetId);
            Assert.That(fleet.FlagshipId, Is.EqualTo(ship.Id));
            Assert.That(fleet.CommanderId, Is.EqualTo(officer.Id));
            Assert.That(fleet.CommanderName, Is.EqualTo(officer.GetName(session.FactionId)));
            Assert.That(fleet.Ships.Single().CommanderId, Is.EqualTo(officer.Id));
        }
    }
}
