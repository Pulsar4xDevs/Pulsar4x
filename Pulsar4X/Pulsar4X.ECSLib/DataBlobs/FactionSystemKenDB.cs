using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    public class FactionSystemKenDB : BaseDataBlob
    {

        [JsonProperty]
        public Dictionary<Entity, List<Entity>> EntitiesKnownByFaction { get; internal set; } = new Dictionary<Entity, List<Entity>>();


        public FactionSystemKenDB()
        {
            EntitiesKnownByFaction = new Dictionary<Entity, List<Entity>>();
        }

        public FactionSystemKenDB(FactionSystemKenDB db)
        {
            EntitiesKnownByFaction = new Dictionary<Entity, List<Entity>>(this.EntitiesKnownByFaction);
        }


        public override object Clone()
        {
            return new FactionSystemKenDB(this);
        }
    }
}
