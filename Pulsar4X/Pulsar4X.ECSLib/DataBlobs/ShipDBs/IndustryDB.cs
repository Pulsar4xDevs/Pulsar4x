namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on the industral capabilities of a ship.
    /// </summary>
    public class IndustryDB : BaseDataBlob
    {
        public double MiningRate { get; set; }
        public double FuelHarvestingRate { get; set; }
        public double SalvageRate { get; set; }
        public double TerraformingRate { get; set; }
        public double JumpGateConstructionRate { get; set; }

    }
}