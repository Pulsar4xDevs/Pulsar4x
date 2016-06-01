using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MoveOrderDB : BaseDataBlob
    {
        [JsonProperty]
        public Entity Ship { get; internal set; }

        [JsonProperty]
        public Entity Target { get; internal set; }



        public MoveOrderDB()
        {
            Ship = Entity.InvalidEntity;
            Target = Entity.InvalidEntity;
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

        public bool isValid()
        {
            if (Ship == null || Ship == Entity.InvalidEntity)
                return false;
            if (Target == null || Target == Entity.InvalidEntity)
                return false;

            // The ship can't move if it doesn't have any ability to propel itself
            if (!Ship.HasDataBlob<PropulsionDB>())
                return false;

            // @todo: jump point, jump survey point
            if (!Target.HasDataBlob<SystemBodyDB>() && !Target.HasDataBlob<ShipInfoDB>())
                return false;

            // @todo: further conditions
            // Target has a location

            return true;
        }
    }
}
