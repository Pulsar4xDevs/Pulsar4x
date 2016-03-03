using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class RefineResourcesDB : BaseDataBlob
    {
        [JsonProperty]
        public List<Guid> RefinableMatsList { get; internal set; }

        [JsonProperty]
        public int RefinaryPoints { get; internal set; }

        public RefineResourcesDB()
        {
        }

        /// <summary>
        /// this is for the parser, it takes a dictionary but turns it into a list of keys, ignoring the values.
        /// </summary>
        /// <param name="refinableMatsList"></param>
        /// <param name="refinaryPoints"></param>
        public RefineResourcesDB(Dictionary<Guid, double> refinableMatsList, double refinaryPoints)
        {
            RefinableMatsList = refinableMatsList.Keys.ToList();
            RefinaryPoints = (int)refinaryPoints;
        }

        public RefineResourcesDB(List<Guid> refinableMatsList, int refinaryPoints)
        {
            RefinableMatsList = refinableMatsList;
            RefinaryPoints = refinaryPoints;
        }

        public RefineResourcesDB(RefineResourcesDB db)
        {
            RefinableMatsList = new List<Guid>(db.RefinableMatsList);
            RefinaryPoints = db.RefinaryPoints;
        }

        public override object Clone()
        {
            return new RefineResourcesDB(this);
        }
    }
}