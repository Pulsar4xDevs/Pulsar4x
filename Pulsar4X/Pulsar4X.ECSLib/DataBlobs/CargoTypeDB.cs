using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on how an entitiy can be stored.
    /// NOTE an entity with this datablob must also have a MassVolumeDB
    /// </summary>
    public class CargoTypeDB : BaseDataBlob , ICargoable
    {
        [JsonProperty]

        public Guid CargoTypeID { get; internal set; }

        [JsonIgnore]
        public Guid ID {
            get { return this.OwningEntity.Guid; }
        }
        
        public float Mass {
            get { return (float)this.OwningEntity.GetDataBlob<MassVolumeDB>().Mass; } 
        }

        public CargoTypeDB()
        {
        }

        public CargoTypeDB(Guid cargoTypeID)
        {
            CargoTypeID = cargoTypeID;
        }

        public CargoTypeDB(CargoTypeDB cargoTypeDB)
        {
            CargoTypeID = cargoTypeDB.CargoTypeID;
        }

        public override object Clone()
        {
            return new CargoTypeDB(this);
        }
    }
}