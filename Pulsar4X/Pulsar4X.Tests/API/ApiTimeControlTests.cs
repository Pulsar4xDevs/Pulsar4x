using System;
using NUnit.Framework;
using Pulsar4X.Api;

namespace Pulsar4X.Tests
{
    /// <summary>The time-control write surface (the clock itself pushes TimeChanged deltas).</summary>
    [TestFixture]
    public class ApiTimeControlTests : ApiTestBase
    {
        [Test]
        public void StepOnce_advances_the_clock()
        {
            var session = Connect();
            var before = _game.TimePulse.GameGlobalDateTime;
            var step = TimeSpan.FromDays(1);

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.StepOnce, StepLength: step));

            Assert.That(_game.TimePulse.GameGlobalDateTime, Is.EqualTo(before + step));
        }

        [Test]
        public void SetTickFrequency_changes_the_real_time_tick_interval()
        {
            var session = Connect();
            var frequency = TimeSpan.FromMilliseconds(250);

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.SetTickFrequency, TickFrequency: frequency));

            Assert.That(_game.TimePulse.TickFrequency, Is.EqualTo(frequency));
        }

        [Test]
        public void SetTickFrequency_is_clamped_to_the_supported_range()
        {
            var session = Connect();

            // PeriodicTimer rejects intervals below 1ms; the engine clamps rather than crashing.
            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.SetTickFrequency, TickFrequency: TimeSpan.Zero));

            Assert.That(_game.TimePulse.TickFrequency, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1)));
        }
    }
}
