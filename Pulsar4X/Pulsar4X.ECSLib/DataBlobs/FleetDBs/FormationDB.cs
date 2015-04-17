using System;

namespace Pulsar4X.ECSLib
{
    class FormationDB : TreeHierarchyDB
    {
        /// <summary>
        /// The Commander of this Formation (or ship if it is a single ship)
        /// </summary>
        public Entity CommandingOfficer { get; set; }

        public FormationDB() 
            : base(null)
        {
            
        }

        public FormationDB(Entity parent)
            : base(parent)
        {
            
        }

        public FormationDB(FormationDB toCopy)
            : base(toCopy.Parent)
        {
            CommandingOfficer = toCopy.CommandingOfficer;
        }

        public override object Clone()
        {
            return new FormationDB(this);
        }
    }
}
