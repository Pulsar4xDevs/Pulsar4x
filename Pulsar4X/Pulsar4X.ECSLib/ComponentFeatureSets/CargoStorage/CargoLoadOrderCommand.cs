using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoXferOrder:EntityCommand
    {
        public List<(Guid ID, long amount)> ItemsGuidsToTransfer;
        [JsonIgnore]
        public List<(ICargoable item, long amount)> ItemICargoablesToTransfer = new List<(ICargoable item, long amount)>();
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

        public static void CreateCommand(Game game, Entity faction, Entity cargoFromEntity, Entity cargoToEntity, List<(Guid ID, long amount)> itemsToMove )
        {
            var cmd = new CargoXferOrder()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = cargoFromEntity.Guid,
                CreatedDate = cargoFromEntity.Manager.ManagerSubpulses.StarSysDateTime,
                SendCargoToEntityGuid = cargoToEntity.Guid,
                ItemsGuidsToTransfer = itemsToMove
            };
            game.OrderHandler.HandleOrder(cmd);
        }
        public static void CreateCommand(Game game, Entity faction, Entity cargoFromEntity, Entity cargoToEntity, List<(ICargoable item, long amount)> itemsToMove )
        {
            List<(Guid item,long amount)> itemGuidAmounts = new List<(Guid, long)>();
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
                ItemsGuidsToTransfer = itemGuidAmounts
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
                _cargoTransferDB.CargoToDB = sendToEntity.GetDataBlob<CargoStorageDB>();
                _cargoTransferDB.CargoFromEntity = EntityCommanding;
                _cargoTransferDB.CargoFromDB = EntityCommanding.GetDataBlob<CargoStorageDB>();

                _cargoTransferDB.ItemsLeftToTransfer = ItemICargoablesToTransfer;
                _cargoTransferDB.OrderedToTransfer = ItemICargoablesToTransfer;

                EntityCommanding.Manager.SetDataBlob(EntityCommanding.ID, _cargoTransferDB);
                CargoTransferProcessor.FirstRun(EntityCommanding);
                IsRunning = true;
            }
        }

        private void GetItemsToTransfer(StaticDataStore staticData)
        {
            foreach (var tup in ItemsGuidsToTransfer)
            {
                //(ICargoable)game.StaticData.FindDataObjectUsingID(ItemToTransfer);
                ItemICargoablesToTransfer.Add((staticData.GetICargoable(tup.ID), tup.amount));
            }
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.FindEntityByGuid(SendCargoToEntityGuid, out sendToEntity))
                {
                    GetItemsToTransfer(game.StaticData);//should I try catch this? nah it's unlikely to be bad here. 
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
