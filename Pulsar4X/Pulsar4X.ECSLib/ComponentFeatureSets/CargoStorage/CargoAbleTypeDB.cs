using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on how an entitiy can be stored.
    /// NOTE an entity with this datablob must also have a MassVolumeDB
    /// </summary>
    public class CargoAbleTypeDB : BaseDataBlob , ICargoable
    {
        [JsonProperty]
        public Guid CargoTypeID { get; internal set; }

        /// <summary>
        /// NOTE! this is an entites *Design* ID, not the EntitesID.
        /// </summary>
        [JsonIgnore]
        public Guid ID {
            get
            {
                if (OwningEntity.HasDataBlob<DesignInfoDB>())
                    return this.OwningEntity.GetDataBlob<DesignInfoDB>().DesignEntity.Guid;
                else
                    return this.OwningEntity.Guid;
            }
        }

        [JsonIgnore]
        public int Mass {
            get { return (int)Math.Ceiling(OwningEntity.GetDataBlob<MassVolumeDB>().Mass); } //TODO: could a storable item ever be too large for an int? this assumes that won't happen.
        }
        
        /// <summary>
        /// This should be set to true if the item has become damaged or in any other way needs to maintain state 
        /// </summary>
        [JsonIgnore]
        public string Name
        {
            get { return this.OwningEntity.GetDataBlob<NameDB>()?.GetName(OwningEntity.GetDataBlob<OwnedDB>()?.OwnedByFaction) ?? "Unknown Object"; }
        }
        
        public CargoAbleTypeDB()
        {
        }

        [JsonProperty]
        internal bool MustBeSpecificCargo { get; set; } = false;

        public CargoAbleTypeDB(Guid cargoTypeID)
        {
            CargoTypeID = cargoTypeID;
        }

        public CargoAbleTypeDB(CargoAbleTypeDB cargoTypeDB)
        {
            CargoTypeID = cargoTypeDB.CargoTypeID;
            MustBeSpecificCargo = cargoTypeDB.MustBeSpecificCargo;
        }

        public override object Clone()
        {
            return new CargoAbleTypeDB(this);
        }
    }
}