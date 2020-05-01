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
    /// This is basicaly a collection of components that the parent entity has installed. 
    /// </summary>
    public class ComponentInstancesDB : BaseDataBlob
    {
 

        [JsonProperty]
        //internal readonly List<ComponentInstanceData> AllComponents = new List<ComponentInstanceData>();
        [JsonIgnore]
        internal readonly Dictionary<Guid, ComponentDesign> AllDesigns = new Dictionary<Guid, ComponentDesign>();
        [JsonIgnore]
        public readonly Dictionary<ComponentDesign, int> DesignsAndComponentCount = new Dictionary<ComponentDesign, int>();
        [JsonIgnore] 
        Dictionary<Type, List<ComponentDesign>> _designsByAtbType = new Dictionary<Type, List<ComponentDesign>>();
        [JsonIgnore]
        public Dictionary<Guid, List<ComponentInstance>> ComponentsByDesign = new Dictionary<Guid, List<ComponentInstance>>();

        public Dictionary<Type, List<ComponentInstance>> ComponentsByAttribute = new Dictionary<Type, List<ComponentInstance>>();

        internal readonly Dictionary<Guid, ComponentInstance> AllComponents = new Dictionary<Guid, ComponentInstance>();
        
        /* Maybe flat arrays would be better? need to test see the mem size difference and speed difference.
        private Guid[] _instanceIDArray = new Guid[0];
        private ComponentInstance[] _instanceArray = new ComponentInstance[0];
        private ComponentDesign[] _designsArray = new ComponentDesign[0];
        private ComponentAbilityState[] _statesArray = new ComponentAbilityState[0];
        */
        public Type[] GetAllAttributeTypes()
        {
            return ComponentsByAttribute.Keys.ToArray();
        }

        public bool TryGetComponentsByAttribute<T>(out List<ComponentInstance> components)
            where T : IComponentDesignAttribute
        {
            return ComponentsByAttribute.TryGetValue(typeof(T), out components);
        }

        public bool TryGetComponentStates<T>(out List<T> componentStates)
            where T : ComponentAbilityState
        {
            componentStates = new List<T>();
            foreach (var comp in AllComponents.Values)
            {
                if( comp.TryGetAbilityState<T>(out T state))
                    componentStates.Add(state);
            }

            if (componentStates.Count > 0)
                return true;
            return false;
        }
        
        public bool TryGetComponentsWithStates<T>(out List<ComponentInstance> instances)
            where T : ComponentAbilityState
        {
            instances = new List<ComponentInstance>();
            foreach (var comp in AllComponents.Values)
            {
                if( comp.HasAblity<T>())
                    instances.Add(comp);
            }

            if (instances.Count > 0)
                return true;
            return false;
        }

        internal void AddComponentInstance(ComponentInstance instance)
        {
            AllComponents.Add(instance.ID, instance);
            
            var design = instance.Design;
            AllDesigns[design.ID] = design;
            foreach (var attbkvp in design.AttributesByType)
            {
                //add the design to the dictionary if it's not already there.
                if (!_designsByAtbType.ContainsKey(attbkvp.Key))
                    _designsByAtbType.Add(attbkvp.Key, new List<ComponentDesign>());
                
                if (!_designsByAtbType[attbkvp.Key].Contains(design))
                    _designsByAtbType[attbkvp.Key].Add(design);
            }

            //add the component instance to the dictionary if it's not already there. 
            if (!ComponentsByDesign.ContainsKey(design.ID))
                ComponentsByDesign.Add(design.ID, new List<ComponentInstance>());
            if (!ComponentsByDesign[design.ID].Contains(instance))
                ComponentsByDesign[design.ID].Add(instance);


            
            
            if (!DesignsAndComponentCount.ContainsKey(design))
                DesignsAndComponentCount.Add(design, 1);
            else
                DesignsAndComponentCount[design] += 1;

            foreach (var atbkvp in instance.Design.AttributesByType)
            {
                if(!ComponentsByAttribute.ContainsKey(atbkvp.Key))
                    ComponentsByAttribute.Add(atbkvp.Key, new List<ComponentInstance>());
                    
                ComponentsByAttribute[atbkvp.Key].Add(instance);
            }
        }
        
        internal void RemoveComponentInstance(ComponentInstance instance)
        {
            
            var design = instance.Design;
            AllDesigns.Remove(design.ID);
            AllComponents.Remove(instance.ID);
            ComponentsByDesign[design.ID].Remove(instance);

            foreach (var atbkvp in design.AttributesByType)
            {
                _designsByAtbType[atbkvp.Key].Remove(design);
                if (_designsByAtbType[atbkvp.Key].Count == 0)
                {
                    _designsByAtbType.Remove(atbkvp.Key);
                }
            }
            DesignsAndComponentCount[design] -= 1;
            if (DesignsAndComponentCount[design] == 0)
                DesignsAndComponentCount.Remove(design);
            
            foreach (var atbkvp in instance.Design.AttributesByType)
            {
                ComponentsByAttribute[atbkvp.Key].Remove(instance);
                
            }
        }

        internal List<ComponentDesign> GetDesignsByType(Type type)
        {
            if (_designsByAtbType.ContainsKey(type))
                return _designsByAtbType[type];
            else
                return new List<ComponentDesign>();
        }
        internal List<ComponentInstance> GetComponentsBySpecificDesign(Guid designGuid)
        {
            return ComponentsByDesign[designGuid];
        }
        internal Dictionary<Guid, List<ComponentInstance>> GetComponentsByDesigns()
        {
            Dictionary<Guid, List<ComponentInstance>> componentsByDesign = new Dictionary<Guid, List<ComponentInstance>>();
            foreach (var designKVP in ComponentsByDesign)
            {
                List<ComponentInstance> instances = new List<ComponentInstance>();
                foreach (var instance in designKVP.Value)
                {
                    instances.Add(instance);
                }
                componentsByDesign.Add(designKVP.Key, instances);
            }
            return componentsByDesign;
        }
        
        
        
        
        internal int GetNumberOfComponentsOfDesign(Guid designGuid)
        {
            return ComponentsByDesign[designGuid].Count;
        }

        public ComponentInstancesDB()
        {
        }


        public ComponentInstancesDB(ComponentInstancesDB db)
        {
            AllComponents = new Dictionary<Guid, ComponentInstance>(db.AllComponents);
            _designsByAtbType = new Dictionary<Type, List<ComponentDesign>>(db._designsByAtbType);
            ComponentsByDesign = new Dictionary<Guid, List<ComponentInstance>>(db.ComponentsByDesign);

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
                    AddComponentInstance(item.Value);
                }
            };

        }
    }
}
