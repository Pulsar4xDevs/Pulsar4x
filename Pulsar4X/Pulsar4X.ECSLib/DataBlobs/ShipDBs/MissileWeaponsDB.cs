namespace Pulsar4X.ECSLib.DataBlobs
{
    /// <summary>
    /// Contains info on the Ships missile weapons, including Fire control and Magazine storage.
    /// </summary>
    public class MissileWeaponsDB : BaseDataBlob
    {
        public int MaximumMagazineCapicity { get; set; } // in MSP
        public int UsedMagazineCapicity { get; set; }
    }
}