using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// Contains info on a ships cargo capicity.
    /// </summary>
    public class CargoStorageDB : BaseDataBlob
    {
        [JsonProperty]
        public Dictionary<Guid, long> CargoCapicity { get; private set; } = new Dictionary<Guid, long>();

        [JsonProperty]
        public Dictionary<Guid, Dictionary<Entity, List<Entity>>> StoredEntities { get; private set; } = new Dictionary<Guid, Dictionary<Entity, List<Entity>>>();
        [JsonProperty]
        public Dictionary<Guid, Dictionary<Guid, long>> MinsAndMatsByCargoType { get; private set;} = new Dictionary<Guid, Dictionary<Guid, long>>();

        [JsonIgnore] //don't store this in the savegame, we'll re-reference this OnDeserialised
        internal Dictionary<Guid, Guid> ItemToTypeMap;

        [JsonIgnore] //don't store this in the savegame, we'll re-reference this OnDeserialised
        private StaticDataStore _staticData;

        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {            
            var game = (Game)context.Context;
            ItemToTypeMap = game.StaticData.StorageTypeMap;
            _staticData = game.StaticData; 
        }

        public CargoStorageDB()
        {
        }

        public CargoStorageDB(StaticDataStore staticDataStore)
        {
            ItemToTypeMap = staticDataStore.StorageTypeMap;
        }

        public CargoStorageDB(CargoStorageDB cargoDB)
        {
            CargoCapicity = new Dictionary<Guid, long>(cargoDB.CargoCapicity);
            MinsAndMatsByCargoType = new Dictionary<Guid, Dictionary<Guid, long>>(cargoDB.MinsAndMatsByCargoType);
            StoredEntities = new Dictionary<Guid, Dictionary<Entity, List<Entity>>>(cargoDB.StoredEntities);
            ItemToTypeMap = cargoDB.ItemToTypeMap; //note that this is not 'new', the dictionary referenced here is static/global and should be the same dictionary throughout the game.
        }










        /// <summary>
        /// gives the cargoType of a given itemID
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public CargoTypeSD CargoType(Guid itemID)
        {
            return _staticData.CargoTypes[ItemToTypeMap[itemID]];
        }

        public Guid CargoTypeID(Guid itemID)
        {
            return ItemToTypeMap[itemID];
        }

        public override object Clone()
        {
            return new CargoStorageDB(this);
        }

    }
}