using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class CargoTransferProcessor : IHotloopProcessor
    {
        public CargoTransferProcessor()
        {
        }

        public TimeSpan RunFrequency {
            get {
                return TimeSpan.FromHours(1);
            }
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            CargoTransferDB datablob = entity.GetDataBlob<CargoTransferDB>();
            if(datablob.DistanceBetweenEntitys <= 100)//todo: this is going to have to be based of mass or something, ie being further away from a colony on a planet is ok, but two ships should be close. 
            {
                
                Guid cargoTypeID = datablob.ItemToTranfer.CargoTypeID;
                int itemMassPerUnit = datablob.ItemToTranfer.Mass; 
                if(!datablob.CargoToDB.StoredCargoTypes.ContainsKey(cargoTypeID)) 
                    datablob.CargoToDB.StoredCargoTypes.Add(cargoTypeID, new CargoTypeStore());

                var toCargoTypeStore = datablob.CargoToDB.StoredCargoTypes[cargoTypeID];        //reference to the cargoType store we're pushing to.
                var toCargoItemAndAmount = toCargoTypeStore.ItemsAndAmounts;                //reference to dictionary holding the cargo we want to send too
                var fromCargoTypeStore = datablob.CargoFromDB.StoredCargoTypes[cargoTypeID];    //reference to the cargoType store we're pulling from.
                var fromCargoItemAndAmount = fromCargoTypeStore.ItemsAndAmounts;            //reference to dictionary we want to pull cargo from. 

                datablob.TransferRate = 100;//todo set transfer rates on cargostorageDB and get either an average or a math.min. probibly an average. 

                long totalweightToTransfer = itemMassPerUnit * (datablob.AmountTransfered - datablob.TotalAmountToTransfer);
                long weightToTransferThisTick = Math.Min(totalweightToTransfer, datablob.TransferRate * deltaSeconds);          //only the amount that can be transfered in this timeframe. 
                weightToTransferThisTick = Math.Min(weightToTransferThisTick, toCargoTypeStore.FreeCapacity);                   //check cargo to has enough weight capacity

                long numberOfItems = weightToTransferThisTick / itemMassPerUnit;                                         //get the number of items from the mass transferable
                numberOfItems = Math.Min(numberOfItems, fromCargoItemAndAmount[datablob.ItemToTranfer.ID]);                     //check from has enough to send. 

                weightToTransferThisTick = numberOfItems * itemMassPerUnit;

                if(!toCargoItemAndAmount.ContainsKey(datablob.ItemToTranfer.ID))
                    toCargoItemAndAmount.Add(datablob.ItemToTranfer.ID, numberOfItems);
                else
                    toCargoItemAndAmount[datablob.ItemToTranfer.ID] += numberOfItems;

                toCargoTypeStore.FreeCapacity -= weightToTransferThisTick;

                fromCargoItemAndAmount[datablob.ItemToTranfer.ID] -= numberOfItems;
                fromCargoTypeStore.FreeCapacity += weightToTransferThisTick;
                datablob.AmountTransfered += numberOfItems;
                            
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
