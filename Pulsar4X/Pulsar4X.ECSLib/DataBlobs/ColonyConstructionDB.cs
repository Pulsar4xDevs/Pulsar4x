using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        public Guid ItemGuid { get; private set; }
        //yes this can be public set just fine. no reason not to here...
        public ushort NumberOrdered { get; set; }
        public ushort NumberCompleted { get; internal set; }
        public int PointsLeft { get; internal set; }
        //again no reason this can't be public set
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
        
        public ConstructionType ConstructionType { get; internal set; }

        public Dictionary<Guid, int> MineralsRequired { get; internal set; }
        public Dictionary<Guid, int> MaterialsRequired { get; internal set; }
        public Dictionary<Guid, int> ComponentsRequired { get; internal set; }

        public ConstructionJob(Guid designGuid, ushort numberOrderd, int jobPoints, bool auto, 
            Dictionary<Guid,int> mineralCost, Dictionary<Guid, int> matCost, Dictionary<Guid,int> componentCost  ): 
            base(designGuid, numberOrderd, jobPoints, auto)
        {
            MineralsRequired = new Dictionary<Guid, int>(mineralCost);
            MaterialsRequired = new Dictionary<Guid, int>(matCost);
            ComponentsRequired = new Dictionary<Guid, int>(componentCost);
        }

    }

    public class  ColonyConstructionDB : BaseDataBlob
    {
        public int PointsPerTick { get; internal set; }

        [JsonProperty]
        public Dictionary<ConstructionType, int> ConstructionRates { get; internal set; }

        [JsonProperty]
        public List<ConstructionJob> JobBatchList { get; internal set; }


        public ColonyConstructionDB()
        {
            ConstructionRates = new Dictionary<ConstructionType, int>
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
