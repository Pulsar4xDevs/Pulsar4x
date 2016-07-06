using Newtonsoft.Json;
using System;

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
            OrderType = orderType.MOVETO;
        }

        public MoveOrder(Entity ship, Entity target, long orbitRadius = 0) : this()
        {
            DelayTime = 0;
            Owner = ship;
            Target = target;
            PositionTarget = null;
            OrbitRadius = orbitRadius;
            MaximumSpeed = ship.GetDataBlob<PropulsionDB>().MaximumSpeed;
        }

        public MoveOrder(Entity ship, PositionDB target, long orbitRadius = 0) : this()
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
        // Returns true if the destination has been reached or the target no longer exists.
        public override bool processOrder()
        {

            double speedMultiplier = 1000.0;
            PositionDB currentPosition = Owner.GetDataBlob<PositionDB>();
            PositionDB targetPosition = null;
            double AUSpeed, kmSpeed;

            kmSpeed = Owner.GetDataBlob<PropulsionDB>().MaximumSpeed * 1000;
            AUSpeed = Distance.ToAU(kmSpeed);

            if(PositionTarget != null)
            {
                // just head straight towards the target position
                targetPosition = PositionTarget;

                // Assume that 1000 is extremely close, 
                if(Distance.ToKm(distanceBetweenPositions(currentPosition, targetPosition)) <= 1000.0)
                {
                    setPositionToTarget(Owner, targetPosition);
                    return true;
                }
                else
                {
                    Owner.GetDataBlob<PropulsionDB>().CurrentSpeed = getSpeed(currentPosition, targetPosition, kmSpeed);
                    return false;
                }
            }
            else if(Target == null || Target == Entity.InvalidEntity) // Target no longer exists
                return true;
            else
            {
                targetPosition = Target.GetDataBlob<PositionDB>();
                
                // Assume that 1000 is extremely close, 
                if (Distance.ToKm(distanceBetweenPositions(currentPosition, targetPosition)) <= 1000.0)
                {
                    setPositionToTarget(Owner, targetPosition);

                    // Enter the orbit of the target
                    if (Target.HasDataBlob<OrbitDB>())
                        Owner.SetDataBlob<OrbitDB>(Target.GetDataBlob<OrbitDB>());

                    return true;
                }



                if (Target.HasDataBlob<OrbitDB>())
                {
                    if (Target.GetDataBlob<OrbitDB>().IsStationary)
                    {
                        // just head straight towards the target position
                        targetPosition = Target.GetDataBlob<PositionDB>();
                        Owner.GetDataBlob<PropulsionDB>().CurrentSpeed = getSpeed(currentPosition, targetPosition, kmSpeed);
                    }
                    else
                    {
                        // TODO: Figure out an intercept based on OrbitProcessor.GetPosition
                        // for now, just head straight towards the target position
                        
                        targetPosition = Target.GetDataBlob<PositionDB>();
                        Owner.GetDataBlob<PropulsionDB>().CurrentSpeed = getSpeed(currentPosition, targetPosition, kmSpeed);
                        return false;
                    }
                }

                else if (Target.HasDataBlob<PropulsionDB>())
                {
                    if (Target.GetDataBlob<PropulsionDB>().MaximumSpeed >= Owner.GetDataBlob<PropulsionDB>().MaximumSpeed)
                        // Target is faster than our ship, and cannot intercept
                    {
                        // Just head in a straight line
                        targetPosition = Target.GetDataBlob<PositionDB>();
                        Owner.GetDataBlob<PropulsionDB>().CurrentSpeed = getSpeed(currentPosition, targetPosition, kmSpeed);
                    }
                    else
                    {
                        // Calculate an intercept
                        targetPosition = Target.GetDataBlob<PositionDB>();
                        Vector4 targetPos = new Vector4(targetPosition.X, targetPosition.Y, targetPosition.Z, targetPosition.W);
                        Vector4 currentPos = new Vector4(currentPosition.X, currentPosition.Y, currentPosition.Z, currentPosition.W);
                        targetPos = Find_collision_point(targetPos, Target.GetDataBlob<PropulsionDB>().CurrentSpeed, currentPos, AUSpeed);
                        targetPosition = new PositionDB(targetPos, targetPosition.SystemGuid);
                        Owner.GetDataBlob<PropulsionDB>().CurrentSpeed = getSpeed(currentPosition, targetPosition, kmSpeed);
                    }

                }
            }

                       
            return false;
        }

        private Vector4 getSpeed(PositionDB currentPosition, PositionDB targetPosition, double speedMagnitude)
        {
            Vector4 speed = new Vector4( 0, 0, 0, 0 );
            double length;


            Vector4 speedMagInKM;

            Vector4 direction = new Vector4(0, 0, 0, 0);
            direction.X = targetPosition.X - currentPosition.X;
            direction.Y = targetPosition.Y - currentPosition.Y;
            direction.Z = targetPosition.Z - currentPosition.Z;
            direction.W = 0;



            length = direction.Length();

            direction.X = (direction.X / length);
            direction.Y = (direction.Y / length);
            direction.Z = (direction.Z / length);



            speedMagInKM.X = direction.X * speedMagnitude;
            speedMagInKM.Y = direction.Y * speedMagnitude;
            speedMagInKM.Z = direction.Z * speedMagnitude;

            speed.X = Distance.ToAU(speedMagInKM.X);
            speed.Y = Distance.ToAU(speedMagInKM.Y);
            speed.Z = Distance.ToAU(speedMagInKM.Z);

            // TODO: reduce speed if ship will overshoot target

            return speed;
        }

        private Vector4 Find_collision_point(Vector4 target_pos, Vector4 target_vel, Vector4 interceptor_pos, double interceptor_speed)
        {
            double k = Vector4.Magnitude(target_vel) / interceptor_speed;
            double distance_to_target = Vector4.Magnitude(interceptor_pos - target_pos);

            Vector4 b_hat = target_vel;
            Vector4 c_hat = interceptor_pos - target_pos;

            var CAB = Vector4.AngleBetween(b_hat, c_hat);
            var ABC = Math.Asin(Math.Sin(CAB) * k);
            var ACB = (Math.PI) - (CAB + ABC);

            var j = distance_to_target / Math.Sin(ACB);
            var a = j * Math.Sin(CAB);
            var b = j * Math.Sin(ABC);


            var time_to_collision = b / Vector4.Magnitude(target_vel);
            var collision_pos = target_pos + (target_vel * time_to_collision);

            return interceptor_pos - collision_pos;
        }

        private double distanceBetweenPositions(PositionDB origin, PositionDB target)
        {
            Vector4 delta = new Vector4();

            delta.X = origin.X - target.X;
            delta.Y = origin.Y - target.Y;
            delta.Z = origin.Z - target.Z;

            return delta.Length();
        }

        private void setPositionToTarget(Entity ship, PositionDB target)
        {
            Owner.GetDataBlob<PropulsionDB>().CurrentSpeed = new Vector4(0.0, 0.0, 0.0, 0.0);
            Owner.GetDataBlob<PositionDB>().X = target.X;
            Owner.GetDataBlob<PositionDB>().Y = target.Y;
            Owner.GetDataBlob<PositionDB>().Z = target.Z;

            return;
        }

        /*
        private Vector4 calculateIntercept(PositionDB currentPosition, Entity target, double speedMagnitude)
        {
            // Given: ux, uy, vmag (projectile speed), Ax, Ay, Bx, By

            Vector4 result = new Vector4();

            PositionDB targetPosition = target.GetDataBlob<PositionDB>();

            double ABx, ABy, Ax, Ay, Bx, By;
            double ABmag;

            double ux, uy, uDotAB, ujx, ujy;
            double uix, uiy;

            double vix, viy, vjx, vjy;
            double viMag, vjMag, vMag;

            double vx, vy;

            Ax = currentPosition.X;
            Ay = currentPosition.Y;
            Bx = targetPosition.X;
            By = targetPosition.Y;

            ux = target.GetDataBlob<PropulsionDB>().CurrentSpeed.X;
            uy = target.GetDataBlob<PropulsionDB>().CurrentSpeed.Y;

            vMag = speedMagnitude;
            
            // Find the vector AB
            ABx = Bx - Ax;
            ABy = By - Ay;


            
            // Normalize it
            ABmag = Math.Sqrt(ABx * ABx + ABy * ABy);
            ABx /= ABmag;
            ABy /= ABmag;

            // Project u onto AB
            uDotAB = ABx * ux + ABy * uy;
            ujx = uDotAB * ABx;
            ujy = uDotAB * ABy;
                             
            // Subtract uj from u to get ui
            uix = ux - ujx;
            uiy = uy - ujy;
           

            // Set vi to ui (for clarity)
            vix = uix;
            viy = uiy;

            
            // Calculate the magnitude of vj
            viMag = Math.Sqrt(vix * vix + viy * viy);
            vjMag = Math.Sqrt(vMag * vMag - viMag * viMag);


            // Get vj by multiplying it's magnitude with the unit vector AB
            vjx = ABx * vjMag;
            vjy = ABy * vjMag;

            // Add vj and vi to get v
            vx = vjx + vix;
            vy = vjy + viy;

            result.X = vx;
            result.Y = vy;
            result.Z = 0;
            result.Z = 0;

            return result;
        }

*/
    }
}
