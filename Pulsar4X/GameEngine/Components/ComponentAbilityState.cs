using Newtonsoft.Json;
using System;

namespace Pulsar4X.Components
{

    public abstract class ComponentAbilityState
    {
        public string Name { get; internal set; }

        public string ID { get; private set; }

        public ComponentInstance ComponentInstance { get; private set; }

        public ComponentAbilityState(ComponentInstance componentInstance)
        {
            ComponentInstance = componentInstance;
            Name = componentInstance.Design.Name;
            ID = componentInstance.UniqueID;
        }
    }
}