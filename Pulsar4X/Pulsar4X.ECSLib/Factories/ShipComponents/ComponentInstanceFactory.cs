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
            { typeof(EnginePowerAtbDB), new Action<Entity>(entity => { if (!entity.HasDataBlob<PropulsionDB>()) entity.SetDataBlob<PropulsionDB>(new PropulsionDB()); }) },
        };


        internal static void AddAttribute(Entity shipOrColony, Entity component)
        {

            //CurrentEntity = shipOrColony;
            foreach (var datablob in component.DataBlobs)
            {
                var t = datablob.GetType();
                if (TypeMap.ContainsKey(t))
                    TypeMap[t].DynamicInvoke(shipOrColony); // invoke appropriate delegate  
            }
        }
    }
}
