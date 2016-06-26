using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class ComponentInstanceFactory
    {

        internal static Entity NewInstanceFromDesignEntity(Entity design)
        {

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            ComponentInstanceInfoDB info = new ComponentInstanceInfoDB(design);
            blobs.Add(info);

            Entity newInstance = new Entity(design.Manager, blobs);
            return newInstance;
        }
    }


    /// <summary>
    /// this is used to give an High level entity such as a ship or colony an ability 
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
