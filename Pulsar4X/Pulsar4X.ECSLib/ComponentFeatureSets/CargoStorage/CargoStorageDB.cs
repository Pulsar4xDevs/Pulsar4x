using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// Contains info on a ships cargo capicity.
    /// </summary>
    public class CargoStorageDB : BaseDataBlob, ICreateViewmodel
    {
        /// <summary>
        /// Key is CargoTypeID (as defined by the ICargoable.CargoTypeID)
        /// </summary>
        [JsonProperty]
        internal Dictionary<Guid, CargoTypeStore> StoredCargoTypes { get; private set; } = new Dictionary<Guid, CargoTypeStore>();


        public CargoStorageDB()
        {
        }

        public CargoStorageDB(CargoStorageDB cargoDB)
        {
            StoredCargoTypes = new Dictionary<Guid, CargoTypeStore>(cargoDB.StoredCargoTypes);
        }

        public override object Clone()
        {
            return new CargoStorageDB(this);
        }

        IDBViewmodel ICreateViewmodel.CreateVM(Game game)
        {
            return new CargoStorageVM(game.StaticData);
        }
    }

    /// <summary>
    /// Lists items of the same CargoType, and the number of those items.
    /// </summary>
    internal class CargoTypeStore
    {
        //[JsonProperty]
        /// <summary>
        /// This is the CargoTypeSD.ID/ICargoable.CargoTypeID
        /// </summary>
        /// <value>The CargoType GUID.</value>
        //internal Guid TypeGuid { get; set; } //irelevent since this is stored in a dictionary with this as a key. 

        /// <summary>
        /// The Maximum that this entity can store of this type.
        /// </summary>
        [JsonProperty]
        internal long MaxCapacity { get; set; }

        /// <summary>
        /// The amount of free space remaining.
        /// </summary>
        [JsonProperty]
        internal long FreeCapacity { get; set; } 
        
        [JsonProperty]
        internal Dictionary<Guid, long> ItemsAndAmounts { get;} = new Dictionary<Guid, long>();
         
        [JsonProperty]
        internal Dictionary<Guid,List<Entity>> SpecificEntites { get; } = new Dictionary<Guid, List<Entity>>();
        
    }
}