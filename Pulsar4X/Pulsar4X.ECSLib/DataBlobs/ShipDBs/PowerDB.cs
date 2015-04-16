namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Info on the ships ability to generate power.
    /// </summary>
    public class PowerDB : BaseDataBlob
    {
        public double TotalPowerOutput { get; set; }

        public PowerDB()
        {
        }

        public PowerDB(PowerDB powerDB)
        {
            TotalPowerOutput = powerDB.TotalPowerOutput;
        }

        public override object Clone()
        {
            return new PowerDB(this);
        }
    }
}