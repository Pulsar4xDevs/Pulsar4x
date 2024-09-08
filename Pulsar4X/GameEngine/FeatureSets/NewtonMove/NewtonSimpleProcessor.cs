using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Industry;

namespace Pulsar4X.Engine;

public class NewtonSimpleProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency => TimeSpan.FromSeconds(1);
    public TimeSpan FirstRunOffset => TimeSpan.FromSeconds(0);
    public Type GetParameterType => typeof(NewtonSimpleMoveDB);
    public CargoDefinitionsLibrary _cargolib = new CargoDefinitionsLibrary();

    public void Init(Game game)
    {
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        NewtonMove(entity.GetDataBlob<NewtonSimpleMoveDB>(), deltaSeconds);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var nmdb = manager.GetAllDataBlobsOfType<NewtonSimpleMoveDB>();
        foreach (var db in nmdb)
        {
            NewtonMove(db, deltaSeconds);
        }
        return nmdb.Count;
    }


    public void NewtonMove(NewtonSimpleMoveDB newtonSimplelMoveDB, int deltaSeconds)
    {
        
        Entity entity = newtonSimplelMoveDB.OwningEntity;
        DateTime dateTimeNow = entity.Manager.StarSysDateTime;
        DateTime dateTimeNext = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds);
        
        var thrustdb = entity.GetDataBlob<NewtonionThrustAbilityDB>();
        var posdb = entity.GetDataBlob<PositionDB>();
        var massdb = entity.GetDataBlob<MassVolumeDB>();

        var currentOrbit = newtonSimplelMoveDB.CurrentTrajectory;
        var targetOrbit = newtonSimplelMoveDB.TargetTrajectory;

        var thrust = thrustdb.ThrustInNewtons;
        var fuelRate = thrustdb.FuelBurnRate;
        var fuelType = _cargolib.GetAny(thrustdb.FuelType);
        var currentState = OrbitalMath.GetStateVectors(currentOrbit, dateTimeNow);
        var targetState = OrbitalMath.GetStateVectors(targetOrbit, dateTimeNow);

        var moveVector = targetState.velocity - currentState.velocity;
        var moveDeltaV = moveVector.Length();

        //if ship has enough fuel to make the manuver:
        if (thrustdb.DeltaV > moveDeltaV)
        {
            //TODO: handle longer "burns" over several turns.
            
            //set entity to new orbit.
            OrbitDB newOrbit = OrbitDB.FromKeplerElements(entity, massdb.MassTotal, targetOrbit, dateTimeNow);
            entity.SetDataBlob(newOrbit);

            //remove fuel.
            var fuelBurned = thrustdb.BurnDeltaV(moveDeltaV, massdb.MassTotal);
            CargoTransferProcessor.AddRemoveCargoMass(entity, fuelType, fuelBurned);
            
            //tag as complete
            newtonSimplelMoveDB.IsComplete = true;
        }
    }

    public static (Vector3 pos, Vector3 vel) GetRelativeState(Entity entity, DateTime atDateTime)
    {
        NewtonSimpleMoveDB db = entity.GetDataBlob<NewtonSimpleMoveDB>();
        var state = OrbitMath.GetStateVectors(db.CurrentTrajectory, atDateTime);
        return (state.position, (Vector3)state.velocity);
    }
    public static (Vector3 pos, Vector3 vel) GetAbsoluteState(Entity entity, DateTime atDateTime)
    {
        NewtonSimpleMoveDB db = entity.GetDataBlob<NewtonSimpleMoveDB>();
        var posdb = entity.GetDataBlob<PositionDB>();

        var state = OrbitMath.GetStateVectors(db.CurrentTrajectory, atDateTime);
        var pos = state.position;
        var vel = (Vector3)state.velocity;
        
        if (posdb.Parent != null)
        {
            pos += posdb.Parent.GetAbsoluteFuturePosition(atDateTime);
            vel += posdb.Parent.GetAbsoluteFutureVelocity(atDateTime);
        }
        return (pos, vel);
    }
}