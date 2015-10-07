using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class RefineResourcesDB : BaseDataBlob
    {

        [JsonProperty]
        private List<Guid> _refinableMatsList;
        public List<Guid> RefinableMatsList { get { return _refinableMatsList; } internal set { _refinableMatsList = value; } }

        [JsonProperty] private int _refinaryPoints;
        public int RefinaryPoints { get { return _refinaryPoints; } internal set { _refinaryPoints = value; } }

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
            _refinableMatsList = refinableMatsList.Keys.ToList();
            _refinaryPoints = (int)refinaryPoints;
        }

        public RefineResourcesDB(List<Guid> refinableMatsList, int refinaryPoints)
        {
            _refinableMatsList = refinableMatsList;
            _refinaryPoints = (int)refinaryPoints;
        }

        public RefineResourcesDB(RefineResourcesDB db)
        {
            _refinableMatsList = new List<Guid>(db.RefinableMatsList);
            _refinaryPoints = db.RefinaryPoints;
        }

        public override object Clone()
        {
            return new RefineResourcesDB(this);
        }
    }
}