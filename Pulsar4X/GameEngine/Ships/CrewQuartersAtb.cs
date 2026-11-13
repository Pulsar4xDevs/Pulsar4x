using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Ships
{
    /// <summary>
    /// Berths for a ship's own crew (crew plan phase 1). Crew quarters carry it, and so does every bridge,
    /// which sleeps a few. Berths are read from the design (ShipCrew.Berths), so installing or removing a
    /// part needs no datablob work here; phase 3 caps crew aboard at the ship's berths.
    /// </summary>
    public class CrewQuartersAtb : IComponentDesignAttribute
    {
        public int Berths { get; set; }

        public CrewQuartersAtb(double berths)
        {
            Berths = berths < 0 ? 0 : (int)berths;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance) { }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance) { }

        public string AtbName() => nameof(CrewQuartersAtb);

        public string AtbDescription() => $"Berths for {Berths} crew";
    }
}
