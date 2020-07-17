using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Pulsar4X.ECSLib
{
/*
    /// <summary>
    /// Contains info on a ships cargo capacity.
    /// TODO: is it going to be easier to store items flat, 
    /// ie a dict of guid,long, and just keep track of max and free space of typestore 
    /// </summary>
    public class CargoStorageDB : BaseDataBlob, ICreateViewmodel
    {
        /// <summary>
        /// Key is CargoTypeID (as defined by the ICargoable.CargoTypeID)
        /// </summary>
        [JsonProperty]
        public Dictionary<Guid, CargoTypeStore> StoredCargoTypes { get; private set; } = new Dictionary<Guid, CargoTypeStore>();

        /// <summary>
        /// Gets or sets the transfer rate in kg hr.
        /// </summary>
        /// <value>The transfer rate in Kg/h</value>
        public int TransferRateInKgHr { get; set; } = 500;

        /// <summary>
        /// Gets or sets the transfer range dv.
        /// </summary>
        /// <value>DeltaV in Km/s</value>
        public double TransferRangeDv_kms
        {
            get { return TransferRangeDv_mps * 0.01; }
            set { TransferRangeDv_mps = value * 1000; }
        }

        public double TransferRangeDv_mps { get; set; } = 100;
    
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

        IDBViewmodel ICreateViewmodel.CreateVM(Game game, CommandReferences cmdRef)
        {
            return new CargoStorageVM(game.StaticData, cmdRef, this);
        }
    }



    /// <summary>
    /// Lists items of the same CargoType, and the number of those items.
    /// </summary>
    public class CargoTypeStore
    {
        /// <summary>
        /// The Maximum that this entity can store of this type.
        /// </summary>
        [JsonProperty]
        public long MaxCapacityKg { get; internal set; } = 0;

        /// <summary>
        /// The amount of free space remaining.
        /// </summary>
        [JsonProperty]
        public long FreeCapacityKg { get; internal set; } = 0;

        /// <summary>
        /// Gets the items and amounts (number of items, not weight)
        /// </summary>
        /// <value>The items and amounts.</value>
        [JsonProperty]
        public Dictionary<Guid,(ICargoable item, long amount)> ItemsAndAmounts { get;} = new Dictionary<Guid,(ICargoable, long)>();
         
        [JsonProperty]
        public Dictionary<Guid,List<Entity>> SpecificEntites { get; } = new Dictionary<Guid, List<Entity>>();
        
    }*/
}