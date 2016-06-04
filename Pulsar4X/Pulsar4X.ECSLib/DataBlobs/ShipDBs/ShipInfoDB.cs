using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Holds all the generic information about a ship
    /// </summary>
    public class ShipInfoDB : BaseDataBlob
    {
        /// <summary>
        /// The guid of the ship class, if this is a ship class then the Guid will be empty. 
        /// use IsClassDefinition() to determin if this is a ship class definmition
        /// </summary>
        public Guid ShipClassDefinition { get; set; }

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

        public float Tonnage { get; set; }

        public double TCS { get {return Tonnage * 0.02;} }

        ///  Ship orders.  
        public Queue<Entity> Orders;


        public ShipInfoDB()
        {
        }

        public ShipInfoDB(ShipInfoDB shipInfoDB)
        {
            if (shipInfoDB.ShipClassDefinition == Guid.Empty) //Class
                ShipClassDefinition = shipInfoDB.OwningEntity.Guid;
            else //Ship
                ShipClassDefinition = shipInfoDB.ShipClassDefinition;
            Obsolete = shipInfoDB.Obsolete;
            Conscript = shipInfoDB.Conscript;
            Tanker = shipInfoDB.Tanker;
            Collier = shipInfoDB.Collier;
            SupplyShip = shipInfoDB.SupplyShip;
            InternalHTK = shipInfoDB.InternalHTK;
            IsMilitary = shipInfoDB.IsMilitary;
            Orders = new Queue<Entity>(shipInfoDB.Orders);
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

        public override object Clone()
        {
            return new ShipInfoDB(this);
        }

        /// <summary>
        /// Orders functions
        /// </summary>

        public Entity CheckNextOrder()
        {
            if (Orders == null)
                return Entity.InvalidEntity;
            if (Orders.ToArray().Length == 0)
                return Entity.InvalidEntity;
            Entity nextOrder = Orders.Peek();
            return nextOrder;
        }

        public void AddOrder(Entity order)
        {
            Orders.Enqueue(order);
            return;
        }

        public void RemoveNextOrder()
        {
            Orders.Dequeue();
            return;
        }
    }
}
