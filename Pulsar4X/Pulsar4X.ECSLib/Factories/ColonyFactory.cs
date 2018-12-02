using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class ColonyFactory
    {
        /// <summary>
        /// Creates a new colony with zero population.
        /// </summary>
        /// <param name="systemEntityManager"></param>
        /// <param name="factionEntity"></param>
        /// <returns></returns>
        public static Entity CreateColony(Entity factionEntity, Entity speciesEntity, Entity planetEntity)
        {
            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            string planetName = planetEntity.GetDataBlob<NameDB>().GetName(factionEntity.Guid);
            NameDB name = new NameDB(planetName + " Colony"); // TODO: Review default name.
            name.SetName(factionEntity.Guid, name.DefaultName);

            blobs.Add(name);
            ColonyInfoDB colonyInfoDB = new ColonyInfoDB(speciesEntity, 0, planetEntity);
            blobs.Add(colonyInfoDB);
            ColonyBonusesDB colonyBonuses = new ColonyBonusesDB();
            blobs.Add(colonyBonuses);       
            MiningDB colonyMinesDB = new MiningDB();
            blobs.Add(colonyMinesDB);
            RefiningDB colonyRefining = new RefiningDB();
            blobs.Add(colonyRefining);
            ConstructionDB colonyConstruction = new ConstructionDB();
            blobs.Add(colonyConstruction);
            OrderableDB orderableDB = new OrderableDB();
            blobs.Add(orderableDB);
            MassVolumeDB mvDB = new MassVolumeDB();
            blobs.Add(mvDB);

            //installations get added to the componentInstancesDB
            ComponentInstancesDB installations = new ComponentInstancesDB();
            blobs.Add(installations);

            Entity colonyEntity = new Entity(planetEntity.Manager, factionEntity.Guid, blobs);
            var factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            factionInfo.Colonies.Add(colonyEntity);
            factionEntity.GetDataBlob<FactionOwnerDB>().SetOwned(colonyEntity);
            planetEntity.GetDataBlob<SystemBodyInfoDB>().Colonies.Add(colonyEntity);
            return colonyEntity;
        }
    }
}