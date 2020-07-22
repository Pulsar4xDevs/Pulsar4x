using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoXferOrder:EntityCommand
    {
        public List<(Guid ID, int amount)> ItemsGuidsToTransfer;
        [JsonIgnore]
        public List<(ICargoable item, int amount)> ItemICargoablesToTransfer = new List<(ICargoable item, int amount)>();
        public Guid SendCargoToEntityGuid { get; set; }

        public override int ActionLanes => 1;
        public override bool IsBlocking => true;

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private CargoTransferDB _cargoTransferDB;

        [JsonIgnore]
        Entity factionEntity;
        [JsonIgnore]
        Entity sendToEntity;
        
        public static void CreateCommand(Game game, Entity faction, Entity cargoFromEntity, Entity cargoToEntity, List<(ICargoable item, int amount)> itemsToMove )
        {
            List<(Guid item,int amount)> itemGuidAmounts = new List<(Guid, int)>();
            foreach (var tup in itemsToMove)
            {
                itemGuidAmounts.Add((tup.item.ID, tup.amount));
            }
            var cmd = new CargoXferOrder()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = cargoFromEntity.Guid,
                CreatedDate = cargoFromEntity.Manager.ManagerSubpulses.StarSysDateTime,
                SendCargoToEntityGuid = cargoToEntity.Guid,
                ItemsGuidsToTransfer = itemGuidAmounts,
                ItemICargoablesToTransfer = itemsToMove
            };
            game.OrderHandler.HandleOrder(cmd);
        }

        /// <summary>
        /// Validates and actions the command. 
        /// may eventualy need to return a responce instead of void. 
        /// This creates a CargoTranferDB from the command, which does all the work.
        /// the command is to create and enqueue a CargoTransferDB.
        /// </summary>
        internal override void ActionCommand(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                _cargoTransferDB = new CargoTransferDB();
                _cargoTransferDB.CargoToEntity = sendToEntity;
                _cargoTransferDB.CargoToDB = sendToEntity.GetDataBlob<VolumeStorageDB>();
                _cargoTransferDB.CargoFromEntity = EntityCommanding;
                _cargoTransferDB.CargoFromDB = EntityCommanding.GetDataBlob<VolumeStorageDB>();

                _cargoTransferDB.ItemsLeftToTransfer = ItemICargoablesToTransfer;
                _cargoTransferDB.OrderedToTransfer = ItemICargoablesToTransfer;

                EntityCommanding.Manager.SetDataBlob(EntityCommanding.ID, _cargoTransferDB);
                CargoTransferProcessor.FirstRun(EntityCommanding);
                IsRunning = true;
            }
        }

        private void GetItemsToTransfer(StaticDataStore staticData)
        {

        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.FindEntityByGuid(SendCargoToEntityGuid, out sendToEntity))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool IsFinished()
        {
            if (_cargoTransferDB == null)
                return false;
            if (AmountLeftToXfer() > 0)
                return false;
            else
                return true;
        }
        long AmountLeftToXfer()
        {
            long amount = 0;
            foreach (var tup in _cargoTransferDB.ItemsLeftToTransfer)
            {
                amount += tup.Item2;
            }
            return amount;
         }

        //public CargoXferOrder(Entity entityCommanding, Entity loadFromEntity, List<Tuple<ID,long>> typesAndAmounts )
        //{

        //}
    }
}
