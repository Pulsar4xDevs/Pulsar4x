using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class RefineingJob : JobBase
    {
        public RefineingJob(Guid matGuid, ushort numberOrderd, int jobPoints, bool auto): base(matGuid, numberOrderd, jobPoints, auto)
        {
        }
    }

    public class ColonyRefiningDB : BaseDataBlob
    {
       
        [JsonIgnore]//recalc this on game load. 
        public int PointsPerTick { get; internal set; }

        [JsonIgnore]//recalc this on game load todo implement this in the processor. 
        public JDictionary<Guid, int> RefiningRates{ get; internal set; }

        [JsonProperty] 
        private List<RefineingJob> _jobBatchList; 
        public List<RefineingJob> JobBatchList { get{return _jobBatchList;} internal set { _jobBatchList = value; } }

        
        public ColonyRefiningDB()
        {
            RefiningRates = new JDictionary<Guid, int>();
            JobBatchList = new List<RefineingJob>();
        }

        public ColonyRefiningDB(ColonyRefiningDB db)
        {
            RefiningRates = new JDictionary<Guid, int>(db.RefiningRates);
            JobBatchList = new List<RefineingJob>(db.JobBatchList);
        }

        public override object Clone()
        {
            return new ColonyRefiningDB(this);
        }
    }
}