using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Row tags, the preview cache, and the porkchop hit test. None of these call the planner.
    /// </summary>
    public class MoveOrderClientTests
    {
        [Test]
        public void MoveTargetHints_TagsTheRow_AndFiveHundredRowsStayCheap()
        {
            var here = Body(10, BodyKind.Planet, orbitParent: 1);
            Assert.AreEqual(MoveTargetHints.Here, MoveTargetHints.Of(here, 10, "sol", "sol", null));

            var moon = Body(11, BodyKind.Moon, orbitParent: 10);
            Assert.AreEqual(MoveTargetHints.SameSphere, MoveTargetHints.Of(moon, 10, "sol", "sol", null));

            var mars = Body(12, BodyKind.Planet, orbitParent: 1);
            Assert.AreEqual(MoveTargetHints.OtherSphere, MoveTargetHints.Of(mars, 10, "sol", "sol", null));

            var star = Body(1, BodyKind.Star, orbitParent: null);
            Assert.IsNull(MoveTargetHints.Of(star, 10, "sol", "sol", null));

            var links = new Dictionary<string, IReadOnlyList<string>>
            {
                ["sol"] = new[] { "alpha" },
            };
            var abroad = Body(40, BodyKind.Planet, orbitParent: 30);
            Assert.AreEqual(MoveTargetHints.OtherSystem, MoveTargetHints.Of(abroad, 10, "sol", "alpha", links));
            Assert.AreEqual(MoveTargetHints.NoRoute, MoveTargetHints.Of(abroad, 10, "sol", "beta", links));

            var colony = new EntitySnapshot
            {
                Id = 50,
                Kind = BodyKind.Colony,
                Views = new IComponentView[] { new ColonyView(0, 12) },
            };
            Assert.AreEqual(12, MoveTargetHints.ParentId(colony));

            var rows = new EntitySnapshot[500];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = Body(1000 + i, BodyKind.Asteroid, i % 2 == 0 ? 10 : 20);
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < rows.Length; i++)
                MoveTargetHints.Of(rows[i], 10, "sol", "sol", null);
            watch.Stop();
            Assert.That(watch.Elapsed.TotalMilliseconds, Is.LessThan(5));
        }

        [Test]
        public void Session_PlansOncePerOption_AndDropsTheOldest()
        {
            int calls = 0;
            var session = new MovePreviewSession();
            TripPreview Load(MoveOrderOptions options)
            {
                calls++;
                return TripPreview.None("planned");
            }

            var options = new MoveOrderOptions();
            session.Get(1, 2, 3, options, Load);
            session.Get(1, 2, 3, options, Load);
            Assert.AreEqual(1, calls);

            session.Get(1, 2, 3, options with { Mode = MoveDriveChoice.Warp }, Load);
            Assert.AreEqual(2, calls, "one more plan when the options change");

            session.Get(1, 2, 3, options, Load);
            Assert.AreEqual(2, calls, "closing the panel does not ask again");

            var held = new MovePreviewSession();
            for (int body = 0; body < MovePreviewSession.Capacity; body++)
                held.Get(1, body, 0, options, Load);
            int filled = calls;
            held.Get(1, MovePreviewSession.Capacity - 1, 0, options, Load);
            Assert.AreEqual(filled, calls);
            held.Get(1, 100, 0, options, Load);
            held.Get(1, 0, 0, options, Load);
            Assert.AreEqual(filled + 2, calls, "the oldest entry was dropped");
        }

        [Test]
        public void PorkchopPanel_PicksTheCellUnderTheMouse()
        {
            Assert.IsTrue(PorkchopPanel.TryCellAt(10, 20, 10, 20, 100, 80, 4, 5, out int i, out int j));
            Assert.AreEqual(0, i);
            Assert.AreEqual(4, j);

            Assert.IsTrue(PorkchopPanel.TryCellAt(10, 20, 109.9f, 99.9f, 100, 80, 4, 5, out int lastI, out int lastJ));
            Assert.AreEqual(3, lastI);
            Assert.AreEqual(0, lastJ);

            Assert.IsFalse(PorkchopPanel.TryCellAt(10, 20, 9.9f, 20, 100, 80, 4, 5, out _, out _));
            Assert.IsFalse(PorkchopPanel.TryCellAt(10, 20, 10, 20, 0, 80, 4, 5, out _, out _));
        }

        [Test]
        public void ClickingARow_DoesNotStartAScan()
        {
            int before = PorkchopPanel.ScansStarted;
            MoveTargetHints.Of(Body(3, BodyKind.Moon, 2), 2, "sol", "sol", null);
            var session = new MovePreviewSession();
            session.Get(1, 3, 0, new MoveOrderOptions(), _ => TripPreview.None("planned"));
            Assert.AreEqual(before, PorkchopPanel.ScansStarted);
        }

        static EntitySnapshot Body(int id, BodyKind kind, int? orbitParent)
            => new EntitySnapshot
            {
                Id = id,
                Kind = kind,
                Views = new IComponentView[] { new OrbitView(1000, 0, 86400, orbitParent) },
            };
    }
}
