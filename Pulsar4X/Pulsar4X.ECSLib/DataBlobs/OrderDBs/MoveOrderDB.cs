using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    // Move order within a system
    class MoveOrderDB : BaseOrderDB
    {
        [JsonProperty]
        public Entity Ship { get; internal set; }

        // A move order may have either a specific target entity or a location
        
        [JsonProperty]
        public Entity Target { get; internal set; }

        [JsonProperty]
        public PositionDB PositionTarget { get; internal set; }

        // A move order may also require a ship to orbit the target at a given radius - only applicable if Target is not invalid
        [JsonProperty]
        public long OrbitRadius { get; internal set; }

        public MoveOrderDB()
        {
            Ship = Entity.InvalidEntity;
            Target = Entity.InvalidEntity;
            PositionTarget = null;
            OrbitRadius = 0;
        }

        public MoveOrderDB(Entity ship, Entity target, long orbitRadius = 0)
        {
            Ship = ship;
            Target = target;
            PositionTarget = null;
            OrbitRadius = orbitRadius;
        }

        public MoveOrderDB(Entity ship, PositionDB target, long orbitRadius = 0)
        {
            Ship = ship;
            Target = Entity.InvalidEntity;
            PositionTarget = new PositionDB(target);
            OrbitRadius = orbitRadius;
        }

        public override object Clone()
        {
            MoveOrderDB order = new MoveOrderDB();
            order.Ship = Ship;
            order.Target = Target;
            order.PositionTarget = PositionTarget;
            OrbitRadius = OrbitRadius;

            return order;
        }

        public override bool isValid()
        {
            if (Ship == null || Ship == Entity.InvalidEntity)
                return false;

            // The ship can't move if it doesn't have any ability to propel itself
            if (!Ship.HasDataBlob<PropulsionDB>())
                return false;

            if (Target == null || Target == Entity.InvalidEntity)
            {
                if (PositionTarget == null)  // Either a location or a target is necessary
                    return false;
            }
            else
                // @todo: jump point, jump survey point
                if (!Target.HasDataBlob<SystemBodyDB>() && !Target.HasDataBlob<ShipInfoDB>())
                    return false;

            // @todo: further conditions

            return true;
        }
    }
}
