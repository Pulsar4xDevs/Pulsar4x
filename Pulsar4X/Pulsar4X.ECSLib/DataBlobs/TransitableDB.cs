using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class TransitableDB : BaseDataBlob
    {
        [JsonProperty]
        public Entity Destination { get; internal set; }

        /// <summary>
        /// Default public constructor for Json
        /// </summary>
        public TransitableDB()
        {

        }

        public TransitableDB(Entity destination)
        {
            Destination = destination;
        }

        public override object Clone()
        {
            return new TransitableDB(Destination);
        }
    }
}
