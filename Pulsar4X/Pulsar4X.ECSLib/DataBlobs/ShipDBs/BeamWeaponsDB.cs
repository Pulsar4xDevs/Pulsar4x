using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on a ships beam weapons, including Firecontrol and turret(s).
    /// </summary>
    public class BeamWeaponsDB : BaseDataBlob
    {
        public int NumFireControls { get; internal set; }
        public int NumBeamWeapons { get; internal set; }
        public int TotalDamage { get; internal set; }
        public int MaxDamage { get; internal set; }
        public int MaxRange { get; internal set; }
        public int MaxTrackingSpeed { get; internal set; }

        public BeamWeaponsDB()
        {

        }

        public BeamWeaponsDB(BeamWeaponsDB beamWeaponsDB)
        {
        }

        public override object Clone()
        {
            return new BeamWeaponsDB(this);
        }
    }
}