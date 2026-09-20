using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// The generic command pipeline — resolution, the uniform ownership pre-check, and dispatch —
    /// exercised through <see cref="RenameCommand"/>, the ported reference command.
    /// </summary>
    [TestFixture]
    public class ApiCommandTests : ApiTestBase
    {
        [Test]
        public void SubmitCommand_rejects_an_unknown_entity()
        {
            var session = Connect();

            var result = _server.SubmitCommand(session, new RenameCommand(TargetEntityId: -123456, NewName: "ghost"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.Not.Null.And.Contains("not found"));
        }

        [Test]
        public void SubmitCommand_rejects_an_unowned_entity()
        {
            var session = Connect();
            // Default-visible bodies are neutral; the faction does not own them.
            int neutralBodyId = ProjectSystem(session).Entities.First().Id;

            var result = _server.SubmitCommand(session, new RenameCommand(neutralBodyId, "Mine Now"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("does not control"));
        }

        [Test]
        public void SubmitCommand_renames_an_owned_entity_and_the_effect_is_visible()
        {
            var session = Connect();
            int bodyId = ProjectSystem(session).Entities.First().Id;

            // Give the connected faction ownership so it may command the entity.
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(bodyId, out var body), Is.True);
            body.FactionOwnerID = session.FactionId;

            var result = _server.SubmitCommand(session, new RenameCommand(bodyId, "Faction Renamed"));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            // The engine applied the faction-scoped rename and the projection reflects it.
            Assert.That(_projector.ProjectEntity(body, session.FactionId).GetView<NameView>()!.Name,
                Is.EqualTo("Faction Renamed"));
        }
    }
}
