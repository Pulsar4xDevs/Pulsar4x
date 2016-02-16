namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on the ships ability to transort troups.
    /// </summary>
    public class TroopTransportDB : BaseDataBlob
    {
        public int TotalCapicity
        {
            get { return CryoCapicity + NormalCapicity; }
        }

        public int NormalCapicity { get; set; }  // from normal Troop Transport Bays
        public int CryoCapicity { get; set; }    // From Cryo Troop Transport Bays

        public int TotalCombatDropCapicity
        {
            get { return NormalCombatDropCapicity + CryoCombatDropCapicity; } 
        }

        public int NormalCombatDropCapicity { get; set; }
        public int CryoCombatDropCapicity { get; set; }

        public TroopTransportDB()
        {
        }

        public TroopTransportDB(TroopTransportDB troopTransportDB)
        {
            NormalCapicity = troopTransportDB.NormalCapicity;
            CryoCapicity = troopTransportDB.CryoCapicity;
            NormalCombatDropCapicity = troopTransportDB.NormalCombatDropCapicity;
            CryoCombatDropCapicity = troopTransportDB.CryoCombatDropCapicity;
        }

        public override object Clone()
        {
            return new TroopTransportDB(this);
        }
    }
}