using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    static class EntityManipulation
    {

        
        internal static void AddComponentToEntity(Entity parentEntity, ComponentInstance componentInstace)
        {
            AddComponentInstanceToEntity(parentEntity, componentInstace);
            ReCalcProcessor.ReCalcAbilities(parentEntity);
        }

        internal static void AddComponentToEntity(Entity parentEntity, List<ComponentDesign> componentDesign)
        {
            if (parentEntity.HasDataBlob<ComponentInstancesDB>())
            {
                foreach (var cd in componentDesign)
                {
                    ComponentInstance instance = new ComponentInstance(cd);
                    AddComponentInstanceToEntity(parentEntity, instance);
                }
                ReCalcProcessor.ReCalcAbilities(parentEntity);
            }
            else throw new Exception("parentEntiy does not contain a ComponentInstanceDB");
        }
        
        internal static void AddComponentToEntity(Entity parentEntity, ComponentDesign componentDesign)
        {
            ComponentInstance instance;

            if (parentEntity.HasDataBlob<ComponentInstancesDB>())
            {
                instance = new ComponentInstance(componentDesign);
                AddComponentInstanceToEntity(parentEntity, instance);
            }
            else throw new Exception("parentEntiy does not contain a ComponentInstanceDB");
            
            ReCalcProcessor.ReCalcAbilities(parentEntity);
        }

        internal static void AddComponentToEntity(Entity parentEntity, List<ComponentInstance> instances)
        {
            foreach (var instance in instances)
                AddComponentToEntity(parentEntity, instance);
            
            ReCalcProcessor.ReCalcAbilities(parentEntity);
        }

        private static void AddComponentInstanceToEntity(Entity parentEntity, ComponentInstance componentInstance)
        {
            componentInstance.ParentEntity = parentEntity;
            ComponentInstancesDB instancesDict = parentEntity.GetDataBlob<ComponentInstancesDB>();
            
            instancesDict.AddComponentInstance(componentInstance);
            foreach (var atbkvp in componentInstance.Design.AttributesByType)
            {
                atbkvp.Value.OnComponentInstallation(parentEntity, componentInstance);
            }
        }

    }
    
}
