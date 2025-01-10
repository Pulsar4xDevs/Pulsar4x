using Newtonsoft.Json;
using System;

namespace Pulsar4X.Components
{

    public abstract class ComponentAbilityState
    {

        public string Name
        {
            get { return ComponentInstance.Design.Name; }
        }

        public string ID
        {
            get { return ComponentInstance.UniqueID; }
        }

        [JsonProperty]
        public ComponentInstance ComponentInstance { get; private set; }

        [JsonConstructor]
        protected ComponentAbilityState(){}
        
        public ComponentAbilityState(ComponentInstance componentInstance)
        {
            ComponentInstance = componentInstance;
        }
    }
}