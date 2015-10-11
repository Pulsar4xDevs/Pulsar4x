using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    public class ConstructionJob
    {
        public Guid ComponentDesignGuid;
        public ushort NumberOrdered;
        public ushort NumberCompleted;
        public ushort PointsLeft;
        public bool Auto;
    }

    public class  ColonyConstructionDB : BaseDataBlob
    {

        [JsonIgnore]//recalc this on game load.
        public int ConstructionPoints { get; internal set; }

        [JsonProperty]
        public JDictionary<Guid, int> ConstructionRates { get; internal set; }

        [JsonProperty]
        public List<ConstructionJob> JobBatchList { get; internal set; }


        public ColonyConstructionDB()
        {
        }

        public ColonyConstructionDB(ColonyConstructionDB db)
        {

        }

        public override object Clone()
        {
            return new ColonyConstructionDB(this);
        }
    }
}
