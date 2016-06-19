using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    // Move order within a system
    public class MoveOrder : BaseOrder
    {
        // Owner is the ship in question

        // A move order may have either a specific target entity or a location
        
        [JsonProperty]
        public Entity Target { get; internal set; }

        [JsonProperty]
        public PositionDB PositionTarget { get; internal set; }

        // A move order may also require a ship to orbit the target at a given radius - only applicable if Target is not invalid
        [JsonProperty]
        public long OrbitRadius { get; internal set; }

        public int MaximumSpeed { get; internal set; }

        public MoveOrder()
        {
            DelayTime = 0;
            Owner = Entity.InvalidEntity;
            Target = Entity.InvalidEntity;
            PositionTarget = null;
            OrbitRadius = 0;
            MaximumSpeed = 0;
        }

        public MoveOrder(Entity ship, Entity target, long orbitRadius = 0)
        {
            DelayTime = 0;
            Owner = ship;
            Target = target;
            PositionTarget = null;
            OrbitRadius = orbitRadius;
            MaximumSpeed = ship.GetDataBlob<PropulsionDB>().MaximumSpeed;
        }

        public MoveOrder(Entity ship, PositionDB target, long orbitRadius = 0)
        {
            DelayTime = 0;
            Owner = ship;
            Target = Entity.InvalidEntity;
            PositionTarget = new PositionDB(target);
            OrbitRadius = orbitRadius;
            MaximumSpeed = ship.GetDataBlob<PropulsionDB>().MaximumSpeed;
        }

        public override object Clone()
        {
            MoveOrder order = new MoveOrder();
            order.DelayTime = DelayTime;
            order.Owner = Owner;
            order.Target = Target;
            order.PositionTarget = PositionTarget;
            order.OrbitRadius = OrbitRadius;
            order.MaximumSpeed = MaximumSpeed;

            return order;
        }

        public override bool isValid()
        {
            if (Owner == null || Owner == Entity.InvalidEntity)
                return false;

            // The ship can't move if it doesn't have any ability to propel itself
            if (!Owner.HasDataBlob<PropulsionDB>())
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

        // sets the speed of the ship to the maximum speed allowed in the direction of the target.
        // Returns true if the destination has been reached.
        public override bool processOrder()
        {
            PositionDB currentPosition = Owner.GetDataBlob<PositionDB>();
            PositionDB targetPosition = null;

            if (Target != Entity.InvalidEntity)
                targetPosition = Target.GetDataBlob<PositionDB>();
            else if (PositionTarget != null)
                targetPosition = PositionTarget;
            
            if (currentPosition == targetPosition) // We have reached the target
            {
                Owner.GetDataBlob<PropulsionDB>().CurrentSpeed = new Vector4(0.0, 0.0, 0.0, 0.0);
                return true;
            }

            // Set the speed of the ship
            // @todo - take into account Task Group speeds and limiting speed set by user
            Owner.GetDataBlob<PropulsionDB>().CurrentSpeed = getSpeed(currentPosition, targetPosition, Owner.GetDataBlob<PropulsionDB>().MaximumSpeed);

            return false;
        }

        private Vector4 getSpeed(PositionDB currentPosition, PositionDB targetPosition, int speedMagnitude)
        {
            Vector4 speed = new Vector4( 0, 0, 0, 0 );
            double length;

            Vector4 direction = new Vector4(0, 0, 0, 0);
            direction.X = targetPosition.X - currentPosition.X;
            direction.Y = targetPosition.Y - currentPosition.Y;
            direction.Z = targetPosition.Z - currentPosition.Z;
            direction.W = 0;

            length = direction.Length();

            direction.X = (direction.X / length);
            direction.Y = (direction.Y / length);
            direction.Z = (direction.Z / length);

            speed.X = direction.X * speedMagnitude;
            speed.Y = direction.Y * speedMagnitude;
            speed.Z = direction.Z * speedMagnitude;

            return speed;
        }
    }
}
