using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    public class CargoStorageAtbDB : BaseDataBlob
    {
        /// <summary>
        /// Storage Capacity of this module.
        /// </summary>
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        public Guid CargoTypeGuid { get; internal set; }
        public CargoTypeSD CargoType { get; internal set; }

        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            // Star system resolver loads myStarSystem from mySystemGuid after the game is done loading.
            var game = (Game)context.Context;
            game.PostLoad += (sender, args) => {
                if (!game.StaticData.CargoTypes.ContainsKey(CargoTypeGuid))
                    CargoType = game.StaticData.CargoTypes[CargoTypeGuid];
                else 
                     throw new GuidNotFoundException(CargoTypeGuid); };
        }

        public CargoStorageAtbDB(double storageCapacity, CargoTypeSD cargoType) : this((int)storageCapacity, cargoType) { }

        public CargoStorageAtbDB(int storageCapacity, CargoTypeSD cargoType)
        {
            StorageCapacity = storageCapacity;
            CargoType = cargoType;
            CargoTypeGuid = cargoType.ID;
        }

        [JsonConstructor]
        public CargoStorageAtbDB(CargoStorageAtbDB db)
        {
            StorageCapacity = db.StorageCapacity;
            CargoTypeGuid = db.CargoTypeGuid;
        }

        public override object Clone()
        {
            return new CargoStorageAtbDB(this);
        }
    }
}
