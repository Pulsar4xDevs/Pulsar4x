using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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

        [JsonIgnore]
        public Guid ID {
            get { return this.OwningEntity.Guid; }
        }

        [JsonIgnore]
        public float Mass {
            get { return (float)this.OwningEntity.GetDataBlob<MassVolumeDB>().Mass; } 
        }

        [JsonIgnore]
        public string Name
        {
            get { return this.OwningEntity.GetDataBlob<NameDB>()?.GetName(OwningEntity.GetDataBlob<OwnedDB>()?.ObjectOwner) ?? "Unknown Object"; }
        }

        public CargoAbleTypeDB()
        {
        }

        public CargoAbleTypeDB(Guid cargoTypeID)
        {
            CargoTypeID = cargoTypeID;
        }

        public CargoAbleTypeDB(CargoAbleTypeDB cargoTypeDB)
        {
            CargoTypeID = cargoTypeDB.CargoTypeID;
        }

        public override object Clone()
        {
            return new CargoAbleTypeDB(this);
        }
    }
}