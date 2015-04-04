namespace Pulsar4X.ECSLib.DataBlobs
{
    /// <summary>
    /// Contains info on a ships cargo capicity.
    /// </summary>
    public class CargoDB : BaseDataBlob
    {
        public int CargoCapicity { get; set; }

        public int TotalColonistCapicity 
        {
            get { return CryoTransportCapicity + LuxuryTransportCapicity; }
        }

        public int CryoTransportCapicity { get; set; }
        public int LuxuryTransportCapicity { get; set; }



    }
}