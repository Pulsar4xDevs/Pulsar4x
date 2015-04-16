namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Info on the shields in a ship.
    /// </summary>
    public class ShieldsDB : BaseDataBlob
    {
        public int MaximumShieldStrength { get; set; }
        public int CurrentShieldStrength { get; set; }
        public int RechargeRate { get; set; }

        public ShieldsDB()
        {
        }

        public ShieldsDB(ShieldsDB shieldsDB)
        {
            MaximumShieldStrength = shieldsDB.MaximumShieldStrength;
            CurrentShieldStrength = shieldsDB.CurrentShieldStrength;
            RechargeRate = shieldsDB.RechargeRate;
        }

        public override object Clone()
        {
            return new ShieldsDB(this);
        }
    }
}