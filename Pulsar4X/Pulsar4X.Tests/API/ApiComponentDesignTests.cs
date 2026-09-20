using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;

namespace Pulsar4X.Tests
{
    /// <summary>The component-design surface: the templates/designs snapshot, the DesignerInputs
    /// extract/replay seam the client-side designer uses, and server-validated design creation.</summary>
    [TestFixture]
    public class ApiComponentDesignTests : ApiTestBase
    {
        private FactionInfoDB Info(PlayerSession session)
            => _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>();

        private FactionTechDB Techs(PlayerSession session)
            => _game.Factions[session.FactionId].GetDataBlob<FactionTechDB>();

        /// <summary>The faction needs at least one unlocked template to design from.</summary>
        private string EnsureATemplate(PlayerSession session)
        {
            var data = Info(session).Data;
            if (data.ComponentTemplates.Count == 0)
                data.Unlock(data.LockedComponentTemplates.Keys.First());
            return data.ComponentTemplates.Keys.First();
        }

        /// <summary>Mimic a real colony-blueprint start (the test start leaves everything locked):
        /// open the full catalogue and level every tech so template formulas resolve.</summary>
        private void UnlockEverything(PlayerSession session)
        {
            var data = Info(session).Data;
            foreach (var id in data.LockedComponentTemplates.Keys.ToList())
                data.Unlock(id);
            foreach (var uniqueId in data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
                data.Unlock(uniqueId);
            foreach (var id in data.LockedTechs.Keys.ToList())
                data.Unlock(id);
            foreach (var id in data.Techs.Keys.ToList())
                data.IncrementTechLevel(id);
        }

        /// <summary>Templates evaluate against the faction's unlocked materials; pick one whose
        /// designer constructs cleanly (the base mod has templates referencing locked materials).</summary>
        private string FindDesignableTemplate(PlayerSession session)
        {
            UnlockEverything(session);
            var info = Info(session);
            var techs = Techs(session);

            foreach (var templateId in info.Data.ComponentTemplates.Keys.ToList())
            {
                try
                {
                    DesignerInputs.Build(info.Data, techs, info.Data.ComponentTemplates[templateId],
                        Array.Empty<DesignerInput>());
                    return templateId;
                }
                catch
                {
                    // template references data this faction can't resolve — skip it
                }
            }

            Assert.Inconclusive("no template in the test mod data evaluates cleanly");
            return "";
        }

        [Test]
        public void ComponentDesigns_snapshot_is_pushed_on_subscribe()
        {
            var session = Connect();
            string templateId = EnsureATemplate(session);

            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);

            var push = received.FirstOrDefault(e => e.Type == GameEventType.ComponentDesignsChanged);
            Assert.That(push, Is.Not.Null, "expected the faction's templates/designs in the initial push");
            Assert.That(push!.ComponentDesigns!.Templates.Select(t => t.Id), Does.Contain(templateId));
        }

        [Test]
        public void DesignerInputs_roundtrip_reproduces_the_designer_state()
        {
            var session = Connect();
            UnlockEverything(session);
            var info = Info(session);
            var techs = Techs(session);

            // Infrastructure has the richest input shape: text displays, plus two range-slider pairs
            // whose upper bounds are GuiHint-less but player-set.
            var template = info.Data.ComponentTemplates["infrastructure"];
            var designer = DesignerInputs.Build(info.Data, techs, template, Array.Empty<DesignerInput>());

            var minGravity = designer.ComponentDesignProperties["Min Gravity"];
            var maxGravity = designer.ComponentDesignProperties["Max Gravity"];
            minGravity.SetMin();
            minGravity.SetMax();
            double newLow = minGravity.MinValue + (minGravity.MaxValue - minGravity.MinValue) * 0.25;
            double newHigh = minGravity.MinValue + (minGravity.MaxValue - minGravity.MinValue) * 0.5;
            minGravity.SetValueFromInput(newLow);
            maxGravity.SetValueFromInput(newHigh);

            var inputs = DesignerInputs.Extract(designer);
            Assert.That(inputs.Select(i => i.PropertyName), Does.Contain("Max Gravity"),
                "the range pair's GuiHint-less upper bound is player-set and must be extracted");
            Assert.That(inputs.Select(i => i.PropertyName), Does.Not.Contain("DBargs"),
                "attribute-constructor bookkeeping properties must not be replayed as inputs");

            var replayed = DesignerInputs.Build(info.Data, techs, template, inputs);
            Assert.That(replayed.ComponentDesignProperties["Min Gravity"].Value, Is.EqualTo(newLow).Within(0.0001));
            Assert.That(replayed.ComponentDesignProperties["Max Gravity"].Value, Is.EqualTo(newHigh).Within(0.0001));
        }

        [Test]
        public void CreateComponentDesign_registers_the_design_and_pushes_designs_and_research()
        {
            var session = Connect();
            string templateId = FindDesignableTemplate(session);

            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);
            received.Clear(); // discard the initial connect push

            var result = _server.SubmitCommand(session,
                new CreateComponentDesignCommand(session.FactionId, templateId, "Test Design", Array.Empty<DesignerInput>()));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            var design = Info(session).ComponentDesigns.Values.FirstOrDefault(d => d.Name == "Test Design");
            Assert.That(design, Is.Not.Null, "expected the design registered on the faction");

            var designsPush = received.LastOrDefault(e => e.Type == GameEventType.ComponentDesignsChanged);
            Assert.That(designsPush, Is.Not.Null, "expected a designs refresh after creation");
            Assert.That(designsPush!.ComponentDesigns!.Designs.Select(d => d.Name), Does.Contain("Test Design"));

            Assert.That(received.Any(e => e.Type == GameEventType.ResearchChanged), Is.True,
                "a new design registers a research project, so research must refresh too");
        }

        [Test]
        public void CreateComponentDesign_with_a_bad_template_or_name_is_rejected()
        {
            var session = Connect();
            string templateId = EnsureATemplate(session);

            var noName = _server.SubmitCommand(session,
                new CreateComponentDesignCommand(session.FactionId, templateId, "  ", Array.Empty<DesignerInput>()));
            Assert.That(noName.Accepted, Is.False);

            var noTemplate = _server.SubmitCommand(session,
                new CreateComponentDesignCommand(session.FactionId, "no-such-template", "Name", Array.Empty<DesignerInput>()));
            Assert.That(noTemplate.Accepted, Is.False);
        }
    }
}
