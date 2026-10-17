using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Ships
{
    /// <summary>
    /// Holds all the generic information about a ship
    /// </summary>
    public class ShipInfoDB : BaseDataBlob
    {

        #region Properties

        public int CommanderID { get; internal set; } = -1;

        public ShipDesign Design { get; private set; }

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

        //public float Tonnage { get; set; }

        //public double TCS { get {return Tonnage * 0.02;} }

        ///  Ship orders.
        //public Queue<BaseOrder> Orders;

        #endregion

        #region Constructors

        [JsonConstructor]
        internal ShipInfoDB()
        {
        }

        public ShipInfoDB(ShipDesign design)
        {
            Design = design;
            Tanker = design.Tanker;
        }

        public ShipInfoDB(ShipInfoDB shipInfoDB)
        {
            CommanderID = shipInfoDB.CommanderID;
            Conscript = shipInfoDB.Conscript;
            Tanker = shipInfoDB.Tanker;
            Collier = shipInfoDB.Collier;
            SupplyShip = shipInfoDB.SupplyShip;
            InternalHTK = shipInfoDB.InternalHTK;
            //Tonnage = shipInfoDB.Tonnage;
            IsMilitary = shipInfoDB.IsMilitary;
            /*
            if (shipInfoDB.Orders == null)
                Orders = null;
            else
                Orders = new Queue<BaseOrder>(shipInfoDB.Orders);
                */
        }

        #endregion

        public override object Clone()
        {
            return new ShipInfoDB(this);
        }

    }
}
