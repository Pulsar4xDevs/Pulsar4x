using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class MineResourcesDB : BaseDataBlob
    {

        private JDictionary<Guid, int> _resourcesPerMonth;
        public JDictionary<Guid, int> ResourcesPerMonth { get { return _resourcesPerMonth; } internal set { _resourcesPerMonth = value; } }

        /// <summary>
        /// Component factory constructor.
        /// </summary>
        /// <param name="resources">values will be cast to ints!</param>
        public MineResourcesDB(Dictionary<Guid,double> resources)
        {
            _resourcesPerMonth = new JDictionary<Guid, int>();
            foreach (var kvp in resources)
            {
                _resourcesPerMonth.Add(kvp.Key,(int)kvp.Value);
            }
        }

        public MineResourcesDB(MineResourcesDB db)
        {
            _resourcesPerMonth = db.ResourcesPerMonth;
        }

        public override object Clone()
        {
            return new MineResourcesDB(this);
        }
    }
}