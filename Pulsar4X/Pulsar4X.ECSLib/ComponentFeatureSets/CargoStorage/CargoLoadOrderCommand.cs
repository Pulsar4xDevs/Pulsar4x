using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoLoadOrder:IEntityCommand
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
        public Guid TargetEntityGuid { get; set; }

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

        [JsonIgnore]
        Entity targetEntity;
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
        public bool ActionCommand(Game game)
        {
            if(IsCargoOrderValid(game.GlobalManager)) {
                CargoTransferDB newTransferDB = new CargoTransferDB();
                newTransferDB.CargoToEntity = targetEntity;
                newTransferDB.CargoToDB = targetEntity.GetDataBlob<CargoStorageDB>();
                newTransferDB.CargoFromEntity = loadFromEntity;
                newTransferDB.CargoFromDB = loadFromEntity.GetDataBlob<CargoStorageDB>();

                newTransferDB.TotalAmountToTransfer = TotalAmountToTransfer;
                newTransferDB.ItemToTranfer = (ICargoable)game.StaticData.FindDataObjectUsingID(ItemToTransfer);
                targetEntity.Manager.SetDataBlob(targetEntity.ID, newTransferDB);
                return true;
            }
            return false;
        }

        private bool IsCargoOrderValid(EntityManager globalManager)
        {
            if(CommandHelpers.IsCommandValid(globalManager, RequestingFactionGuid, TargetEntityGuid, out factionEntity, out targetEntity)) {
                if(globalManager.TryGetEntityByGuid(LoadCargoFromEntityGuid, out loadFromEntity)) {
                    return true;
                }
            }
            return false;
        }

        public CargoLoadOrder()
        {

        }
    }
}
