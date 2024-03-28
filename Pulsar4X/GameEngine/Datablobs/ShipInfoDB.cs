using Newtonsoft.Json;
using Pulsar4X.Engine.Designs;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// Holds all the generic information about a ship
    /// </summary>
    public class ShipInfoDB : BaseDataBlob
    {

        #region Properties

        [JsonProperty]
        public int CommanderID { get; internal set; } = -1;

        [JsonProperty]
        public ShipDesign Design { get; private set; }

        [JsonProperty]
        public bool Conscript { get; set; }

        // Should we have these: ??
        [JsonProperty]
        public bool Tanker { get; set; }
        [JsonProperty]
        public bool Collier { get; set; }
        [JsonProperty]
        public bool SupplyShip { get; set; }

        /// <summary>
        /// The Ships health minus its armour and sheilds, i.e. the total HTK of all its internal Components.
        /// </summary>
        [JsonProperty]
        public int InternalHTK { get; set; }

        [JsonProperty]
        public bool IsMilitary { get; set; }

        //public float Tonnage { get; set; }

        //public double TCS { get {return Tonnage * 0.02;} }

        ///  Ship orders.
        //public Queue<BaseOrder> Orders;

        #endregion

        #region Constructors

        [JsonConstructor]
        private ShipInfoDB()
        {
        }

        public ShipInfoDB(ShipDesign design)
        {
            Design = design;
            //design.ID
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
