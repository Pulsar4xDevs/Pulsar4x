using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.GeoSurveys;

namespace Pulsar4X.Tests
{
    /// <summary>Projection of star systems and their entities into faction-scoped snapshots/views.</summary>
    [TestFixture]
    public class ApiSystemProjectionTests : ApiTestBase
    {
        [Test]
        public void SystemSnapshot_projects_visible_bodies_with_views()
        {
            var session = Connect();
            var snapshot = ProjectSystem(session);

            Assert.That(snapshot.Entities, Is.Not.Empty, "expected the system's default-visible bodies");
            Assert.That(snapshot.Entities.All(e => e.GetView<NameView>() != null), Is.True);
            Assert.That(snapshot.Entities.Any(e => e.HasView<PositionView>()), Is.True);
            Assert.That(snapshot.Entities.Any(e => e.HasView<OrbitView>()), Is.True);

            // Round-tripping a single entity yields the same identity.
            var first = snapshot.Entities.First();
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(first.Id, out var entity), Is.True);
            Assert.That(_projector.ProjectEntity(entity, session.FactionId).Id, Is.EqualTo(first.Id));
        }

        [Test]
        public void SystemSnapshot_projects_body_star_and_mass_views()
        {
            var session = Connect();
            var entities = ProjectSystem(session).Entities;

            // A generated system always has a star and orbiting bodies with mass.
            Assert.That(entities.Any(e => e.HasView<StarView>()), Is.True, "expected a StarView");
            Assert.That(entities.Any(e => e.HasView<BodyView>()), Is.True, "expected a BodyView");
            Assert.That(entities.Any(e => e.HasView<MassVolumeView>()), Is.True, "expected a MassVolumeView");

            var star = entities.First(e => e.HasView<StarView>()).GetView<StarView>()!;
            Assert.That(star.SurfaceTemperatureC, Is.GreaterThan(0));
            Assert.That(string.IsNullOrEmpty(star.SpectralType), Is.False);
        }

        [Test]
        public void SystemSnapshot_classifies_body_kinds()
        {
            var session = Connect();
            var entities = ProjectSystem(session).Entities;

            Assert.That(entities.Any(e => e.Kind == BodyKind.Star), Is.True, "expected a star");
            Assert.That(
                entities.Any(e => e.Kind is BodyKind.Planet or BodyKind.DwarfPlanet or BodyKind.Moon
                                       or BodyKind.Asteroid or BodyKind.Comet),
                Is.True, "expected at least one orbiting celestial body");
        }

        [Test]
        public void GeoSurveyView_is_faction_scoped()
        {
            var session = Connect();
            // The procedurally-generated test universe doesn't attach survey blobs (only the
            // blueprint/JSON body paths do), so attach one to a visible body explicitly.
            int bodyId = ProjectSystem(session).Entities.First(e => e.HasView<BodyView>()).Id;
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(bodyId, out var body), Is.True);
            body.SetDataBlob(new GeoSurveyableDB { PointsRequired = 100 });

            var view = _projector.ProjectEntity(body, session.FactionId).GetView<GeoSurveyView>();
            Assert.That(view, Is.Not.Null, "expected a GeoSurveyView once the body is surveyable");
            Assert.That(view!.IsSurveyComplete, Is.False);

            // Completing the survey for this faction flips its (faction-scoped) view.
            body.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[session.FactionId] = 0;
            Assert.That(_projector.ProjectEntity(body, session.FactionId).GetView<GeoSurveyView>()!.IsSurveyComplete,
                Is.True);
        }
    }
}
