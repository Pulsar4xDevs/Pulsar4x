using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;
using Pulsar4X.Orbital;

namespace GameEngine.Movement;


public class MoveStateDB : BaseDataBlob
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
    
    public Vector2 Position { get; internal set; }
    
    public Vector2 Velocity { get; internal set; }

    public Entity SOIParent { get; internal set; }
    public double SGP { get; internal set; }
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
            stateDB = new MoveStateDB();
            orbitDB.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.Orbit;
        stateDB.SOIParent = orbitDB.Parent;
        stateDB.SGP = orbitDB.GravitationalParameter_m3S2;
        stateDB.GetKeplerElements = orbitDB.GetElements();
        stateDB.Position = (Vector2)orbitDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition;
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
            stateDB = new MoveStateDB();
            orbitDB.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.Orbit;
        stateDB.SOIParent = orbitDB.Parent;
        stateDB.SGP = orbitDB.GravitationalParameter_m3S2;
        stateDB.GetKeplerElements = orbitDB.GetElements();
        stateDB.Position = (Vector2)orbitDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition;
        stateDB.Velocity = (Vector2)orbitDB.InstantaneousOrbitalVelocityVector_m(atDateTime);
    }

    public static void ProcessForType(List<NewtonSimpleMoveDB> moves, DateTime atDateTime)
    {
        foreach (var movedb in moves)
        {
            if(!movedb.OwningEntity.TryGetDatablob(out MoveStateDB stateDB))
            {
                stateDB = new MoveStateDB();
                movedb.OwningEntity.SetDataBlob(stateDB);
            }
            
            stateDB.MoveType = MoveStateDB.MoveTypes.NewtonSimple;
            stateDB.SOIParent = movedb.SOIParent;
            var myMass = movedb.OwningEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            var pMass = movedb.SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
            stateDB.SGP = GeneralMath.StandardGravitationalParameter(myMass + pMass);
            var state = OrbitMath.GetStateVectors(movedb.CurrentTrajectory, atDateTime);
            stateDB.Position = (Vector2)state.position;
            stateDB.Velocity = (Vector2)state.velocity;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(stateDB.SGP, state.position, (Vector3)state.velocity, atDateTime);
            stateDB.GetKeplerElements = ke;
        }
    }
    public static void ProcessForType(NewtonSimpleMoveDB movedb, DateTime atDateTime)
    {
        if(!movedb.OwningEntity.TryGetDatablob(out MoveStateDB stateDB))
        {
            stateDB = new MoveStateDB();
            movedb.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.NewtonSimple;
        stateDB.SOIParent = movedb.SOIParent;
        var myMass = movedb.OwningEntity.GetDataBlob<MassVolumeDB>().MassTotal;
        var pMass = movedb.SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
        stateDB.SGP = GeneralMath.StandardGravitationalParameter(myMass + pMass);
        var state = OrbitMath.GetStateVectors(movedb.CurrentTrajectory, atDateTime);
        stateDB.Position = (Vector2)state.position;
        stateDB.Velocity = (Vector2)state.velocity;
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
            stateDB = new MoveStateDB();
            movedb.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.NewtonSimple;
        stateDB.SOIParent = movedb.SOIParent;
        stateDB.GetKeplerElements = movedb.GetElements();
        stateDB.SGP = stateDB.GetKeplerElements.StandardGravParameter;
        stateDB.Position = (Vector2)movedb.OwningEntity.GetDataBlob<PositionDB>().RelativePosition;
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
            stateDB = new MoveStateDB();
            warpdb.OwningEntity.SetDataBlob(stateDB);
        }
        
        stateDB.MoveType = MoveStateDB.MoveTypes.Warp;
        var pos = warpdb.OwningEntity.GetDataBlob<PositionDB>();
        stateDB.SOIParent = pos.Parent;
        stateDB.GetKeplerElements = warpdb.TargetEndpointOrbit;
        stateDB.SGP = stateDB.GetKeplerElements.StandardGravParameter;
        stateDB.Position = (Vector2)pos.RelativePosition;
        stateDB.Velocity = (Vector2)warpdb.CurrentNonNewtonionVectorMS;
        
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
}
    
    
    