namespace Pulsar4X.ECSLib.DataBlobs
{
    /// <summary>
    /// Info on the shields in a ship.
    /// </summary>
    public class SheildsDB : BaseDataBlob
    {
        public int MaximumShieldStrength { get; set; }
        public int CurrentShieldStrength { get; set; }
        public int RechargeRate { get; set; }
    }
}