using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoLoadOrder:EntityCommand
    {
        /// <summary>
        /// This is the faction that has requested the command. 
        /// </summary>
        /// <value>The requesting faction GUID.</value>
        public Guid RequestingFactionGuid { get; set; }

        /// <summary>
        /// The Entity this command is targeted at
        /// </summary>
        /// <value>The entity GUID.</value>
        public Guid EntityCommandingGuid { get; set; }

        /// <summary>
        /// Gets or sets the datetime this command was created by the player/client. 
        /// </summary>
        /// <value>The created date.</value>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the datetime this command was actioned/processed by the server. 
        /// this may be needed by the client to ensure it stays in synch with the server. 
        /// </summary>
        /// <value>The actioned on date.</value>
        public DateTime ActionedOnDate { get; set; }

        public long TotalAmountToTransfer { get; set; }

        public Guid LoadCargoFromEntityGuid { get; set; }

        public Guid ItemToTransfer { get; set; }

        public int ActionLanes => 1;

        public bool IsBlocking => true;
        public bool IsRunning { get; private set; } = false;
        private Entity _entityCommanding;
        public Entity EntityCommanding { get { return _entityCommanding; } }

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
        public void ActionCommand(Game game)
        {
            CargoTransferDB newTransferDB = new CargoTransferDB();
            newTransferDB.CargoToEntity = _entityCommanding;
            newTransferDB.CargoToDB = _entityCommanding.GetDataBlob<CargoStorageDB>();
            newTransferDB.CargoFromEntity = loadFromEntity;
            newTransferDB.CargoFromDB = loadFromEntity.GetDataBlob<CargoStorageDB>();

            newTransferDB.TotalAmountToTransfer = TotalAmountToTransfer;
            newTransferDB.ItemToTranfer = (ICargoable)game.StaticData.FindDataObjectUsingID(ItemToTransfer);
            _entityCommanding.Manager.SetDataBlob(_entityCommanding.ID, newTransferDB);
            IsRunning = true;
        }


        public bool IsValidCommand(Game game)
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

        public bool IsFinished()
        {
            throw new NotImplementedException();
        }

        public CargoLoadOrder()
        {

        }
    }
}
