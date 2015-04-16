namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Info on how this ship looks to sensors.
    /// </summary>
    public class SensorProfileDB : BaseDataBlob
    {
        public int TotalCrossSection { get; set; }
        public int ThermalSignature { get; set; }
        public int EMSignature { get; set; }

        public SensorProfileDB()
        {
        }

        public SensorProfileDB(SensorProfileDB sensorProfileDB)
        {
            TotalCrossSection = sensorProfileDB.TotalCrossSection;
            ThermalSignature = sensorProfileDB.ThermalSignature;
            EMSignature = sensorProfileDB.EMSignature;
        }

        public override object Clone()
        {
            return new SensorProfileDB(this);
        }
    }
}