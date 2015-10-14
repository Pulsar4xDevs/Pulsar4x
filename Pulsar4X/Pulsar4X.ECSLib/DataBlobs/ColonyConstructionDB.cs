using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    public enum ConstructionType
    {
        Facility,
        ShipComponent,
        Fighter,
        Ammo
    }

    public class JobBase
    {
        public Guid ItemGuid { get; set; }
        public ushort NumberOrdered { get; set; }
        public ushort NumberCompleted { get; set; }
        public int PointsLeft { get; set; }
        public bool Auto { get; set; }
    }


    public class ConstructionJob : JobBase
    {
        
        public ConstructionType ConstructionType { get; set; }

        public JDictionary<Guid, int> MineralsLeft { get; set; }
        public JDictionary<Guid, int> MaterialsLeft { get; set; }
        public JDictionary<Guid, int> ComponentsLeft { get; set; }
        
    }

    public class  ColonyConstructionDB : BaseDataBlob
    {

        [JsonIgnore]//recalc this on game load.
        public int ConstructionPoints { get; internal set; }

        [JsonProperty]
        public JDictionary<ConstructionType, int> ConstructionRates { get; internal set; }

        [JsonProperty]
        public List<ConstructionJob> JobBatchList { get; internal set; }


        public ColonyConstructionDB()
        {
            ConstructionRates = new JDictionary<ConstructionType, int>();
            JobBatchList = new List<ConstructionJob>();
        }

        public ColonyConstructionDB(ColonyConstructionDB db)
        {
            ConstructionRates = db.ConstructionRates;
            JobBatchList = db.JobBatchList;
        }

        public override object Clone()
        {
            return new ColonyConstructionDB(this);
        }
    }
}
