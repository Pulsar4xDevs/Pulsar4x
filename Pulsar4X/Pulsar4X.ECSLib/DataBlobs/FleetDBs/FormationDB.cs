using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs.FleetDBs
{
    class FormationDB : TreeHierarchyDB
    {
        /// <summary>
        /// The Commander of this Formation (or ship if it is a single ship)
        /// @todo Swap this with a reference to the actual Commander entity
        /// </summary>
        public String Commander { get; set; }

        public FormationDB(Guid parentGuid) : base(parentGuid)
        {
            
        }
    }
}
