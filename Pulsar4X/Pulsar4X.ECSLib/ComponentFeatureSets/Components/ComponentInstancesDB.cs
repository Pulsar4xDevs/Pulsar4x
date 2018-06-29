using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This is basicaly a collection of component Entites that the parent entity has installed. 
    /// </summary>
    public class ComponentInstancesDB : BaseDataBlob
    {
        /// <summary>
        /// Key is the component design entity
        /// Value is a list of specific instances of that component design, that entity will hold info on damage, cooldown etc.
        /// </summary>
        [JsonProperty]
        [PublicAPI]
        public Dictionary<Entity, List<Entity>> ComponentsByDesign { get; internal set; } = new Dictionary<Entity, List<Entity>>();


        // list of components and where in the ship they are - this is how likely a component is to take damage. 
        public Dictionary<Entity, double> ComponentDictionary { get; set; } = new Dictionary<Entity, double>();

        //maybe it'd be better to store references to the instanceInfoDB? we'd have to JsonIgnore then build it afterwards. 
        //internal Dictionary<Guid, Entity> ComponentsByGuid = new Dictionary<Guid, Entity>();

        [JsonProperty]
        List<Entity> allComponents = new List<Entity>();
        [JsonProperty]
        Dictionary<Type, List<Entity>> designsByAtbType = new Dictionary<Type, List<Entity>>();
        [JsonIgnore]
        Dictionary<Guid, List<ComponentInstanceInfoDB>> componentsByDesign = new Dictionary<Guid, List<ComponentInstanceInfoDB>>();



        internal void AddComponentInstance(Entity entity)
        {
            if (!entity.HasDataBlob<ComponentInstanceInfoDB>())
                throw new Exception("Not a componentInstance");

            var info = entity.GetDataBlob<ComponentInstanceInfoDB>();
            var design = info.DesignEntity;
            foreach (var datablob in design.DataBlobs)
            {
                if (datablob is IComponentDesignAttribute)
                {
                    //add the design to the dictionary if it's not already there.
                    if (!designsByAtbType.ContainsKey(datablob.GetType()))
                        designsByAtbType.Add(datablob.GetType(), new List<Entity>());
                    
                    if (!designsByAtbType[datablob.GetType()].Contains(design))
                        designsByAtbType[datablob.GetType()].Add(design);
                }
            }

            //add the component instance to the dictionary if it's not already there. 
            if (!componentsByDesign.ContainsKey(design.Guid))
                componentsByDesign.Add(design.Guid, new List<ComponentInstanceInfoDB>());
            if (!componentsByDesign[design.Guid].Contains(info))
                componentsByDesign[design.Guid].Add(info);

            if (!allComponents.Contains(entity))
                allComponents.Add(entity);
        }
        internal void RemoveComponentInstance(Entity entity)
        {
            var info = entity.GetDataBlob<ComponentInstanceInfoDB>();
            var design = info.DesignEntity;
            
            allComponents.Remove(entity);
            componentsByDesign[design.Guid].Remove(info);

            foreach (var datablob in design.DataBlobs)
            {
                if (datablob is IComponentDesignAttribute)
                {
                    designsByAtbType[datablob.GetType()].Remove(design);
                    if (designsByAtbType[datablob.GetType()].Count == 0)
                    {
                        designsByAtbType.Remove(datablob.GetType());
                    }
                }
            }
        }

        internal List<Entity> GetDesignsByType(Type type)
        {
            if (designsByAtbType.ContainsKey(type))
                return designsByAtbType[type];
            else
                return new List<Entity>();
        }
        internal List<ComponentInstanceInfoDB> GetComponentsByDesign(Guid designGuid)
        {
            return componentsByDesign[designGuid];
        }
        internal int GetNumberOfComponentsOfDesign(Guid designGuid)
        {
            return componentsByDesign[designGuid].Count;
        }

        public ComponentInstancesDB()
        {
        }


        public ComponentInstancesDB(ComponentInstancesDB db)
        {
            allComponents = new List<Entity>(db.allComponents);
            designsByAtbType = new Dictionary<Type, List<Entity>>(db.designsByAtbType);
            componentsByDesign = new Dictionary<Guid, List<ComponentInstanceInfoDB>>(db.componentsByDesign);

            ComponentsByDesign = new Dictionary<Entity, List<Entity>>(db.ComponentsByDesign);
            ComponentDictionary = new Dictionary<Entity, double>(db.ComponentDictionary);
        }

        /// <summary>
        /// this is a somewhat shallow clone. it does not clone the referenced component instance entities!!!
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            return new ComponentInstancesDB(this);
        }

        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            foreach (var item in allComponents)
            {
                AddComponentInstance(item);
            }
        }
    }
}
