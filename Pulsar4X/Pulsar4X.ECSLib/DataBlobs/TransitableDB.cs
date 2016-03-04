using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class TransitableDB : BaseDataBlob
    {
        [JsonProperty]
        public Entity Destination { get; internal set; }

        public ReadOnlyDictionary<Entity, int> BridgeDictionary => new ReadOnlyDictionary<Entity, int>(_bridgeDictionary);
        [JsonProperty]
        internal Dictionary<Entity, int> _bridgeDictionary = new Dictionary<Entity, int>();

        [JsonProperty]
        public bool IsStabilized { get; internal set; }

        public TransitableDB() { }

        public TransitableDB(Entity destination) : this(destination, false, new Dictionary<Entity, int>()) { }

        public TransitableDB(Entity destination, bool isStabilized, IDictionary<Entity, int> bridgeDictionary)
        {
            Destination = destination;
            IsStabilized = isStabilized;
            _bridgeDictionary = new Dictionary<Entity, int>(bridgeDictionary);
        }

        public override object Clone()
        {
            return new TransitableDB(Destination, IsStabilized, _bridgeDictionary);
        }
    }
}
