namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on a ships beam weapons, including Firecontrol and turret(s).
    /// </summary>
    public class BeamWeaponsDB : BaseDataBlob
    {
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