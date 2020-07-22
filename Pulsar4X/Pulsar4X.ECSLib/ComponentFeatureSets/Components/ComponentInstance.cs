using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;

namespace Pulsar4X.ECSLib
{

    public abstract class ComponentAbilityState
    {
        public string Name { get; internal set; }

        public Guid ID { get; private set; }
        
        public ComponentInstance ComponentInstance { get; private set; }
        
        public ComponentAbilityState(ComponentInstance componentInstance)
        {
            ComponentInstance = componentInstance;
            Name = componentInstance.Design.Name;
            ID = componentInstance.ID;
        }
        //public ComponentInstance ComponentInstance;

        //public ComponentAbilityState(ComponentInstance componentInstance)
        //{
        //   ComponentInstance = componentInstance;
        //}
    }

    public abstract class ComponentTreeHeirarchyAbilityState : ComponentAbilityState
    {
        public ComponentTreeHeirarchyAbilityState ParentState { get; private set; }
        public List<ComponentTreeHeirarchyAbilityState> ChildrenStates { get; private set; } = new List<ComponentTreeHeirarchyAbilityState>();

        

        public ComponentTreeHeirarchyAbilityState(ComponentInstance componentInstance) : base(componentInstance)
        {
            
        }

        /// <summary>
        /// Sets the parent of this. (no need to set this as a child on the parent)
        /// </summary>
        /// <param name="newParent"></param>
        public void SetParent(ComponentTreeHeirarchyAbilityState newParent)
        {
            if (ParentState != null)
            {
                ParentState.ChildrenStates.Remove(this);
            }

            ParentState = newParent;
            if(newParent != null)
                ParentState.ChildrenStates.Add(this);
        }

        /// <summary>
        /// Clears any exsisting children and sets children to the given list (no need to set parent on children seperately)
        /// </summary>
        /// <param name="children"></param>
        public void SetChildren(ComponentTreeHeirarchyAbilityState[] children)
        {
            var oldChilders = new List<ComponentTreeHeirarchyAbilityState>(ChildrenStates);
            ChildrenStates.Clear();
            foreach (ComponentTreeHeirarchyAbilityState orphan in oldChilders)
            {
                orphan.SetParent(null);
            }         
            
            foreach (var child in children)
            {
                child.SetParent(this);
            }


        }

        /*
         /// <summary>
         ///some ideas, implement if actualy needed
        /// </summary>
        public BaseDataBlob ThisRelatedDatablob; 
        
        public InstancesDB ThisEntitesInstancesDB;
                
        (call this from Set Parent, virtual would be empty, inherited classes would have something below eg)
        protected virtual void FilterParents(ComponentTreeHeirarchyAbilityState parentToSet) 
        {
            Type T = typeof(FireControlAbilityState)
            if(parentToSet is T)
            {            
                if (ParentState != null)
                {
                    ParentState.ChildrenStates.Remove(this);
                }

                ParentState = newParent;
                if(newParent != null)
                    ParentState.ChildrenStates.Add(this);
            }
            else
                throw exception? fail silently? log?
        }
        
        protected virtual void FilterChildren(ComponentTreeHeirarchyAbilityState childToAdd) 
        {
            if(childToAdd is T)
                ChildrenState.Add(childToAdd);
            else
                throw exception? fail silently? log?
        }
        
        
                /// <summary>
        /// If parent is null, will return an empty list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetSiblingsOfType<T>()
            where T : ComponentTreeHeirarchyAbilityState
        {
            if(ParentState == null)
                return new List<T>();
            return ParentState.GetChildrenOfType<T>();
        }
        
        
        public List<T> GetChildrenOfType<T>() where T: ComponentTreeHeirarchyAbilityState
        {
            List<T> children = new List<T>();
            foreach (var child in ChildrenStates)
            {
                if(child is T)
                    children.Add((T)child);
            }
            return children;
        }
        
        public ComponentInstance[] GetChildrenInstancesOfType<T>() where T: ComponentTreeHeirarchyAbilityState
        {
            var childrenStates = GetChildrenOfType<T>();
            ComponentInstance[] instances = new ComponentInstance[childrenStates.Count];
            for (int i = 0; i < childrenStates.Count; i++)
            {
                instances[i] = childrenStates[i].ComponentInstance;
            }
            return instances;
        }
        
        */

        public ComponentTreeHeirarchyAbilityState GetRoot()
        {
            if (ParentState != null)
                return ParentState.GetRoot();
            else
                return this;
        }

        
        
        public ComponentInstance GetRootInstance()
        {
            return GetRoot().ComponentInstance;
        }

        public List<ComponentTreeHeirarchyAbilityState> GetSiblings()
        {
            return ParentState.ChildrenStates;
        }
        
        public ComponentInstance[] GetChildrenInstances()
        {
            ComponentInstance[] instances = new ComponentInstance[ChildrenStates.Count];
            for (int i = 0; i < ChildrenStates.Count; i++)
            {
                instances[i] = ChildrenStates[i].ComponentInstance;
            }
            return instances;
        }
        

        public Guid[] GetChildrenIDs()
        {
            Guid[] ids = new Guid[ChildrenStates.Count];
            for (int i = 0; i < ChildrenStates.Count; i++)
            {
                ids[i] = ChildrenStates[i].ID;
            }

            return ids;
        }
        
        
    }

    public class ComponentInstance : ICargoable
    {

        #region ICargoable

        public Guid ID { get; }
        public string Name { get; }
        public Guid CargoTypeID { get; }
        public int MassPerUnit
        {
            get { return Design.MassPerUnit; }
        }

        public double VolumePerUnit
        {
            get { return Design.VolumePerUnit; }
        }

        public double Density
        {
            get { return Design.Density; }
        }

        #endregion

        /// <summary>
        /// This is the entity that this component  is mounted on. 
        /// </summary>
        /// <value>The parent entity.</value>
        [JsonProperty]
        public Entity ParentEntity
        {
            get { return _parentEntity; }
            internal set { _parentEntity = value; ParentInstances = ParentEntity.GetDataBlob<ComponentInstancesDB>(); }
        }

        private Entity _parentEntity;

        [JsonIgnore]
        public ComponentInstancesDB ParentInstances { get; private set; }
        /// <summary>
        /// This is the design of this component. 
        /// </summary>
        /// <value>The design entity.</value>
        [JsonProperty]
        public ComponentDesign Design { get; internal set; }
        [JsonProperty]
        public bool IsEnabled { get; internal set; }
        [JsonProperty]
        public PercentValue ComponentLoadPercent { get; internal set; }        
        [JsonProperty]
        public int HTKRemaining { get; internal set; }
        [JsonProperty]
        public int HTKMax { get; private set; }
        

        private Dictionary<Type, ComponentAbilityState> _instanceAbilities = new Dictionary<Type, ComponentAbilityState>();

        public Dictionary<Type, ComponentAbilityState> GetAllStates()
        {
            return new Dictionary<Type, ComponentAbilityState>(_instanceAbilities);
        }

        public bool HasAblity<T>()
            where T : ComponentAbilityState
        {
            return _instanceAbilities.ContainsKey(typeof(T));
        }

        public T GetAbilityState<T>() 
            where T : ComponentAbilityState
        {
            return (T)_instanceAbilities[typeof(T)];
        }
        
        public bool TryGetAbilityState<T>(out T attribute)
            where T : ComponentAbilityState
        {
            if (_instanceAbilities.ContainsKey(typeof(T)))
            {
                attribute = (T)_instanceAbilities[typeof(T)];
                return true;
            }
            attribute = default(T);
            return false;
        }

        internal void SetAbilityState<T>(ComponentAbilityState abilityState) where T: ComponentAbilityState
        {
            _instanceAbilities[typeof(T)] = abilityState;
        }

        /// <summary>
        /// returns the design name and index of this component ie "Thruster5k# 1"
        /// Avoid doing this each frame, it looks up the name via doing a GetDataBlob and an List.IndexOf()
        /// </summary>
        public string GetName()
        {
            string designName = Design.Name;
            return designName + "# " + ParentInstances.GetComponentsBySpecificDesign(Design.ID).IndexOf(this);
        }

        [JsonConstructor]
        private ComponentInstance() { }

        /// <summary>
        /// Constructor for a componentInstance.
        /// ComponentInstance stores component specific data such as hit points remaining etc.
        /// </summary>
        /// <param name="design">The Component Entity, MUST have a ComponentInfoDB</param>
        /// <param name="isEnabled">whether the component is enabled on construction. default=true</param>
        public ComponentInstance(ComponentDesign design, bool isEnabled = true)
        {
            ID = Guid.NewGuid();
            Design = design;
            IsEnabled = isEnabled;
            HTKRemaining = design.HTK;
            HTKMax = design.HTK;
            CargoTypeID = design.CargoTypeID;
            Name = design.Name;

        }


        public ComponentInstance(ComponentInstance instance)
        {
            ID = instance.ID;
            Design = instance.Design;
            IsEnabled = instance.IsEnabled;
            ComponentLoadPercent = instance.ComponentLoadPercent;
            HTKRemaining = instance.HTKRemaining;
            HTKMax = instance.HTKMax;
            CargoTypeID = instance.CargoTypeID;
            Name = instance.Name;

        }
        

        public float HealthPercent()
        { return HTKRemaining / HTKMax; }

    }
}
