using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Names;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Ships;


namespace Pulsar4X.Storage;

public class CargoTransferOrder : EntityCommand
{
    public enum Conditionals
    {
        TakeAvailibleAtOrder,
        WaitTillFull,
        WailTillEmpty,
        TakeAvailible
        
    }
    [JsonProperty]
    public Conditionals Condition {get; private set;} = Conditionals.TakeAvailibleAtOrder;
    [JsonProperty]
    public bool IsPrimaryEntity { get; private set; }

    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name { get; } = "Cargo Transfer";

    public override string Details
    {
        get
        {
            string otherEntity;
            if(_entityCommanding == TransferData.PrimaryEntity)
                otherEntity = TransferData.SecondaryEntity.GetName(RequestingFactionGuid);
            else
                otherEntity = TransferData.PrimaryEntity.GetName(RequestingFactionGuid);
            string detailStr = "With " + otherEntity + ".";
            if (!IsRunning)
                detailStr += " Waiting to start";
            else
            {
                detailStr += " Transfering, " + Stringify.Quantity(AmountLeftToXfer(), "#.#") + " remaining." ;
            }
            return detailStr;
        }
    }

    Entity _entityCommanding;

    internal override Entity EntityCommanding { get { return _entityCommanding; } }

    private CargoTransferDataDB TransferData { get; }


    

    private CargoTransferOrder(CargoTransferDataDB transferData)
    {
        TransferData = transferData;
    }
    
    public static void CreateCommands(int faction, Entity primaryEntity, Entity secondaryEntity, List<(ICargoable item, long amount)> itemsToMove )
    {
        CargoTransferDataDB cargoData = new(primaryEntity, secondaryEntity, itemsToMove);
        var cmd1 = new CargoTransferOrder(cargoData)
        {
            RequestingFactionGuid = faction,
            EntityCommandingGuid = primaryEntity.Id,
            CreatedDate = primaryEntity.Manager.ManagerSubpulses.StarSysDateTime,
            IsPrimaryEntity = true,
        };
        primaryEntity.Manager.Game.OrderHandler.HandleOrder(cmd1);
        
        var cmd2 = new CargoTransferOrder(cargoData)
        {
            RequestingFactionGuid = faction,
            EntityCommandingGuid = secondaryEntity.Id,
            CreatedDate = primaryEntity.Manager.ManagerSubpulses.StarSysDateTime,
            IsPrimaryEntity = false
        };
        secondaryEntity.Manager.Game.OrderHandler.HandleOrder(cmd2);
    }
    
    /// <summary>
    /// Single item conditional order.
    /// Assumes transfer from secondary to primary
    /// </summary>
    /// <param name="faction"></param>
    /// <param name="primaryEntity"></param>
    /// <param name="secondaryEntity"></param>
    /// <param name="item"></param>
    /// <param name="condition"></param>
    public static void CreateCommands(int faction, Entity primaryEntity, Entity secondaryEntity, ICargoable item,  Conditionals condition )
    {
        long amount = 0;
        if (condition == Conditionals.WaitTillFull)
        {
            amount = CargoMath.GetFreeUnitSpace(primaryEntity.GetDataBlob<CargoStorageDB>(), item);
        }

        List<(ICargoable item, long amount)> itemList = new List<(ICargoable item, long amount)>();
        itemList.Add((item, amount));
        CargoTransferDataDB cargoData = new(primaryEntity, secondaryEntity, itemList);
        
        var cmd1 = new CargoTransferOrder(cargoData)
        {
            RequestingFactionGuid = faction,
            EntityCommandingGuid = primaryEntity.Id,
            CreatedDate = primaryEntity.Manager.ManagerSubpulses.StarSysDateTime,
            IsPrimaryEntity = true,
            Condition = condition
        };
        primaryEntity.Manager.Game.OrderHandler.HandleOrder(cmd1);
        
        var cmd2 = new CargoTransferOrder(cargoData)
        {
            RequestingFactionGuid = faction,
            EntityCommandingGuid = secondaryEntity.Id,
            CreatedDate = primaryEntity.Manager.ManagerSubpulses.StarSysDateTime,
            IsPrimaryEntity = false,
            Condition = condition
        };
        secondaryEntity.Manager.Game.OrderHandler.HandleOrder(cmd2);
    }

    public static void CreateRefuelFleetCommand(Entity cargoFromEntity, Entity fleet)
    {
        var fleetOwner = fleet.GetFactionOwner;
        var cargoLibrary = fleetOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
        if(fleet.TryGetDatablob<FleetDB>(out var fleetDB))
        {
            var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());

            foreach (var ship in ships)
            {
                var fuelInfo = ship.GetFuelInfo(cargoLibrary);
                ICargoable fuel = fuelInfo.Item1;
                long amountToMove = ship.GetDataBlob<CargoStorageDB>().GetFreeUnitSpace(fuel);
                var fuelAndAmount =(fuel, amountToMove);
                var list = new List<(ICargoable, long)>();
                list.Add(fuelAndAmount);

                CreateCommands(fleet.FactionOwnerID, ship, cargoFromEntity,  fuel, Conditionals.WaitTillFull);
            }
        }
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
            CargoTransferDB transferDB = new CargoTransferDB(TransferData);
            transferDB.ParentStorageDB = EntityCommanding.GetDataBlob<CargoStorageDB>();
            EntityCommanding.SetDataBlob(transferDB);
            IsRunning = true;
        }
    }

    internal override bool IsValidCommand(Game game)
    {
        if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out var factionEntity, out _entityCommanding))
        {
            return true;
        }
        return false;
    }

    internal override bool IsFinished()
    {
        if(!IsRunning)
            return _isFinished = false;
        
        switch (Condition)
        {
            case Conditionals.TakeAvailibleAtOrder:
            {
                if (AmountLeftToXfer() > 0)
                    _isFinished = false;
                else
                    _isFinished = true;
                break;
            }
            case Conditionals.WaitTillFull:
            {
                if (AmountLeftToXfer() > 0)
                    _isFinished = false;
                else //if we've transfered everything from the inital order, check if we can fit more
                {
                    for (int index = 0; index < TransferData.OrderedToTransfer.Count; index++)
                    {
                        (ICargoable item, long amount) tup = TransferData.OrderedToTransfer[index];
                        var amount = CargoMath.GetFreeUnitSpace(TransferData.PrimaryStorageDB, tup.item);
                        TransferData.UpdateEscro(tup.item, amount);
                    }
                    if (AmountLeftToXfer() > 0)
                        _isFinished = false;
                    else
                        _isFinished = true;
                }
                break;
            }
            case Conditionals.WailTillEmpty:
                throw new NotImplementedException();
                break;
            case Conditionals.TakeAvailible:
                throw new NotImplementedException();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        if(_isFinished)
        {
            //TransferData.PrimaryStorageDB.EscroItems.Remove(TransferData);
            //TransferData.SecondaryStorageDB.EscroItems.Remove(TransferData);
            _entityCommanding.GetDataBlob<CargoStorageDB>().EscroItems.Remove(TransferData);
            _entityCommanding.RemoveDataBlob<CargoTransferDB>();
        }
        return _isFinished;
    }

    long AmountLeftToXfer()
    {
        long amount = 0;
        foreach (var tup in TransferData.EscroHeldInPrimary)
        {
            amount += tup.count;
        }
        foreach (var tup in TransferData.EscroHeldInSecondary)
        {
            amount += tup.count;
        }
        return amount;
    }
    
    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
    
}