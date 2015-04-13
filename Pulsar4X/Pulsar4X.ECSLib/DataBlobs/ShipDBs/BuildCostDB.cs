using System;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Info on the build cost of the Ship.
    /// </summary>
    public class BuildCostDB : BaseDataBlob
    {
        public DateTime BuildTime { get; set; }
        public double BuildPointCost { get; set; }
 
        // add minerials

        public BuildCostDB()
        {
        }

        public BuildCostDB(BuildCostDB buildCostDB)
        {
            BuildTime = buildCostDB.BuildTime; //Struct
            BuildPointCost = buildCostDB.BuildPointCost;
        }
    }
}