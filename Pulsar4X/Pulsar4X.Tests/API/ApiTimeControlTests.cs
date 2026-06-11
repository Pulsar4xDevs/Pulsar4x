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
        public void SetSpeed_changes_the_clock_multiplier()
        {
            var session = Connect();

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.SetSpeed, Multiplier: 4f));

            Assert.That(_game.TimePulse.TimeMultiplier, Is.EqualTo(4f));
        }
    }
}
