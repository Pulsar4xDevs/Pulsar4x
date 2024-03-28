using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine
{
    public static class ColonyFactory
    {
        /// <summary>
        /// Creates a new colony with zero population unless specified.
        /// </summary>
        /// <param name="systemEntityManager"></param>
        /// <param name="factionEntity"></param>
        /// <returns></returns>
        public static Entity CreateColony(Entity factionEntity, Entity speciesEntity, Entity planetEntity, long initialPopulation = 0)
        {
            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            string planetName = planetEntity.GetDataBlob<NameDB>().GetName(factionEntity.Id);
            NameDB name = new NameDB(planetName + " Colony"); // TODO: Review default name.
            name.SetName(factionEntity.Id, name.DefaultName);

            blobs.Add(name);
            ColonyInfoDB colonyInfoDB = new ColonyInfoDB(speciesEntity, initialPopulation, planetEntity);
            blobs.Add(colonyInfoDB);
            ColonyBonusesDB colonyBonuses = new ColonyBonusesDB();
            blobs.Add(colonyBonuses);
            MiningDB colonyMinesDB = new MiningDB();
            blobs.Add(colonyMinesDB);
            OrderableDB orderableDB = new OrderableDB();
            blobs.Add(orderableDB);
            MassVolumeDB mvDB = new MassVolumeDB();
            blobs.Add(mvDB);
            VolumeStorageDB storageDB = new VolumeStorageDB();
            blobs.Add(storageDB);
            var pos = new Vector3(planetEntity.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0);
            PositionDB posDB = new PositionDB(pos, planetEntity);
            blobs.Add(posDB);
            TeamsHousedDB th = new TeamsHousedDB();
            blobs.Add(th);

            //installations get added to the componentInstancesDB
            ComponentInstancesDB installations = new ComponentInstancesDB();
            blobs.Add(installations);

            Entity colonyEntity = Entity.Create();
            colonyEntity.FactionOwnerID = factionEntity.Id;
            planetEntity.Manager.AddEntity(colonyEntity, blobs);
            var factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            factionInfo.Colonies.Add(colonyEntity);
            factionEntity.GetDataBlob<FactionOwnerDB>().SetOwned(colonyEntity);
            planetEntity.GetDataBlob<SystemBodyInfoDB>().Colonies.Add(colonyEntity);
            return colonyEntity;
        }
    }
}