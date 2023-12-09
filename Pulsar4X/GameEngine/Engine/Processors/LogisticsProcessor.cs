using System;
using System.Collections.Generic;
using GameEngine.WarpMove;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Logistics;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Engine;

public class LogiBaseProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency
    {
        get { return TimeSpan.FromHours(1); }
    }

    public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.25);

    public Type GetParameterType => typeof(LogiBaseDB);

    public void Init(Game game)
    {

    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        LogisticsCycle.LogiBaseBidding(entity.GetDataBlob<LogiBaseDB>());
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var tradingBases = manager.GetAllDataBlobsOfType<LogiBaseDB>();
        var shippingEntities = manager.GetAllEntitiesWithDataBlob<LogiShipperDB>();
        foreach(LogiBaseDB tradeBase in tradingBases)
        {
            LogisticsCycle.LogiBaseBidding(tradeBase);
        }
        return tradingBases.Count;
    }
}

public class LogiShipProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency
    {
        get { return TimeSpan.FromHours(1); }
    }

    public TimeSpan FirstRunOffset => TimeSpan.FromHours(0);

    public Type GetParameterType => typeof(LogiShipperDB);

    public void Init(Game game)
    {

    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        var tradingBases = entity.Manager.GetAllDataBlobsOfType<LogiBaseDB>();
        LogisticsCycle.LogiShipBidding(entity, tradingBases);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var tradingBases = manager.GetAllDataBlobsOfType<LogiBaseDB>();
        var shippingEntities = manager.GetAllEntitiesWithDataBlob<LogiShipperDB>();
        foreach(Entity shipper in shippingEntities)
        {
            LogisticsCycle.LogiShipBidding(shipper, tradingBases);
        }

        return shippingEntities.Count;
    }
}
public static class LogisticsCycle
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


    public static void LogiShipBidding(Entity shippingEntity, List<LogiBaseDB> tradingBases)
    {

        DateTime currentDateTime = shippingEntity.StarSysDateTime;
        LogiShipperDB shiperdb = shippingEntity.GetDataBlob<LogiShipperDB>();
        List<CargoTask> cargoTasks = new List<CargoTask>();

        if(shiperdb.CurrentState != LogiShipperDB.States.Waiting)
            return;
        shiperdb.CurrentState = LogiShipperDB.States.Bidding;

        double fuelCost = 1; //we need to work out how much fuel costs from the economy at some point.
        double timeCost = 1; //per second. the ship will need to work this out for itself, should include maintanance, amount of crew etc.

        var sourceTradeItems = new List<CargoTask>();
        var demandTradeItems = new List<CargoTask>();
        double travelTimeToSource = 0;

        foreach(var tbase in tradingBases)
        {
            OrbitDB odb;// = tbase.OwningEntity.GetDataBlob<OrbitDB>();
            if(tbase.OwningEntity.HasDataBlob<OrbitDB>())
                 odb = tbase.OwningEntity.GetDataBlob<OrbitDB>();
            else
                odb = tbase.OwningEntity.GetSOIParentEntity().GetDataBlob<OrbitDB>();



            if(shippingEntity.Manager.Game.Settings.StrictNewtonion)
            {
                travelTimeToSource = LogisticsNewtonion.TravelTimeToSource(shippingEntity, tbase, odb, currentDateTime);
            }
            else
            {
                travelTimeToSource = LogisticsSimple.TravelTimeToSource(shippingEntity, tbase, odb, currentDateTime);
            }

            double fuelToSource = 1; //todo calcualte actual amount of fuel to get there. this is going to be orbital manuver fuel since warp just uses power...



            // var hohmann = InterceptCalcs.Hohmann(sgp, r1, r2); //need to calculate the ideal ranges
            // var dvDif = hohmann[0].deltaV.Length() + hohmann[1].deltaV.Length();

            // double loadtime = 3600;

            // double unloadTime = 3600;

            // double profit = 0; //this is used by the ship to decide where it should source and destination from.

            foreach(var cargoSpace in shiperdb.TradeSpace) //for each type of cargospace
            {
                double possibleTradeVolume = cargoSpace.Value;

                //cycle through the base items and add them to the above dictionary if we can carry them
                foreach(var tradingItems in tbase.ListedItems)
                {
                    if(tradingItems.Key.CargoTypeID == cargoSpace.Key)
                    {
                        //we abs this since if it's demand, it'll be negative. we want to find how much to carry.
                        var vpu = tradingItems.Key.VolumePerUnit;
                        var mpu = tradingItems.Key.MassPerUnit;
                        var tradeCount = tradingItems.Value.count;
                        var shipCount = Math.Min(mpu * shiperdb.MaxTradeMass, Math.Pow(possibleTradeVolume,3) / vpu);
                        long numCanCarry = (long)Math.Min(Math.Abs(tradeCount), shipCount);


                        var ct = new CargoTask();

                        ct.item = tradingItems.Key;
                        ct.NumberOfItems = numCanCarry;
                        ct.Profit = tradingItems.Value.demandSupplyWeight;
                        if(tradingItems.Value.count > 0) //if it's a supply (we ship FROM)
                        {

                            //var loadRate = CargoTransferProcessor.CalcTransferRate(dvDif, tbase.OwningEntity.GetDataBlob<VolumeStorageDB>(), shiperdb.OwningEntity.GetDataBlob<VolumeStorageDB>())
                            ct.Source = tbase.OwningEntity;
                            sourceTradeItems.Add(ct);
                            ct.timeInSeconds =  travelTimeToSource;
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
                (Vector3 position, DateTime atDateTime) targetIntercept = WarpMath.GetInterceptPosition
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
            var tbdb = pcombo.Destination.GetDataBlob<LogiBaseDB>(); //we're going to add it to the *DESTINATON* base

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
        shiperdb.BiddingTasks = possibleCombos;


    }

    public static void LogiBaseBidding(LogiBaseDB tradeBase)
    {
        if (tradeBase.TradeShipBids.Count == 0)
            return;
        int last = tradeBase.TradeShipBids.Count -1;
        var cargoTask = tradeBase.TradeShipBids[last].cargoTask;
        var ship = tradeBase.TradeShipBids[last].ship;
        var shiptradedb = ship.GetDataBlob<LogiShipperDB>();
        Entity source = cargoTask.Source;
        Entity destin = cargoTask.Destination;

        //start at last item in index, we can remove bidTasks from the tradebase.TradeShipBids.
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

                    var shipOwner = ship.FactionOwnerID;//.GetDataBlob<ObjectOwnershipDB>().OwningEntity;
                    Entity? currentSOIParent = ship.GetSOIParentEntity();
                    Entity? sourceSOIParent = source.GetSOIParentEntity(); //might need some checks in here.

                    if(currentSOIParent == null) throw new NullReferenceException("currentSOIParent cannot be null");
                    if(sourceSOIParent == null) throw new NullReferenceException("sourceSOIParent cannot be null");

                    //moveto source(if requred)
                    var myMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
                    var at = ship.StarSysDateTime;
                    var pos = ship.GetDataBlob<PositionDB>().RelativePosition;
                    var state = ship.GetRelativeState();
                    var curstate = new ManuverState()
                    {
                        At = at,
                        Mass = myMass,
                        Position = state.pos,
                        Velocity = state.Velocity
                    };
                    var manuverState = Manuvers(ship, currentSOIParent, source, curstate);
                    curstate = manuverState.endState;

                    CargoLoadFromOrder.CreateCommand(shipOwner, source, ship, tradeItems);


                    var smass = sourceSOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
                    var sgp = GeneralMath.StandardGravitationalParameter(curstate.Mass + smass);
                    var sstate = currentSOIParent.GetRelativeFutureState(curstate.At);
                    var dvd = CargoTransferProcessor.CalcDVDifference_m(sgp, (curstate.Position, curstate.Velocity), sstate);
                    var svs = source.GetDataBlob<VolumeStorageDB>();
                    var mvs = ship.GetDataBlob<VolumeStorageDB>();
                    var ctime = CargoTransferProcessor.CalcTransferRate(dvd, svs, mvs);
                    curstate.At = curstate.At + TimeSpan.FromSeconds(ctime);

                    //moveto destination.

                    Manuvers(ship, sourceSOIParent, destin, curstate);
                    CargoUnloadToOrder.CreateCommand(shipOwner, ship, destin, tradeItems);

                }

            }
        }
    }

    public static (ManuverState endState, double fuelBurned) Manuvers(Entity ship, Entity cur, Entity target, ManuverState startState)
    {

        var shipMass = startState.Mass;
        // double tsec = 0;
        DateTime dateTime = startState.At;
        double fuelUse = 0;
        Vector3 pos = startState.Position;
        Vector3 vel = startState.Velocity;


        var targetBody = target.GetSOIParentEntity();

        //var myMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
        var tgtBdyMass = target.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
        var sgpTgtBdy = GeneralMath.StandardGravitationalParameter(shipMass + tgtBdyMass);
        var curBdyMass = cur.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
        var sgpCurBdy = GeneralMath.StandardGravitationalParameter(shipMass + curBdyMass);
        var ke = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, startState.Position, startState.Velocity, startState.At);

        (ManuverState mstate, double fuelBurned) mfstate = (startState, fuelUse);

        if (ship.GetSOIParentEntity() == target.GetSOIParentEntity())
        {
            var dvdif = CargoTransferProcessor.CalcDVDifference_m(target, ship);
            var cargoDBLeft = ship.GetDataBlob<VolumeStorageDB>();
            var cargoDBRight = target.GetDataBlob<VolumeStorageDB>();
            var dvMaxRangeDiff_ms = Math.Max(cargoDBLeft.TransferRangeDv_mps, cargoDBRight.TransferRangeDv_mps);

            if (target.HasDataBlob<ColonyInfoDB>())  //if target is a colony,
            {
                if (dvdif > dvMaxRangeDiff_ms * 0.01) //TODO:, whats the best dv dif for a colony on a planet? can we land? do we want to?
                {
                    if(ship.Manager.Game.Settings.StrictNewtonion)
                        mfstate = LogisticsNewtonion.ManuverToParentColony(ship, cur, target, startState);
                    else
                        mfstate = LogisticsSimple.ManuverToParentColony(ship, cur, target, startState);
                }
            }

            //we're moving between two objects who are in orbit, we shoudl be able to match orbit.
            else
            {
                if (dvdif > dvMaxRangeDiff_ms * 0.01)//if we're less than 10% of perfect
                {
                    if (ship.Manager.Game.Settings.StrictNewtonion)
                        mfstate = LogisticsNewtonion.ManuverToSiblingObject(ship, cur, target, startState);
                    else
                        mfstate = LogisticsSimple.ManuverToSiblingObject(ship, cur, target, startState);
                }
            }
        }
        else //if we're not orbiting the same parent as the source, we have to warpmove
        {
            if (ship.Manager.Game.Settings.StrictNewtonion)
                mfstate = LogisticsNewtonion.ManuverToExternalObject(ship, cur, target, startState);
            else
            {
                mfstate = LogisticsSimple.ManuverToExternalObject(ship, cur, target, startState);
            }
        }

        return mfstate;
    }
}