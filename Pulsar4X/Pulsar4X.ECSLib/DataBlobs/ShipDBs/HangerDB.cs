namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on the hanger space in the ship.
    /// </summary>
    public class HangerDB : BaseDataBlob
    {
        /// <summary>
        /// Total amount of hanger space in the ship, in tons.
        /// </summary>
        public int HangerSpace { get; set; }

        public HangerDB()
        {
        }

        public HangerDB(HangerDB hangerDB)
        {
            HangerSpace = hangerDB.HangerSpace;
        }

        public override object Clone()
        {
            return new HangerDB(this);
        }
    }
}