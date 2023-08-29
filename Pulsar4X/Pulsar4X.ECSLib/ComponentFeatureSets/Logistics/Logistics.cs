using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
namespace Pulsar4X.ECSLib
{
    
    public struct ManuverState
    {
        public DateTime At;
        public double Mass;
        public Vector3 Position;
        public Vector3 Velocity;
    }

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

        public Guid From; 
        public List<(ICargoable item, int count)>  ItemsToShip =  new List<(ICargoable item, int count)>();
        public Dictionary<Guid, double>  TradeSpace =  new Dictionary<Guid, double>();
        public double MaxTradeMass = 1;
        public Guid To; 

        public States CurrentState = States.Waiting;
        public List<LogisticsCycle.CargoTask> BiddingTasks = new List<LogisticsCycle.CargoTask>();
        public List<LogisticsCycle.CargoTask> ActiveCargoTasks = new List<LogisticsCycle.CargoTask>();

        public LogiShipperDB()
        {


        }

        internal override void OnSetToEntity()
        {
            var cdb = base.OwningEntity.GetDataBlob<VolumeStorageDB>();
            TradeSpace = new Dictionary<Guid, double>();
            foreach(var kvp in cdb.TypeStores)
            {
                TradeSpace.Add(kvp.Key, 0);
            }
            if(!OwningEntity.HasDataBlob<NewtonThrustAbilityDB>())
                throw new Exception("Non moving entites can't be shippers");
            if(OwningEntity.HasDataBlob<OrderableDB>())
            {
                var order = new ShipLogisticsOrders()
                {
                    EntityCommandingGuid = OwningEntity.Guid,
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
            TradeSpace = new Dictionary<Guid, double>(db.TradeSpace);
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