using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class MineResourcesDB : BaseDataBlob
    {

        private Dictionary<Guid, int> _resourcesPerEconTick;
        public Dictionary<Guid, int> ResourcesPerEconTick { get { return _resourcesPerEconTick; } internal set { _resourcesPerEconTick = value; } }

        public MineResourcesDB()
        {
        }

        /// <summary>
        /// Component factory constructor.
        /// </summary>
        /// <param name="resources">values will be cast to ints!</param>
        public MineResourcesDB(Dictionary<Guid,double> resources)
        {
            _resourcesPerEconTick = new Dictionary<Guid, int>();
            foreach (var kvp in resources)
            {
                _resourcesPerEconTick.Add(kvp.Key,(int)kvp.Value);
            }
        }

        public MineResourcesDB(MineResourcesDB db)
        {
            _resourcesPerEconTick = db.ResourcesPerEconTick;
        }

        public override object Clone()
        {
            return new MineResourcesDB(this);
        }
    }
}