using System;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>
/// Databolob/movetype agnostic position and movement math for entites
/// </summary>
public static class MoveMath
{

    /// <summary>
    /// Gets future velocity for this entity, datablob agnostic.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="atDateTime"></param>
    /// <returns>Velocity in m/s relative to SOI parent</returns>
    /// <exception cref="Exception"></exception>
    public static Vector3 GetRelativeFutureVelocity(Entity entity, DateTime atDateTime)
    {

        if (entity.HasDataBlob<OrbitDB>())
        {
            return entity.GetDataBlob<OrbitDB>().InstantaneousOrbitalVelocityVector_m(atDateTime);
        }
        if (entity.HasDataBlob<OrbitUpdateOftenDB>())
        {
            return entity.GetDataBlob<OrbitUpdateOftenDB>().InstantaneousOrbitalVelocityVector_m(atDateTime);
        }
        else if (entity.HasDataBlob<NewtonMoveDB>())
        {
            return NewtonionMovementProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).vel;
        }
        else if (entity.HasDataBlob<NewtonSimpleMoveDB>())
        {
            return NewtonSimpleProcessor.GetRelativeState(entity, atDateTime).vel;
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
    public static Vector3 GetAbsoluteFutureVelocity(Entity entity, DateTime atDateTime)
    {
        PositionDB posDB = entity.GetDataBlob<PositionDB>();


        if (entity.HasDataBlob<OrbitDB>())
        {
            return OrbitMath.InstantaneousOrbitalVelocityVector_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
        }
        if (entity.HasDataBlob<OrbitUpdateOftenDB>())
        {
            return OrbitMath.InstantaneousOrbitalVelocityVector_m(entity.GetDataBlob<OrbitUpdateOftenDB>(), atDateTime);
        }
        else if (entity.HasDataBlob<NewtonMoveDB>())
        {
            var vel = NewtonionMovementProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).vel;
            var parentEntity = posDB.Parent;
            if(parentEntity == null) throw new NullReferenceException("parentEntity cannot be null");
            //recurse
            return GetAbsoluteFutureVelocity(parentEntity, atDateTime) + vel;
        }
        else if (entity.HasDataBlob<NewtonSimpleMoveDB>())
        {
            return NewtonSimpleProcessor.GetAbsoluteState(entity, atDateTime).vel;
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

    public static Vector2 GetAbsoluteFuturePosition(Entity entity, DateTime atDateTime)
    {
        PositionDB position = entity.GetDataBlob<PositionDB>();
        Vector2 pos = new Vector2(0,0);
        switch (position.MoveType)
        {
            case PositionDB.MoveTypes.None:
            {
                pos = position.AbsolutePosition2;
                break;
            }
            case PositionDB.MoveTypes.Orbit:
            {
                if(entity.TryGetDataBlob<OrbitDB>(out var orbitDB))
                {
                    pos = (Vector2)OrbitMath.GetAbsolutePosition(orbitDB, atDateTime);
                }
                else if (entity.TryGetDataBlob<OrbitUpdateOftenDB>(out var orbitDB2))
                {
                    pos = (Vector2)OrbitMath.GetAbsolutePosition(orbitDB2, atDateTime);
                }
            }
                break;
            case PositionDB.MoveTypes.NewtonSimple:
            {
                pos = (Vector2)NewtonSimpleProcessor.GetAbsoluteState(entity, atDateTime).pos;
            }
                break;

            case PositionDB.MoveTypes.NewtonComplex:
            {
                var db = entity.GetDataBlob<NewtonMoveDB>();
                pos = (Vector2)NewtonionMovementProcessor.GetAbsoluteState(entity, db, atDateTime).pos;
            }
                break;
            case PositionDB.MoveTypes.Warp:
            {
                var db = entity.GetDataBlob<WarpMovingDB>();
                if (atDateTime < db.PredictedExitTime)
                {
                    var t = (atDateTime - db.LastProcessDateTime).TotalSeconds;
                    pos = db._position + (Vector2)(db.CurrentNonNewtonionVectorMS * t);
                }
                else
                {
                    var endOrbit = db.EndpointTargetOrbit;
                    var rpos = (Vector2)OrbitalMath.GetPosition(endOrbit, atDateTime);
                    pos = GetAbsoluteFuturePosition(db.TargetEntity, atDateTime);
                    if (rpos.X is not double.NaN)
                        pos += rpos;
                }

            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return pos;
    }

    public static Vector2 GetRelativeFuturePosition(Entity entity, DateTime atDateTime)
    {
        PositionDB position = entity.GetDataBlob<PositionDB>();
        Vector2 pos = new Vector2(0,0);
        switch (position.MoveType)
        {
            case PositionDB.MoveTypes.None:
            {
                pos = position.RelativePosition2;
                break;
            }
            case PositionDB.MoveTypes.Orbit:
            {
                if(entity.TryGetDataBlob<OrbitDB>(out var orbitDB))
                {
                    pos = (Vector2)OrbitMath.GetPosition(orbitDB, OrbitMath.GetTrueAnomaly(orbitDB, atDateTime));
                }
                else if (entity.TryGetDataBlob<OrbitUpdateOftenDB>(out var orbitDB2))
                {
                    pos = (Vector2)OrbitMath.GetPosition(orbitDB2, OrbitMath.GetTrueAnomaly(orbitDB2, atDateTime));
                }
            }
                break;
            case PositionDB.MoveTypes.NewtonSimple:
            {
                pos = (Vector2)NewtonSimpleProcessor.GetRelativeState(entity, atDateTime).pos;
            }
                break;

            case PositionDB.MoveTypes.NewtonComplex:
            {
                var db = entity.GetDataBlob<NewtonMoveDB>();
                pos = (Vector2)NewtonionMovementProcessor.GetRelativeState(entity, db, atDateTime).pos;
            }
                break;
            case PositionDB.MoveTypes.Warp:
            {
                var db = entity.GetDataBlob<WarpMovingDB>();
                if (atDateTime < db.PredictedExitTime)
                {
                    var t = (atDateTime - db.LastProcessDateTime).TotalSeconds;
                    pos = db._position + (Vector2)(db.CurrentNonNewtonionVectorMS * t);
                }
                else
                {
                    var endOrbit = db.EndpointTargetOrbit;
                    pos = (Vector2)OrbitMath.GetPosition(endOrbit, atDateTime);
                }
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return pos;
    }

    public static (Vector3 pos, Vector3 Velocity) GetRelativeFutureState(Entity entity, DateTime atDateTime)
    {
        var fvel = MoveMath.GetRelativeFutureVelocity(entity, atDateTime);
        var fpos = (Vector3)MoveMath.GetRelativeFuturePosition(entity, atDateTime);

        return (fpos, fvel);
    }


    public static (Vector3 pos, Vector3 Velocity) GetRelativeState(Entity entity)
    {
        var pos = entity.GetDataBlob<PositionDB>().RelativePosition;
        var datetime = entity.StarSysDateTime;
        if (entity.HasDataBlob<OrbitDB>())
        {
            datetime = entity.StarSysDateTime;
            var orbit = entity.GetDataBlob<OrbitDB>();

            var vel = orbit.InstantaneousOrbitalVelocityVector_m(datetime);
            return (pos, vel);
        }
        if (entity.HasDataBlob<OrbitUpdateOftenDB>())
        {
            datetime = entity.StarSysDateTime;
            var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
            var vel = orbit.InstantaneousOrbitalVelocityVector_m(datetime);
            return (pos, vel);
        }

        if (entity.HasDataBlob<NewtonMoveDB>())
        {
            var move = entity.GetDataBlob<NewtonMoveDB>();

            var vel = move.CurrentVector_ms;
            return (pos, vel);
        }

        if (entity.HasDataBlob<NewtonSimpleMoveDB>())
        {
            NewtonSimpleProcessor.GetRelativeState(entity, datetime);
        }

        if (entity.HasDataBlob<ColonyInfoDB>())
        {
            var daylen = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyInfoDB>().LengthOfDay.TotalSeconds;
            var radius = pos.Length();
            var d = 2 * Math.PI * radius;
            double speed = 0;
            if(daylen !=0)
               speed = d / daylen;

            Vector3 vel = new Vector3(0, speed, 0);

            var posAngle = Math.Atan2(pos.Y, pos.X);
            var mtx = Matrix3d.IDRotateZ(posAngle + (Math.PI * 0.5));

            Vector3 transformedVector = mtx.Transform(vel);
            return (pos, transformedVector);
        }
        if(entity.HasDataBlob<WarpMovingDB>())
        {
            var warpdb = entity.GetDataBlob<WarpMovingDB>();
            return (pos, warpdb.CurrentNonNewtonionVectorMS);
        }
        else
        {
            return(pos, Vector3.Zero);
        }
    }

    public static (Vector3 pos, Vector3 velocity) GetAbsoluteState(Entity entity, DateTime atDateTime)
    {
        var pos = (Vector3)GetAbsoluteFuturePosition(entity, atDateTime);
        var vel = GetAbsoluteFutureVelocity(entity, atDateTime);
        return (pos, vel);
    }

    public static (Vector3 pos, Vector3 velocity) GetAbsoluteState(Entity entity)
    {
        var posdb = entity.GetDataBlob<PositionDB>();
        var pos = posdb.AbsolutePosition;
        if (entity.HasDataBlob<OrbitDB>())
        {
            var atDatetime = entity.StarSysDateTime;
            var orbit = entity.GetDataBlob<OrbitDB>();
            var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbit, atDatetime);
            if (posdb.Parent != null)
            {
                vel += GetAbsoluteFutureVelocity(posdb.Parent, atDatetime);
            }

            return (pos, vel);
        }
        if (entity.HasDataBlob<OrbitUpdateOftenDB>())
        {
            var atDatetime = entity.StarSysDateTime;
            var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
            var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbit, atDatetime);
            if (posdb.Parent != null)
            {
                vel += GetAbsoluteFutureVelocity(posdb.Parent, atDatetime);
            }
            return (pos, vel);
        }

        if (entity.HasDataBlob<NewtonMoveDB>())
        {
            var move = entity.GetDataBlob<NewtonMoveDB>();
            var vel = move.CurrentVector_ms;
            return (pos, vel);
        }

        if (entity.HasDataBlob<NewtonSimpleMoveDB>())
        {
            return  NewtonSimpleProcessor.GetAbsoluteState(entity, entity.StarSysDateTime);
        }

        if(entity.HasDataBlob<WarpMovingDB>())
        {
            var vel = entity.GetDataBlob<WarpMovingDB>().CurrentNonNewtonionVectorMS;
            return(pos,vel);
        }
        else
        {
            return(pos, Vector3.Zero);
        }
    }

    /// <summary>
    /// This is mostly syntatic sugar.
    /// For more efficent, get and store a reference to PositionDB.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static Vector3 GetAbsolutePosition(Entity entity)
    {
        return entity.GetDataBlob<PositionDB>().AbsolutePosition;
    }

    public static PositionDB? GetSOIParentPositionDB(Entity entity)
    {
        return (PositionDB?)entity.GetDataBlob<PositionDB>().ParentDB;
    }

    public static double GetDistanceBetween(Entity a, Entity b)
    {
        var dba = a.GetDataBlob<PositionDB>();
        var dbb = b.GetDataBlob<PositionDB>();
        return (dba.AbsolutePosition - dbb.AbsolutePosition).Length();
    }
}