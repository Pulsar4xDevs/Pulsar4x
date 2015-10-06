using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public struct RefineingJob
    {
        public Guid jobGuid;
        public int numberOrdered;
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

        /// <summary>
        /// on current job
        /// </summary>        
        public int RemainingPoints
        {
            get { return _remainingPoints; }
            internal set{ _remainingPoints = value; }
        }
        [JsonProperty]
        private int _remainingPoints;


        /// <summary>
        /// in current batch
        /// </summary>        
        public int RemainingJobs
        {
            get { return _remainingJobs; }
            internal set { _remainingJobs = value; }
        }
        [JsonProperty]
        private int _remainingJobs;


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