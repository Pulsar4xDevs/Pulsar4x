using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    public class CargoTransferProcessor : IHotloopProcessor
    {


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

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            CargoTransferDB transferDB = entity.GetDataBlob<CargoTransferDB>();
            SetTransferRate(entity, transferDB);
            for (int i = 0; i < transferDB.ItemsLeftToTransfer.Count; i++)
            {
                (ICargoable item, long amount) itemsToXfer = transferDB.ItemsLeftToTransfer[i];
                ICargoable cargoItem = itemsToXfer.item;
                long amountToXfer = itemsToXfer.amount;

                Guid cargoTypeID = cargoItem.CargoTypeID;
                double itemMassPerUnit = cargoItem.MassPerUnit;

                if (!transferDB.CargoToDB.TypeStores.ContainsKey(cargoTypeID))
                {
                    string errmsg = "This entity cannot store this type of cargo";
                    StaticRefLib.EventLog.AddGameEntityErrorEvent(entity, errmsg);
                }

                var toCargoTypeStore = transferDB.CargoToDB.TypeStores[cargoTypeID]; //reference to the cargoType store we're pushing to.
                var toCargoItemAndAmount = toCargoTypeStore.CurrentStoreInUnits; //reference to dictionary holding the cargo we want to send too
                var fromCargoTypeStore = transferDB.CargoFromDB.TypeStores[cargoTypeID]; //reference to the cargoType store we're pulling from.
                var fromCargoItemAndAmount = fromCargoTypeStore.CurrentStoreInUnits; //reference to dictionary we want to pull cargo from. 

                //the transfer speed is mass based, not unit based. 
                double totalMassToTransfer = itemMassPerUnit * amountToXfer;
                double massToTransferThisTick = Math.Min(totalMassToTransfer, transferDB.TransferRateInKG * deltaSeconds); //only the amount that can be transfered in this timeframe. 

                //TODO: this wont handle objects that have a larger unit mass than the availible transferRate,
                //but maybe that makes for a game mechanic
                long countToTransferThisTick = (long)(massToTransferThisTick / itemMassPerUnit);
                
                long amountFrom = transferDB.CargoFromDB.RemoveCargoByUnit(cargoItem, countToTransferThisTick);
                long amountTo = transferDB.CargoToDB.AddCargoByUnit(cargoItem, countToTransferThisTick);
                
                //update the total masses for these entites
                transferDB.CargoFromDB.OwningEntity.GetDataBlob<MassVolumeDB>().UpdateMassTotal(transferDB.CargoFromDB);
                transferDB.CargoToDB.OwningEntity.GetDataBlob<MassVolumeDB>().UpdateMassTotal(transferDB.CargoToDB);


                if(amountTo != amountFrom)
                    throw new Exception("something went wrong here");
                long newAmount = transferDB.ItemsLeftToTransfer[i].amount - amountTo;
                transferDB.ItemsLeftToTransfer[i] = (cargoItem, newAmount);
            }
            
        }



        /// <summary>
        /// Add cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double AddCargoItems(Entity entity, ICargoable item, int amount)
        {
            VolumeStorageDB cargo = entity.GetDataBlob<VolumeStorageDB>();
            double amountSuccess = cargo.AddCargoByUnit(item, amount);
            MassVolumeDB mv = entity.GetDataBlob<MassVolumeDB>();
            mv.UpdateMassTotal(cargo);
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
            VolumeStorageDB cargo = entity.GetDataBlob<VolumeStorageDB>();
            double amountSuccess = cargo.RemoveCargoByUnit(item, amount);
            MassVolumeDB mv = entity.GetDataBlob<MassVolumeDB>();
            mv.UpdateMassTotal(cargo);
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
            VolumeStorageDB cargo = entity.GetDataBlob<VolumeStorageDB>();
            double amountSuccess = cargo.AddRemoveCargoByMass(item, amountInMass);
            MassVolumeDB mv = entity.GetDataBlob<MassVolumeDB>();
            mv.UpdateMassTotal(cargo);
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
            VolumeStorageDB cargo = entity.GetDataBlob<VolumeStorageDB>();
            double amountSuccess = cargo.AddRemoveCargoByVolume(item, amountInVolume);
            MassVolumeDB mv = entity.GetDataBlob<MassVolumeDB>();
            mv.UpdateMassTotal(cargo);
            return amountSuccess;
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
            
            Entity soi1 = entity1.GetSOIParentEntity();
            Entity soi2 = entity2.GetSOIParentEntity();
            
            
            if(soi1 == soi2)
            {
                parent = soi1;
                parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
                sgp = GeneralMath.StandardGravitationalParameter(parentMass);
                
                (Vector3 pos, Vector3 Velocity) state1 = entity1.GetRelativeState();
                (Vector3 pos, Vector3 Velocity) state2 = entity2.GetRelativeState();
                r1 = state1.pos.Length();
                r2 = state2.pos.Length();
            }
            else
            {
                StaticRefLib.EventLog.AddEvent(new Event("Cargo calc failed, entities must have same soi parent"));
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
        public static int CalcTransferRate(double dvDifference_mps, VolumeStorageDB from, VolumeStorageDB to)
        {
            //var from = transferDB.CargoFromDB;
            //var to = transferDB.CargoToDB;
            var fromDVRange = from.TransferRangeDv_mps;
            var toDVRange = to.TransferRangeDv_mps;

            double maxRange;
            double maxXferAtMaxRange;
            double bestXferRange_ms = Math.Min(fromDVRange, toDVRange);
            double maxXferAtBestRange = from.TransferRateInKgHr + to.TransferRateInKgHr;

            double transferRate;

            if (from.TransferRangeDv_mps > to.TransferRangeDv_mps)
            {
                maxRange = fromDVRange;
                if (from.TransferRateInKgHr > to.TransferRateInKgHr)
                    maxXferAtMaxRange = from.TransferRateInKgHr;
                else
                    maxXferAtMaxRange = to.TransferRateInKgHr;
            }
            else
            {
                maxRange = toDVRange;
                if (to.TransferRateInKgHr > from.TransferRateInKgHr)
                    maxXferAtMaxRange = to.TransferRateInKgHr;
                else
                    maxXferAtMaxRange = from.TransferRateInKgHr;
            }

            if (dvDifference_mps < bestXferRange_ms)
                transferRate = (int)maxXferAtBestRange;
            else if (dvDifference_mps < maxRange)
                transferRate = (int)maxXferAtMaxRange;
            else
                transferRate = 0;
            return (int)transferRate;
        }

        internal static void SetTransferRate(Entity entity, CargoTransferDB transferDB)
        {
            double dv_mps = CalcDVDifference_m(entity, transferDB.CargoToEntity);    
            var rate = CalcTransferRate(dv_mps, transferDB.CargoFromDB, transferDB.CargoToDB);
            transferDB.TransferRateInKG = rate;
        }

        
        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithCargoTransfers = manager.GetAllEntitiesWithDataBlob<CargoTransferDB>();
            foreach(var entity in entitysWithCargoTransfers) 
            {                
                ProcessEntity(entity, deltaSeconds);
            }

            return entitysWithCargoTransfers.Count;
        }
    }

}
