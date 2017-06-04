using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// Contains info on a ships cargo capicity.
    /// </summary>
    public class CargoStorageDB : BaseDataBlob
    {
        [JsonProperty]
        public PrIwObsDict<Guid, long> CargoCapicity { get; private set; } = new PrIwObsDict<Guid, long>();

        [JsonProperty]
        public PrIwObsDict<Guid, PrIwObsDict<Entity, PrIwObsList<Entity>>> StoredEntities { get; private set; } = new PrIwObsDict<Guid, PrIwObsDict<Entity, PrIwObsList<Entity>>>();
        
        [JsonProperty] //TODO maybe change the ICargoable key to a GUID.
        public PrIwObsDict<Guid, PrIwObsDict<ICargoable, long>> MinsAndMatsByCargoType { get; private set;} = new PrIwObsDict<Guid, PrIwObsDict<ICargoable, long>>();

        /// <summary>
        /// in tones per hour?
        /// </summary>
        [JsonProperty]
        public int TransferRate { get; internal set; } = 10;

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
            _staticData = staticDataStore;
            ItemToTypeMap = staticDataStore.StorageTypeMap;
        }

        public CargoStorageDB(CargoStorageDB cargoDB)
        {
            CargoCapicity = new PrIwObsDict<Guid, long>(cargoDB.CargoCapicity);
            MinsAndMatsByCargoType = new PrIwObsDict<Guid, PrIwObsDict<ICargoable, long>>(cargoDB.MinsAndMatsByCargoType);
            StoredEntities = new PrIwObsDict<Guid, PrIwObsDict<Entity, PrIwObsList<Entity>>>(cargoDB.StoredEntities);
            ItemToTypeMap = cargoDB.ItemToTypeMap; //note that this is not 'new', the dictionary referenced here is static and should be the same dictionary throughout the game.
        
            AmountToTransfer = cargoDB.AmountToTransfer;
            PartAmount = cargoDB.PartAmount;
            OrderTransferRate = cargoDB.OrderTransferRate;
            LastRunDate = cargoDB.LastRunDate;
            _staticData = cargoDB._staticData;

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
        
        [JsonProperty]
        internal CargoAction CurrentAction { get; set; }

        [JsonProperty]
        public int AmountToTransfer { get; internal set; }
        [JsonProperty]
        public double PartAmount { get; internal set; }
        [JsonProperty]
        public int OrderTransferRate { get; internal set; } //an average of the transfer rates of the two entites.

        [JsonProperty] private Guid _orderTranferItemGuid;
        
        internal Guid OrderTransferItemGuid {
            get { return _orderTranferItemGuid; }
            set
            {
                _orderTranferItemGuid = value;  
                OrderTransferItem = _staticData.GetICargoable(value);
            }
        }

        [JsonIgnore]
        internal ICargoable OrderTransferItem { get; set; }

        [JsonProperty]
        public DateTime LastRunDate { get; internal set; }
    }
}