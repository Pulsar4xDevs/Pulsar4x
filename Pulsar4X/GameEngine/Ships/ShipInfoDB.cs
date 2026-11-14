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

        /// <summary>The captain. Seating or clearing one boards or lands them in the crew count (ShipCrew.SyncCaptain).</summary>
        [JsonIgnore]
        public int CommanderID
        {
            get => _commanderID;
            internal set
            {
                _commanderID = value;
                if (OwningEntity != null)
                    ShipCrew.SyncCaptain(OwningEntity);
            }
        }

        // Saved under the old property name; loading sets the field, so it does not board anyone.
        [JsonProperty("CommanderID")] int _commanderID = -1;

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

        /// <summary>A finished refit whose parts match a chosen design adopts that design.</summary>
        internal void SetDesign(ShipDesign design)
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
