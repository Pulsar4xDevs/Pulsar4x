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

        public JobBase(Guid guid, ushort numberOrderd, int jobPoints, bool auto)
        {
            ItemGuid = guid;
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            PointsLeft = jobPoints;
            Auto = auto;
        }
    }


    public class ConstructionJob : JobBase
    {
        
        public ConstructionType ConstructionType { get; set; }

        public JDictionary<Guid, int> MineralsLeft { get; set; }
        public JDictionary<Guid, int> MaterialsLeft { get; set; }
        public JDictionary<Guid, int> ComponentsLeft { get; set; }

        public ConstructionJob(Guid designGuid, ushort numberOrderd, int jobPoints, bool auto, 
            JDictionary<Guid,int> mineralCost, JDictionary<Guid, int> matCost, JDictionary<Guid,int> componentCost  ): 
            base(designGuid, numberOrderd, jobPoints, auto)
        {
            MineralsLeft = mineralCost;
            MaterialsLeft = matCost;
            ComponentsLeft = componentCost;
        }

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
            ConstructionRates = new JDictionary<ConstructionType, int>
            {
                {ConstructionType.Ammo, 0}, 
                {ConstructionType.Facility, 0}, 
                {ConstructionType.Fighter, 0}, 
                {ConstructionType.ShipComponent, 0}
            };
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
