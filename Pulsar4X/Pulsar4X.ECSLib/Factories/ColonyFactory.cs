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
            //InstallationsDB colonyInstallationsDB = new InstallationsDB();
            //blobs.Add(colonyInstallationsDB);
            ComponentInstancesDB installations = new ComponentInstancesDB();
            blobs.Add(installations);

            Entity colonyEntity = new Entity(planetEntity.Manager, blobs);
            factionEntity.GetDataBlob<FactionInfoDB>().Colonies.Add(colonyEntity);
            return colonyEntity;
        }

        /// <summary>
        /// This is for adding components and installations to ships and colonies. 
        /// TODO: Should this be in the factory, processor, or a helper?
        /// </summary>
        /// <param name="designToAdd">entity that contains an componentInfoDB</param>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB</param>
        internal static void AddComponentDesignToEntity(Entity designToAdd, Entity parentEntity)
        {
            ComponentInstance specificInstance = new ComponentInstance(designToAdd);
            AddComponentDesignToEntity(specificInstance, parentEntity);                
        }

        /// <summary>
        /// This is for adding and exsisting component or installation instance to ships and colonies. 
        /// TODO: Should this be in the factory, processor, or a helper?
        /// </summary>
        /// <param name="specificInstance">an exsisting componentInstance</param>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB</param>
        internal static void AddComponentDesignToEntity(ComponentInstance specificInstance, Entity parentEntity)
        {
            if (parentEntity.HasDataBlob<ComponentInstancesDB>())
            {
                ComponentInstancesDB componentInstance = parentEntity.GetDataBlob<ComponentInstancesDB>();
    
                if (!componentInstance.SpecificInstances.ContainsKey(specificInstance.DesignEntity)) //if the entity doesnt already have this component design listed, 
                    componentInstance.SpecificInstances.Add(specificInstance.DesignEntity, new List<ComponentInstance>()); //add the design ID to the dictionary with a new empty list
                componentInstance.SpecificInstances[specificInstance.DesignEntity].Add(specificInstance); //add the specificInstance
                ReCalcProcessor.ReCalcAbilities(parentEntity);
            }
            else throw new Exception("parentEntiy does not contain a ComponentInstanceDB");
        }
    }
}