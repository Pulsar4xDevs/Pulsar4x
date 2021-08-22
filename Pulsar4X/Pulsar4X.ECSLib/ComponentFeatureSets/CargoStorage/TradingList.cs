using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on how an entitiy can be stored.
    /// NOTE an entity with this datablob must also have a MassVolumeDB
    /// </summary>
    public class TradingBaseDB : BaseDataBlob 
    {

        public Dictionary<ICargoable,(int count, int demandSupplyWeight)> ListedItems = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>(); 
    
        public Dictionary<ICargoable,(Guid shipingEntity, double amountVolume)> ItemsWaitingPickup = new Dictionary<ICargoable, (Guid shipingEntity, double amountVolume)>();
        public Dictionary<ICargoable,(Guid shipingEntity, double amountVolume)> ItemsInTransit = new Dictionary<ICargoable, (Guid shipingEntity, double amountVolume)>();

        public List<(Entity ship, TradeCycle.CargoTask cargoTask)> TradeShipBids = new List<(Entity ship, TradeCycle.CargoTask cargoTask)>();

        public TradingBaseDB()
        {

        }

        internal override void OnSetToEntity()
        {
            
        }
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class TradingShipperDB : BaseDataBlob
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

        public Guid From; 
        public List<(ICargoable item, int count)>  ItemsToShip =  new List<(ICargoable item, int count)>();
        public Dictionary<Guid, double>  TradeSpace =  new Dictionary<Guid, double>();
        public Guid To; 

        public States CurrentState = States.Waiting;

        public List<TradeCycle.CargoTask> ActiveCargoTasks = new List<TradeCycle.CargoTask>();

        public TradingShipperDB()
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
        }
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }



    public class LogisticsShipProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency
        {
            get { return TimeSpan.FromHours(1); }
        }

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(4);

        public Type GetParameterType => typeof(TradingShipperDB);

        public void Init(Game game)
        {
            
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            throw new NotImplementedException();
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var tradingBases = manager.GetAllDataBlobsOfType<TradingBaseDB>();
            var shippingEntities = manager.GetAllEntitiesWithDataBlob<TradingShipperDB>();
            foreach(Entity shipper in shippingEntities)
            {
               TradeCycle.ShipBidding(shipper, tradingBases);
            }
        }
    }


    public class LogisticsBaseProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency
        {
            get { return TimeSpan.FromHours(1); }
        }

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(4.25);

        public Type GetParameterType => typeof(TradingBaseDB);

        public void Init(Game game)
        {
            
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            throw new NotImplementedException();
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var tradingBases = manager.GetAllDataBlobsOfType<TradingBaseDB>();
            var shippingEntities = manager.GetAllEntitiesWithDataBlob<TradingShipperDB>();
            foreach(TradingBaseDB tradeBase in tradingBases)
            {
                TradeCycle.TradeBaseBidding(tradeBase);
            }
        }
    }
    public static class TradeCycle
    {
        public static List<Entity> ShippingEntites;


        public class CargoTask
        {
            public double Profit = 0;
            public Entity Source;
            public Entity Destination;

            public ICargoable item;

            public long NumberOfItems;
            public double timeInSeconds;
            public double fuelUseDV;
        }


        public static void ShipBidding(Entity shippingEntity, List<TradingBaseDB> tradingBases)
        {
            
            DateTime currentDateTime = shippingEntity.StarSysDateTime;
            TradingShipperDB shiperdb = shippingEntity.GetDataBlob<TradingShipperDB>();
            List<CargoTask> cargoTasks = new List<CargoTask>();
            if(shiperdb.CurrentState != TradingShipperDB.States.Waiting)
                return;
            shiperdb.CurrentState = TradingShipperDB.States.Bidding;

            double fuelCost = 1; //we need to work out how much fuel costs from the economy at some point. 
            double timeCost = 1; //per second. the ship will need to work this out for itself, should include maintanance, amount of crew etc. 

            var sourceTradeItems = new List<CargoTask>(); 
            var demandTradeItems = new List<CargoTask>();

            foreach(var tbase in tradingBases)
            {
                //tbase.OwningEntity.GetAbsoluteFuturePosition
                OrbitDB odb;// = tbase.OwningEntity.GetDataBlob<OrbitDB>();
                if(tbase.OwningEntity.HasDataBlob<OrbitDB>())
                     odb = tbase.OwningEntity.GetDataBlob<OrbitDB>();
                else
                    odb = tbase.OwningEntity.GetSOIParentEntity().GetDataBlob<OrbitDB>();
                    //throw new NotImplementedException("Currently we can only predict the movement of stable orbits - target must have an orbitDB");
                (Vector3 position, DateTime atDateTime) sourceIntercept = OrbitProcessor.GetInterceptPosition_m
                (
                    shippingEntity, 
                    odb, 
                    currentDateTime
                );

                double fuelToSource = 1; //todo calcualte actual amount of fuel to get there. this is going to be orbital manuver fuel since warp just uses power...
                double travelTimeToSource = (shippingEntity.StarSysDateTime - sourceIntercept.atDateTime).TotalSeconds;
                

                // var hohmann = InterceptCalcs.Hohmann(sgp, r1, r2); //need to calculate the ideal ranges 
                // var dvDif = hohmann[0].deltaV.Length() + hohmann[1].deltaV.Length();
                
                double loadtime = 3600;
                
                double unloadTime = 3600;

                double profit = 0; //this is used by the ship to decide where it should source and destination from. 

                foreach(var cargoSpace in shiperdb.TradeSpace) //for each type of cargospace
                {
                    double possibleTradeVolume = cargoSpace.Value;

                    //cycle through the base items and add them to the above dictionary if we can carry them
                    foreach(var tradingItems in tbase.ListedItems)
                    {
                        if(tradingItems.Key.CargoTypeID == cargoSpace.Key)
                        {
                            //we abs this since if it's demand, it'll be negative. we want to find how much to carry.
                            
                            var volCanCarry = Math.Min(possibleTradeVolume, Math.Abs(tradingItems.Value.count * tradingItems.Key.VolumePerUnit));
                            long numCanCarry =  (long)(volCanCarry / tradingItems.Key.VolumePerUnit);
                            var ct = new CargoTask();
                            
                            ct.item = tradingItems.Key;
                            ct.NumberOfItems = numCanCarry;
                            ct.Profit = tradingItems.Value.demandSupplyWeight;
                            if(tradingItems.Value.count > 0) //if it's a supply (we ship FROM)
                            {

                                //var loadRate = CargoTransferProcessor.CalcTransferRate(dvDif, tbase.OwningEntity.GetDataBlob<VolumeStorageDB>(), shiperdb.OwningEntity.GetDataBlob<VolumeStorageDB>())
                                ct.Source = tbase.OwningEntity;
                                sourceTradeItems.Add(ct);
                                ct.timeInSeconds = (currentDateTime - sourceIntercept.atDateTime).TotalSeconds;
                                ct.fuelUseDV = fuelToSource;
                            }
                            else //it's a demand item. (we ship TO)
                            {
                                //var unloadRate = CargoTransferProcessor.CalcTransferRate(dvDif, shiperdb.OwningEntity.GetDataBlob<VolumeStorageDB>(), tbase.OwningEntity.GetDataBlob<VolumeStorageDB>())
                
                                ct.Destination = tbase.OwningEntity;
                                demandTradeItems.Add(ct);
                            }
                        }
                    }
                }

            }

            //now we need to compare the source and demand lists and find the most profitable combinations.

            List<CargoTask> possibleCombos = new List<CargoTask>();
            foreach(var stask in sourceTradeItems)
            {
                foreach(var dtask in demandTradeItems)
                {
                    var sourceEntity = stask.Source;
                    var destinEntity = dtask.Destination;

                    var pcombo = new CargoTask();
                    
                    pcombo.Source = sourceEntity;
                    pcombo.Destination = destinEntity;
                    pcombo.item = stask.item;
                    pcombo.NumberOfItems = Math.Min(stask.NumberOfItems, dtask.NumberOfItems);

                    DateTime arriveSourceTime = currentDateTime + TimeSpan.FromSeconds(stask.timeInSeconds);
                    TimeSpan loadTime = TimeSpan.FromSeconds(3600); //TODO: calculate this properly
                    DateTime DepartSourceTime = arriveSourceTime + loadTime;

                    OrbitDB odb;// = tbase.OwningEntity.GetDataBlob<OrbitDB>();
                    if(destinEntity.HasDataBlob<OrbitDB>())
                        odb = destinEntity.GetDataBlob<OrbitDB>();
                    else
                        odb = destinEntity.GetSOIParentEntity().GetDataBlob<OrbitDB>();
                        //throw new NotImplementedException("Currently we can only predict the movement of stable orbits - target must have an orbitDB");
                    (Vector3 position, DateTime atDateTime) targetIntercept = OrbitProcessor.GetInterceptPosition_m
                    (
                        shippingEntity, 
                        odb, 
                        DepartSourceTime
                    );

                    double timeFromSourceToDestination = (targetIntercept.atDateTime - currentDateTime).TotalSeconds;

                    double fuelToDestDV = 1; //TODO calculate amount of fuel required for manuvers for orbital manuvers.
                    double fuelToSourceDV = stask.fuelUseDV;


                    double unloadTime = 36000; //TODO: calculate this.

                    double totalTimeInSeconds = stask.timeInSeconds + timeFromSourceToDestination + unloadTime;
                    
                    double totalTimeCost = timeCost * totalTimeInSeconds;
                    double totalFuelCost = fuelToDestDV + fuelToSourceDV * fuelCost;

                    //stask.profit should be a negitive number, dtask.profit should be positive, and for the trade to be profitable more than the others.
                    double totalcosts = stask.Profit - (totalTimeCost + totalFuelCost);
                    pcombo.Profit = dtask.Profit - totalcosts;
                    
                    pcombo.timeInSeconds = totalTimeInSeconds;

                    if(possibleCombos.Count < 1)
                    {
                        possibleCombos.Add(pcombo);
                    }
                    else //add to the list in order, the list *should* be small enough that this won't be inefficent... I think...
                    {
                        for (int i = possibleCombos.Count - 1; i >= 0; i--)
                        {
                            var listItem = possibleCombos[i];
                            if(listItem.Profit > pcombo.Profit || i == 0)
                            {
                                possibleCombos.Insert(i, pcombo);
                                i = -1;
                            }
                        }
                    }
                }
            }



            //I could maybe just remove the possibleCombos list and go straight to the tradebase TradeShipBids list. however it might be usefull for now to see the full possilbeCombos list. 
            //now we have a list of potential trade routes this ship is capable of doing, we need to bid for them:
            //var minimumProfit = 0;

            foreach (var pcombo in possibleCombos)
            {
                var tbdb = pcombo.Destination.GetDataBlob<TradingBaseDB>(); //we're going to add it to the *DESTINATON* base
                
                if(tbdb.TradeShipBids.Count < 1)
                {
                    tbdb.TradeShipBids.Add((shippingEntity, pcombo));
                }
                else //add to the list in order of the shortest time, the list *should* be small enough that this won't be inefficent... I think...
                { //note! TODO: this needs to incorperate volume somehow, ie more volume on a slightly slower transport shouldn't be penalised too much, but maybe that will come into the profit side fo things
                    for (int i = tbdb.TradeShipBids.Count - 1; i >= 0; i--)
                    {
                        var listItem = tbdb.TradeShipBids[i];
                        if(listItem.cargoTask.timeInSeconds > pcombo.timeInSeconds || i == 0) //TODONEXT: this needs to be largest to smallest not smallest to largest
                        {
                            tbdb.TradeShipBids.Insert(i, (shippingEntity, pcombo));
                            i = -1;
                        }
                    }
                }                
            }

            
        }
    
        public static void TradeBaseBidding(TradingBaseDB tradeBase)
        {
            if (tradeBase.TradeShipBids.Count == 0)
                return;
            int last = tradeBase.TradeShipBids.Count -1;
            var cargoTask = tradeBase.TradeShipBids[last].cargoTask;
            var ship = tradeBase.TradeShipBids[last].ship;
            var shiptradedb = ship.GetDataBlob<TradingShipperDB>();
            Entity source = cargoTask.Source;
            Entity destin = cargoTask.Destination;

            for (int i = last; i > -1 ; i--)
            {
                var bidTask = tradeBase.TradeShipBids[i];
                if(bidTask.ship == ship && bidTask.cargoTask.Source == source && bidTask.cargoTask.Destination == destin) //if the ship and the source are the same (it should be for the first one)
                {
                    
                    var freespace = shiptradedb.TradeSpace[cargoTask.item.CargoTypeID];
                    var volOfItems = cargoTask.NumberOfItems * cargoTask.item.VolumePerUnit;
                    if(freespace >= volOfItems)
                    {
                        tradeBase.TradeShipBids.RemoveAt(i); //remove it from the list
                        shiptradedb.ActiveCargoTasks.Add(bidTask.cargoTask);
                        shiptradedb.TradeSpace[cargoTask.item.CargoTypeID]-= cargoTask.NumberOfItems;
                        List<(ICargoable, long)> tradeItems = new List<(ICargoable, long)>();
                        tradeItems.Add((cargoTask.item, cargoTask.NumberOfItems));

                        var shipOwner = ship.FactionOwner;//.GetDataBlob<ObjectOwnershipDB>().OwningEntity;
                        //StaticRefLib.
                        //moveto source order.
                        var dvdif =  CargoTransferProcessor.CalcDVDifference_m(source, ship);
                        var cargoDBLeft = source.GetDataBlob<VolumeStorageDB>();
                        var cargoDBRight = destin.GetDataBlob<VolumeStorageDB>();
                        var dvMaxRangeDiff_ms = Math.Max(cargoDBLeft.TransferRangeDv_mps, cargoDBRight.TransferRangeDv_mps);

                        var myMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
                        var parentMass = source.GetDataBlob<MassVolumeDB>().MassTotal;
                        var sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, parentMass);

                        if(source.HasDataBlob<ColonyInfoDB>())
                        { 
                            
                            if (dvdif > dvMaxRangeDiff_ms * 0.01)//fix, whats the best dv dif for a colony on a planet?
                            {
                                Entity target = source.GetSOIParentEntity();
                                double mySMA = 0;
                                if (ship.HasDataBlob<OrbitDB>())
                                    mySMA = ship.GetDataBlob<OrbitDB>().SemiMajorAxis;
                                if (ship.HasDataBlob<OrbitUpdateOftenDB>())
                                    mySMA = ship.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
                                if (ship.HasDataBlob<NewtonMoveDB>())
                                    mySMA = ship.GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;

                                double targetSMA = 0;
                                if (target.HasDataBlob<OrbitDB>())
                                    targetSMA = target.GetDataBlob<OrbitDB>().SemiMajorAxis;
                                if (target.HasDataBlob<OrbitUpdateOftenDB>())
                                    targetSMA = target.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
                                if (target.HasDataBlob<NewtonMoveDB>())
                                    targetSMA = target.GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;

                                var manuvers = InterceptCalcs.Hohmann2(sgp, mySMA, targetSMA);
                                var tnow = ship.Manager.StarSysDateTime;
                                NewtonThrustCommand.CreateCommand(shipOwner, ship, tnow, manuvers[0].deltaV);
                                DateTime futureDate = tnow + TimeSpan.FromSeconds(manuvers[1].timeInSeconds);
                                NewtonThrustCommand.CreateCommand(shipOwner, ship, futureDate, manuvers[1].deltaV);
                            }


                        }
                        else //we're moving between two objects who are in orbit, we shoudl be able to match orbit.
                        {
                            if(dvdif > dvMaxRangeDiff_ms * 0.01)//if we're less than 10% of perfect
                            {

                                double mySMA = 0;
                                if (ship.HasDataBlob<OrbitDB>())
                                    mySMA = ship.GetDataBlob<OrbitDB>().SemiMajorAxis;
                                if (ship.HasDataBlob<OrbitUpdateOftenDB>())
                                    mySMA = ship.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
                                if (ship.HasDataBlob<NewtonMoveDB>())
                                    mySMA = ship.GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;

                                double targetSMA = 0;
                                if (source.HasDataBlob<OrbitDB>())
                                    targetSMA = source.GetDataBlob<OrbitDB>().SemiMajorAxis;
                                if (source.HasDataBlob<OrbitUpdateOftenDB>())
                                    targetSMA = source.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
                                if (source.HasDataBlob<NewtonMoveDB>())
                                    targetSMA = source.GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;

                                var manuvers = InterceptCalcs.Hohmann2(sgp, mySMA, targetSMA);
                                var tnow = ship.Manager.StarSysDateTime;
                                NewtonThrustCommand.CreateCommand(shipOwner, ship, tnow, manuvers[0].deltaV);
                                DateTime futureDate = tnow + TimeSpan.FromSeconds(manuvers[1].timeInSeconds);
                                NewtonThrustCommand.CreateCommand(shipOwner, ship, futureDate, manuvers[1].deltaV);
                            }
                        }
                        //CargoLoadFromOrder.CreateCommand(shipOwner, tradeBase.OwningEntity, ship, tradeItems);
                        CargoUnloadToOrder.CreateCommand(shipOwner, source, ship, tradeItems);
                        //moveto destination order.
                        CargoUnloadToOrder.CreateCommand(shipOwner, ship, destin, tradeItems);
                        
                        
                    }

                }
            }
            



        }
    }



    public class SetTradeOrder : EntityCommand
    {
        public enum OrderTypes
        {
            AddTradebaseDB,
            RemoveTradebaseDB,
            SetBaseItems,
            AddTradeshipDB,
            RemoveTradeshipDB,
            SetShipTypeAmounts
        }

        public SetTradeOrder Order;
        private OrderTypes _type;
        public override int ActionLanes { get; }

        public override bool IsBlocking { get; } = false;

        public override string Name { get; } = "Set TradeObject";

        public override string Details
        {
            get { return Order.Details; }
        }

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        Entity factionEntity;

        private Dictionary<ICargoable,(int count, int demandSupplyWeight)> _baseChanges;
        private Dictionary<Guid, double> _shipChanges;

        internal override bool IsValidCommand(Game game)
        {
            
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out factionEntity, out _entityCommanding))
            {
                
                return true;
                
            }
            return false;
        }

        public static void CreateCommand(Entity entity, OrderTypes ordertype )
        {

            SetTradeOrder cmd = new SetTradeOrder();
            cmd.EntityCommandingGuid = entity.Guid;
            cmd.RequestingFactionGuid = entity.FactionOwner;
            cmd._type = ordertype;


            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        public static void CreateCommand_SetBaseItems(Entity entity, Dictionary<ICargoable,(int count, int demandSupplyWeight)> changes )
        {

            SetTradeOrder cmd = new SetTradeOrder();
            cmd.EntityCommandingGuid = entity.Guid;
            cmd.RequestingFactionGuid = entity.FactionOwner;
            cmd._type = OrderTypes.SetBaseItems;
            cmd._baseChanges = changes;


            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }
        
        public static void CreateCommand_SetShipTypeAmounts(Entity entity, Dictionary<Guid, double> changes )
        {

            SetTradeOrder cmd = new SetTradeOrder();
            cmd.EntityCommandingGuid = entity.Guid;
            cmd.RequestingFactionGuid = entity.FactionOwner;
            cmd._type = OrderTypes.SetShipTypeAmounts;
            cmd._shipChanges = changes;

            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            
            switch (_type)
            {
                case OrderTypes.AddTradebaseDB:
                {
                    var db = new TradingBaseDB();
                    EntityCommanding.SetDataBlob(db);
                    break;
                }
                case OrderTypes.RemoveTradebaseDB:
                {
                    EntityCommanding.RemoveDataBlob<TradingBaseDB>();
                    break;
                }
                case OrderTypes.SetBaseItems:
                {

                    var db = EntityCommanding.GetDataBlob<TradingBaseDB>();
                    foreach (var item in _baseChanges)
                    {
                        db.ListedItems[item.Key] = item.Value;
                    }
                    //NOTE: possibly some conflict here. 
                    //we might need to consider what to do if a ship is already contracted to grab stuff, 
                    //and then we change this and remove the items before the ship has collected them.
                    
                    break;
                }


                case OrderTypes.AddTradeshipDB:
                {
                    var db = new TradingShipperDB();
                    EntityCommanding.SetDataBlob(db);
                    break;
                }
                case OrderTypes.RemoveTradeshipDB:
                {
                    EntityCommanding.RemoveDataBlob<TradingShipperDB>();
                    break;
                }
                case OrderTypes.SetShipTypeAmounts:
                {
                    var db = EntityCommanding.GetDataBlob<TradingShipperDB>();
                    foreach (var item in _shipChanges)
                    {
                        db.TradeSpace[item.Key] = item.Value;
                    }
                    //db.TradeSpace = 
                    
                    break;
                }
                
            }
        }

        public override bool IsFinished()
        {
            return true;
        }
    }

}