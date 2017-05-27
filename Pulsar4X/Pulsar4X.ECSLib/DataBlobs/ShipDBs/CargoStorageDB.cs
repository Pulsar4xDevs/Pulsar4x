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
        [JsonProperty]
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
            ItemToTypeMap = staticDataStore.StorageTypeMap;
        }

        public CargoStorageDB(CargoStorageDB cargoDB)
        {
            CargoCapicity = new PrIwObsDict<Guid, long>(cargoDB.CargoCapicity);
            MinsAndMatsByCargoType = new PrIwObsDict<Guid, PrIwObsDict<ICargoable, long>>(cargoDB.MinsAndMatsByCargoType);
            StoredEntities = new PrIwObsDict<Guid, PrIwObsDict<Entity, PrIwObsList<Entity>>>(cargoDB.StoredEntities);
            ItemToTypeMap = cargoDB.ItemToTypeMap; //note that this is not 'new', the dictionary referenced here is static and should be the same dictionary throughout the game.
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

    public class CargoOrderableDB : BaseDataBlob
    {
        [JsonProperty]
        public CargoOrder CurrentOrder { get; internal set; }
        [JsonProperty]
        public PercentValue PercentComplete { get; internal set; }
        [JsonProperty]
        public int AmountToTransfer { get; internal set; }
        [JsonProperty]
        public double PartAmount { get; internal set; }
        [JsonProperty]
        public int TransferRate { get; internal set; } //an average of the transfer rates of the two entites.
        [JsonProperty]
        public DateTime LastRunDate { get; internal set; }

        //Normaly datablobs shouldn't be referenced in datablobs, but if we're not serialising them it should be ok?
        //mostly this is just to clean up some logic and not require it to figure out which is which each time the logic is run.
        //it could be removed from here if it is really a problem.
        [JsonIgnore]
        public CargoStorageDB CargoFrom { get; internal set; }
        [JsonIgnore]
        public CargoStorageDB CargoTo { get; internal set; }

        public CargoOrderableDB()
        {
        }

        public CargoOrderableDB(CargoOrderableDB db)
        {
            CurrentOrder = db.CurrentOrder;
            PercentComplete = db.PercentComplete;
            AmountToTransfer = db.AmountToTransfer;
            PartAmount = db.PartAmount;
            TransferRate = db.TransferRate;
            CargoFrom = db.CargoFrom;
            CargoTo = db.CargoTo;
            LastRunDate = db.LastRunDate;
        }

        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            StorageSpaceProcessor.SetToFrom(this); //this sets CargoTo and CargoFrom
        }

        public override object Clone()
        {
            return new CargoOrderableDB(this);
        }
    }
}