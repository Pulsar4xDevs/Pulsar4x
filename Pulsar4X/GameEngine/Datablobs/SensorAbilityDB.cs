using System;
using System.Collections.Generic;
using Pulsar4X.Engine.Sensors;

namespace Pulsar4X.Datablobs
{
    public class SensorAbilityDB : BaseDataBlob
    {
        internal Dictionary<string, SensorReturnValues> CurrentContacts = new ();
        internal Dictionary<string, SensorReturnValues> OldContacts = new ();

        public SensorAbilityDB()
        {
        }

        public SensorAbilityDB(SensorAbilityDB db)
        {
            CurrentContacts = new Dictionary<string, SensorReturnValues>(db.CurrentContacts);
            OldContacts = new Dictionary<string, SensorReturnValues>(db.OldContacts);
        }

        public override object Clone()
        {
            return new SensorAbilityDB(this);
        }
    }
}