using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;

namespace Pulsar4X.Tests
{
    /// <summary>Connecting a session and the initial state pushed on Subscribe.</summary>
    [TestFixture]
    public class ApiConnectionTests : ApiTestBase
    {
        [Test]
        public void Connect_binds_to_a_real_faction()
        {
            var session = Connect();
            Assert.That(_game.Factions.ContainsKey(session.FactionId), Is.True);
            Assert.That(session.SessionId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Subscribe_pushes_initial_state()
        {
            var session = Connect();
            var received = new List<GameEventEnvelope>();

            // The initial state is pushed synchronously on Subscribe (no client fetch).
            using (_server.Subscribe(session, received.Add))
            {
                Assert.That(received.Any(e => e.Type == GameEventType.TimeChanged && e.Time != null), Is.True,
                    "expected an initial TimeChanged push");
                Assert.That(received.Any(e => e.Type == GameEventType.FactionChanged && e.Faction != null), Is.True,
                    "expected an initial FactionChanged push");
                Assert.That(received.Any(e => e.Type == GameEventType.FleetsChanged && e.Fleets != null), Is.True,
                    "expected an initial FleetsChanged push");
            }
        }
    }
}
