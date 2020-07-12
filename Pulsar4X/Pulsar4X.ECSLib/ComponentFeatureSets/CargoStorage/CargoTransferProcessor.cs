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
            CargoTransferDB datablob = entity.GetDataBlob<CargoTransferDB>();
            OrbitDB orbitDB = entity.GetDataBlob<OrbitDB>();

            if (datablob.DistanceBetweenEntitys <= 100) //todo: this is going to have to be based of mass or something, ie being further away from a colony on a planet is ok, but two ships should be close. 
            {
                for (int i = 0; i < datablob.ItemsLeftToTransfer.Count; i++)
                {
                    (ICargoable item, long amount) itemsToXfer = datablob.ItemsLeftToTransfer[i];
                    ICargoable cargoItem = itemsToXfer.item;
                    long amountToXfer = itemsToXfer.amount;

                    Guid cargoTypeID = cargoItem.CargoTypeID;
                    double itemMassPerUnit = cargoItem.Density;

                    if (!datablob.CargoToDB.StoredCargoTypes.ContainsKey(cargoTypeID))
                        datablob.CargoToDB.StoredCargoTypes.Add(cargoTypeID, new CargoTypeStore());

                    var toCargoTypeStore = datablob.CargoToDB.StoredCargoTypes[cargoTypeID]; //reference to the cargoType store we're pushing to.
                    var toCargoItemAndAmount = toCargoTypeStore.ItemsAndAmounts; //reference to dictionary holding the cargo we want to send too
                    var fromCargoTypeStore = datablob.CargoFromDB.StoredCargoTypes[cargoTypeID]; //reference to the cargoType store we're pulling from.
                    var fromCargoItemAndAmount = fromCargoTypeStore.ItemsAndAmounts; //reference to dictionary we want to pull cargo from. 

                    double totalweightToTransfer = itemMassPerUnit * amountToXfer;
                    double weightToTransferThisTick = Math.Min(totalweightToTransfer, datablob.TransferRateInKG * deltaSeconds); //only the amount that can be transfered in this timeframe. 

                    weightToTransferThisTick = Math.Min(weightToTransferThisTick, toCargoTypeStore.FreeCapacityKg); //check cargo to has enough weight capacity

                    long numberXfered = (long)(weightToTransferThisTick / itemMassPerUnit); //get the number of items from the mass transferable
                    numberXfered = Math.Min(numberXfered, fromCargoItemAndAmount[cargoItem.ID].amount); //check from has enough to send. 

                    weightToTransferThisTick = (long)(numberXfered * itemMassPerUnit);

                    if (!toCargoItemAndAmount.ContainsKey(cargoItem.ID))
                        toCargoItemAndAmount.Add(cargoItem.ID, (cargoItem, numberXfered));
                    else
                    {
                        long totalTo = toCargoItemAndAmount[cargoItem.ID].amount + numberXfered;
                        toCargoItemAndAmount[cargoItem.ID] = (cargoItem, totalTo);
                    }

                    toCargoTypeStore.FreeCapacityKg -= (long)weightToTransferThisTick;

                    long totalFrom = toCargoItemAndAmount[cargoItem.ID].amount - numberXfered;
                    fromCargoItemAndAmount[cargoItem.ID] = (cargoItem, totalFrom);
                    fromCargoTypeStore.FreeCapacityKg += (long)weightToTransferThisTick;
                    datablob.ItemsLeftToTransfer[i] = (cargoItem, amountToXfer - numberXfered);
                }
            }
        }

        internal static void FirstRun(Entity entity)
        {
            CargoTransferDB datablob = entity.GetDataBlob<CargoTransferDB>();

            double? dv_mps;
            if (entity.HasDataBlob<OrbitDB>() && datablob.CargoToEntity.HasDataBlob<OrbitDB>())
                dv_mps = CalcDVDifference_m(entity, datablob.CargoToEntity);
            else
            {
                OrbitDB orbitDB;
                if (entity.HasDataBlob<ColonyInfoDB>())
                {
                    orbitDB = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<OrbitDB>();
                }
                else //if (datablob.CargoToEntity.HasDataBlob<ColonyInfoDB>())
                {
                    orbitDB = datablob.CargoToEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<OrbitDB>();
                }

                dv_mps = OrbitMath.MeanOrbitalVelocityInm(orbitDB);
            }

            if (dv_mps != null)
                datablob.TransferRateInKG = CalcTransferRate((double)dv_mps, datablob.CargoFromDB, datablob.CargoToDB);
        }

        /// <summary>
        /// Calculates the DVD ifference.
        /// </summary>
        /// <returns>The delaV Difference in Km/s, null if not in imediate orbit</returns>
        public static double? CalcDVDifferenceKmPerSecond(OrbitDB orbitDBFrom, OrbitDB orbitDBTo)
        {
            Entity toEntity = orbitDBTo.OwningEntity;
            double? dv = null;
            if (orbitDBFrom.Parent == toEntity) //Cargo going up the gravity well
            {
                dv = OrbitMath.MeanOrbitalVelocityInAU(orbitDBFrom);
            }
            else if (orbitDBFrom.Children.Contains(toEntity)) //Cargo going down the gravity well
            {
                dv = OrbitMath.MeanOrbitalVelocityInAU(orbitDBTo);
            }
            else if (orbitDBFrom.Parent == toEntity.GetDataBlob<OrbitDB>().Parent) //cargo going between objects orbiting the same body
            {
                dv = Math.Abs(OrbitMath.MeanOrbitalVelocityInAU(orbitDBFrom) - OrbitMath.MeanOrbitalVelocityInAU(orbitDBTo));
            }

            if (dv == null)
                return dv;
            else
                return Distance.AuToKm((double)dv);
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
                
                
                var state1 = Entity.GetRalitiveState(entity1);
                var state2 = Entity.GetRalitiveState(entity2);
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
        public static int CalcTransferRate(double dvDifference_mps, CargoStorageDB from, CargoStorageDB to)
        {
            //var from = transferDB.CargoFromDB;
            //var to = transferDB.CargoToDB;
            var fromDVRange = from.TransferRangeDv_kms;
            var toDVRange = to.TransferRangeDv_kms;

            double maxRange;
            double maxXferAtMaxRange;
            double bestXferRange_ms = Math.Min(fromDVRange, toDVRange);
            double maxXferAtBestRange = from.TransferRateInKgHr + to.TransferRateInKgHr;

            double transferRate;

            if (from.TransferRangeDv_kms > to.TransferRangeDv_kms)
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
