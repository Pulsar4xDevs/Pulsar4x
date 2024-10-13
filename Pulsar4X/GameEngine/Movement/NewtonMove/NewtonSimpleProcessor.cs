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
    public TimeSpan RunFrequency => TimeSpan.FromSeconds(30);
    public TimeSpan FirstRunOffset => TimeSpan.FromSeconds(0);
    public Type GetParameterType => typeof(NewtonSimpleMoveDB);

    public void Init(Game game)
    {
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        var nmdb = entity.GetDataBlob<NewtonSimpleMoveDB>();
        DateTime todateTime = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
        NewtonMove(nmdb, todateTime);
        MoveStateProcessor.ProcessForType(nmdb, todateTime);
    }

    public static void ProcessEntity(Entity entity, DateTime toDateTime)
    {
        var db = entity.GetDataBlob<NewtonSimpleMoveDB>();
        NewtonMove(db, toDateTime);
        MoveStateProcessor.ProcessForType(db, toDateTime);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var nmdb = manager.GetAllDataBlobsOfType<NewtonSimpleMoveDB>();
        DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
        foreach (var db in nmdb)
        {
            NewtonMove(db, toDate);
        }
        MoveStateProcessor.ProcessForType(nmdb, toDate);
        return nmdb.Count;
    }


    public static void NewtonMove(NewtonSimpleMoveDB newtonSimplelMoveDB, DateTime toDateTime)
    {
        Entity entity = newtonSimplelMoveDB.OwningEntity;
        var thrustdb = entity.GetDataBlob<NewtonThrustAbilityDB>();
        var posdb = entity.GetDataBlob<PositionDB>();
        var massdb = entity.GetDataBlob<MassVolumeDB>();

        
        //update deltav
        CargoDefinitionsLibrary cargoLib = entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
        var fuelTypeID = thrustdb.FuelType;
        var fuelType = cargoLib.GetAny(fuelTypeID);
        var storage = entity.GetDataBlob<VolumeStorageDB>();
        var fuelMass = storage.GetMassStored(fuelType);
        
        var currentOrbit = newtonSimplelMoveDB.CurrentTrajectory;
        var targetOrbit = newtonSimplelMoveDB.TargetTrajectory;

        var thrust = thrustdb.ThrustInNewtons;
        var fuelRate = thrustdb.FuelBurnRate;
        
        var currentState = OrbitalMath.GetStateVectors(currentOrbit, toDateTime);
        var targetState = OrbitalMath.GetStateVectors(targetOrbit, toDateTime);

        var moveVector = targetState.velocity - currentState.velocity;
        var moveDeltaV = moveVector.Length();
        
        //if ship has enough fuel to make the manuver:
        if (thrustdb.DeltaV > moveDeltaV)
        {
            //TODO: handle longer "burns" over several turns.
            
            //set entity to new orbit.
            OrbitDB newOrbit = OrbitDB.FromKeplerElements(entity, massdb.MassTotal, targetOrbit, toDateTime);
            entity.SetDataBlob(newOrbit);

            //remove fuel
            double fuelBurned = OrbitMath.TsiolkovskyFuelUse(massdb.MassTotal, thrustdb.ExhaustVelocity, moveDeltaV);
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
            pos += MoveStateProcessor.GetAbsoluteFuturePosition(posdb.Parent,atDateTime);
            vel += posdb.Parent.GetAbsoluteFutureVelocity(atDateTime);
        }
        return (pos, vel);
    }
}