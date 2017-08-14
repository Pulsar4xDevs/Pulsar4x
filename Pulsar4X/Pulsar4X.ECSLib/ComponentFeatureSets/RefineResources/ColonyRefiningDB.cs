using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        public int PointsPerTick { get; internal set; }

        //recalc this on game load todo implement this in the processor. 
        public Dictionary<Guid, int> RefiningRates{ get; internal set; }

        [JsonProperty] 
        private List<RefineingJob> _jobBatchList; 
        public List<RefineingJob> JobBatchList { get{return _jobBatchList;} internal set { _jobBatchList = value; } }

        
        public ColonyRefiningDB()
        {
            RefiningRates = new Dictionary<Guid, int>();
            JobBatchList = new List<RefineingJob>();
        }

        public ColonyRefiningDB(ColonyRefiningDB db)
        {
            RefiningRates = new Dictionary<Guid, int>(db.RefiningRates);
            JobBatchList = new List<RefineingJob>(db.JobBatchList);
        }

        public override object Clone()
        {
            return new ColonyRefiningDB(this);
        }
    }
}