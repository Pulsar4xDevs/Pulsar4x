using System;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Factions;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;
using Pulsar4X.Galaxy;
using Pulsar4X.Engine;

namespace Pulsar4X.Movement;

public class NewtonSimpleProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency => TimeSpan.FromSeconds(1);
    public TimeSpan FirstRunOffset => TimeSpan.FromSeconds(0);
    public Type GetParameterType => typeof(NewtonSimpleMoveDB);

    public void Init(Game game)
    {
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        DateTime todateTime = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
        ProcessEntity(entity, todateTime);
    }

    public static void ProcessEntity(Entity entity, DateTime toDateTime)
    {
        if (!entity.TryGetDataBlob<NewtonSimpleMoveDB>(out var db))
            return;
        NewtonMove(db, toDateTime);
        RefreshMoveState(entity, db, toDateTime);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var nmdb = manager.GetAllDataBlobsOfType<NewtonSimpleMoveDB>();
        DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
        foreach (var db in nmdb)
        {
            NewtonMove(db, toDate);
            if (db.OwningEntity != null)
                RefreshMoveState(db.OwningEntity, db, toDate);
        }
        return nmdb.Count;
    }

    static void RefreshMoveState(Entity entity, NewtonSimpleMoveDB db, DateTime at)
    {
        if (entity.TryGetDataBlob<NewtonSimpleMoveDB>(out var still) && !still.IsComplete && !still.IsFailed)
            MoveStateProcessor.ProcessForType(still, at);
        else if (entity.TryGetDataBlob<OrbitDB>(out var orbit))
            MoveStateProcessor.ProcessForType(orbit, at);
    }


    public static void NewtonMove(NewtonSimpleMoveDB db, DateTime toDateTime)
    {
        if (db.IsComplete || db.IsFailed)
            return;

        Entity entity = db.OwningEntity;
        var thrustdb = entity.GetDataBlob<NewtonThrustAbilityDB>();
        var massdb = entity.GetDataBlob<MassVolumeDB>();
        CargoDefinitionsLibrary cargoLib = entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
        var fuelType = cargoLib.GetAny(thrustdb.FuelType);
        var storage = entity.GetDataBlob<CargoStorageDB>();
        double fuelOnBoard = storage.GetMassStored(fuelType, false);
        double mass = massdb.MassTotal;
        double ve = thrustdb.ExhaustVelocity;
        double sgp = GeneralMath.StandardGravitationalParameter(mass + db.ParentMass);

        if (db.FuelTotal < 0)
        {
            var startState = OrbitalMath.GetStateVectors(db.StartTrajectory, db.ActionOnDateTime);
            var targetState = OrbitalMath.GetStateVectors(db.TargetTrajectory, db.ActionOnDateTime);
            double dvTotal = ((Vector3)targetState.velocity - (Vector3)startState.velocity).Length();
            if (!double.IsFinite(dvTotal) || dvTotal < 0.01)
            {
                Complete(db, entity, mass, toDateTime);
                return;
            }

            db.FuelTotal = OrbitMath.TsiolkovskyFuelUse(mass, ve, dvTotal);
        }

        double fuelFromTank = thrustdb.DeltaV > 0
            ? OrbitMath.TsiolkovskyFuelUse(mass, ve, thrustdb.DeltaV)
            : 0;
        double fuelAvailable = Math.Min(fuelOnBoard, fuelFromTank);
        double fuelRemaining = db.FuelTotal - db.FuelBurned;
        if (fuelAvailable + 1e-6 < fuelRemaining)
        {
            Fail(db, entity, mass, toDateTime);
            return;
        }

        double dt = (toDateTime - db.LastProcessDateTime).TotalSeconds;
        if (dt < 0)
            dt = 0;
        double fuelThisTick = Math.Min(thrustdb.FuelBurnRate * dt, Math.Min(fuelAvailable, fuelRemaining));
        if (fuelThisTick <= 0)
        {
            db.LastProcessDateTime = toDateTime;
            return;
        }

        db.FuelBurned += fuelThisTick;
        CargoTransferProcessor.AddRemoveCargoMass(entity, fuelType, -fuelThisTick);
        db.LastProcessDateTime = toDateTime;

        double f = db.FuelBurned / db.FuelTotal;
        if (f >= 1 - 1e-6)
        {
            Complete(db, entity, mass, toDateTime);
            return;
        }

        db.CurrentTrajectory = InterpolateOrbit(
            db.StartTrajectory, db.TargetTrajectory, db.ActionOnDateTime, f, sgp);
    }

    static void Complete(NewtonSimpleMoveDB db, Entity entity, double mass, DateTime at)
    {
        entity.SetDataBlob(OrbitDB.FromKeplerElements(db.SOIParent, mass, db.TargetTrajectory, at));
        db.CurrentTrajectory = db.TargetTrajectory;
        db.IsComplete = true;
        db.LastProcessDateTime = at;
    }

    static void Fail(NewtonSimpleMoveDB db, Entity entity, double mass, DateTime at)
    {
        entity.SetDataBlob(OrbitDB.FromKeplerElements(db.SOIParent, mass, db.CurrentTrajectory, at));
        db.IsFailed = true;
        db.LastProcessDateTime = at;
    }

    /// <summary>
    /// Blend start→target at the burn node, not at "now". Evaluating both Kepler
    /// states at the current time then lerping r,v is a chord between two drifted
    /// orbits and goes hyperbolic even when both ends are elliptical.
    /// Epoch stays the node so GetStateVectors(ke, now) coasts the intermediate orbit.
    /// </summary>
    static KeplerElements InterpolateOrbit(
        KeplerElements start, KeplerElements target, DateTime nodeTime, double f, double sgp)
    {
        var a = OrbitalMath.GetStateVectors(start, nodeTime);
        var b = OrbitalMath.GetStateVectors(target, nodeTime);
        var r = a.position + f * (b.position - a.position);
        var v = (Vector3)a.velocity + f * ((Vector3)b.velocity - (Vector3)a.velocity);
        return OrbitMath.KeplerFromPositionAndVelocity(sgp, r, v, nodeTime);
    }

    public static (Vector3 pos, Vector3 vel) GetRelativeState(Entity entity, DateTime atDateTime)
    {
        if (!entity.TryGetDataBlob<NewtonSimpleMoveDB>(out var db))
        {
            if (entity.TryGetDataBlob<OrbitDB>(out var orbit))
            {
                var os = OrbitMath.GetStateVectors(orbit.GetElements(), atDateTime);
                return (os.position, (Vector3)os.velocity);
            }
            return (Vector3.Zero, Vector3.Zero);
        }
        var state = OrbitMath.GetStateVectors(db.CurrentTrajectory, atDateTime);
        return (state.position, (Vector3)state.velocity);
    }
    public static (Vector3 pos, Vector3 vel) GetAbsoluteState(Entity entity, DateTime atDateTime)
    {
        var (pos, vel) = GetRelativeState(entity, atDateTime);
        if (entity.TryGetDataBlob<PositionDB>(out var posdb) && posdb.Parent != null)
        {
            pos += MoveMath.GetAbsoluteFuturePosition(posdb.Parent, atDateTime);
            vel += MoveMath.GetAbsoluteFutureVelocity(posdb.Parent, atDateTime);
        }
        return (pos, vel);
    }
}