using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// TransitableDB defines an entity as capable of being used as a jump point.
    /// </summary>
    public class TransitableDB : BaseDataBlob
    {
        /// <summary>
        /// Destination that this jump point goes to.
        /// </summary>
        [JsonProperty]
        public Entity Destination { get; internal set; }

        [JsonProperty]
        public bool IsStabilized { get; internal set; }

        public TransitableDB() { }

        public TransitableDB(Entity destination) : this(destination, false) { }

        public TransitableDB(Entity destination, bool isStabilized)
        {
            Destination = destination;
            IsStabilized = isStabilized;
        }

        public override object Clone()
        {
            return new TransitableDB(Destination, IsStabilized);
        }
    }
}
