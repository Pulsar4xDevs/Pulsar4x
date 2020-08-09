using Pulsar4X.Orbital;
using System;

namespace Pulsar4X.ECSLib
{
    public static class EntityExtensions
    {
        public static string GetDefaultName(this Entity entity)
        {
            if (entity.HasDataBlob<NameDB>())
                return entity.GetDataBlob<NameDB>().DefaultName;
            return "Unknown";
        }

        public static PositionDB GetSOIParentPositionDB(this Entity entity)
        {
            return (PositionDB)entity.GetDataBlob<PositionDB>().ParentDB;
        }


        public static Entity GetSOIParentEntity(this Entity entity, PositionDB positionDB = null)
        {
            if (positionDB == null)
                positionDB = entity.GetDataBlob<PositionDB>();
            return positionDB.Parent;
        }

        public static (Vector3 pos, Vector3 Velocity) GetRelativeState(this Entity entity)
        {
            var pos = entity.GetDataBlob<PositionDB>().RelativePosition_m;
            if (entity.HasDataBlob<OrbitDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitDB>();

                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_m(orbit, datetime);
                return (pos, vel);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_m(orbit, datetime);
                return (pos, vel);
            }

            if (entity.HasDataBlob<NewtonMoveDB>())
            {
                var move = entity.GetDataBlob<NewtonMoveDB>();

                var vel = move.CurrentVector_ms;
                return (pos, vel);
            }

            if (entity.HasDataBlob<ColonyInfoDB>())
            {
                var daylen = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyInfoDB>().LengthOfDay.TotalSeconds;
                var radius = pos.Length();
                var d = 2 * Math.PI * radius;
                var speed = d / daylen;

                Vector3 vel = new Vector3(0, speed, 0);

                var posAngle = Math.Atan2(pos.Y, pos.X);
                var mtx = Matrix3d.IDRotateZ(posAngle + (Math.PI * 0.5));

                Vector3 transformedVector = mtx.Transform(vel);
                return (pos, transformedVector);
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }

        public static (Vector3 pos, Vector3 Velocity) GetAbsoluteState(this Entity entity)
        {
            var posdb = entity.GetDataBlob<PositionDB>();
            var pos = posdb.AbsolutePosition_m;
            if (entity.HasDataBlob<OrbitDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitDB>();
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_m(orbit, datetime);
                if (posdb.Parent != null)
                {
                    vel += posdb.Parent.GetAbsoluteState().Velocity;
                }

                return (pos, vel);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_m(orbit, datetime);
                if (posdb.Parent != null)
                {
                    vel += posdb.Parent.GetAbsoluteState().Velocity;
                }
                return (pos, vel);
            }

            if (entity.HasDataBlob<NewtonMoveDB>())
            {
                var move = entity.GetDataBlob<NewtonMoveDB>();
                var vel = move.CurrentVector_ms;
                return (pos, vel);
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }

        /// <summary>
        /// Gets future velocity for this entity, datablob agnostic.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <returns>Velocity in m/s relative to SOI parent</returns>
        /// <exception cref="Exception"></exception>
        public static Vector3 GetRelativeFutureVelocity(this Entity entity, DateTime atDateTime)
        {

            if (entity.HasDataBlob<OrbitDB>())
            {
                return OrbitProcessor.InstantaneousOrbitalVelocityVector_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                return OrbitProcessor.InstantaneousOrbitalVelocityVector_m(entity.GetDataBlob<OrbitUpdateOftenDB>(), atDateTime);
            }
            else if (entity.HasDataBlob<NewtonMoveDB>())
            {
                return NewtonionMovementProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).vel;
            }
            else if (entity.HasDataBlob<WarpMovingDB>())
            {
                return entity.GetDataBlob<WarpMovingDB>().SavedNewtonionVector;
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }

        /// <summary>
        /// Gets future velocity for this entity, datablob agnostic.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Vector3 GetAbsoluteFutureVelocity(this Entity entity, DateTime atDateTime)
        {
            if (entity.HasDataBlob<OrbitDB>())
            {
                return OrbitProcessor.AbsoluteOrbitalVector_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                return OrbitProcessor.AbsoluteOrbitalVector_m(entity.GetDataBlob<OrbitUpdateOftenDB>(), atDateTime);
            }
            else if (entity.HasDataBlob<NewtonMoveDB>())
            {
                var vel = NewtonionMovementProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).vel;
                //recurse
                return GetAbsoluteFutureVelocity(GetSOIParentEntity(entity), atDateTime) + vel;
            }
            else if (entity.HasDataBlob<WarpMovingDB>())
            {
                return entity.GetDataBlob<WarpMovingDB>().SavedNewtonionVector;
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }



        /// <summary>
        /// Gets a future position for this entity, regarless of wheter it's orbit or newtonion trajectory
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <returns>In Meters</returns>
        /// <exception cref="Exception"> if entity doesn't have one of the correct datablobs</exception>
        public static Vector3 GetRelativeFuturePosition(this Entity entity, DateTime atDateTime)
        {
            if (entity.HasDataBlob<OrbitDB>())
            {
                return OrbitProcessor.GetPosition_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
            }
            else if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                return OrbitProcessor.GetPosition_m(entity.GetDataBlob<OrbitUpdateOftenDB>(), atDateTime);
            }
            else if (entity.HasDataBlob<NewtonMoveDB>())
            {
                return NewtonionMovementProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).pos;
            }
            else if (entity.HasDataBlob<PositionDB>())
            {
                return entity.GetDataBlob<PositionDB>().RelativePosition_m;
            }
            else
            {
                throw new Exception("Entity is positionless");
            }
        }

        /// <summary>
        /// Gets a future position for this entity, regarless of wheter it's orbit or newtonion trajectory
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <returns>In Meters</returns>
        /// <exception cref="Exception"> if entity doesn't have one of the correct datablobs</exception>
        public static Vector3 GetAbsoluteFuturePosition(this Entity entity, DateTime atDateTime)
        {
            if (entity.HasDataBlob<OrbitDB>())
            {
                return OrbitProcessor.GetAbsolutePosition_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
            }
            else if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                return OrbitProcessor.GetAbsolutePosition_m(entity.GetDataBlob<OrbitUpdateOftenDB>(), atDateTime);
            }
            else if (entity.HasDataBlob<NewtonMoveDB>())
            {
                return NewtonionMovementProcessor.GetAbsoluteState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).pos;
            }
            else if (entity.HasDataBlob<PositionDB>())
            {
                return entity.GetDataBlob<PositionDB>().AbsolutePosition_m;
            }
            else
            {
                throw new Exception("Entity is positionless");
            }
        }
    }
}
