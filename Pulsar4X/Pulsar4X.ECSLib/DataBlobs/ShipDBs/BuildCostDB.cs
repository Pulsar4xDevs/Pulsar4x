using System;

namespace Pulsar4X.ECSLib.DataBlobs
{
    /// <summary>
    /// Info on the build cost of the Ship.
    /// </summary>
    public class BuildCostDB : BaseDataBlob
    {
        public DateTime BuildTime { get; set; }
        public double BuildPointCost { get; set; }
 
        // add minerials
    }
}