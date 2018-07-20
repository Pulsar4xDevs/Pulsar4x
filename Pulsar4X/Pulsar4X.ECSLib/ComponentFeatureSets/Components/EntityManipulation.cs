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
        /// Adds the component to entity owned by the parentEntites factionOwner.
        /// </summary>
        /// <param name="parentEntity">Parent entity.</param>
        /// <param name="componentEntity">Component entity.</param>
        internal static void AddComponentToEntity(Entity parentEntity, Entity componentEntity)
        {
            AddComponentToEntity(parentEntity, componentEntity, parentEntity.FactionOwner);
        }

        /// <summary>
        /// This is for adding components and installations to ships and colonies. 
        /// </summary>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB</param>        
        /// <param name="componentEntity">Can be either a design or instance entity, if a design a new instance will be made</param>
        /// <param name="factionID">Guid of the faction that this entity should be owned by</param>
        internal static void AddComponentToEntity(Entity parentEntity, Entity componentEntity, Guid factionID)
        {
            Entity instance;

            if (parentEntity.HasDataBlob<ComponentInstancesDB>())
            {
                if (!componentEntity.HasDataBlob<ComponentInstanceInfoDB>())
                {
                    if (componentEntity.HasDataBlob<ComponentInfoDB>())
                    {
                        instance = ComponentInstanceFactory.NewInstanceFromDesignEntity(componentEntity, factionID, parentEntity.Manager);
                    }
                    else throw new Exception("componentEntity does not contain either a ComponentInfoDB or a ComponentInstanceInfoDB. Entity Not a ComponentDesign or ComponentInstance");
                }
                else
                    instance = componentEntity;

                AddComponentInstanceToEntity(parentEntity, instance);
            }
            else throw new Exception("parentEntiy does not contain a ComponentInstanceDB");
            ObjectOwnershipDB parentOwner;
            if (!parentEntity.HasDataBlob<ObjectOwnershipDB>())
            {
                //StarSystem starSys = parentEntity.GetDataBlob<PositionDB>().SystemGuid
                parentOwner = new ObjectOwnershipDB();
                parentEntity.SetDataBlob(parentOwner);
            }
            else
                parentOwner = parentEntity.GetDataBlob<ObjectOwnershipDB>();
            parentOwner.Children.Add(instance);
            ReCalcProcessor.ReCalcAbilities(parentEntity);
        }

        /// <summary>
        /// This is for adding components and installations to ships and colonies.
        /// batch add is faster than single add as it recalcs only once.  
        /// </summary>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB</param>        
        /// <param name="componentEntitys">Can be either a design or instance entity</param>
        internal static void AddComponentToEntity(Entity parentEntity, List<Entity> componentEntitys, Guid factionID)
        {
            Entity instance;
            foreach (var componentEntity in componentEntitys)
            {
                if (parentEntity.HasDataBlob<ComponentInstancesDB>())
                {
                    if (!componentEntity.HasDataBlob<ComponentInstanceInfoDB>())
                    {
                        if (componentEntity.HasDataBlob<ComponentInfoDB>())
                        {
                            instance = ComponentInstanceFactory.NewInstanceFromDesignEntity(componentEntity, factionID, parentEntity.Manager);
                        }
                        else throw new Exception("componentEntity does not contain either a ComponentInfoDB or a ComponentInstanceInfoDB. Entity Not a ComponentDesign or ComponentInstance");
                    }
                    else
                        instance = componentEntity;

                    AddComponentInstanceToEntity(parentEntity, instance);
                }
                else throw new Exception("parentEntiy does not contain a ComponentInstanceDB");
            }
            ReCalcProcessor.ReCalcAbilities(parentEntity);
        }

        /// <summary>
        /// This is for adding and exsisting component or installation *instance* to ships and colonies. 
        /// doesn't force a Recalc. hence being private. 
        /// </summary>
        /// <param name="instance">an exsisting componentInstance</param>
        /// <param name="parentEntity">entity that contains an ComponentInstancesDB ie a ship or colony</param>
        private static void AddComponentInstanceToEntity(Entity parentEntity, Entity instance)
        {

            Entity design = instance.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity;
            foreach (IComponentDesignAttribute attribute in design.DataBlobs.Where( item => item is IComponentDesignAttribute ))
            {
                attribute.OnComponentInstalation(parentEntity, instance);
            }
            instance.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity = parentEntity;
            //AttributeToAbilityMap.AddAbility(parentEntity, design, instance);

            ComponentInstancesDB instancesDict = parentEntity.GetDataBlob<ComponentInstancesDB>();
            instancesDict.AddComponentInstance(instance);

        }
    }

    /// <summary>
    /// this is used to give an High level entity such as a ship or colony an abilityDB 
    /// </summary>
    /*
    internal static class AttributeToAbilityMap
    {
        //[ThreadStatic]
        //private static Entity CurrentEntity;
        [ThreadStatic]
        private static StaticDataStore _staticDataStore;
        internal static Dictionary<Type, Delegate> TypeMap = new Dictionary<Type, Delegate>
        {
            { typeof(EnginePowerAtbDB), new Action<Entity, Entity>((shipOrColonyEntity, componentInstanceEntity) => { if (!shipOrColonyEntity.HasDataBlob<PropulsionDB>()) shipOrColonyEntity.SetDataBlob<PropulsionDB>(new PropulsionDB()); }) },
            { typeof(CargoStorageAtbDB), new Action<Entity, Entity>((shipOrColonyEntity, componentInstanceEntity)=> { if (!shipOrColonyEntity.HasDataBlob<CargoStorageDB>()) shipOrColonyEntity.SetDataBlob(new CargoStorageDB()); }) },
            { typeof(BeamWeaponAtbDB), new Action<Entity, Entity>((shipOrColonyEntity, componentInstanceEntity) => { if (!componentInstanceEntity.HasDataBlob<BeamWeaponsDB>()) shipOrColonyEntity.SetDataBlob(new BeamWeaponsDB()); }) },
            { typeof(SimpleBeamWeaponAtbDB), new Action<Entity, Entity>((shipOrColonyEntity, componentInstanceEntity) => { if (!componentInstanceEntity.HasDataBlob<BeamWeaponsDB>()) shipOrColonyEntity.SetDataBlob(new BeamWeaponsDB()); }) },
            { typeof(BeamFireControlAtbDB), new Action<Entity, Entity>((shipOrColonyEntity, componentInstanceEntity) => { if (!componentInstanceEntity.HasDataBlob<FireControlInstanceAbilityDB>()) BeamFireControlAtbDB.SetToShip(shipOrColonyEntity);})}//shipOrColonyEntity.SetDataBlob(new FireControlInstanceAbilityDB()); }) },
        };

        /// <summary>
        /// adds abilites to a ship or component instance
        /// </summary>
        /// <param name="shipOrColony"></param>
        /// <param name="componentDesign"></param>
        /// <param name="componentInstance"></param>
        internal static void AddAbility(Entity shipOrColony, Entity componentDesign, Entity componentInstance)
        {
            _staticDataStore = shipOrColony.Manager.Game.StaticData;
            foreach (var datablob in componentDesign.DataBlobs)
            {
                var t = datablob.GetType();
                if (TypeMap.ContainsKey(t))
                    TypeMap[t].DynamicInvoke(shipOrColony, componentInstance); // invoke appropriate delegate  
            }
            componentInstance.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity = shipOrColony;
        }
    }*/
}
