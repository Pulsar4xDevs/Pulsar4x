using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    /*
     * mins and mats <guid type, int amount>
     * components guid type, int amount/ each int is a specific instance with its own guid.
     
         */
    /// <summary>
    /// Contains info on a ships cargo capicity.
    /// </summary>
    public class CargoDB : BaseDataBlob
    {
        [JsonProperty]
        public Dictionary<Guid, int> CargoCapicity { get; private set; } = new Dictionary<Guid, int>();

        [JsonProperty]
        public Dictionary<Guid, List<Entity>> StoredEntities { get; private set; } = new Dictionary<Guid, List<Entity>>();
        [JsonProperty]
        public Dictionary<Guid, Dictionary<Guid, int>> MinsAndMatsByCargoType { get; private set;} = new Dictionary<Guid, Dictionary<Guid, int>>();

        [JsonIgnore] //don't store this in the savegame, we'll re-reference this OnDeserialised
        private Dictionary<Guid, Guid> _itemToTypeMap;

        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {            
            var game = (Game)context.Context;
            _itemToTypeMap = game.StaticData.StorageTypeMap;           
        }

        public CargoDB()
        {
        }

        public CargoDB(StaticDataStore staticDataStore)
        {
            _itemToTypeMap = staticDataStore.StorageTypeMap;
        }

        public CargoDB(CargoDB cargoDB)
        {
            CargoCapicity = new Dictionary<Guid, int>(cargoDB.CargoCapicity);
            MinsAndMatsByCargoType = new Dictionary<Guid, Dictionary<Guid, int>>(cargoDB.MinsAndMatsByCargoType);
            StoredEntities = new Dictionary<Guid, List<Entity>>(cargoDB.StoredEntities);
            _itemToTypeMap = cargoDB._itemToTypeMap; //note that this is not 'new', the dictionary referenced here is static/global and should be the same dictionary throughout the game.
        }

        /// <summary>
        /// Adds a value to the dictionary, if the item does not exsist, it will get added to the dictionary.
        /// </summary>
        /// <param name="item">the guid of the item to add</param>
        /// <param name="value">the amount of the item to add</param>
        internal void AddValue(Guid item, int value)
        {
            Guid cargoTypeID = _itemToTypeMap[item];
            if (!MinsAndMatsByCargoType.ContainsKey(cargoTypeID))
                MinsAndMatsByCargoType.Add(cargoTypeID, new CargoDictionary());
            if (!MinsAndMatsByCargoType[cargoTypeID].ContainsKey(item))
                MinsAndMatsByCargoType[cargoTypeID].Add(item, value);
            else
                MinsAndMatsByCargoType[cargoTypeID][item] += value;
        }



        /// <summary>
        /// Will remove the item from the dictionary if subtracting the value causes the dictionary value to be 0.
        /// </summary>
        /// <param name="item">the guid of the item to subtract</param>
        /// <param name="value">the amount of the item to subtract</param>
        /// <returns>the amount succesfully taken from the dictionary(will not remove more than what the dictionary contains)</returns>
        internal int SubtractValue(Guid item, int value)
        {
            Guid cargoTypeID = _itemToTypeMap[item];
            int returnValue = 0;
            if(MinsAndMatsByCargoType.ContainsKey(cargoTypeID))
                if (MinsAndMatsByCargoType[cargoTypeID].ContainsKey(item))
                {
                    if (MinsAndMatsByCargoType[cargoTypeID][item] >= value)
                    {
                        MinsAndMatsByCargoType[cargoTypeID][item] -= value;
                        returnValue = value;
                    }
                    else
                    {
                        returnValue = MinsAndMatsByCargoType[cargoTypeID][item];
                        MinsAndMatsByCargoType[cargoTypeID].Remove(item);
                    }
                }
            return returnValue;
        }

        public int GetAmountOf(Guid itemID)
        {
            Guid cargoTypeID = _itemToTypeMap[itemID];
            int returnValue = 0;
            if (MinsAndMatsByCargoType.ContainsKey(cargoTypeID))
            {
                if (MinsAndMatsByCargoType[cargoTypeID].ContainsKey(itemID))
                {
                    returnValue = MinsAndMatsByCargoType[cargoTypeID][itemID];
                }
            }


            return returnValue;
        }

        public override object Clone()
        {
            return new CargoDB(this);
        }

    }
}