using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// JumpPointDB defines a jump point
    /// </summary>
    public class JumpPointDB : BaseDataBlob
    {
        /// <summary>
        /// The Id of the entity that this jump point goes to
        /// </summary>
        [JsonProperty]
        public int DestinationId { get; internal set; }

        /// <summary>
        /// Determination if this jump point has a "gate" on it.
        /// </summary>
        /// <remarks>
        /// TODO: Gameplay Review
        /// We might want to use a TransitType enum, to allow different types of FTL using the same type of DB
        /// </remarks>
        [JsonProperty]
        public bool IsStabilized { get; internal set; }

        [JsonProperty]
        public HashSet<int> IsDiscovered { get; internal set; } = new HashSet<int>();

        public JumpPointDB() { }

        public JumpPointDB(int destinationId, bool isStabilized = false)
        {
            DestinationId = destinationId;
            IsStabilized = isStabilized;
        }

        public JumpPointDB(Entity destination, bool isStabilized = false)
            : this(destination.Id, isStabilized)
        {
        }

        public override object Clone()
        {
            return new JumpPointDB(DestinationId, IsStabilized);
        }
    }
}
