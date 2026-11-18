using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Ships
{
    /// <summary>
    /// Officer cabins on a bridge (crew rules phase 1). Officers (the captain for now) are housed here, not in
    /// crew quarters, and are not part crew. Read from the design (ShipCrew.OfficerSlots), so installing or
    /// removing a part needs no datablob work. A bridge saved before this attribute existed carries a
    /// <see cref="CrewQuartersAtb"/> instead; ShipCrew reads that as officer slots on a bridge.
    /// </summary>
    public class OfficerSlotsAtb : IComponentDesignAttribute
    {
        public int Slots { get; set; }

        public OfficerSlotsAtb(double slots)
        {
            Slots = slots < 0 ? 0 : (int)slots;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance) { }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance) { }

        public string AtbName() => nameof(OfficerSlotsAtb);

        public string AtbDescription() => $"Cabins for {Slots} officers";
    }
}
