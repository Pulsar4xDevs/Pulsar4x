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
    }



    public class ComponentInstance : ICargoable
    {

        #region ICargoable

        public Guid ID { get; }
        public string Name { get; }
        public Guid CargoTypeID { get; }
        public int Mass
        {
            get { return Design.Mass; }
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
