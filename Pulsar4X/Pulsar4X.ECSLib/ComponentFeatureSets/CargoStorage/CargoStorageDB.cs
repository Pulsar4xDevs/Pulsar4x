using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// Contains info on a ships cargo capicity.
    /// </summary>
    public class CargoStorageDB : BaseDataBlob, ICreateViewmodel
    {
        [JsonProperty]
        internal Dictionary<Guid, CargoTypeStore> StoredCargos { get; private set; } = new Dictionary<Guid, CargoTypeStore>();


        public CargoStorageDB()
        {
        }



        public CargoStorageDB(CargoStorageDB cargoDB)
        {
            StoredCargos = new Dictionary<Guid, CargoTypeStore>(cargoDB.StoredCargos);
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

    internal class CargoTypeStore
    {
        //[JsonProperty]
        /// <summary>
        /// This is the CargoTypeSD.ID/ICargoable.CargoTypeID
        /// </summary>
        /// <value>The CargoType GUID.</value>
        //internal Guid TypeGuid { get; set; } //irelevent since this is stored in a dictionary with this as a key. 

        [JsonProperty]
        internal long MaxCapacity { get; set; }

        [JsonProperty]
        internal long FreeCapacity { get; set; }

        [JsonProperty]
        /// <summary>
        /// For Minerals etc: The key is the ICargoable.ID, and the value is the amount stored. 
        /// For Entites the key is the designs ICargoable.ID and the value is the number of that design stored.
        /// </summary>
        /// <value>The item and amount.</value>
        internal Dictionary<Guid, long> ItemsAndAmounts { get;} = new Dictionary<Guid, long>();
    }

    internal class CargoTypeStoreEntites : CargoTypeStore
    {
        [JsonProperty]
        /// <summary>
        /// This stores the specific Entites. 
        /// </summary>
        /// <value>The specific entites.</value>
        internal List<Entity> SpecificEntites { get; } = new List<Entity>();
    }
}