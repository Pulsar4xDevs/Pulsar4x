using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    static class EntityManipulation
    {

        /// <summary>
        /// This is for adding components and installations to ships and colonies. 
        /// </summary>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB</param>        
        /// <param name="componentEntity">Can be either a design or instance entity</param>
        internal static void AddComponentToEntity(Entity parentEntity, Entity componentEntity)
        {
            Entity instance;
            if (parentEntity.HasDataBlob<ComponentInstancesDB>())
            {
                if (!componentEntity.HasDataBlob<ComponentInstanceInfoDB>() )
                {
                    if (componentEntity.HasDataBlob<ComponentInfoDB>())
                        instance = ComponentInstanceFactory.NewInstanceFromDesignEntity(componentEntity);
                    else throw new Exception("componentEntity does not contain either a ComponentInfoDB or a ComponentInstanceInfoDB. Entity Not a ComponentDesign or ComponentInstance");
                }
                else
                    instance = componentEntity;

                AddComponentInstanceToEntity(parentEntity, instance);
            }
            else throw new Exception("parentEntiy does not contain a ComponentInstanceDB");

            ReCalcProcessor.ReCalcAbilities(parentEntity);
        }

        /// <summary>
        /// This is for adding and exsisting component or installation *instance* to ships and colonies. 
        /// </summary>
        /// <param name="instance">an exsisting componentInstance</param>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB ie a ship or colony</param>
        private static void AddComponentInstanceToEntity(Entity parentEntity, Entity instance)
        {

                Entity design = instance.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity;
                AttributeToAbilityMap.AddAbility(parentEntity, design, instance);

                ComponentInstancesDB instancesDict = parentEntity.GetDataBlob<ComponentInstancesDB>();

                if (!instancesDict.SpecificInstances.ContainsKey(design))
                    instancesDict.SpecificInstances.Add(design, new List<Entity>() { instance });
                else
                    instancesDict.SpecificInstances[design].Add(instance);           
        }
    }
}
