using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    public class CargoStorageAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        /// <summary>
        /// Storage Capacity of this module.
        /// </summary>
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        /// <summary>
        /// Type of cargo this stores
        /// </summary>
        [JsonProperty]
        public Guid CargoTypeGuid { get; internal set; }

        //todo maybe move these to thier own attribute. 
        /// <summary>
        /// Gets or sets the transfer rate.
        /// </summary>
        /// <value>The transfer rate in Kg/h</value>
        public int TransferRate { get; internal set; }
        /// <summary>
        /// Gets or sets the transfer range.
        /// </summary>
        /// <value>DeltaV in km/s, Low Earth Orbit is about 10km/s</value>
        public double TransferRange { get; internal set; }

        /// <summary>
        /// JSON constructor
        /// </summary>
        public CargoStorageAtbDB() { }

        /// <summary>
        /// Parser Constructor
        /// </summary>
        /// <param name="storageCapacity">will get cast to an int</param>
        /// <param name="cargoType">cargo type ID as defined in StaticData CargoTypeSD</param>
        public CargoStorageAtbDB(double storageCapacity, Guid cargoType, double transferRate, double TransferRange) : this((int)storageCapacity, cargoType, (int)transferRate, TransferRange ) { }

        public CargoStorageAtbDB(int storageCapacity, Guid cargoType, int transferRate, double transferRange)
        {
            StorageCapacity = storageCapacity;
            CargoTypeGuid = cargoType;
            TransferRate = transferRate;
            TransferRange = transferRange;
        }

        public CargoStorageAtbDB(CargoStorageAtbDB db)
        {
            StorageCapacity = db.StorageCapacity;
            CargoTypeGuid = db.CargoTypeGuid;
        }

        public override object Clone()
        {
            return new CargoStorageAtbDB(this);
        }
        
        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<CargoStorageDB>())
            {
                var db = new CargoStorageDB();
                parentEntity.SetDataBlob(db);
                StorageSpaceProcessor.ReCalcCapacity(parentEntity);
            }
        }
    }


}
