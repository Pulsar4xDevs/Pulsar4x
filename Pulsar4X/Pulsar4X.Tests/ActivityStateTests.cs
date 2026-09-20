using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Engine;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class ActivityStateTests
    {
        private Game _game;
        private StarSystem[] _distinctSystems;

        [SetUp]
        public void Setup()
        {
            _game = TestingUtilities.CreateTestUniverse(3, new DateTime(2100, 1, 1), false);
            _game.Settings.EnforceSingleThread = true;
            // CreateTestUniverse double-adds systems (Initialize + explicit Add),
            // so deduplicate to get the actual distinct systems.
            _distinctSystems = _game.Systems.Distinct().ToArray();
        }

        [Test]
        public void StasisSystemsDoNotAdvanceTime()
        {
            var system = _distinctSystems[0];
            system.SetActivityState(SystemActivityState.Stasis);
            var timeBefore = system.StarSysDateTime;

            _game.TimePulse.TimeStep();

            Assert.AreEqual(timeBefore, system.StarSysDateTime,
                "Stasis system should not advance its StarSysDateTime.");
        }

        [Test]
        public void ForegroundSystemsAdvanceTime()
        {
            var system = _distinctSystems[0];
            system.SetActivityState(SystemActivityState.Foreground);
            var timeBefore = system.StarSysDateTime;

            _game.TimePulse.TimeStep();

            Assert.Greater(system.StarSysDateTime, timeBefore,
                "Foreground system should advance its StarSysDateTime.");
        }

        [Test]
        public void BackgroundSystemsAdvanceTime()
        {
            var system = _distinctSystems[0];
            system.SetActivityState(SystemActivityState.Background);
            var timeBefore = system.StarSysDateTime;

            _game.TimePulse.TimeStep();

            Assert.Greater(system.StarSysDateTime, timeBefore,
                "Background system should advance its StarSysDateTime.");
        }

        [Test]
        public void DefaultActivityStateIsStasis()
        {
            var starSystem = new StarSystem();
            Assert.AreEqual(SystemActivityState.Stasis, starSystem.ActivityState);
        }

        [Test]
        public void SetActivityStateSetsFrequencyMultiplier()
        {
            var system = _distinctSystems[0];

            system.SetActivityState(SystemActivityState.Foreground);
            Assert.AreEqual(1.0, system.ManagerSubpulses.FrequencyMultiplier);

            system.SetActivityState(SystemActivityState.Background);
            Assert.AreEqual(10.0, system.ManagerSubpulses.FrequencyMultiplier);

            system.SetActivityState(SystemActivityState.Stasis);
            Assert.AreEqual(1.0, system.ManagerSubpulses.FrequencyMultiplier);
        }

        [Test]
        public void CatchUpFromStasisBringsTimeForward()
        {
            var system = _distinctSystems[0];
            system.SetActivityState(SystemActivityState.Stasis);

            // Advance game time without the stasis system
            _game.TimePulse.TimeStep();
            var globalTime = _game.TimePulse.GameGlobalDateTime;

            Assert.Less(system.StarSysDateTime, globalTime,
                "Stasis system should be behind global time.");

            // Promote to Background — should catch up
            system.SetActivityState(SystemActivityState.Background);

            Assert.AreEqual(globalTime, system.StarSysDateTime,
                "System should catch up to global time after leaving Stasis.");
        }

        [Test]
        public void TransferPromotesStasisSystem()
        {
            var sourceSystem = _distinctSystems[0];
            var targetSystem = _distinctSystems[1];
            sourceSystem.SetActivityState(SystemActivityState.Foreground);
            targetSystem.SetActivityState(SystemActivityState.Stasis);

            Assert.AreEqual(SystemActivityState.Stasis, targetSystem.ActivityState,
                "Pre-condition: target system should be Stasis.");

            // Create an entity in source and transfer to target
            var entity = Entity.Create();
            entity.FactionOwnerID = 1;
            sourceSystem.AddEntity(entity);
            targetSystem.Transfer(entity);

            Assert.AreEqual(SystemActivityState.Background, targetSystem.ActivityState,
                "Target system should be promoted to Background after receiving an entity.");
        }

        [Test]
        public void ExternalObserverPromotesToBackground()
        {
            var system = _distinctSystems[0];
            system.SetActivityStateInternal(SystemActivityState.Stasis);
            Assert.AreEqual(SystemActivityState.Stasis, system.ActivityState,
                "Pre-condition: system should be Stasis.");

            system.IncrementExternalObserver(false);
            Assert.AreEqual(SystemActivityState.Background, system.ActivityState,
                "System should be promoted to Background after gaining an external observer.");

            system.DecrementExternalObserver(false);
            Assert.AreEqual(SystemActivityState.Stasis, system.ActivityState,
                "System should return to Stasis after losing external observer.");
        }

        [Test]
        public void PriorityExternalObserverPromotesToForeground()
        {
            var system = _distinctSystems[0];
            system.SetActivityStateInternal(SystemActivityState.Stasis);
            Assert.AreEqual(SystemActivityState.Stasis, system.ActivityState,
                "Pre-condition: system should be Stasis.");

            system.IncrementExternalObserver(true);
            Assert.AreEqual(SystemActivityState.Foreground, system.ActivityState,
                "System should be promoted to Foreground after gaining a priority external observer.");

            system.DecrementExternalObserver(true);
            Assert.AreEqual(SystemActivityState.Stasis, system.ActivityState,
                "System should return to Stasis after losing priority external observer.");
        }

        [Test]
        public void ExternalObserverPromotionPromotesToForeground()
        {
            var system = _distinctSystems[0];
            system.SetActivityStateInternal(SystemActivityState.Stasis);
            Assert.AreEqual(SystemActivityState.Stasis, system.ActivityState,
                "Pre-condition: system should be Stasis.");

            system.IncrementExternalObserver(false);
            Assert.AreEqual(SystemActivityState.Background, system.ActivityState,
                "System should be promoted to Background after gaining an external observer.");

            system.PromoteExternalObserver();
            Assert.AreEqual(SystemActivityState.Foreground, system.ActivityState,
                "System should be promoted to Foreground after promoting an external observer.");

            system.DemoteExternalObserver();
            Assert.AreEqual(SystemActivityState.Background, system.ActivityState,
                "System should return to Background after demoting an external observer.");

            system.DecrementExternalObserver(false);
            Assert.AreEqual(SystemActivityState.Stasis, system.ActivityState,
                "System should return to Stasis after losing all external observers.");
        }

        [Test]
        public void MultipleExternalObserversMaintainState()
        {
            var system = _distinctSystems[0];
            system.SetActivityStateInternal(SystemActivityState.Stasis);
            Assert.AreEqual(SystemActivityState.Stasis, system.ActivityState,
                "Pre-condition: system should be Stasis.");

            system.IncrementExternalObserver(false);
            system.IncrementExternalObserver(false);
            Assert.AreEqual(SystemActivityState.Background, system.ActivityState,
                "System should be Background with multiple external observers.");

            system.IncrementExternalObserver(true);
            Assert.AreEqual(SystemActivityState.Foreground, system.ActivityState,
                "System should be Foreground with a priority external observer.");

            system.DecrementExternalObserver(true);
            Assert.AreEqual(SystemActivityState.Background, system.ActivityState,
                "System should return to Background after losing priority external observer.");

            system.DecrementExternalObserver(false);
            Assert.AreEqual(SystemActivityState.Background, system.ActivityState,
                "System should remain Background with one remaining external observer.");

            system.DecrementExternalObserver(false);
            Assert.AreEqual(SystemActivityState.Stasis, system.ActivityState,
                "System should return to Stasis after losing all external observers.");
        }
    }
}
