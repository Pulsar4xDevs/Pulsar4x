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
        //[JsonProperty]
        //[PublicAPI]
        //public Dictionary<Entity, List<Entity>> ComponentsByDesign { get; internal set; } = new Dictionary<Entity, List<Entity>>();


        // list of components and where in the ship they are - this is how likely a component is to take damage. 
        public Dictionary<Entity, double> ComponentDictionary { get; set; } = new Dictionary<Entity, double>();

        //maybe it'd be better to store references to the instanceInfoDB? we'd have to JsonIgnore then build it afterwards. 
        //internal Dictionary<Guid, Entity> ComponentsByGuid = new Dictionary<Guid, Entity>();

        [JsonProperty]
        internal readonly List<Entity> AllComponents = new List<Entity>();
        [JsonIgnore]
        internal readonly Dictionary<Guid, Entity> AllDesigns = new Dictionary<Guid, Entity>();
        [JsonIgnore]
        internal readonly Dictionary<Entity, int> DesignsAndComponentCount = new Dictionary<Entity, int>();
        [JsonIgnore] 
        Dictionary<Type, List<Entity>> _designsByAtbType = new Dictionary<Type, List<Entity>>();
        [JsonIgnore]
        Dictionary<Guid, List<ComponentInstanceInfoDB>> _componentsByDesign = new Dictionary<Guid, List<ComponentInstanceInfoDB>>();



        internal void AddComponentInstance(Entity entity)
        {
            if (!entity.HasDataBlob<ComponentInstanceInfoDB>())
                throw new Exception("Not a componentInstance");

            var info = entity.GetDataBlob<ComponentInstanceInfoDB>();
            var design = info.DesignEntity;
            AllDesigns[design.Guid] = design;
            foreach (var datablob in design.DataBlobs)
            {
                if (datablob is IComponentDesignAttribute)
                {
                    //add the design to the dictionary if it's not already there.
                    if (!_designsByAtbType.ContainsKey(datablob.GetType()))
                        _designsByAtbType.Add(datablob.GetType(), new List<Entity>());
                    
                    if (!_designsByAtbType[datablob.GetType()].Contains(design))
                        _designsByAtbType[datablob.GetType()].Add(design);
                }
            }

            //add the component instance to the dictionary if it's not already there. 
            if (!_componentsByDesign.ContainsKey(design.Guid))
                _componentsByDesign.Add(design.Guid, new List<ComponentInstanceInfoDB>());
            if (!_componentsByDesign[design.Guid].Contains(info))
                _componentsByDesign[design.Guid].Add(info);

            if (!AllComponents.Contains(entity))
                AllComponents.Add(entity);
            if (!DesignsAndComponentCount.ContainsKey(design))
                DesignsAndComponentCount.Add(design, 1);
            else
                DesignsAndComponentCount[design] += 1;
        }
        internal void RemoveComponentInstance(Entity entity)
        {
            var info = entity.GetDataBlob<ComponentInstanceInfoDB>();
            var design = info.DesignEntity;
            AllDesigns.Remove(design.Guid);
            AllComponents.Remove(entity);
            _componentsByDesign[design.Guid].Remove(info);


            foreach (var datablob in design.DataBlobs)
            {
                if (datablob is IComponentDesignAttribute)
                {
                    _designsByAtbType[datablob.GetType()].Remove(design);
                    if (_designsByAtbType[datablob.GetType()].Count == 0)
                    {
                        _designsByAtbType.Remove(datablob.GetType());
                    }
                }
            }
            DesignsAndComponentCount[design] -= 1;
            if (DesignsAndComponentCount[design] == 0)
                DesignsAndComponentCount.Remove(design);
        }

        internal List<Entity> GetDesignsByType(Type type)
        {
            if (_designsByAtbType.ContainsKey(type))
                return _designsByAtbType[type];
            else
                return new List<Entity>();
        }
        internal List<ComponentInstanceInfoDB> GetComponentsBySpecificDesign(Guid designGuid)
        {
            return _componentsByDesign[designGuid];
        }
        internal Dictionary<Guid, List<Entity>> GetComponentsByDesigns()
        {
            Dictionary<Guid, List<Entity>> componentsByDesign = new Dictionary<Guid, List<Entity>>();
            foreach (var designKVP in _componentsByDesign)
            {
                List<Entity> entitys = new List<Entity>();
                foreach (var instance in designKVP.Value)
                {
                    entitys.Add(instance.OwningEntity);
                }
                componentsByDesign.Add(designKVP.Key, entitys);
            }
            return componentsByDesign;
        }
        internal int GetNumberOfComponentsOfDesign(Guid designGuid)
        {
            return _componentsByDesign[designGuid].Count;
        }

        public ComponentInstancesDB()
        {
        }


        public ComponentInstancesDB(ComponentInstancesDB db)
        {
            AllComponents = new List<Entity>(db.AllComponents);
            _designsByAtbType = new Dictionary<Type, List<Entity>>(db._designsByAtbType);
            _componentsByDesign = new Dictionary<Guid, List<ComponentInstanceInfoDB>>(db._componentsByDesign);

            //ComponentsByDesign = new Dictionary<Entity, List<Entity>>(db.ComponentsByDesign);
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
            var game = (Game)context.Context;
            game.PostLoad += (sender, args) => 
            { 
                foreach (var item in AllComponents)
                {
                    AddComponentInstance(item);
                }
            };

        }
    }
}
