using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class CargoTransferProcessor : IHotloopProcessor
    {


        public TimeSpan RunFrequency
        {
            get { return TimeSpan.FromHours(1); }
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
                
                if(amountTo != amountFrom)
                    throw new Exception("something went wrong here");
                long newAmount = transferDB.ItemsLeftToTransfer[i].amount - amountTo;
                transferDB.ItemsLeftToTransfer[i] = (cargoItem, newAmount);
            }
            
        }

        internal static void FirstRun(Entity entity)
        {
            CargoTransferDB transferDB = entity.GetDataBlob<CargoTransferDB>();
            double dv_mps = CalcDVDifference_m(entity, transferDB.CargoToEntity);    
            var rate = CalcTransferRate(dv_mps, transferDB.CargoFromDB, transferDB.CargoToDB);
            transferDB.TransferRateInKG = rate;
        }
        

        public static double CalcDVDifference_m(Entity entity1, Entity entity2)
        {
            double dvDif = 0;

            Entity parent;
            double parentMass;
            double sgp;
            double r1;
            double r2;
            
            Entity soi1 = Entity.GetSOIParentEntity(entity1);
            Entity soi2 = Entity.GetSOIParentEntity(entity2);
            
            
            if(soi1 == soi2)
            {
                parent = soi1;
                parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
                sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(0, parentMass);
                
                
                var state1 = Entity.GetRelativeState(entity1);
                var state2 = Entity.GetRelativeState(entity2);
                r1 = state1.pos.Length();
                r2 = state2.pos.Length();
            }
            else
            {
                StaticRefLib.EventLog.AddEvent(new Event("Cargo calc failed, entities must have same soi parent"));
                return double.PositiveInfinity;
            }

            var hohmann = InterceptCalcs.Hohmann(sgp, r1, r2);
            return dvDif = hohmann[0].deltaV.Length() + hohmann[1].deltaV.Length();


        }

    /// <summary>
        /// Calculates the transfer rate.
        /// </summary>
        /// <returns>The transfer rate.</returns>
        /// <param name="dvDifference_mps">Dv difference in Km/s</param>
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

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithCargoTransfers = manager.GetAllEntitiesWithDataBlob<CargoTransferDB>();
            foreach(var entity in entitysWithCargoTransfers) 
            {                
                ProcessEntity(entity, deltaSeconds);
            }
        }
    }

}
