using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.ComTypes;
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

        public JDictionary<Guid, int> MineralsRequired { get; internal set; }
        public JDictionary<Guid, int> MaterialsRequired { get; internal set; }
        public JDictionary<Guid, int> ComponentsRequired { get; internal set; }

        public ConstructionJob(Guid designGuid, ushort numberOrderd, int jobPoints, bool auto, 
            JDictionary<Guid,int> mineralCost, JDictionary<Guid, int> matCost, JDictionary<Guid,int> componentCost  ): 
            base(designGuid, numberOrderd, jobPoints, auto)
        {
            MineralsRequired = new JDictionary<Guid, int>(mineralCost);
            MaterialsRequired = new JDictionary<Guid, int>(matCost);
            ComponentsRequired = new JDictionary<Guid, int>(componentCost);
        }

    }

    public class  ColonyConstructionDB : BaseDataBlob
    {

        [JsonIgnore]//recalc this on game load.
        public int PointsPerTick { get; internal set; }

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
