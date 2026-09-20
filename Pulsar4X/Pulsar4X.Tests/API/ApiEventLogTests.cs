using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Events;

namespace Pulsar4X.Tests
{
    /// <summary>The game-log stream (phase 5): the engine's EventManager bridged into
    /// faction-filtered, display-ready LogEvent envelopes, plus the backlog push on subscribe.</summary>
    [TestFixture]
    public class ApiEventLogTests : ApiTestBase
    {
        private static List<LogEvent> LogEntries(IEnumerable<GameEventEnvelope> received)
            => received.Where(e => e.Type == GameEventType.LogEvent)
                       .SelectMany(e => e.Log ?? Array.Empty<LogEvent>())
                       .ToList();

        [Test]
        public void A_live_event_for_the_faction_is_delivered_with_resolved_names()
        {
            var session = Connect();
            int entityId = ProjectSystem(session).Entities
                .First(e => e.GetView<NameView>() != null).Id;
            string systemId = _game.Systems[0].ID;

            var received = new List<GameEventEnvelope>();
            using var _ = _server.Subscribe(session, received.Add);
            received.Clear();

            EventManager.Instance.Publish(Event.Create(
                EventType.ResearchCompleted, _game.TimePulse.GameGlobalDateTime, "research done",
                session.FactionId, systemId, entityId));

            var log = LogEntries(received);
            Assert.That(log, Has.Count.EqualTo(1));
            Assert.That(log[0].Message, Is.EqualTo("research done"));
            Assert.That(log[0].EventType, Is.EqualTo(nameof(EventType.ResearchCompleted)));
            Assert.That(log[0].SystemId, Is.EqualTo(systemId));
            Assert.That(log[0].EntityId, Is.EqualTo(entityId));
            Assert.That(log[0].EntityName, Is.Not.Null.And.Not.Empty,
                "entity names are resolved server-side so the client renders without lookups");
            Assert.That(log[0].FactionName, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Another_factions_event_is_filtered_out()
        {
            var session = Connect();
            var received = new List<GameEventEnvelope>();
            using var _ = _server.Subscribe(session, received.Add);
            received.Clear();

            EventManager.Instance.Publish(Event.Create(
                EventType.ResearchCompleted, _game.TimePulse.GameGlobalDateTime, "not for you",
                factionId: -12345));

            Assert.That(LogEntries(received), Is.Empty);
        }

        [Test]
        public void A_concerned_faction_receives_an_event_addressed_elsewhere()
        {
            var session = Connect();
            var received = new List<GameEventEnvelope>();
            using var _ = _server.Subscribe(session, received.Add);
            received.Clear();

            EventManager.Instance.Publish(Event.Create(
                EventType.ShipDestroyed, _game.TimePulse.GameGlobalDateTime, "mutual battle",
                factionId: -12345, concernedFactions: new List<int> { session.FactionId }));

            var log = LogEntries(received);
            Assert.That(log, Has.Count.EqualTo(1));
            Assert.That(log[0].Message, Is.EqualTo("mutual battle"));
        }

        [Test]
        public void The_persisted_backlog_is_pushed_on_subscribe()
        {
            var session = Connect();

            // Published before subscribing: lands only in the faction's persisted event log.
            EventManager.Instance.Publish(Event.Create(
                EventType.MineralsLocated, _game.TimePulse.GameGlobalDateTime, "from the backlog",
                session.FactionId));

            var received = new List<GameEventEnvelope>();
            using var _ = _server.Subscribe(session, received.Add);

            var log = LogEntries(received);
            Assert.That(log.Select(e => e.Message), Does.Contain("from the backlog"));
        }

        [Test]
        public void Disposing_the_subscription_stops_the_stream()
        {
            var session = Connect();
            var received = new List<GameEventEnvelope>();
            var subscription = _server.Subscribe(session, received.Add);
            subscription.Dispose();
            received.Clear();

            EventManager.Instance.Publish(Event.Create(
                EventType.ResearchCompleted, _game.TimePulse.GameGlobalDateTime, "after dispose",
                session.FactionId));

            Assert.That(LogEntries(received), Is.Empty);
        }
    }
}
