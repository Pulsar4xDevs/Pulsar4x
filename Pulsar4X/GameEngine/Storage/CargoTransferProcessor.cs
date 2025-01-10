using System;
using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Orbital;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.Storage
{
    public class CargoTransferProcessor : IHotloopProcessor
    {
        public static CargoDefinitionsLibrary CargoDefs;

        public TimeSpan RunFrequency
        {
            get { return TimeSpan.FromMinutes(1); }
        }

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0);

        public Type GetParameterType => typeof(CargoTransferDB);

        public void Init(Game game)
        {
            //unneeded
        }
        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<CargoTransferDB> dblist = manager.GetAllDataBlobsOfType<CargoTransferDB>();
            foreach(var db in dblist)
            {
                ProcessEntity(db, deltaSeconds);
            }
            return dblist.Count;
        }
        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            ProcessEntity(entity.GetDataBlob<CargoTransferDB>(), deltaSeconds);
        }


        public void ProcessEntity(CargoTransferDB transferDB, int deltaSeconds)
        {
            var transferData = transferDB.TransferData;
            var transferRange = transferDB.ParentStorageDB.TransferRangeDv_mps;
            var transferRate = transferDB.ParentStorageDB.TransferRate;
            double dv_mps = CalcDVDifference_m(transferData.PrimaryEntity, transferData.SecondaryEntity);
            

            double massTransferable = transferRate * deltaSeconds;
            if(dv_mps > transferRange || massTransferable <=0)
                return;//early out if we're out of range or no more mass to move.
            
            massTransferable -= MoveFromEscro(transferData.EscroHeldInPrimary, transferData.SecondaryStorageDB, transferData.PrimaryStorageDB, massTransferable);
            massTransferable -= MoveFromEscro(transferData.EscroHeldInSecondary, transferData.PrimaryStorageDB, transferData.SecondaryStorageDB, massTransferable);
            
            UpdateMassFuelAndDeltaV(transferData.PrimaryEntity);
            UpdateMassFuelAndDeltaV(transferData.SecondaryEntity);

        }

        private double MoveFromEscro(SafeList<(ICargoable item, long count, double mass)> escroList, CargoStorageDB moveTo, CargoStorageDB moveFrom, double massTransferable)
        {
            double totalMassXfered = 0;
            for (int index = 0; index < escroList.Count; index++)
            {
                (ICargoable cargoItem, long count, double mass) tuple = escroList[index];
                var cargoItem = tuple.cargoItem;
                double itemMassPerUnit = cargoItem.MassPerUnit;
                double massToXfer = Math.Min(massTransferable, tuple.mass);
                massToXfer = Math.Min(massToXfer, CargoMath.GetFreeMass(moveTo, cargoItem));
                
                double massLeft = tuple.mass - massToXfer;
                //we use Ceaaling here to signify whole part items not fully moved yet. 
                long itemsLeft = (int)Math.Ceiling(massLeft * itemMassPerUnit);
                var countToXfer = tuple.count - itemsLeft;
                escroList[index] = (cargoItem,itemsLeft, massLeft);
                
                //add items to cargo of seconddary entity store
                moveTo.AddCargoByUnit(cargoItem, countToXfer);
                
                //update mass and volume of primary entity store.
                double volumeStoring = countToXfer * cargoItem.VolumePerUnit;
                double massStoring = countToXfer * cargoItem.MassPerUnit;
                TypeStore store = moveFrom.TypeStores[cargoItem.CargoTypeID];
                store.FreeVolume += volumeStoring;
                moveFrom.TotalStoredMass += massStoring;
                
                massTransferable -= massToXfer;
                totalMassXfered += massToXfer;
            }
            return totalMassXfered;
        }
        
        /// <summary>
        /// Add cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double AddCargoItems(Entity entity, ICargoable item, int amount)
        {
            CargoStorageDB cargo = entity.GetDataBlob<CargoStorageDB>();
            double amountSuccess = cargo.AddCargoByUnit(item, amount);
            UpdateMassFuelAndDeltaV(entity);
            return amountSuccess;
        }

        /// <summary>
        /// Removes cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double RemoveCargoItems(Entity entity, ICargoable item, int amount)
        {
            CargoStorageDB cargo = entity.GetDataBlob<CargoStorageDB>();
            double amountSuccess = cargo.RemoveCargoByUnit(item, amount);
            UpdateMassFuelAndDeltaV(entity);
            return amountSuccess;
        }

        /// <summary>
        /// Add or Removes cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double AddRemoveCargoMass(Entity entity, ICargoable item, double amountInMass)
        {
            CargoStorageDB cargo = entity.GetDataBlob<CargoStorageDB>();
            double amountSuccess = cargo.AddRemoveCargoByMass(item, amountInMass);
            UpdateMassFuelAndDeltaV(entity);
            return amountSuccess;
        }
        /// <summary>
        /// Add or Removes cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="storeDB"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double AddRemoveCargoMass(CargoStorageDB storeDB, ICargoable item, double amountInMass)
        {
            double amountSuccess = storeDB.AddRemoveCargoByMass(item, amountInMass);
            UpdateMassFuelAndDeltaV(storeDB.OwningEntity);
            return amountSuccess;
        }


        
        /// <summary>
        /// Add or Removes cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInVolume"></param>
        internal static double AddRemoveCargoVolume(Entity entity, ICargoable item, double amountInVolume)
        {
            CargoStorageDB cargo = entity.GetDataBlob<CargoStorageDB>();
            double amountSuccess = cargo.AddRemoveCargoByVolume(item, amountInVolume);
            UpdateMassFuelAndDeltaV(entity);
            return amountSuccess;
        }

        internal static void UpdateMassFuelAndDeltaV(Entity entity)
        {
            if(!entity.TryGetDatablob(out NewtonThrustAbilityDB newtdb))
                return;
            if (!entity.TryGetDatablob(out MassVolumeDB massdb))
                return;
            if(!entity.TryGetDatablob(out CargoStorageDB storedb))
                return;

            massdb.UpdateMassTotal();
            var cargoLib = entity.GetFactionCargoDefinitions();
            var fuelTypeID = newtdb.FuelType;
            var fuelType = cargoLib.GetAny(fuelTypeID);
            var fuelMass = storedb.GetMassStored(fuelType, false);
            newtdb.SetFuel(fuelMass, massdb.MassTotal);
        }

        /// <summary>
        /// Calculates a simplified difference in DeltaV between two enties who have the same parent
        /// for the purposes of calculating cargo transfer rate
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="entity2"></param>
        /// <returns></returns>
        public static double CalcDVDifference_m(Entity entity1, Entity entity2)
        {
            double dvDif = 0;

            Entity parent;
            double parentMass;
            double sgp;
            double r1;
            double r2;

            Entity? soi1 = entity1.GetSOIParentEntity();
            Entity? soi2 = entity2.GetSOIParentEntity();


            if(soi1 is not null && soi2 is not null && soi1 == soi2)
            {
                parent = soi1;
                parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
                sgp = GeneralMath.StandardGravitationalParameter(parentMass);

                (Vector3 pos, Vector3 Velocity) state1 = MoveMath.GetRelativeState(entity1);
                (Vector3 pos, Vector3 Velocity) state2 = MoveMath.GetRelativeState(entity2);
                r1 = state1.pos.Length();
                r2 = state2.pos.Length();
            }
            else
            {
                //StaticRefLib.EventLog.AddEvent(new Event("Cargo calc failed, entities must have same soi parent"));
                return double.PositiveInfinity;
            }

            var hohmann = OrbitalMath.Hohmann(sgp, r1, r2);
            dvDif = hohmann[0].deltaV.Length() + hohmann[1].deltaV.Length();
            return dvDif;
            
        }



        /// <summary>
        /// Calculates a simplified difference in DeltaV between two enties who have the same parent
        /// for the purposes of calculating cargo transfer rate
        /// </summary>
        /// <param name="sgp"></param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <returns></returns>
        public static double CalcDVDifference_m(double sgp, (Vector3 pos, Vector3 Velocity) state1, (Vector3 pos, Vector3 Velocity) state2)
        {
            var r1 = state1.pos.Length();
            var r2 = state2.pos.Length();
            var hohmann = OrbitalMath.Hohmann(sgp, r1, r2);
            return hohmann[0].deltaV.Length() + hohmann[1].deltaV.Length();
        }


        /// <summary>
        /// Calculates the transfer rate.
        /// </summary>
        /// <returns>The transfer rate.</returns>
        /// <param name="dvDifference_mps">Dv difference in m/s</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public static int CalcTransferRate(double dvDifference_mps, CargoStorageDB from, CargoStorageDB to)
        {
            //var from = transferDB.CargoFromDB;
            //var to = transferDB.CargoToDB;
            var fromDVRange = from.TransferRangeDv_mps;
            var toDVRange = to.TransferRangeDv_mps;

            double maxRange;
            double maxXferAtMaxRange;
            double bestXferRange_ms = Math.Min(fromDVRange, toDVRange);
            double maxXferAtBestRange = from.TransferRate + to.TransferRate;

            double transferRate;

            if (from.TransferRangeDv_mps > to.TransferRangeDv_mps)
            {
                maxRange = fromDVRange;
                if (from.TransferRate > to.TransferRate)
                    maxXferAtMaxRange = from.TransferRate;
                else
                    maxXferAtMaxRange = to.TransferRate;
            }
            else
            {
                maxRange = toDVRange;
                if (to.TransferRate > from.TransferRate)
                    maxXferAtMaxRange = to.TransferRate;
                else
                    maxXferAtMaxRange = from.TransferRate;
            }

            if (dvDifference_mps < bestXferRange_ms)
                transferRate = (int)maxXferAtBestRange;
            else if (dvDifference_mps < maxRange)
                transferRate = (int)maxXferAtMaxRange;
            else
                transferRate = 0;
            return (int)transferRate;
        }
        

        public static (double bestDVRange, double bestRate) GetBestRangeRate(Entity from, Entity to)
        {
            var fromdb = from.GetDataBlob<CargoStorageDB>();
            var todb = to.GetDataBlob<CargoStorageDB>();
            var fromDVRange = fromdb.TransferRangeDv_mps;
            var toDVRange = todb.TransferRangeDv_mps;
            double bestXferRange_ms = Math.Min(fromDVRange, toDVRange);
            double maxXferAtBestRange = fromdb.TransferRate + todb.TransferRate;
            return (maxXferAtBestRange, bestXferRange_ms);
        }

        public static (double maxDVRange, double lowRate) GetMaxRangeRate(Entity from, Entity to)
        {
            var fromdb = from.GetDataBlob<CargoStorageDB>();
            var todb = to.GetDataBlob<CargoStorageDB>();
            var fromDVRange = fromdb.TransferRangeDv_mps;
            var toDVRange = todb.TransferRangeDv_mps;
            double maxRange;
            double maxXferAtMaxRange;

            if (fromdb.TransferRangeDv_mps > todb.TransferRangeDv_mps)
            {
                maxRange = fromDVRange;
                if (fromdb.TransferRate > todb.TransferRate)
                    maxXferAtMaxRange = fromdb.TransferRate;
                else
                    maxXferAtMaxRange = todb.TransferRate;
            }
            else
            {
                maxRange = toDVRange;
                if (todb.TransferRate > fromdb.TransferRate)
                    maxXferAtMaxRange = todb.TransferRate;
                else
                    maxXferAtMaxRange = fromdb.TransferRate;
            }

            return(maxRange, maxXferAtMaxRange);
        }
    }
}
