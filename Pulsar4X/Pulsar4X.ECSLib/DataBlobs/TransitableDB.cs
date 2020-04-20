
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

        /// <summary>
        /// Determination if this jump point has a "gate" on it.
        /// </summary>
        /// <remarks>
        /// TODO: Gameplay Review
        /// We might want to use a TransitType enum, to allow different types of FTL using the same type of DB
        /// </remarks>
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
