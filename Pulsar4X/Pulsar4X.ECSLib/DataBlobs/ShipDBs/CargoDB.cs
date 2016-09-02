using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        public Dictionary<Guid, List<Entity>> StoredEntities { get; } = new Dictionary<Guid, List<Entity>>();
        [JsonProperty]
        public Dictionary<Guid, Dictionary<Guid, int>> MinsAndMatsByCargoType { get; } = new Dictionary<Guid, Dictionary<Guid, int>>();



        public CargoDB()
        {
        }

        public CargoDB(CargoDB cargoDB)
        {
            CargoCapicity = new Dictionary<Guid, int>(cargoDB.CargoCapicity);
        }


        /// <summary>
        /// this must be owned by an entity
        /// </summary>
        /// <param name="guid">guid of an ICargoable object</param>
        /// <returns>guid of cargotype</returns>
        public Guid GetCargoTypeIDForID(Guid guid)
        {
            ICargoable cargoable = (ICargoable)this.OwningEntity.Manager.Game.StaticData.FindDataObjectUsingID(guid);
            return cargoable.CargoTypeID;
        }

        /// <summary>
        /// this must be owned by an entity
        /// </summary>
        /// <param name="guid">guid of an ICargoable object</param>
        /// <returns>CargoTypeSD </returns>
        public CargoTypeSD GetCargoTypeForID(Guid guid)
        {
            ICargoable cargoable = (ICargoable)this.OwningEntity.Manager.Game.StaticData.FindDataObjectUsingID(guid);
            return OwningEntity.Manager.Game.StaticData.CargoTypes[cargoable.CargoTypeID];
        }

        public override object Clone()
        {
            return new CargoDB(this);
        }
    }
}