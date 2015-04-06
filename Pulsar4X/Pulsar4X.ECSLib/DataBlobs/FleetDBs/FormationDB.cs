using System;

namespace Pulsar4X.ECSLib.DataBlobs
{
    class FormationDB : TreeHierarchyDB
    {
        /// <summary>
        /// The Commander of this Formation (or ship if it is a single ship)
        /// @todo Swap this with a reference to the actual Commander entity
        /// </summary>
        public DataBlobRef<CommanderDB> CommandingOfficer { get; set; }

        public FormationDB(Guid parentGuid) : base(parentGuid)
        {
            
        }
    }
}
