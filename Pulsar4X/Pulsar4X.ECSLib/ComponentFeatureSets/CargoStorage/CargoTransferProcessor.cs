using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class CargoTransferProcessor : IHotloopProcessor
    {


        public TimeSpan RunFrequency {
            get {
                return TimeSpan.FromHours(1);
            }
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


 



            if (datablob.DistanceBetweenEntitys <= 100)//todo: this is going to have to be based of mass or something, ie being further away from a colony on a planet is ok, but two ships should be close. 
                {
                    for (int i = 0; i < datablob.ItemsLeftToTransfer.Count; i++)
                    {
                        var item = datablob.ItemsLeftToTransfer[i];
                        ICargoable cargoItem = item.Item1;
                        long amountToXfer = item.Item2;

                        Guid cargoTypeID = cargoItem.CargoTypeID;
                        int itemMassPerUnit = cargoItem.Mass;

                        if (!datablob.CargoToDB.StoredCargoTypes.ContainsKey(cargoTypeID))
                            datablob.CargoToDB.StoredCargoTypes.Add(cargoTypeID, new CargoTypeStore());

                        var toCargoTypeStore = datablob.CargoToDB.StoredCargoTypes[cargoTypeID];        //reference to the cargoType store we're pushing to.
                        var toCargoItemAndAmount = toCargoTypeStore.ItemsAndAmounts;                //reference to dictionary holding the cargo we want to send too
                        var fromCargoTypeStore = datablob.CargoFromDB.StoredCargoTypes[cargoTypeID];    //reference to the cargoType store we're pulling from.
                        var fromCargoItemAndAmount = fromCargoTypeStore.ItemsAndAmounts;            //reference to dictionary we want to pull cargo from. 

                        long totalweightToTransfer = itemMassPerUnit * amountToXfer;
                        long weightToTransferThisTick = Math.Min(totalweightToTransfer, datablob.TransferRateInKG * deltaSeconds);          //only the amount that can be transfered in this timeframe. 

                        weightToTransferThisTick = Math.Min(weightToTransferThisTick, toCargoTypeStore.FreeCapacityKg); //check cargo to has enough weight capacity

                        long numberXfered = weightToTransferThisTick / itemMassPerUnit;//get the number of items from the mass transferable
                        numberXfered = Math.Min(numberXfered, fromCargoItemAndAmount[cargoItem.ID]); //check from has enough to send. 

                        weightToTransferThisTick = numberXfered * itemMassPerUnit;

                        if (!toCargoItemAndAmount.ContainsKey(cargoItem.ID))
                            toCargoItemAndAmount.Add(cargoItem.ID, numberXfered);
                        else
                            toCargoItemAndAmount[cargoItem.ID] += numberXfered;

                        toCargoTypeStore.FreeCapacityKg -= weightToTransferThisTick;

                        fromCargoItemAndAmount[cargoItem.ID] -= numberXfered;
                        fromCargoTypeStore.FreeCapacityKg += weightToTransferThisTick;
                        datablob.ItemsLeftToTransfer[i] = new Tuple<ICargoable, long>(cargoItem, amountToXfer -= numberXfered);
                    }
                } 
        }

        internal static void FirstRun(Entity entity)
        {
            CargoTransferDB datablob = entity.GetDataBlob<CargoTransferDB>();

            double? dv;
            if(entity.HasDataBlob<OrbitDB>() && datablob.CargoToEntity.HasDataBlob<OrbitDB>())
                dv = CalcDVDifferenceKmPerSecond(entity.GetDataBlob<OrbitDB>(), datablob.CargoToEntity.GetDataBlob<OrbitDB>());
            else
            {
                OrbitDB orbitDB;
                if (entity.HasDataBlob<ColonyInfoDB>())
                {
                    orbitDB = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<OrbitDB>();
                }
                else//if (datablob.CargoToEntity.HasDataBlob<ColonyInfoDB>())
                {
                    orbitDB = datablob.CargoToEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<OrbitDB>(); 
                }
                dv = Distance.AuToKm( OrbitMath.MeanOrbitalVelocityInAU(orbitDB));
            }
            if (dv != null)
                datablob.TransferRateInKG = CalcTransferRate((double)dv, datablob.CargoFromDB, datablob.CargoToDB);
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

        /// <summary>
        /// Calculates the transfer rate.
        /// </summary>
        /// <returns>The transfer rate.</returns>
        /// <param name="dvDifferenceKmPerSecond">Dv difference in Km/s</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public static int CalcTransferRate(double dvDifferenceKmPerSecond, CargoStorageDB from, CargoStorageDB to)
        {
            //var from = transferDB.CargoFromDB;
            //var to = transferDB.CargoToDB;
            var fromRange = from.TransferRangeDv;
            var toRange = to.TransferRangeDv;

            double maxRange;
            double maxXferAtMaxRange;
            double bestXferRange = Math.Min(from.TransferRangeDv, to.TransferRangeDv);
            double maxXferAtBestRange = from.TransferRateInKgHr + to.TransferRateInKgHr;

            double transferRate;

            if (from.TransferRangeDv > to.TransferRangeDv)
            {
                maxRange = from.TransferRangeDv;
                if (from.TransferRateInKgHr > to.TransferRateInKgHr)
                    maxXferAtMaxRange = from.TransferRateInKgHr;
                else
                    maxXferAtMaxRange = to.TransferRateInKgHr;
            }
            else
            {
                maxRange = to.TransferRangeDv;
                if (to.TransferRateInKgHr > from.TransferRateInKgHr)
                    maxXferAtMaxRange = to.TransferRateInKgHr;
                else
                    maxXferAtMaxRange = from.TransferRateInKgHr;
            }

            if (dvDifferenceKmPerSecond < bestXferRange)
                transferRate = (int)maxXferAtBestRange;
            else if (dvDifferenceKmPerSecond < maxRange)
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
