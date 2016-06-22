using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// ship and colonyInfo processors
    /// </summary>
    public static class ShipAndColonyInfoProcessor
    {
        public static void ReCalculateShipTonnaageAndHTK(Entity shipEntity)
        {
            ShipInfoDB shipInfo = shipEntity.GetDataBlob<ShipInfoDB>();
            ComponentInstancesDB componentInstances = shipEntity.GetDataBlob<ComponentInstancesDB>();
            float totalTonnage = 0;
            int totalHTK = 0;
            foreach (var componentDesign in componentInstances.SpecificInstances)
            {
                totalTonnage = componentDesign.Key.GetDataBlob<ComponentInfoDB>().SizeInTons;
                foreach (var componentInstance in componentDesign.Value)
                {
                    totalHTK = componentInstance.GetDataBlob<ComponentInstanceInfoDB>().HTKRemaining;
                }
            }
            shipInfo.Tonnage = totalTonnage;
            shipInfo.InternalHTK = totalHTK;
        }

        /// <summary>
        /// This is for adding components and installations to ships and colonies. 
        /// TODO: Should this be in the factory, processor, or a helper?
        /// </summary>
        /// <param name="instance">entity that contains an componentInfoDB</param>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB</param>
        internal static void AddComponentDesignToEntity(Entity designToAdd, Entity parentEntity)
        {
            Entity newInstance = ComponentInstanceFactory.NewInstanceFromDesignEntity(designToAdd);
            AddComponentInstanceToEntity(newInstance, parentEntity);
        }

        /// <summary>
        /// This is for adding and exsisting component or installation instance to ships and colonies. 
        /// TODO: Should this be in the factory, processor, or a helper?
        /// </summary>
        /// <param name="instance">an exsisting componentInstance</param>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB ie a ship or colony</param>
        internal static void AddComponentInstanceToEntity(Entity instance, Entity parentEntity)
        {
            if (parentEntity.HasDataBlob<ComponentInstancesDB>())
            {
                Entity design = instance.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity;
                AttributeToAbilityMap.AddAttribute(parentEntity, design);
                
                ComponentInstancesDB instancesDict = parentEntity.GetDataBlob<ComponentInstancesDB>();

                if (!instancesDict.SpecificInstances.ContainsKey(design))
                    instancesDict.SpecificInstances.Add(design, new List<Entity>() { instance });
                else
                    instancesDict.SpecificInstances[design].Add(instance);
                //ComponentInstancesDB componentInstance = parentEntity.GetDataBlob<ComponentInstancesDB>();

                //if (!componentInstance.SpecificInstances.ContainsKey(specificInstance.DesignEntity)) //if the entity doesnt already have this component design listed, 
                //    componentInstance.SpecificInstances.Add(specificInstance.DesignEntity, new List<ComponentInstanceInfoDB>()); //add the design ID to the dictionary with a new empty list
                //componentInstance.SpecificInstances[specificInstance.DesignEntity].Add(specificInstance); //add the specificInstance
                //ReCalcProcessor.ReCalcAbilities(parentEntity);
            }
            else throw new Exception("parentEntiy does not contain a ComponentInstanceDB");
        }

    }
}
