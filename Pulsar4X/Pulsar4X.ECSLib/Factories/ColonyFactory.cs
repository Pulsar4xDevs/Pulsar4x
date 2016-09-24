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
            string planetName = planetEntity.GetDataBlob<NameDB>().GetName(factionEntity);
            var OwnedDB = new OwnedDB(factionEntity);
            blobs.Add(OwnedDB);
            NameDB name = new NameDB(planetName + " Colony"); // TODO: Review default name.
            blobs.Add(name);
            ColonyInfoDB colonyInfoDB = new ColonyInfoDB(speciesEntity, 0, planetEntity);
            blobs.Add(colonyInfoDB);
            ColonyBonusesDB colonyBonuses = new ColonyBonusesDB();
            blobs.Add(colonyBonuses);       
            ColonyMinesDB colonyMinesDB = new ColonyMinesDB();
            blobs.Add(colonyMinesDB);
            ColonyRefiningDB colonyRefining = new ColonyRefiningDB();
            blobs.Add(colonyRefining);
            ColonyConstructionDB colonyConstruction = new ColonyConstructionDB();
            blobs.Add(colonyConstruction);

            MassVolumeDB mvDB = new MassVolumeDB();
            blobs.Add(mvDB);

            //installations get added to the componentInstancesDB
            ComponentInstancesDB installations = new ComponentInstancesDB();
            blobs.Add(installations);

            Entity colonyEntity = new Entity(planetEntity.Manager, blobs);
            factionEntity.GetDataBlob<FactionInfoDB>().Colonies.Add(colonyEntity);
            return colonyEntity;
        }
    }
}