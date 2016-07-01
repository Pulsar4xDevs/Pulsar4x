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

    /// <summary>
    /// this is used to give an High level entity such as a ship or colony an abilityDB 
    /// </summary>
    internal static class AttributeToAbilityMap
    {
        //[ThreadStatic]
        //private static Entity CurrentEntity;
        internal static Dictionary<Type, Delegate> TypeMap = new Dictionary<Type, Delegate>
        {
            { typeof(EnginePowerAtbDB), new Action<Entity, Entity>((shipOrColonyEntity, componentInstanceEntity) => { if (!shipOrColonyEntity.HasDataBlob<PropulsionDB>()) shipOrColonyEntity.SetDataBlob<PropulsionDB>(new PropulsionDB()); }) },
            { typeof(BeamWeaponAtbDB), new Action<Entity, Entity>((shipOrColonyEntity, componentInstanceEntity) => { if (!componentInstanceEntity.HasDataBlob<BeamWeaponsDB>()) shipOrColonyEntity.SetDataBlob(new BeamWeaponsDB()); }) },
            { typeof(SimpleBeamWeaponAtbDB), new Action<Entity, Entity>((shipOrColonyEntity, componentInstanceEntity) => { if (!componentInstanceEntity.HasDataBlob<BeamWeaponsDB>()) shipOrColonyEntity.SetDataBlob(new BeamWeaponsDB()); }) },
        };

        /// <summary>
        /// adds abilites to a ship or component instance
        /// </summary>
        /// <param name="shipOrColony"></param>
        /// <param name="componentDesign"></param>
        /// <param name="componentInstance"></param>
        internal static void AddAbility(Entity shipOrColony, Entity componentDesign, Entity componentInstance)
        {

            //CurrentEntity = shipOrColony;
            foreach (var datablob in componentDesign.DataBlobs)
            {
                var t = datablob.GetType();
                if (TypeMap.ContainsKey(t))
                    TypeMap[t].DynamicInvoke(shipOrColony, componentInstance); // invoke appropriate delegate  
            }
            componentInstance.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity = shipOrColony;
        }
    }
}
