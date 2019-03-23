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
            if(datablob.DistanceBetweenEntitys <= 100)//todo: this is going to have to be based of mass or something, ie being further away from a colony on a planet is ok, but two ships should be close. 
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
