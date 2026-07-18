using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Datablobs;
using Pulsar4X.Movement;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics
{
    public class LogiShipperDB : BaseDataBlob
    {
        public enum States
        {
            Bidding,
            MoveToSupply,
            Loading,
            MoveToDestination,
            Unloading,
            ResuplySelf,
            Waiting
        }

        public string StateString = "";

        public string From;
        public List<(ICargoable item, int count)>  ItemsToShip =  new List<(ICargoable item, int count)>();
        public Dictionary<string, double>  TradeSpace =  new Dictionary<string, double>();
        public double MaxTradeMass = 1;
        public string To;

        public States CurrentState = States.Waiting;
        public List<LogisticsCycle.CargoTask> BiddingTasks = new List<LogisticsCycle.CargoTask>();
        public List<LogisticsCycle.CargoTask> ActiveCargoTasks = new List<LogisticsCycle.CargoTask>();

        public LogiShipperDB()
        {


        }

        internal override void OnSetToEntity()
        {
            var cdb = base.OwningEntity.GetDataBlob<CargoStorageDB>();
            TradeSpace = new Dictionary<string, double>();
            foreach(var kvp in cdb.TypeStores)
            {
                TradeSpace.Add(kvp.Key, 0);
            }
            if(!OwningEntity.HasDataBlob<NewtonThrustAbilityDB>())
                throw new Exception("Non moving entites can't be shippers");
            if(OwningEntity.HasDataBlob<ActionQueueDB>())
            {
                var order = new ShipLogisticsOrders()
                {
                    EntityCommandingGuid = OwningEntity.Id,
                    RequestingFactionGuid = OwningEntity.FactionOwnerID,
                };
                //StaticRefLib.Game.OrderHandler.HandleOrder(order);
            }
        }

        private LogiShipperDB(LogiShipperDB db)
        {
            StateString = db.StateString;
            From = db.From;
            ItemsToShip = new List<(ICargoable item, int count)>(db.ItemsToShip);
            TradeSpace = new Dictionary<string, double>(db.TradeSpace);
            MaxTradeMass = db.MaxTradeMass;
            To = db.To;
            CurrentState = db.CurrentState;
            BiddingTasks = new List<LogisticsCycle.CargoTask>(db.BiddingTasks);
            ActiveCargoTasks = new List<LogisticsCycle.CargoTask>(db.ActiveCargoTasks);
        }
        public override object Clone()
        {
            return new LogiShipperDB(this);
        }
    }
}