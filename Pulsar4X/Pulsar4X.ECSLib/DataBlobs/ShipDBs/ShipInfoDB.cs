using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    /// <summary>
    /// Holds all the generic information about a ship
    /// </summary>
    public class ShipInfoDB : BaseDataBlob
    {
        public string Name { get; set; }

        /// <summary>
        /// The guid of the ship class, if this is a ship calss then the Guid will be empty. 
        /// use IsClassDefinition() to determin if this is a ship class definmition
        /// </summary>
        public Guid ShipClassDefinition { get; set; }

        /// <summary>
        /// The name of the class to which this ship belongs.
        /// </summary>
        public string ClassName { get; set; }

        public bool Obsolete { get; set; }
        public bool Conscript { get; set; }

        // Should we have these: ??
        public bool Tanker { get; set; }
        public bool Collier { get; set; }
        public bool SupplyShip { get; set; }

        /// <summary>
        /// The Ships health minus its armour and sheilds, i.e. the total HTK of all its internal Components.
        /// </summary>
        public int InternalHTK { get; set; }

        public bool IsMilitary { get; set; }

        /// <summary>
        /// List of the ships components. Each component is an entity, thus the list stores its int ID.
        /// The Components should be in the same EntityManager as the Ship.
        /// </summary>
        public List<int> ComponentList { get; set; } 


        public ShipInfoDB()
        {
            ComponentList = new List<int>();
        }

        /// <summary>
        /// Returns true if this is a definition of a class.
        /// </summary>
        public bool IsClassDefinition()
        {
            if (ShipClassDefinition != Guid.Empty)
                return false;

            return true;
        }
    }
}
