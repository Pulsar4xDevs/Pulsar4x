using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;
using Pulsar4X.Orbital;

namespace Pulsar4X.Datablobs;


public class MoveStateDB : TreeHierarchyDB, IPosition
{
    public enum MoveTypes
    {
        Orbit,
        NewtonSimple,
        NewtonComplex,
        Warp,
    }
    
    public MoveTypes MoveType { get; internal set; }
    
    public KeplerElements GetKeplerElements { get; internal set; }
    
    public Vector3 RelativePosition { get; internal set; }

    public Vector2 RelativePosition2
    {
        get { return (Vector2)RelativePosition; }
        set { RelativePosition = (Vector3)value; }
    }
    public Vector3 AbsolutePosition
    {
        get
        {
            if ( Parent == null || !Parent.IsValid ) //migth be better than crashing if parent is suddenly not valid. should be handled before this though.
                return RelativePosition;
            else if (Parent == OwningEntity)
                throw new Exception("Infinite loop triggered");
            else
            {
                MoveStateDB? parentpos = (MoveStateDB?)ParentDB;
                if(parentpos == this)
                    throw new Exception("Infinite loop triggered");
                return parentpos.AbsolutePosition + RelativePosition;
            }
        }
        internal set
        {
            if (Parent == null)
                RelativePosition = value;
            else
            {
                MoveStateDB? parentpos = (MoveStateDB?)ParentDB;
                RelativePosition = value - parentpos.AbsolutePosition;
            }
        } 
    }

    public Vector2 AbsolutePosition2     {
        get { return (Vector2)AbsolutePosition; }
        set { AbsolutePosition = (Vector3)value; }
    }
    
    public Vector2 Velocity { get; internal set; }
    
    public double SGP { get; internal set; }

    public MoveStateDB(Entity? parent) : base(parent)
    {
    }
}

public class MoveStateProcessor : IInstanceProcessor
{
    public void Init(Game game)
    {

    }
    
    public static void ProcessForType(List<OrbitDB> orbits, DateTime atDateTime)
    {
        foreach (var orbitDB in orbits)
        {
            if(orbitDB.OwningEntity is not null)
                ProcessForType(orbitDB, atDateTime);
        }
    }
    
    public static void ProcessForType(OrbitDB orbitDB, DateTime atDateTime)
    {
        if(!orbitDB.OwningEntity.TryGetDatablob(out MoveStateDB stateDB))
        {
            stateDB = new MoveStateDB(orbitDB.Parent);
            orbitDB.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.Orbit;
        stateDB.SetParent(orbitDB.Parent);
        stateDB.SGP = orbitDB.GravitationalParameter_m3S2;
        stateDB.GetKeplerElements = orbitDB.GetElements();
        stateDB.RelativePosition2 = orbitDB._position; //(Vector2)orbitDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition;
        orbitDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition = (Vector3)orbitDB._position;
        stateDB.Velocity = (Vector2)orbitDB.InstantaneousOrbitalVelocityVector_m(atDateTime);
    }
    
    public static void ProcessForType(List<OrbitUpdateOftenDB> orbits, DateTime atDateTime)
    {
        foreach (var orbitDB in orbits)
        {
            if(orbitDB.OwningEntity is not null)
                ProcessForType(orbitDB, atDateTime);
        }
    }
    
    public static void ProcessForType(OrbitUpdateOftenDB orbitDB, DateTime atDateTime)
    {
        if(!orbitDB.OwningEntity.TryGetDatablob(out MoveStateDB stateDB))
        {
            stateDB = new MoveStateDB(orbitDB.Parent);
            orbitDB.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.Orbit;
        stateDB.SetParent(orbitDB.Parent);
        stateDB.SGP = orbitDB.GravitationalParameter_m3S2;
        stateDB.GetKeplerElements = orbitDB.GetElements();
        stateDB.RelativePosition2 = orbitDB._position;
        orbitDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition = (Vector3)orbitDB._position;
        stateDB.Velocity = (Vector2)orbitDB.InstantaneousOrbitalVelocityVector_m(atDateTime);
    }

    public static void ProcessForType(List<NewtonSimpleMoveDB> moves, DateTime atDateTime)
    {
        foreach (var movedb in moves)
        {
            if(!movedb.OwningEntity.TryGetDatablob(out MoveStateDB stateDB))
            {
                stateDB = new MoveStateDB(movedb.SOIParent);
                movedb.OwningEntity.SetDataBlob(stateDB);
            }
            
            stateDB.MoveType = MoveStateDB.MoveTypes.NewtonSimple;
            stateDB.SetParent(movedb.SOIParent);
            var myMass = movedb.OwningEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            var pMass = movedb.SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
            stateDB.SGP = GeneralMath.StandardGravitationalParameter(myMass + pMass);
            var state = OrbitMath.GetStateVectors(movedb.CurrentTrajectory, atDateTime);
            stateDB.RelativePosition = state.position;
            stateDB.Velocity = state.velocity;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(stateDB.SGP, state.position, (Vector3)state.velocity, atDateTime);
            stateDB.GetKeplerElements = ke;
        }
    }
    public static void ProcessForType(NewtonSimpleMoveDB movedb, DateTime atDateTime)
    {
        if(!movedb.OwningEntity.TryGetDatablob(out MoveStateDB stateDB))
        {
            stateDB = new MoveStateDB(movedb.SOIParent);
            movedb.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.NewtonSimple;
        stateDB.SetParent(movedb.SOIParent);
        var myMass = movedb.OwningEntity.GetDataBlob<MassVolumeDB>().MassTotal;
        var pMass = movedb.SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
        stateDB.SGP = GeneralMath.StandardGravitationalParameter(myMass + pMass);
        var state = OrbitMath.GetStateVectors(movedb.CurrentTrajectory, atDateTime);
        stateDB.RelativePosition = state.position;
        stateDB.Velocity = state.velocity;
        var ke = OrbitMath.KeplerFromPositionAndVelocity(stateDB.SGP, state.position, (Vector3)state.velocity, atDateTime);
        stateDB.GetKeplerElements = ke;
    }
    
    public static void ProcessForType(List<NewtonMoveDB> moves, DateTime atDateTime)
    {
        foreach (var movedb in moves)
        {
            if(movedb.OwningEntity is not null)
                ProcessForType(movedb, atDateTime);
        }
    }
    
    public static void ProcessForType(NewtonMoveDB movedb, DateTime atDateTime)
    {
        if(!movedb.OwningEntity.TryGetDatablob(out MoveStateDB stateDB))
        {
            stateDB = new MoveStateDB(movedb.SOIParent);
            movedb.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.NewtonSimple;
        stateDB.SetParent(movedb.SOIParent);
        stateDB.GetKeplerElements = movedb.GetElements();
        stateDB.SGP = stateDB.GetKeplerElements.StandardGravParameter;
        //newtonmove processor still updates positon in the processor.
        //stateDB.RelativePosition = (Vector2)movedb.OwningEntity.GetDataBlob<PositionDB>().RelativePosition;
        stateDB.Velocity = (Vector2)movedb.CurrentVector_ms;
    }

    public static void ProcessForType(List<WarpMovingDB> warps, DateTime atDateTime)
    {
        foreach (var warpdb in warps)
        {
            
            if(warpdb.OwningEntity is not null)
                ProcessForType(warpdb, atDateTime);
        }
    }
    
    public static void ProcessForType(WarpMovingDB warpdb, DateTime atDateTime)
    {
        
        if(!warpdb.OwningEntity.TryGetDatablob(out MoveStateDB stateDB))
        {
            stateDB = new MoveStateDB(warpdb._parentEnitity);
            warpdb.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.Warp;
        
        stateDB.SetParent(warpdb._parentEnitity);
        stateDB.GetKeplerElements = warpdb.TargetEndpointOrbit;
        stateDB.SGP = stateDB.GetKeplerElements.StandardGravParameter;
        stateDB.RelativePosition2 = warpdb._position;
        stateDB.Velocity = (Vector2)warpdb.CurrentNonNewtonionVectorMS;
        stateDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition = (Vector3)warpdb._position;
    }

    public Type GetParameterType => typeof(MoveStateDB);
    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {
        
        if(entity.TryGetDatablob(out OrbitDB odb))
            ProcessForType(odb, atDateTime);
        else if(entity.TryGetDatablob(out OrbitUpdateOftenDB oudb))
            ProcessForType(oudb, atDateTime);
        else if(entity.TryGetDatablob(out NewtonMoveDB mdb))
            ProcessForType(mdb, atDateTime);
        else if(entity.TryGetDatablob(out NewtonSimpleMoveDB nmdb))
            ProcessForType(nmdb, atDateTime);
        else if(entity.TryGetDatablob(out NewtonSimpleMoveDB warpdb))
            ProcessForType(warpdb, atDateTime);
    }

    /// <summary>
    /// This allows easy single entity move processing regardless of move type.
    /// this should only be used in rare cases where you need to update a position ouside of the normal move tick.
    /// this WILL update the position, to get the position without updating, use GetFuturePosition() instead.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="toDateTime"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal static void ProcessEntityMove(Entity entity, DateTime toDateTime)
    {
        var movestate = entity.GetDataBlob<MoveStateDB>();
        switch (movestate.MoveType)
        {
            case MoveStateDB.MoveTypes.Orbit:
            {
                OrbitProcessor.ProcessEntity(entity, toDateTime);
                break;
            }
            case MoveStateDB.MoveTypes.NewtonSimple:
            {
                NewtonSimpleProcessor.ProcessEntity(entity, toDateTime);
                break;
            }
            case MoveStateDB.MoveTypes.NewtonComplex:
            {
                NewtonionMovementProcessor.ProcessEntity(entity, toDateTime);
                break;
            }
            case MoveStateDB.MoveTypes.Warp:
            {
                WarpMoveProcessor.ProcessEntity(entity, toDateTime);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static Vector2 GetFuturePosition(Entity entity, DateTime atDateTime)
    {
        MoveStateDB moveState = entity.GetDataBlob<MoveStateDB>();
        Vector2 pos = new Vector2();
        switch (moveState.MoveType)
        {
            case MoveStateDB.MoveTypes.Orbit:
            {
                var orbitdb = entity.GetDataBlob<OrbitDB>();
                pos = (Vector2)OrbitMath.GetPosition(orbitdb, OrbitMath.GetTrueAnomaly(orbitdb, atDateTime));
            }
                break;
            case MoveStateDB.MoveTypes.NewtonSimple:
            {
                pos = (Vector2)NewtonSimpleProcessor.GetRelativeState(entity, atDateTime).pos;
            }
                break;

            case MoveStateDB.MoveTypes.NewtonComplex:
            {
                var db = entity.GetDataBlob<NewtonMoveDB>();
                pos = (Vector2)NewtonionMovementProcessor.GetRelativeState(entity, db, atDateTime).pos;
            }
                break;
            case MoveStateDB.MoveTypes.Warp:
            {
                var db = entity.GetDataBlob<WarpMovingDB>();
                if (atDateTime < db.PredictedExitTime)
                {
                    var t = (atDateTime - db.LastProcessDateTime).TotalSeconds;
                    pos = db._position + (Vector2)(db.CurrentNonNewtonionVectorMS * t);
                }
                else
                {
                    var endOrbit = db.TargetEndpointOrbit;
                    pos = (Vector2)OrbitMath.GetPosition(endOrbit, atDateTime);
                }
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return pos;
    }
}
    
    
    