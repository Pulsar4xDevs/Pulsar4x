using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Names;

namespace Pulsar4X.Tests
{
    /// <summary>Visibility enforcement on the event stream: entity pushes a faction can't see are
    /// dropped at the server boundary.</summary>
    [TestFixture]
    public class ApiVisibilityTests : ApiTestBase
    {
        [Test]
        public void EntityAdded_pushes_are_visibility_filtered()
        {
            var session = Connect();
            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);
            received.Clear();

            Assert.That(session.FactionId, Is.Not.EqualTo(_game.GameMasterFaction.Id),
                "a default connect must never bind to the all-seeing GameMaster faction");

            // Another faction's entity, with no sensor contact: must not reach this client.
            int otherFactionId = _game.Factions.Keys
                .First(id => id != session.FactionId && id != _game.GameMasterFaction.Id);
            var hostile = Entity.Create(otherFactionId);
            _game.Systems[0].AddEntity(hostile, new List<BaseDataBlob>
            {
                new NameDB("Unseen", otherFactionId, "Unseen"),
            });

            var leaked = received.Where(e => e.EntityId == hostile.Id).ToList();
            Assert.That(leaked, Is.Empty,
                "an unseen foreign entity must not be pushed; got: "
                + string.Join(", ", leaked.Select(e => $"{e.Type}(entity={(e.Entity == null ? "null" : "set")},faction={e.FactionId})")));

            var own = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(own, new List<BaseDataBlob>
            {
                new NameDB("Ours", session.FactionId, "Ours"),
            });

            Assert.That(received.Any(e => e.Type == GameEventType.EntityAdded && e.EntityId == own.Id), Is.True,
                "the faction's own entity add must be pushed");
        }

        [Test]
        public void Connect_will_not_bind_to_the_GameMaster_faction_without_the_SM_credential()
        {
            int gmId = _game.GameMasterFaction.Id;

            // Requesting the GameMaster without the credential falls back to a player faction.
            var sneaky = _server.Connect(new ConnectRequest { FactionId = gmId });
            Assert.That(sneaky.Success, Is.True);
            Assert.That(sneaky.Session.FactionId, Is.Not.EqualTo(gmId),
                "binding to the GameMaster faction must require the SM credential");

            var sm = _server.Connect(new ConnectRequest
            {
                FactionId = gmId,
                Credential = ConnectRequest.SpaceMasterCredential,
            });
            Assert.That(sm.Success, Is.True);
            Assert.That(sm.Session.FactionId, Is.EqualTo(gmId),
                "the SM credential authorises binding to the GameMaster faction");
        }
    }
}
