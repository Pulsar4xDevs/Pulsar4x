using System;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;


namespace Pulsar4X.Engine.Orders;

public class ShipLogisticsOrders : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.IneteractWithSelf;

    public override bool IsBlocking => false;

    public override string Name {get {return _name;}}
    string _name = "Logisitics";

    public override string Details {get {return _details;}}
    string _details = "";

    internal override Entity EntityCommanding { get { return _entityCommanding; } }
    Entity _entityCommanding;
    Entity _factionEntity;

    LogiShipperDB _logiShipperDB;

    public override bool IsFinished()
    {

        if(_logiShipperDB != null && _entityCommanding.HasDataBlob<LogiShipperDB>())
            return false;
        return true;
    }

    internal override void Execute(DateTime atDateTime)
    {

    }
    public override void UpdateDetailString()
    {
        _details = _logiShipperDB.CurrentState.ToString();
        switch (_logiShipperDB.CurrentState)
        {
            case LogiShipperDB.States.Bidding:
            {
                _details = "Bidding on " + _logiShipperDB.BiddingTasks.Count + " consignments";
            }
                break;
            case LogiShipperDB.States.MoveToSupply:
            {
                _details = "Traveling to Supply to collect goods";
            }
                break;
            case LogiShipperDB.States.Loading:
            {
                _details = "Loading goods ";
            }
                break;
            case LogiShipperDB.States.MoveToDestination:
            {
                _details = "Moving to Destination";
            }
                break;
            case LogiShipperDB.States.Unloading:
            {
                _details = "Unloading goods";
            }
                break;
            case LogiShipperDB.States.ResuplySelf:
            {
                _details = "Refueling";
            }
                break;
            case LogiShipperDB.States.Waiting:
            {
                _details = "Waiting for suply and/or demand";
            }
                break;

            default:
                break;
        }
        //_logiShipperDB.ActiveCargoTasks[0].
        _logiShipperDB.StateString = _details;
    }

    internal override bool IsValidCommand(Game game)
    {
        if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
        {
            _logiShipperDB = _entityCommanding.GetDataBlob<LogiShipperDB>();
            return true;

        }
        return false;
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
}