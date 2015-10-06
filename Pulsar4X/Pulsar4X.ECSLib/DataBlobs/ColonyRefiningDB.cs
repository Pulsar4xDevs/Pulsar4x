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
       
        public int RefinaryPoints { get; internal set; }

        public JDictionary<Guid, int> RefiningRates { get; internal set; }

        public JDictionary<Guid, float> MineralStockpile { get { return OwningEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile; } }
        public JDictionary<Guid, float> MaterialsStockpile { get { return OwningEntity.GetDataBlob<ColonyInfoDB>().RefinedStockpile; } }

        [JsonProperty]
        public JDictionary<RefineingJob, int> JobList { get; set; }

        public ColonyRefiningDB()
        {
            RefiningRates = new JDictionary<Guid, int>();
            JobList = new JDictionary<RefineingJob, int>();
        }

        public ColonyRefiningDB(ColonyRefiningDB db)
        {
            RefiningRates = new JDictionary<Guid, int>(db.RefiningRates);
            JobList = new JDictionary<RefineingJob, int>(db.JobList);
        }

        public override object Clone()
        {
            return new ColonyRefiningDB(this);
        }
    }
}