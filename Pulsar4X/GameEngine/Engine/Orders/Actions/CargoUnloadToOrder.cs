using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Orders
{
    public class CargoUnloadToOrder : EntityCommand
    {
        public List<(string ID, long amount)> ItemsGuidsToTransfer;

        [JsonIgnore]
        public List<(ICargoable item, long amount)> ItemICargoablesToTransfer = new List<(ICargoable item, long amount)>();

        public string SendCargoToEntityGuid { get; set; }

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

        public override bool IsBlocking => true;

        public override string Name { get; } = "Cargo Transfer";

        public override string Details
        {
            get
            {
                string fromEntityName = _entityCommanding.GetDataBlob<NameDB>().GetName(factionEntity);
                string toEntityName = sendToEntity.GetDataBlob<NameDB>().GetName(factionEntity);
                string detailStr = "From " + fromEntityName + " To " + toEntityName;
                return detailStr;
            }
        }

        Entity _entityCommanding;

        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private CargoTransferDB _cargoTransferDB;

        [JsonIgnore]
        Entity factionEntity;

        [JsonIgnore]
        Entity sendToEntity;

        public static void CreateCommand(string faction, Entity cargoFromEntity, Entity cargoToEntity, List<(ICargoable item, long amount)> itemsToMove )
        {
            List<(string item, long amount)> itemGuidAmounts = new List<(string, long)>();
            foreach (var tup in itemsToMove)
            {
                itemGuidAmounts.Add((tup.item.UniqueID, tup.amount));
            }

            var cmd = new CargoUnloadToOrder()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = cargoFromEntity.Guid,
                CreatedDate = cargoFromEntity.Manager.ManagerSubpulses.StarSysDateTime,
                SendCargoToEntityGuid = cargoToEntity.Guid,
                ItemsGuidsToTransfer = itemGuidAmounts,
                ItemICargoablesToTransfer = itemsToMove
            };

            cargoFromEntity.Manager.Game.OrderHandler.HandleOrder(cmd);
        }

        /// <summary>
        /// Validates and actions the command.
        /// may eventualy need to return a responce instead of void.
        /// This creates a CargoTranferDB from the command, which does all the work.
        /// the command is to create and enqueue a CargoTransferDB.
        /// </summary>
        internal override void Execute(DateTime atDateTime)
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
                CargoTransferProcessor.SetTransferRate(EntityCommanding, _cargoTransferDB);
                IsRunning = true;
            }
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

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }
}