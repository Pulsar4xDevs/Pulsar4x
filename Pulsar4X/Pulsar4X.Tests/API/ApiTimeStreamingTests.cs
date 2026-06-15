using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Pulsar4X.Api;

namespace Pulsar4X.Tests
{
    /// <summary>The focused system's sub-step clock must stream to the client during a long pulse so
    /// the map keeps moving, rather than freezing until the whole pulse completes (a regression that
    /// appeared when TimeMultiplier was removed and the only clock source was the coarse global date).</summary>
    [TestFixture]
    public class ApiTimeStreamingTests : ApiTestBase
    {
        [Test]
        public void Focused_system_streams_progressive_clock_during_long_pulse()
        {
            _game.Settings.EnforceSingleThread = false;
            var session = Connect();
            _server.SetSystemFocus(session, _game.Systems[0].ID);

            var times = new ConcurrentQueue<DateTime>();
            using var sub = _server.Subscribe(session, e =>
            {
                if (e.Type == GameEventType.TimeChanged && e.Time != null) times.Enqueue(e.Time.GameDateTime);
            });

            var start = _game.TimePulse.GameGlobalDateTime;
            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.SetTickLength, TickLength: TimeSpan.FromDays(365)));
            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.SetTickFrequency, TickFrequency: TimeSpan.FromMilliseconds(10)));
            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.Start));
            Thread.Sleep(800);
            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.Pause));
            Thread.Sleep(150);

            var distinct = times.Distinct().Where(t => t > start).OrderBy(t => t).ToList();
            TestContext.WriteLine($"distinct progressive clock values = {distinct.Count}");
            if (distinct.Any())
            {
                TestContext.WriteLine($"first advance = {(distinct.First() - start).TotalDays:0.##} days (TickLength=365)");
                TestContext.WriteLine($"last  advance = {(distinct.Last() - start).TotalDays:0.##} days");
            }

            Assert.That(distinct.Count, Is.GreaterThan(3), "expected progressive sub-step clock updates within the long pulse, not one jump");
            Assert.That((distinct.First() - start), Is.LessThan(TimeSpan.FromDays(365)), "first clock update should be a sub-step (mid-pulse), not a full-TickLength jump");
        }

        [Test]
        public void Pausing_pushes_a_final_stopped_clock_so_controls_unlock()
        {
            _game.Settings.EnforceSingleThread = false;
            var session = Connect();
            _server.SetSystemFocus(session, _game.Systems[0].ID);

            var states = new ConcurrentQueue<TimeState>();
            using var sub = _server.Subscribe(session, e =>
            {
                if (e.Type == GameEventType.TimeChanged && e.Time != null) states.Enqueue(e.Time);
            });

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.SetTickFrequency, TickFrequency: TimeSpan.FromMilliseconds(10)));
            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.Start));
            Thread.Sleep(300);
            Assert.That(_game.TimePulse.IsRunning, Is.True, "should be running before pause");

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.Pause));

            // Wait for the sim loop to actually finish its in-flight pulse and stop.
            for (int i = 0; i < 100 && _game.TimePulse.IsRunning; i++) Thread.Sleep(20);
            Assert.That(_game.TimePulse.IsRunning, Is.False, "the clock should have stopped after pause");
            Thread.Sleep(100); // let the stop notification drain

            // The LAST clock the client received must report the stopped state, or its controls stay locked.
            Assert.That(states.IsEmpty, Is.False);
            var last = states.Last();
            Assert.That(last.IsRunning, Is.False, "final pushed clock must be IsRunning=false so the UI unlocks");
            Assert.That(last.IsStopping, Is.False, "final pushed clock must clear IsStopping");
        }
    }
}
