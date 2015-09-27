using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class RefineResourcesDB : BaseDataBlob
    {

        [JsonProperty]
        private JDictionary<Guid, int> _refinaryJobsPerEconTick;
        /// <summary>
        /// RefinaryJobSD guid, amount
        /// </summary>
        public JDictionary<Guid, int> RefinaryJobsPerEconTick { get { return _refinaryJobsPerEconTick; } internal set { _refinaryJobsPerEconTick = value; } }


        public RefineResourcesDB()
        {
        }

        public RefineResourcesDB(Dictionary<Guid, double> jobsPerEconTick)
        {
            _refinaryJobsPerEconTick = new JDictionary<Guid, int>();
            foreach (var kvp in jobsPerEconTick)
            {
                _refinaryJobsPerEconTick.Add(kvp.Key, (int)kvp.Value);
            }
        }

        public RefineResourcesDB(RefineResourcesDB db)
        {
            _refinaryJobsPerEconTick = new JDictionary<Guid, int>(db.RefinaryJobsPerEconTick);
        }

        public override object Clone()
        {
            return new RefineResourcesDB(this);
        }
    }
}