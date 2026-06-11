using System;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Tests
{
    /// <summary>The OrbitView Keplerian-element surface: a client must be able to rebuild the
    /// engine's positions from the projected elements alone (client-side orbit propagation).</summary>
    [TestFixture]
    public class ApiOrbitPropagationTests : ApiTestBase
    {
        /// <summary>The same OrbitView→KeplerElements conversion the client-side
        /// SnapshotOrbits helper performs (duplicated here — these tests carry no UI reference).</summary>
        private static KeplerElements ToKeplerElements(OrbitView orbit)
        {
            double a = orbit.SemiMajorAxisM;
            double e = orbit.Eccentricity;
            return new KeplerElements
            {
                StandardGravParameter = orbit.StandardGravParameter,
                SemiMajorAxis = a,
                SemiMinorAxis = a * Math.Sqrt(1 - e * e),
                Eccentricity = e,
                LinearEccentricity = e * a,
                Periapsis = (1 - e) * a,
                Apoapsis = (1 + e) * a,
                LoAN = orbit.LongitudeOfAscendingNodeRad,
                AoP = orbit.ArgumentOfPeriapsisRad,
                Inclination = orbit.InclinationRad,
                MeanMotion = orbit.MeanMotionRadPerSec,
                Period = orbit.OrbitalPeriodSeconds,
                MeanAnomalyAtEpoch = orbit.MeanAnomalyAtEpochRad,
                Epoch = orbit.Epoch,
            };
        }

        [Test]
        public void OrbitView_elements_reproduce_the_engines_positions()
        {
            var session = Connect();

            // Step well past the epoch so a stale projection would show up as a position error.
            _server.SetTimeControl(session,
                new TimeControlRequest(TimeControlAction.StepOnce, StepLength: TimeSpan.FromDays(5)));

            var system = _game.Systems[0];
            DateTime now = system.StarSysDateTime;

            int verified = 0;
            foreach (var entity in system.GetAllEntitiesWithDataBlob<OrbitDB>())
            {
                var orbitDB = entity.GetDataBlob<OrbitDB>();
                if (orbitDB.Parent == null || !orbitDB.Parent.IsValid)
                    continue;

                var view = _projector.ProjectEntity(entity, session.FactionId).GetView<OrbitView>();
                Assert.That(view, Is.Not.Null, "expected an OrbitView for an orbiting body");

                var fromView = OrbitalMath.GetPosition(ToKeplerElements(view!), now);
                var fromEngine = orbitDB.GetPosition(now);

                Assert.That((fromView - fromEngine).Length(), Is.LessThan(1000),
                    $"client-propagated position diverged for entity {entity.Id}");
                verified++;
            }

            Assert.That(verified, Is.GreaterThan(0), "expected at least one orbiting body to verify");
        }
    }
}
