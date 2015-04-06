namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on the ships engines and fuel reserves.
    /// </summary>
    public class PropulsionDB : BaseDataBlob
    {
        public int MaximumSpeed { get; set; }
        public int CurrentSpeed { get; set; }

        public int FuelStorageCapicity { get; set; }
        public int CurrentFuelStored { get; set; }
    }
}