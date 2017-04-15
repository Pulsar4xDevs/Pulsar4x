using System.Collections.Generic;
using System.Linq;
using System;

namespace Pulsar4X.ECSLib
{
    public class ShipMoveOrderProcessor : OrderableProcessor
    {
        public void processOrder(Game game, Order order)
        {
            Entity orderableEntity;
            game.GlobalManager.FindEntityByGuid (order.EntityForOrderReq, out orderableEntity);
            MoveOrderableDB db = orderableEntity.GetDataBlob<MoveOrderableDB> ();

        }

        void AddToQueue (EntityManager manager, Entity entity, MoveOrder2 order)
        {
            MoveOrderableDB db = entity.GetDataBlob<MoveOrderableDB>();
            if(db.waypointQueue.Count == 0) //then it's sitting there with no orders. 
            { 
                db.HelmState = MoveOrderableDB.HelmStatus.Underway; //set status so helm processor knows to process next waypoint.
                db.waypointQueue.Enqueue(order.orderObj);
                MoveOrderableProcessor.ProcessSingleEntity(manager, entity, 0);//kick off the helm processor
            }
            else
                db.waypointQueue.Enqueue(order.orderObj);
        }
        void ReplaceQueue ()
        { }
    }

    public class  MoveOrder2 : Order
    {
        enum MoveOrderType
        {
            AddWaypoint,
            ReplaceQueue,

        }

        public MoveOrderableDB.waypointOrderObj orderObj;
    }

    public class MoveOrderableProcessor
    {
        internal static void Process(EntityManager manager, int deltaSeconds)
        {
            foreach (Entity entityWithHelm in manager.GetAllEntitiesWithDataBlob<MoveOrderableDB> ()) 
            {
                ProcessSingleEntity(manager, entityWithHelm, deltaSeconds);
            }
        }

        internal static void ProcessSingleEntity(EntityManager manager, Entity entityWithHelm, int deltaSeconds)
        {
            PositionDB positionDB = entityWithHelm.GetDataBlob<PositionDB>();
            PropulsionDB propulsionDB = entityWithHelm.GetDataBlob<PropulsionDB>();
            MoveOrderableDB helm = entityWithHelm.GetDataBlob<MoveOrderableDB>();
            Vector4 absolutePos = positionDB.AbsolutePosition;

            if(helm.HelmState == MoveOrderableDB.HelmStatus.Orbiting || helm.HelmState == MoveOrderableDB.HelmStatus.HoldingUnderPower) {
                if(propulsionDB.CurrentSpeed.Length() != 0) { propulsionDB.CurrentSpeed = new Vector4(0, 0, 0, 0); } //set speed to 0
                //Do nothing we're waiting for another order system to finish. 
            }
            else if(helm.HelmState == MoveOrderableDB.HelmStatus.Underway) //we've not started moving to the next waypoint. 
            {

                Vector4 waypoint = helm.waypointQueue.Peek().waypoint;
                propulsionDB.CurrentSpeed = GMath.GetVector(absolutePos, waypoint, propulsionDB.MaximumSpeed);//Full steam ahead!
                DateTime currentTime = manager.Game.CurrentDateTime;
                DateTime estDT = currentTime.AddSeconds(ETA(propulsionDB.MaximumSpeed, absolutePos, waypoint));
                manager.ManagerSubpulses.AddEntityInterupt(estDT, PulseActionEnum.MoveOnlyProcessor, entityWithHelm);
                helm.EstTimeToWaypoint = estDT;

            }
            else if(helm.HelmState == MoveOrderableDB.HelmStatus.Makingway)//we're on our way to the current waypoint
            {  
                Vector4 waypoint = helm.waypointQueue.Peek().waypoint;
                double distanceToTarget = (absolutePos - waypoint).Length();
                if(absolutePos == waypoint) //we've reached our destination
                {   
                    propulsionDB.CurrentSpeed = new Vector4(0, 0, 0, 0);  //set speed to 0
                    helm.HelmState = MoveOrderableDB.HelmStatus.HoldingUnderPower; //TODO decide which of these we're doing, get this info from helm state 
                    helm.HelmState = MoveOrderableDB.HelmStatus.Orbiting;
                }
            }
             

        }

        /// <summary>
        /// Estimated time to waypoint
        /// </summary>
        /// <returns>The time in seconds to arrive at the next waypoint.</returns>
        /// <param name="atSpeed">At Speed.</param>
        /// <param name="curPos">Current position.</param>
        /// <param name="waypoint">Waypoint.</param>
        private static double ETA(double atSpeed, Vector4 absolutePos, Vector4 waypoint)
        {            
            double distanceToTarget = (absolutePos - waypoint).Length();
            double time = distanceToTarget / atSpeed;
            return time;
        }
    }

    public class MoveOrderableDB:BaseDataBlob
    {
        public enum HelmStatus
        {
            Orbiting, // anchored, no move orders or waiting for non move orders to complete while not under power. 
            Makingway, //moving to next waypoint under power
            Underway, //used to indicate helm needs to start towards next waypoint. 
            HoldingUnderPower //keeping at an absolute position waiting for non move orders to complete. 
        }
        public HelmStatus HelmState = HelmStatus.Orbiting;

        public struct waypointOrderObj
        {
            public Vector4 waypoint;
            Order actionWhileMove;
            Order actionAtWaypoint;
        }
        public Queue<waypointOrderObj> waypointQueue;

        public DateTime? EstTimeToWaypoint = null;

        public override object Clone()
        {
            throw new NotImplementedException ();
        }
    }

    /// <summary>
    /// Non newtonion translational movement. 
    /// Warps space around the ship changing the position in a system without changing newtonion velocity (acceleration)
    /// </summary>
    internal static class TranslationMovementProcessor
    {
        internal static void Process(EntityManager manager, int deltaSeconds)
        {
            OrderProcessor.ProcessSystem (manager);
            foreach (Entity shipEntity in manager.GetAllEntitiesWithDataBlob<PropulsionDB> ()) {
                PositionDB positionDB = shipEntity.GetDataBlob<PositionDB> ();
                PropulsionDB propulsionDB = shipEntity.GetDataBlob<PropulsionDB> ();

                shipEntity.GetDataBlob<PositionDB> ().AbsolutePosition += shipEntity.GetDataBlob<PropulsionDB> ().CurrentSpeed * deltaSeconds;
            }
        }
    }

 
    internal static class ShipMovementProcessor
    {
        public static void Initialize()
        {
        }

        /// <summary>
        /// Sets a ships position.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="systems"></param>
        /// <param name="deltaSeconds"></param>
        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            foreach (var system in systems)
            {
                foreach (Entity shipEntity in system.SystemManager.GetAllEntitiesWithDataBlob<PropulsionDB>())
                {
                    //TODO: do we need to check if the ship has an orbitDB?
                    //TODO: if the ship will arrive at the destination in the next deltaSeconds, don't go past it.
                    shipEntity.GetDataBlob<PositionDB>().AbsolutePosition += shipEntity.GetDataBlob<PropulsionDB>().CurrentSpeed * deltaSeconds;
                    //shipEntity.GetDataBlob<PositionDB>().AddMeters(shipEntity.GetDataBlob<PropulsionDB>().CurrentSpeed * deltaSeconds);
                    //TODO: use fuel.
                }
            }
        }

        /// <summary>
        /// process PropulsionDB movement for a single system
        /// </summary>
        /// <param name="manager">the system to process</param>
        /// <param name="deltaSeconds">amount of time in seconds</param>
        internal static void Process(EntityManager manager, int deltaSeconds)
        {
            OrderProcessor.ProcessSystem(manager);
            foreach (Entity shipEntity in manager.GetAllEntitiesWithDataBlob<PropulsionDB>())
            {
                PositionDB positionDB = shipEntity.GetDataBlob<PositionDB>();
                PropulsionDB propulsionDB = shipEntity.GetDataBlob<PropulsionDB>();

            
                Queue < BaseOrder > orders = shipEntity.GetDataBlob<ShipInfoDB>().Orders;

                if(orders.Count > 0)
                {

                    if (orders.Peek().OrderType == orderType.MOVETO)
                    {

                        // Check to see if we will overtake the target

                        MoveOrder order = (MoveOrder)orders.Peek();
                        Vector4 shipPos = positionDB.AbsolutePosition;
                        Vector4 targetPos;
                        Vector4 currentSpeed = shipEntity.GetDataBlob<PropulsionDB>().CurrentSpeed;
                        Vector4 nextTPos = shipPos + (currentSpeed * deltaSeconds);
                        Vector4 newPos = shipPos;
                        Vector4 deltaVecToTarget;
                        Vector4 deltaVecToNextT;

                        double distanceToTarget;
                        double distanceToNextTPos;

                        double speedDelta;
                        double distanceDelta;
                        double newDistanceDelta;
                        double fuelMaxDistanceAU;

                        double currentSpeedLength = currentSpeed.Length();

                        CargoStorageDB storedResources = shipEntity.GetDataBlob<CargoStorageDB>();                       
                        Dictionary<Guid, double> fuelUsePerMeter = propulsionDB.FuelUsePerKM;
                        int maxKMeters = CalcMaxFuelDistance(shipEntity);

                        if (order.PositionTarget == null)
                            targetPos = order.Target.GetDataBlob<PositionDB>().AbsolutePosition;
                        else
                            targetPos = order.PositionTarget.AbsolutePosition;

                        deltaVecToTarget = shipPos - targetPos;

                        distanceToTarget = deltaVecToTarget.Length();  //in au


                        deltaVecToNextT = shipPos - nextTPos;
                        fuelMaxDistanceAU = GameConstants.Units.KmPerAu * maxKMeters;


                        distanceToNextTPos = deltaVecToNextT.Length();
                        if (fuelMaxDistanceAU < distanceToNextTPos)
                        {
                            newDistanceDelta = fuelMaxDistanceAU;
                            double percent = fuelMaxDistanceAU / distanceToNextTPos;
                            newPos = nextTPos + deltaVecToNextT * percent;
                            Event usedAllFuel = new Event(manager.ManagerSubpulses.SystemLocalDateTime, "Used all Fuel", shipEntity.GetDataBlob<OwnedDB>().ObjectOwner, shipEntity);
                            usedAllFuel.EventType = EventType.FuelExhausted;
                            manager.Game.EventLog.AddEvent(usedAllFuel);
                        }
                        else
                        {
                            newDistanceDelta = distanceToNextTPos;
                            newPos = nextTPos;
                        }



                        if (distanceToTarget < newDistanceDelta) // moving would overtake target, just go directly to target
                        {
                            newDistanceDelta = distanceToTarget;
                            propulsionDB.CurrentSpeed = new Vector4(0, 0, 0, 0);
                            newPos = targetPos;
                            if (order.Target != null && order.Target.HasDataBlob<SystemBodyInfoDB>())
                                positionDB.SetParent(order.Target);
                            if (order.Target != null)
                            {
                                if (order.Target.HasDataBlob<SystemBodyInfoDB>())  // Set position to the target body
                                {
                                    positionDB.SetParent(order.Target);

                                }
                            }
                                
                            else // We arrived, get rid of the order
                            {

                                shipEntity.GetDataBlob<ShipInfoDB>().Orders.Dequeue();
                            }
                                
                        }
                        positionDB.AbsolutePosition = newPos;
                        int kMetersMoved = (int)(newDistanceDelta * GameConstants.Units.KmPerAu);
                        Dictionary<Guid, int> fuelAmounts = new Dictionary<Guid, int>();
                        foreach (var item in propulsionDB.FuelUsePerKM)
                        {
                            fuelAmounts.Add(item.Key, (int)item.Value * kMetersMoved);
                        }
                        StorageSpaceProcessor.RemoveResources(storedResources, fuelAmounts);
                        
                    }                   
                }               
            }           
        }

        public static int CalcMaxFuelDistance(Entity shipEntity)
        {
            CargoStorageDB storedResources = shipEntity.GetDataBlob<CargoStorageDB>();
            PropulsionDB propulsionDB = shipEntity.GetDataBlob<PropulsionDB>();
            StaticDataStore staticData = shipEntity.Manager.Game.StaticData;
            ICargoable resource = (ICargoable)staticData.FindDataObjectUsingID(propulsionDB.FuelUsePerKM.Keys.First());
            int kmeters = (int)(StorageSpaceProcessor.GetAmountOf(storedResources, resource.ID) / propulsionDB.FuelUsePerKM[resource.ID]); 
            foreach (var usageKVP in propulsionDB.FuelUsePerKM)
            {
                resource = (ICargoable)staticData.FindDataObjectUsingID(usageKVP.Key);
                if (kmeters > (StorageSpaceProcessor.GetAmountOf(storedResources, usageKVP.Key) / usageKVP.Value))
                    kmeters = (int)(StorageSpaceProcessor.GetAmountOf(storedResources, usageKVP.Key) / usageKVP.Value);
            }
            return kmeters;
        }

        public static void CalcFuelUsePerMeter(Entity entity)
        {

            Dictionary<Guid, double> fuelUse = new Dictionary<Guid, double>();
            var instancesDB = entity.GetDataBlob<ComponentInstancesDB>();
            foreach (var engineKVP in instancesDB.SpecificInstances.GetInternalDictionary().Where(i => i.Key.HasDataBlob<EnginePowerAtbDB>()))
            {
                foreach (var item in engineKVP.Key.GetDataBlob<ResourceConsumptionAtbDB>().MaxUsage)
                {
                    fuelUse.SafeValueAdd(item.Key, item.Value * engineKVP.Value.Count); //todo only count non damaged enabled engines
                }               
            }

            entity.GetDataBlob<PropulsionDB>().FuelUsePerKM = fuelUse;
        }

        /// <summary>
        /// recalculates a shipsMaxSpeed.
        /// </summary>
        /// <param name="ship"></param>
        public static void CalcMaxSpeed(Entity ship)
        {
            int totalEnginePower = 0;
            Dictionary<Guid, double> totalFuelUsage = new Dictionary<Guid, double>();
            var instancesDB = ship.GetDataBlob<ComponentInstancesDB>();
            List<KeyValuePair<Entity,PrIwObsList<Entity>>> engineEntities = instancesDB.SpecificInstances.GetInternalDictionary().Where(item => item.Key.HasDataBlob<EnginePowerAtbDB>()).ToList();
            foreach (var engineDesign in engineEntities)
            {
                foreach (var engineInstance in engineDesign.Value)
                {
                    //todo check if it's damaged
                    totalEnginePower += engineDesign.Key.GetDataBlob<EnginePowerAtbDB>().EnginePower;
                    foreach (var kvp in engineDesign.Key.GetDataBlob<ResourceConsumptionAtbDB>().MaxUsage)
                    {
                        totalFuelUsage.SafeValueAdd(kvp.Key, kvp.Value);
                    }                    
                }
            }

            //Note: TN aurora uses the TCS for max speed calcs. 
            PropulsionDB propulsionDB = ship.GetDataBlob<PropulsionDB>();
            propulsionDB.TotalEnginePower = totalEnginePower;
            propulsionDB.FuelUsePerKM = totalFuelUsage;
            propulsionDB.MaximumSpeed = MaxSpeedCalc(totalEnginePower,  ship.GetDataBlob<ShipInfoDB>().Tonnage);
        }

        public static int MaxSpeedCalc(int power, float tonage)
        {
            // From Aurora4x wiki:  Speed = (Total Engine Power / Total Class Size in HS) * 1000 km/s
            // 1 HS = 50 tons

            return (int)(power / tonage) * 20;
        }
    }
}