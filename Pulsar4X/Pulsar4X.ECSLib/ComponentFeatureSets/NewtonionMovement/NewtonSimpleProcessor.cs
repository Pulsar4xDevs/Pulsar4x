using System;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib;

public class NewtonSimpleProcessor : IHotloopProcessor
{
    private static readonly int _obtDBIdx = EntityManager.GetTypeIndex<OrbitDB>();
    private static readonly int _nmDBIdx = EntityManager.GetTypeIndex<NewtonSimpleMoveDB>();
    private static readonly int _nthDBIdx = EntityManager.GetTypeIndex<NewtonThrustAbilityDB>();
    private static readonly int _posDBIdx = EntityManager.GetTypeIndex<PositionDB>();
    private static readonly int _massDBIdx = EntityManager.GetTypeIndex<MassVolumeDB>();
    public NewtonSimpleProcessor()
    {
    }

    public TimeSpan RunFrequency => TimeSpan.FromSeconds(1);

    public TimeSpan FirstRunOffset => TimeSpan.FromSeconds(0);

    public Type GetParameterType => typeof(NewtonSimpleMoveDB);

    public void Init(Game game)
    {

    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        NewtonSimpleMove(entity.GetDataBlob<NewtonSimpleMoveDB>(), deltaSeconds);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        //List<Entity> entites = manager.GetAllEntitiesWithDataBlob<NewtonMoveDB>(_nmDBIdx);
        var nmdb = manager.GetAllDataBlobsOfType<NewtonSimpleMoveDB>(_nmDBIdx);
        foreach (var db in nmdb)
        {
            NewtonSimpleMove(db, deltaSeconds);
        }

        return nmdb.Count;
    }
    
    public static void NewtonSimpleMove(NewtonSimpleMoveDB newtonMoveDB, int deltaSeconds)
    {
        var entity = newtonMoveDB.OwningEntity;
        //NewtonMoveDB newtonMoveDB = entity.GetDataBlob<NewtonMoveDB>();
        NewtonThrustAbilityDB newtonThrust = entity.GetDataBlob<NewtonThrustAbilityDB>(_nthDBIdx);
        PositionDB positionDB = entity.GetDataBlob<PositionDB>(_posDBIdx);
        double massTotal_Kg = entity.GetDataBlob<MassVolumeDB>(_massDBIdx).MassTotal;
        double parentMass_kg = newtonMoveDB.ParentMass;

        var manager = entity.Manager;
        DateTime dateTimeFrom = newtonMoveDB.LastProcessDateTime;
        DateTime dateTimeNow = manager.ManagerSubpulses.StarSysDateTime;
        DateTime dateTimeFuture = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds);
        double deltaT = (dateTimeFuture - dateTimeFrom).TotalSeconds;

        double sgp = GeneralMath.StandardGravitationalParameter(massTotal_Kg + parentMass_kg);

        var curTraj = newtonMoveDB.CurrentTrajectory;
        var curVec = OrbitalMath.GetStateVectors(curTraj, dateTimeNow);

        var tgtTraj = newtonMoveDB.TargetTrajectory;
        var tgtVec = OrbitalMath.GetStateVectors(tgtTraj, dateTimeNow);

        var manuverDeltaV = tgtVec.velocity.Length() - curVec.velocity.Length();
        var maxShipDv = newtonThrust.DeltaV;

        var fuelUseForManuver = OrbitalMath.TsiolkovskyFuelUse(massTotal_Kg, newtonThrust.ExhaustVelocity, manuverDeltaV);
        var maxDVToUse = Math.Min(manuverDeltaV, maxShipDv);

        if (manuverDeltaV < maxShipDv)
        {
            double fuelUsed = newtonThrust.BurnDeltaV(manuverDeltaV, massTotal_Kg);
            var ft = newtonThrust.FuelType;
            ProcessedMaterialSD fuel = StaticRefLib.StaticData.CargoGoods.GetMaterials()[ft];
            var massRemoved = CargoTransferProcessor.AddRemoveCargoMass(entity, fuel, -fuelUsed);

            var newmass = massTotal_Kg - massRemoved;
            var soi = newtonMoveDB.OwningEntity.GetSOIParentEntity();
            OrbitDB newOrbit = OrbitDB.FromKeplerElements(soi, newmass, tgtTraj, dateTimeNow);
            entity.SetDataBlob(newOrbit);
            OrbitProcessor.UpdateOrbit(entity, entity.GetDataBlob<OrbitDB>().Parent.GetDataBlob<PositionDB>(), dateTimeFuture);
            newtonMoveDB.IsComplete = true;
        }
        else
        {
            throw new NotImplementedException("Yeah need to handle half a manuver when not enough fuel");
        }



    }
}


public class NewtonSimpleMoveDB : BaseDataBlob
{
    internal DateTime LastProcessDateTime = new DateTime();
    public DateTime ActionOnDateTime { get; internal set; }
    public KeplerElements CurrentTrajectory { get; internal set; }
    public KeplerElements TargetTrajectory { get; internal set; }

    public bool IsComplete = false;
    public Entity SOIParent { get; internal set; }
    public double ParentMass { get; internal set; }

    public NewtonSimpleMoveDB(Entity SoiParent, KeplerElements start, KeplerElements end, DateTime onDateTime)
    {
        LastProcessDateTime = onDateTime;
        ActionOnDateTime = onDateTime;
        CurrentTrajectory = start;
        TargetTrajectory = end;
        SOIParent = SOIParent;
        ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
    }
    
    public override object Clone()
    {
        throw new NotImplementedException();
    }
}