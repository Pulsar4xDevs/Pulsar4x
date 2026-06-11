using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Weapons;

namespace Pulsar4X.Tests
{
    /// <summary>The fire-control read surface (FireControlView) and its command set.</summary>
    [TestFixture]
    public class ApiFireControlTests : ApiTestBase
    {
        /// <summary>Unlocks the catalogue and registers component designs until one fire control
        /// and one beam weapon exist; returns them.</summary>
        private (ComponentDesign FireControl, ComponentDesign Weapon) SetUpWeaponDesigns(PlayerSession session)
        {
            var info = _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>();
            var data = info.Data;
            foreach (var id in data.LockedComponentTemplates.Keys.ToList())
                data.Unlock(id);
            foreach (var uniqueId in data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
                data.Unlock(uniqueId);
            foreach (var id in data.LockedTechs.Keys.ToList())
                data.Unlock(id);
            foreach (var id in data.Techs.Keys.ToList())
                data.IncrementTechLevel(id);

            ComponentDesign? fireControl = null, weapon = null;
            foreach (var templateId in data.ComponentTemplates.Keys.ToList())
            {
                var result = _server.SubmitCommand(session, new CreateComponentDesignCommand(
                    session.FactionId, templateId, $"Design {templateId}", Array.Empty<DesignerInput>()));
                if (!result.Accepted) continue;

                var design = info.ComponentDesigns.Values.First(d => d.Name == $"Design {templateId}");
                if (design.AttributesByType.ContainsKey(typeof(BeamFireControlAtbDB)))
                    fireControl ??= design;
                else if (design.AttributesByType.ContainsKey(typeof(GenericBeamWeaponAtb)))
                    weapon ??= design;

                if (fireControl != null && weapon != null)
                    return (fireControl, weapon);
            }

            Assert.Inconclusive("the test mod data has no usable fire control + beam weapon templates");
            throw new InvalidOperationException();
        }

        private Entity MakeArmedShip(PlayerSession session, ComponentDesign fireControl, ComponentDesign weapon)
        {
            var ship = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(ship, new List<BaseDataBlob>
            {
                new ComponentInstancesDB(),
                new PositionDB { AbsolutePosition = Vector3.Zero },
                new MassVolumeDB { MassDry = 10000 },
                new NameDB("Gunship", session.FactionId, "Gunship"),
                new OrderableDB(),
            });
            ship.AddComponent(fireControl);
            ship.AddComponent(weapon);
            return ship;
        }

        private Entity MakeTarget(int factionId)
        {
            var target = Entity.Create(factionId);
            _game.Systems[0].AddEntity(target, new List<BaseDataBlob>
            {
                new PositionDB { AbsolutePosition = Vector3.Zero },
                new NameDB("Target", factionId, "Target"),
            });
            return target;
        }

        [Test]
        public void FireControlView_projects_fire_controls_and_weapons_for_the_owner_only()
        {
            var session = Connect();
            var (fcDesign, wpnDesign) = SetUpWeaponDesigns(session);
            var ship = MakeArmedShip(session, fcDesign, wpnDesign);

            var view = _projector.ProjectEntity(ship, session.FactionId).GetView<FireControlView>();

            Assert.That(view, Is.Not.Null, "expected a FireControlView for the owner");
            Assert.That(view!.FireControls, Has.Count.EqualTo(1));
            Assert.That(view.FireControls[0].TargetId, Is.Null);
            Assert.That(view.FireControls[0].IsEngaging, Is.False);
            Assert.That(view.FireControls[0].AssignedWeaponIds, Is.Empty);
            Assert.That(view.Weapons, Has.Count.EqualTo(1));
            Assert.That(view.Weapons[0].FireControlId, Is.Null, "the weapon starts unassigned");
            Assert.That(view.Weapons[0].MagazineSize, Is.GreaterThan(0));

            int otherFactionId = _game.Factions.Keys.First(id => id != session.FactionId);
            var otherView = _projector.ProjectEntity(ship, otherFactionId).GetView<FireControlView>();
            Assert.That(otherView, Is.Null, "weapon internals must not leak to other factions");
        }

        [Test]
        public void FireControl_commands_assign_weapons_set_target_and_toggle_fire()
        {
            var session = Connect();
            var (fcDesign, wpnDesign) = SetUpWeaponDesigns(session);
            var ship = MakeArmedShip(session, fcDesign, wpnDesign);
            var target = MakeTarget(_game.Factions.Keys.First(id => id != session.FactionId));

            var view = _projector.ProjectEntity(ship, session.FactionId).GetView<FireControlView>()!;
            string fcId = view.FireControls[0].Id;
            string weaponId = view.Weapons[0].Id;

            var assign = _server.SubmitCommand(session,
                new SetFireControlWeaponsCommand(ship.Id, fcId, new[] { weaponId }));
            Assert.That(assign.Accepted, Is.True, assign.RejectionReason);

            var setTarget = _server.SubmitCommand(session,
                new SetFireControlTargetCommand(ship.Id, fcId, target.Id));
            Assert.That(setTarget.Accepted, Is.True, setTarget.RejectionReason);

            var openFire = _server.SubmitCommand(session,
                new SetFireModeCommand(ship.Id, fcId, OpenFire: true));
            Assert.That(openFire.Accepted, Is.True, openFire.RejectionReason);

            view = _projector.ProjectEntity(ship, session.FactionId).GetView<FireControlView>()!;
            Assert.That(view.FireControls[0].AssignedWeaponIds, Is.EquivalentTo(new[] { weaponId }));
            Assert.That(view.Weapons[0].FireControlId, Is.EqualTo(fcId));
            Assert.That(view.FireControls[0].TargetId, Is.EqualTo(target.Id));
            Assert.That(view.FireControls[0].IsEngaging, Is.True);

            var ceaseFire = _server.SubmitCommand(session,
                new SetFireModeCommand(ship.Id, fcId, OpenFire: false));
            Assert.That(ceaseFire.Accepted, Is.True, ceaseFire.RejectionReason);
            view = _projector.ProjectEntity(ship, session.FactionId).GetView<FireControlView>()!;
            Assert.That(view.FireControls[0].IsEngaging, Is.False);
        }

        [Test]
        public void FireControl_commands_reject_an_unknown_fire_control()
        {
            var session = Connect();
            var (fcDesign, wpnDesign) = SetUpWeaponDesigns(session);
            var ship = MakeArmedShip(session, fcDesign, wpnDesign);

            var result = _server.SubmitCommand(session,
                new SetFireModeCommand(ship.Id, "not-a-fire-control", OpenFire: true));

            Assert.That(result.Accepted, Is.False);
        }
    }
}
