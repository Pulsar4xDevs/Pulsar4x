using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib;

public class SetLogisticsOrder : EntityCommand
{
    public enum OrderTypes
    {
        RemoveLogiBaseDB,
        SetBaseItems,
        AddLogiShipDB,
        RemoveLogiShipDB,
        SetShipTypeAmounts
    }

    //public SetLogisticsOrder Order;
    private OrderTypes _type;
    public override ActionLaneTypes ActionLanes { get; } = ActionLaneTypes.InstantOrder;

    public override bool IsBlocking { get; } = false;

    public override string Name { get; } = "Set TradeObject";

    public override string Details { get; } = "Set Logi";



    internal override Entity EntityCommanding { get { return _entityCommanding; } }
    Entity _entityCommanding;
    Entity _factionEntity;

    private Dictionary<ICargoable,(int count, int demandSupplyWeight)> _baseChanges;
    private Changes _shipChanges;

    internal override bool IsValidCommand(Game game)
    {

        if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
        {
            if (_type == OrderTypes.SetBaseItems && _entityCommanding.TryGetDatablob<LogiBaseDB>(out LogiBaseDB lbdb))
            {
                if(lbdb.ListedItems.Count >= _baseChanges.Count)
                    return true;
                return false;
            }

            return true;


        }
        return false;
    }

    public static void CreateCommand(Entity entity, OrderTypes ordertype )
    {
        SetLogisticsOrder cmd = new SetLogisticsOrder()
        {
            EntityCommandingGuid = entity.Guid,
            RequestingFactionGuid = entity.FactionOwnerID,
            _type = ordertype
        };

        StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
    }

    public static void CreateCommand_SetBaseItems(Entity entity, Dictionary<ICargoable,(int count, int demandSupplyWeight)> changes )
    {
        SetLogisticsOrder cmd = new SetLogisticsOrder()
        {
            EntityCommandingGuid = entity.Guid,
            RequestingFactionGuid = entity.FactionOwnerID,
            _type = OrderTypes.SetBaseItems,
            _baseChanges = changes
        };

        StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
    }

    public class Changes//maybe should be a struct, but would need to not use a dictionary and need to check mutability.
    {
        public Dictionary<Guid, double> VolumeAmounts;
        public int MaxMass;

        public Changes()
        {
            VolumeAmounts = new Dictionary<Guid, double>();
            MaxMass = 0;
        }
    }

    public static void CreateCommand_SetShipTypeAmounts(Entity entity, Changes changes )
    {

        SetLogisticsOrder cmd = new SetLogisticsOrder();
        cmd.EntityCommandingGuid = entity.Guid;
        cmd.RequestingFactionGuid = entity.FactionOwnerID;
        cmd._type = OrderTypes.SetShipTypeAmounts;
        cmd._shipChanges = changes;

        StaticRefLib.Game.OrderHandler.HandleOrder(cmd);

        StaticRefLib.ProcessorManager.GetProcessor<LogiShipperDB>().ProcessEntity(entity, 0);
        StaticRefLib.ProcessorManager.GetProcessor<LogiBaseDB>().ProcessManager(entity.Manager, 0);
        cmd.UpdateDetailString();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (!IsRunning)
        {
            IsRunning = true;
            switch (_type)
            {
                case OrderTypes.SetBaseItems:
                {

                    var db = EntityCommanding.GetDataBlob<LogiBaseDB>();
                    foreach (var item in _baseChanges)
                    {
                        if (item.Value.count == 0)
                            db.ListedItems.Remove(item.Key);
                        else
                            db.ListedItems[item.Key] = item.Value;
                    }
                    //NOTE: possibly some conflict here.
                    //we might need to consider what to do if a ship is already contracted to grab stuff,
                    //and then we change this and remove the items before the ship has collected them.

                    break;
                }


                case OrderTypes.AddLogiShipDB:
                {
                    var db = new LogiShipperDB();
                    EntityCommanding.SetDataBlob(db);
                    break;
                }
                case OrderTypes.RemoveLogiShipDB:
                {
                    EntityCommanding.RemoveDataBlob<LogiShipperDB>();
                    break;
                }
                case OrderTypes.SetShipTypeAmounts:
                {
                    var db = EntityCommanding.GetDataBlob<LogiShipperDB>();
                    foreach (var item in _shipChanges.VolumeAmounts)
                    {
                        db.TradeSpace[item.Key] = item.Value;
                    }

                    db.MaxTradeMass = _shipChanges.MaxMass;

                    break;
                }

            }
        }
    }

    public override bool IsFinished()
    {
        return true;
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
}