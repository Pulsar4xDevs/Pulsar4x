using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class RefineingJob
    {
        public Guid jobGuid;
        public int numberOrdered;
        public int numberCompleted;
        public int pointsLeft;
        public bool auto;
    }

    public class ColonyRefiningDB : BaseDataBlob
    {
       
        [JsonIgnore]//recalc this on game load.
        public int RefinaryPoints { get; internal set; }

        [JsonProperty]
        public JDictionary<Guid, int> RefiningRates { get; internal set; }

        [JsonProperty]
        public List<RefineingJob> JobBatchList { get; internal set; }

        
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