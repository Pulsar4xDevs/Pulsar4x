using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;

namespace Pulsar4X.Sensors
{
    public class SensorAbilityDB : BaseDataBlob
    {
        public new static List<Type> GetDependencies() => new List<Type>() { typeof(ComponentInstancesDB) };

 
        public List<(Entity entity, SensorReturnValues returnValues)> CurrentContacts = new();
        /// <summary>
        /// NOTE: the below InstanceAtributes list assumes parity with the InstanceStates list,
        /// Do not manupulate either list other than with SensorTools.SetInstances
        /// </summary>
        public List<SensorReceiverAtb> InstanceAtributes = new List<SensorReceiverAtb>();
        /// <summary>
        /// NOTE: the below InstanceStates list assumes parity with the above InstanceAtributes list,
        /// Do not manupulate either list other than with SensorTools.SetInstances
        /// </summary>
        public List<SensorReceiverAbility> InstanceStates = new List<SensorReceiverAbility>();
        public SensorAbilityDB()
        {
        }

        public SensorAbilityDB(SensorAbilityDB db)
        {

        }

        public override object Clone()
        {
            return new SensorAbilityDB(this);
        }
    }
}