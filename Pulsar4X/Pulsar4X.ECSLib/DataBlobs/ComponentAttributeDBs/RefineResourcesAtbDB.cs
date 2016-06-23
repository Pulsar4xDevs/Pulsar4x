using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class RefineResourcesAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public List<Guid> RefinableMatsList { get; internal set; }

        [JsonProperty]
        public int RefineryPoints { get; internal set; }

        public RefineResourcesAtbDB()
        {
        }

        /// <summary>
        /// this is for the parser, it takes a dictionary but turns it into a list of keys, ignoring the values.
        /// </summary>
        /// <param name="refinableMatsList"></param>
        /// <param name="RefineryPoints"></param>
        public RefineResourcesAtbDB(Dictionary<Guid, double> refinableMatsList, double RefineryPoints)
        {
            RefinableMatsList = refinableMatsList.Keys.ToList();
            RefineryPoints = (int)RefineryPoints;
        }

        public RefineResourcesAtbDB(List<Guid> refinableMatsList, int RefineryPoints)
        {
            RefinableMatsList = refinableMatsList;
            RefineryPoints = RefineryPoints;
        }

        public RefineResourcesAtbDB(RefineResourcesAtbDB db)
        {
            RefinableMatsList = new List<Guid>(db.RefinableMatsList);
            RefineryPoints = db.RefineryPoints;
        }

        public override object Clone()
        {
            return new RefineResourcesAtbDB(this);
        }
    }
}