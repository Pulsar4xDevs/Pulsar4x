using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Components;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.People;
using Pulsar4X.Energy;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Movement;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests
{
    /// <summary>The ship-design write surface: save (create/update), delete, obsolete. The
    /// interactive designer runs client-side; the server resolves the referenced component/armor
    /// ids, recalculates and validates on save.</summary>
    [TestFixture]
    public class ApiShipDesignTests : ApiTestBase
    {
        private FactionInfoDB Info(PlayerSession session)
            => _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>();

        /// <summary>Unlock the full catalogue (incl. armor) and register one component design the
        /// ship can be built from; returns (componentDesignId, armorId).</summary>
        private (string ComponentId, string ArmorId) SetUpDesignData(PlayerSession session)
        {
            var info = Info(session);
            var data = info.Data;
            foreach (var id in data.LockedComponentTemplates.Keys.ToList())
                data.Unlock(id);
            foreach (var uniqueId in data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
                data.Unlock(uniqueId);
            foreach (var id in data.LockedTechs.Keys.ToList())
                data.Unlock(id);
            foreach (var id in data.Techs.Keys.ToList())
                data.IncrementTechLevel(id);
            foreach (var id in data.LockedArmor.Keys.ToList())
                data.Unlock(id);

            Assert.That(data.Armor, Is.Not.Empty, "expected armor blueprints in the mod data");
            string armorId = data.Armor.ContainsKey("plastic-armor") ? "plastic-armor" : data.Armor.Keys.First();

            // Register a component design through the same seam the client uses.
            foreach (var templateId in data.ComponentTemplates.Keys.ToList())
            {
                var result = _server.SubmitCommand(session, new CreateComponentDesignCommand(
                    session.FactionId, templateId, "Ship Part", Array.Empty<DesignerInput>()));
                if (!result.Accepted) continue;

                var component = info.ComponentDesigns.Values.First(d => d.Name == "Ship Part");
                return (component.UniqueID, armorId);
            }

            Assert.Inconclusive("no component template in the test mod data evaluates cleanly");
            return ("", "");
        }

        [Test]
        public void SaveShipDesign_creates_and_then_updates_in_place()
        {
            var session = Connect();
            var (componentId, armorId) = SetUpDesignData(session);
            var info = Info(session);

            var create = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Test Class",
                new[] { new ShipComponentCount(componentId, 2) }, armorId, 3, IsObsolete: false));
            Assert.That(create.Accepted, Is.True, create.RejectionReason);

            var design = info.ShipDesigns.Values.FirstOrDefault(d => d.Name == "Test Class");
            Assert.That(design, Is.Not.Null, "expected the design registered on the faction");
            Assert.That(design!.MassPerUnit, Is.GreaterThan(0), "expected recalculated mass");
            Assert.That(design.IsValid, Is.False, "a single non-propulsion component can't make a valid ship");
            Assert.That(info.IndustryDesigns.ContainsKey(design.UniqueID), Is.True,
                "a registered design is constructible by industry");

            var update = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, design.UniqueID, "Test Class II",
                new[] { new ShipComponentCount(componentId, 4) }, armorId, 5, IsObsolete: false));
            Assert.That(update.Accepted, Is.True, update.RejectionReason);

            Assert.That(info.ShipDesigns.Values.Count(d => d.Name.StartsWith("Test Class")), Is.EqualTo(1),
                "updating must edit the design in place, not duplicate it");
            var updated = info.ShipDesigns[design.UniqueID];
            Assert.That(updated.Name, Is.EqualTo("Test Class II"));
            Assert.That(updated.Components.Single().count, Is.EqualTo(4));
            Assert.That(updated.MassPerUnit, Is.GreaterThan(design.MassPerUnit / 2), "mass recalculated from new counts");
        }

        [Test]
        public void DeleteShipDesign_removes_it_and_obsolete_marks_it()
        {
            var session = Connect();
            var (componentId, armorId) = SetUpDesignData(session);
            var info = Info(session);

            _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Keeper", new[] { new ShipComponentCount(componentId, 1) }, armorId, 3, false));
            _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Goner", new[] { new ShipComponentCount(componentId, 1) }, armorId, 3, false));

            var keeper = info.ShipDesigns.Values.First(d => d.Name == "Keeper");
            var goner = info.ShipDesigns.Values.First(d => d.Name == "Goner");

            var delete = _server.SubmitCommand(session, new DeleteShipDesignCommand(session.FactionId, goner.UniqueID));
            Assert.That(delete.Accepted, Is.True, delete.RejectionReason);
            Assert.That(info.ShipDesigns.ContainsKey(goner.UniqueID), Is.False);

            var obsolete = _server.SubmitCommand(session, new SetShipDesignObsoleteCommand(session.FactionId, keeper.UniqueID));
            Assert.That(obsolete.Accepted, Is.True, obsolete.RejectionReason);
            Assert.That(info.ShipDesigns[keeper.UniqueID].IsObsolete, Is.True);
        }

        [Test]
        public void SaveShipDesign_rejects_bad_references()
        {
            var session = Connect();
            var (componentId, armorId) = SetUpDesignData(session);

            var noName = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, " ", Array.Empty<ShipComponentCount>(), armorId, 3, false));
            Assert.That(noName.Accepted, Is.False);

            var badArmor = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Name", Array.Empty<ShipComponentCount>(), "no-such-armor", 3, false));
            Assert.That(badArmor.Accepted, Is.False);

            var badComponent = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Name", new[] { new ShipComponentCount("no-such-component", 1) }, armorId, 3, false));
            Assert.That(badComponent.Accepted, Is.False);

            var badDesignId = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, "no-such-design", "Name", new[] { new ShipComponentCount(componentId, 1) }, armorId, 3, false));
            Assert.That(badDesignId.Accepted, Is.False);
        }

        [Test]
        public void SaveShipDesign_persists_tanker_role_onto_spawned_hull()
        {
            var session = Connect();
            var (componentId, armorId) = SetUpDesignData(session);
            var info = Info(session);

            var create = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Tank Class",
                new[] { new ShipComponentCount(componentId, 1) }, armorId, 3,
                IsObsolete: false, Tanker: true));
            Assert.That(create.Accepted, Is.True, create.RejectionReason);

            var design = info.ShipDesigns.Values.First(d => d.Name == "Tank Class");
            Assert.That(design.Tanker, Is.True);
            Assert.That(new ShipInfoDB(design).Tanker, Is.True,
                "spawn copies the class Tanker flag onto ShipInfoDB");

            var update = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, design.UniqueID, "Tank Class",
                new[] { new ShipComponentCount(componentId, 1) }, armorId, 3,
                IsObsolete: false, Tanker: false));
            Assert.That(update.Accepted, Is.True, update.RejectionReason);
            Assert.That(info.ShipDesigns[design.UniqueID].Tanker, Is.False);
        }

        /// <summary>Newtonian MoveTo does not need a warp drive, so a hull with thrust, a reactor,
        /// and a battery is a valid design. Dropping the engine is still invalid.</summary>
        [Test]
        public void SaveShipDesign_accepts_a_newtonian_design_with_no_warp_drive()
        {
            var session = Connect();
            var (_, armorId) = SetUpDesignData(session);
            var info = Info(session);

            string engineId = CreatePart(session, "conventional-engine", "Newton Engine");
            string reactorId = CreatePart(session, "reactor", "Newton Reactor");
            string batteryId = CreatePart(session, "battery-bank", "Newton Battery");

            var engine = info.ComponentDesigns[engineId];
            var reactor = info.ComponentDesigns[reactorId];
            var battery = info.ComponentDesigns[batteryId];
            Assert.That(engine.HasAttribute<WarpDriveAtb>(), Is.False);
            Assert.That(reactor.HasAttribute<WarpDriveAtb>(), Is.False);
            Assert.That(battery.HasAttribute<WarpDriveAtb>(), Is.False);
            Assert.That(engine.HasAttribute<NewtonionThrustAtb>(), Is.True);
            Assert.That(reactor.HasAttribute<EnergyGenerationAtb>(), Is.True);
            Assert.That(battery.HasAttribute<EnergyStoreAtb>(), Is.True);

            var save = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Newton Hull",
                new[]
                {
                    new ShipComponentCount(engineId, 1),
                    new ShipComponentCount(reactorId, 1),
                    new ShipComponentCount(batteryId, 1),
                },
                armorId, 1, IsObsolete: false));
            Assert.That(save.Accepted, Is.True, save.RejectionReason);

            var design = info.ShipDesigns.Values.Single(d => d.Name == "Newton Hull");
            Assert.That(design.IsValid, Is.True, "a newtonian hull with power is valid without a warp drive");
            Assert.That(design.TryGetComponentsByAttribute<WarpDriveAtb>(out _), Is.False);
            Assert.That(design.TryGetComponentsByAttribute<NewtonionThrustAtb>(out _), Is.True);

            var noThrust = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "No Thrust",
                new[]
                {
                    new ShipComponentCount(reactorId, 1),
                    new ShipComponentCount(batteryId, 1),
                },
                armorId, 1, IsObsolete: false));
            Assert.That(noThrust.Accepted, Is.True, noThrust.RejectionReason);
            Assert.That(info.ShipDesigns.Values.Single(d => d.Name == "No Thrust").IsValid, Is.False,
                "thrust is still required");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SaveShipDesign_pushes_fresh_shipyard_choices_without_a_tick(bool overSlip)
        {
            var session = Connect();
            var (_, armorId) = SetUpDesignData(session);
            var faction = _game.Factions[session.FactionId];
            var species = SpeciesFactory.CreateSpeciesHuman(faction, _game.GlobalManager);
            var planet = _game.Systems[0].GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
                .First(b => b.HasDataBlob<MassVolumeDB>() && b.HasDataBlob<Pulsar4X.Names.NameDB>());
            var colony = ColonyFactory.CreateColony(faction, species, planet, initialPopulation: 9_000_000);
            colony.SetDataBlob(new IndustryAbilityDB("test-yard", new IndustryAbilityDB.ProductionLine
            {
                Name = "Test Yard",
                MaxVolume = overSlip ? 0 : double.MaxValue,
                IndustryTypeRates = { ["ship-assembly"] = 10 },
            }));
            string engineId = CreatePart(session, "conventional-engine", "Yard Engine");
            string reactorId = CreatePart(session, "reactor", "Yard Reactor");
            string batteryId = CreatePart(session, "battery-bank", "Yard Battery");
            var parts = new[]
            {
                new ShipComponentCount(engineId, 1),
                new ShipComponentCount(reactorId, 1),
                new ShipComponentCount(batteryId, 1),
            };
            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);
            received.Clear();
            var before = _game.TimePulse.GameGlobalDateTime;

            var save = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Fresh Yard Hull", parts, armorId, 1, false));
            Assert.That(save.Accepted, Is.True, save.RejectionReason);
            var design = Info(session).ShipDesigns.Values.Single(d => d.Name == "Fresh Yard Hull");
            var pushed = received.LastOrDefault(e => e.Entity?.Id == colony.Id)?.Entity;
            Assert.That(pushed, Is.Not.Null, "saving a faction design must refresh the colony's cached industry view");
            var item = pushed!.GetView<IndustryView>()!.ProductionLines.Single().Constructibles
                .Single(d => d.DesignId == design.UniqueID);
            Assert.That(item.Name, Is.EqualTo("Fresh Yard Hull"));
            Assert.That(item.OverSlip, Is.EqualTo(overSlip), "slip limits must still apply");

            received.Clear();
            var update = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, design.UniqueID, "Renamed Yard Hull", parts, armorId, 1, false));
            Assert.That(update.Accepted, Is.True, update.RejectionReason);
            pushed = received.LastOrDefault(e => e.Entity?.Id == colony.Id)?.Entity;
            Assert.That(pushed, Is.Not.Null);
            Assert.That(pushed!.GetView<IndustryView>()!.ProductionLines.Single().Constructibles
                .Single(d => d.DesignId == design.UniqueID).Name, Is.EqualTo("Renamed Yard Hull"));

            received.Clear();
            var invalid = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, design.UniqueID, "No Thrust Yard Hull",
                parts.Skip(1).ToArray(), armorId, 1, false));
            Assert.That(invalid.Accepted, Is.True, invalid.RejectionReason);
            pushed = received.LastOrDefault(e => e.Entity?.Id == colony.Id)?.Entity;
            Assert.That(pushed, Is.Not.Null);
            Assert.That(pushed!.GetView<IndustryView>()!.ProductionLines.Single().Constructibles
                .Any(d => d.DesignId == design.UniqueID), Is.False, "invalid designs must stay excluded");
            Assert.That(_game.TimePulse.GameGlobalDateTime, Is.EqualTo(before), "no tick or reload is needed");
        }

        /// <summary>The designer's Copy Design saves its draft with no design id. That must create a
        /// separate design and leave the source untouched.</summary>
        [Test]
        public void SaveShipDesign_with_no_id_copies_without_touching_the_source()
        {
            var session = Connect();
            var (_, armorId) = SetUpDesignData(session);
            var info = Info(session);
            string engineId = CreatePart(session, "conventional-engine", "Copy Engine");
            string reactorId = CreatePart(session, "reactor", "Copy Reactor");

            var original = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Source Hull",
                new[] { new ShipComponentCount(engineId, 1) }, armorId, 1, IsObsolete: false));
            Assert.That(original.Accepted, Is.True, original.RejectionReason);
            var source = info.ShipDesigns.Values.Single(d => d.Name == "Source Hull");
            int before = info.ShipDesigns.Count;

            var copy = _server.SubmitCommand(session, new SaveShipDesignCommand(
                session.FactionId, null, "Source Hull copy",
                new[] { new ShipComponentCount(engineId, 1), new ShipComponentCount(reactorId, 1) },
                armorId, 2, IsObsolete: false));
            Assert.That(copy.Accepted, Is.True, copy.RejectionReason);

            Assert.That(info.ShipDesigns.Count, Is.EqualTo(before + 1));
            var copied = info.ShipDesigns.Values.Single(d => d.Name == "Source Hull copy");
            Assert.That(copied.UniqueID, Is.Not.EqualTo(source.UniqueID));
            Assert.That(info.ShipDesigns[source.UniqueID].Name, Is.EqualTo("Source Hull"));
            Assert.That(info.ShipDesigns[source.UniqueID].Components.Count, Is.EqualTo(1));
            Assert.That(info.ShipDesigns[source.UniqueID].Armor.thickness, Is.EqualTo(1f));
            Assert.That(copied.Components.Count, Is.EqualTo(2));
        }

        private string CreatePart(PlayerSession session, string templateId, string name)
        {
            var result = _server.SubmitCommand(session, new CreateComponentDesignCommand(
                session.FactionId, templateId, name, Array.Empty<DesignerInput>()));
            Assert.That(result.Accepted, Is.True, templateId + ": " + result.RejectionReason);
            return Info(session).ComponentDesigns.Values.First(d => d.Name == name).UniqueID;
        }
    }
}
