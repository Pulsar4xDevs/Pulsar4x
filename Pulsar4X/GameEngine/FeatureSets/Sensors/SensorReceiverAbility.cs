using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Components;

namespace Pulsar4X.Engine.Sensors
{
    public class SensorReceiverAbility : ComponentAbilityState
    {
        [JsonProperty]
        public Dictionary<int, SensorReturnValues> CurrentContacts = new ();
        public Dictionary<int, SensorReturnValues> OldContacts = new ();

        public SensorReceiverAbility(ComponentInstance componentInstance) : base(componentInstance)
        {
        }
    }
}