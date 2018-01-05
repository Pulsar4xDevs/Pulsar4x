using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoLoadOrder:EntityCommand
    {

        public long TotalAmountToTransfer { get; set; }

        public Guid LoadCargoFromEntityGuid { get; set; }

        public Guid ItemToTransfer { get; set; }

        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        [JsonIgnore]
        Entity factionEntity;
        [JsonIgnore]
        Entity loadFromEntity;

        /// <summary>
        /// Validates and actions the command. 
        /// may eventualy need to return a responce instead of void. 
        /// This creates a CargoTranferDB from the command, which does all the work.
        /// the command is to create and enqueue a CargoTransferDB.
        /// </summary>
        internal override void ActionCommand(Game game)
        {
            CargoTransferDB newTransferDB = new CargoTransferDB();
            newTransferDB.CargoToEntity = EntityCommanding;
            newTransferDB.CargoToDB = EntityCommanding.GetDataBlob<CargoStorageDB>();
            newTransferDB.CargoFromEntity = loadFromEntity;
            newTransferDB.CargoFromDB = loadFromEntity.GetDataBlob<CargoStorageDB>();

            newTransferDB.TotalAmountToTransfer = TotalAmountToTransfer;
            newTransferDB.ItemToTranfer = (ICargoable)game.StaticData.FindDataObjectUsingID(ItemToTransfer);
            EntityCommanding.Manager.SetDataBlob(EntityCommanding.ID, newTransferDB);
            IsRunning = true;
        }


        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.TryGetEntityByGuid(LoadCargoFromEntityGuid, out loadFromEntity))
                {
                    return true;
                }
            }
            return false;
        }

        internal override bool IsFinished()
        {
            throw new NotImplementedException();
        }

        public CargoLoadOrder()
        {

        }
    }
}
