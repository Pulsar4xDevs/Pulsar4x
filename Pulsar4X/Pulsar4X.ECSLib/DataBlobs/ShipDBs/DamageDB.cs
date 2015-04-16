namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Information on damage done to a ship and its Damage Control
    /// </summary>
    public class DamageDB : BaseDataBlob
    {
         public int DamageControlRating { get; set; }

        public DamageDB()
        {
        }

        public DamageDB(DamageDB damageDB)
        {
            DamageControlRating = damageDB.DamageControlRating;
        }

        public override object Clone()
        {
            return new DamageDB(this);
        }
    }
}