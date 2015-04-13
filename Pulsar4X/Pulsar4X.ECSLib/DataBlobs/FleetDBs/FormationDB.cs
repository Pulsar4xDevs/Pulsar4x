using System;

namespace Pulsar4X.ECSLib
{
    class FormationDB : BaseDataBlob
    {
        /// <summary>
        /// The Commander of this Formation (or ship if it is a single ship)
        /// @todo Swap this with a reference to the actual Commander entity
        /// </summary>
        public Entity CommandingOfficer { get; set; }

        public FormationDB(Guid parentGuid)
        {
            
        }

        public FormationDB(FormationDB formationDB)
        {
            
        }
    }
}
