using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Events;

namespace Pulsar4X.Tests
{
    public class GameLogPauseOptionsTests
    {
        [Test]
        public void BudgetDay_IsAnEconomyPauseOption()
        {
            var options = GameLogWindow.PauseOptions().ToList();
            Assert.That(options, Has.Some.EqualTo(("Economy", "BudgetDay", "Budget day")));
        }

        [Test]
        public void CrewTransferred_IsAnEconomyPauseOption()
        {
            var options = GameLogWindow.PauseOptions().ToList();
            Assert.That(options, Has.Some.EqualTo(("Economy", "CrewTransferred", "Crew transferred")));
        }

        [Test]
        public void NpcCompanySpawnedAndFolded_AreEconomyPauseOptions()
        {
            var options = GameLogWindow.PauseOptions().ToList();
            Assert.That(options, Has.Some.EqualTo(("Economy", "NpcCompanySpawned", "Company founded")));
            Assert.That(options, Has.Some.EqualTo(("Economy", "NpcCompanyFolded", "Company folded")));
        }

        [Test]
        public void EveryPauseOption_ParsesAsAnEngineEventType_LikeTheHaltCommand()
        {
            // SetEventHaltCommand is accepted only when its EventType string parses, case-sensitively, to a defined EventType.
            foreach (var (_, type, _) in GameLogWindow.PauseOptions())
            {
                Assert.That(Enum.TryParse<EventType>(type, ignoreCase: false, out var parsed), Is.True, type);
                Assert.That(Enum.IsDefined(typeof(EventType), parsed), Is.True, type);
            }
        }
    }
}
