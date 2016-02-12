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
        public ReadOnlyDictionary<Entity, int> BridgeDictionary => new ReadOnlyDictionary<Entity, int>(bridgeDictionary);
        [JsonProperty]
        internal Dictionary<Entity, int> bridgeDictionary = new Dictionary<Entity, int>();

        public bool IsStabilized => isStabilized;
        [JsonProperty]
        private bool isStabilized;

        /// <summary>
        /// Default public constructor for Json
        /// </summary>
        public TransitableDB()
        {

        }

        public TransitableDB(Entity destination) : this(destination, false)
        {
        }

        public TransitableDB(Entity destination, bool isStabilized)
        {
            Destination = destination;
            this.isStabilized = isStabilized;
        }

        public override object Clone()
        {
            return new TransitableDB(Destination);
        }
    }
}
