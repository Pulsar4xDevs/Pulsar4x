using Newtonsoft.Json;

namespace Pulsar4X.ECSLib.DataBlobs.OrderDBs
{
    class MoveOrderDB : BaseDataBlob
    {
        [JsonProperty]
        public Entity Ship { get; internal set; }

        [JsonProperty]
        public Entity Target { get; internal set; }



        public MoveOrderDB()
        {

        }

        public MoveOrderDB(Entity ship, Entity target)
        {
            Ship = ship;
            Target = target;
        }

        public override object Clone()
        {
            return new MoveOrderDB(Ship, Target);
        }
    }
}
