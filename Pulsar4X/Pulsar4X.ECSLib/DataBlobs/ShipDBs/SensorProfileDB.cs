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
    }
}