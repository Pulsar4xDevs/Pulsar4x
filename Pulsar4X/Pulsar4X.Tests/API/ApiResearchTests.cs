using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Interfaces;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests
{
    /// <summary>The research read surface (ResearchSnapshot, ResearcherView) and lab commands.</summary>
    [TestFixture]
    public class ApiResearchTests : ApiTestBase
    {
        /// <summary>Unlocks the first locked tech for the session's faction and returns its id.</summary>
        private string UnlockATech(PlayerSession session)
        {
            var data = _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>().Data;
            string techId = data.LockedTechs.Keys.First();
            data.Unlock(techId);
            return techId;
        }

        /// <summary>Labs are normally spawned on component installation; fabricate one directly.</summary>
        private Entity MakeLab(PlayerSession session, ResearcherDB researcherDB)
        {
            var lab = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(lab, new List<BaseDataBlob>
            {
                researcherDB,
                new OrderableDB(),
            });
            return lab;
        }

        [Test]
        public void ProjectResearch_returns_categories_and_unlocked_techs()
        {
            var session = Connect();
            string techId = UnlockATech(session);

            var research = _projector.ProjectResearch(session.FactionId);

            Assert.That(research, Is.Not.Null);
            Assert.That(research!.Categories, Is.Not.Empty, "expected tech categories from mod data");
            var tech = research.Techs.FirstOrDefault(t => t.Id == techId);
            Assert.That(tech, Is.Not.Null, "expected the unlocked tech in the snapshot");
            Assert.That(tech!.IsResearchable, Is.True);
        }

        [Test]
        public void Research_lab_commands_drive_queue_and_funding_and_push_a_refresh()
        {
            var session = Connect();
            string techId = UnlockATech(session);
            var lab = MakeLab(session, new ResearcherDB(new StubLabDesign()));

            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);
            received.Clear(); // discard the initial connect push

            var addResult = _server.SubmitCommand(session, new AddTechToQueueCommand(lab.Id, techId));
            Assert.That(addResult.Accepted, Is.True, addResult.RejectionReason);

            var view = _projector.ProjectEntity(lab, session.FactionId).GetView<ResearcherView>();
            Assert.That(view, Is.Not.Null, "expected a ResearcherView for the owning faction");
            Assert.That(view!.TechQueue, Does.Contain(techId));

            var fundingResult = _server.SubmitCommand(session, new SetResearchFundingCommand(lab.Id, 3));
            Assert.That(fundingResult.Accepted, Is.True, fundingResult.RejectionReason);
            view = _projector.ProjectEntity(lab, session.FactionId).GetView<ResearcherView>();
            Assert.That(view!.FundingLevel, Is.EqualTo(3));

            // Instant orders don't raise engine messages; the server itself pushes a refresh of the
            // commanded entity after each accepted command.
            Assert.That(received.Any(e => e.Type == GameEventType.EntityChanged
                                          && e.EntityId == lab.Id
                                          && e.Entity?.GetView<ResearcherView>() != null),
                Is.True, "expected a post-command EntityChanged push carrying the lab's new state");
        }

        [Test]
        public void Research_completion_pushes_the_advanced_lab_queue()
        {
            var session = Connect();
            string techId = UnlockATech(session);

            // Enough output to finish any tech in a single daily research pulse, at no cost.
            var lab = MakeLab(session, new ResearcherDB(new StubLabDesign())
            {
                PointsPerDay = new(int.MaxValue / 2),
                CostPerDay = new(0),
            });

            var queueResult = _server.SubmitCommand(session, new AddTechToQueueCommand(lab.Id, techId));
            Assert.That(queueResult.Accepted, Is.True, queueResult.RejectionReason);

            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);
            received.Clear(); // discard the initial connect push

            // Step past the daily research pulse so the tech completes and is dequeued mid-tick.
            _server.SetTimeControl(session,
                new TimeControlRequest(TimeControlAction.StepOnce, StepLength: TimeSpan.FromDays(2)));

            Assert.That(lab.GetDataBlob<ResearcherDB>().TechQueue, Does.Not.Contain(techId),
                "expected the engine to dequeue the completed tech");

            // The completion is dequeued mid-tick with no engine message, so the engine publishes an
            // EntityChanged message that the server turns into a push carrying the advanced queue.
            var push = received.LastOrDefault(e => e.Type == GameEventType.EntityChanged && e.EntityId == lab.Id);
            Assert.That(push, Is.Not.Null, "expected an EntityChanged push for the lab on research completion");
            Assert.That(push!.Entity!.GetView<ResearcherView>()!.TechQueue, Does.Not.Contain(techId));
        }

        /// <summary>Minimal design so a ResearcherDB can exist outside the component pipeline.</summary>
        private sealed class StubLabDesign : IConstructableDesign
        {
            public ConstructableGuiHints GuiHints => ConstructableGuiHints.None;
            public string UniqueID => "stub-lab-design";
            public string Name => "Stub Lab";
            public bool IsValid => true;
            public Dictionary<string, long> ResourceCosts { get; } = new();
            public long IndustryPointCosts => 0;
            public string IndustryTypeID => "";
            public ushort OutputAmount => 1;
            public void OnConstructionComplete(Entity industryEntity, Pulsar4X.Storage.CargoStorageDB storage,
                string productionLine, Pulsar4X.Industry.IndustryJob batchJob, IConstructableDesign designInfo)
            { }
        }
    }
}
