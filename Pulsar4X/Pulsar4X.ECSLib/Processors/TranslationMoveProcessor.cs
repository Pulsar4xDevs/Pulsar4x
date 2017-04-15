using System;
using System.Collections;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// Handels the order from the messagePump.
    /// </summary>
    public class TranslationOrderProcessor : OrderableProcessor
    {
        public void ProcessOrder(Game game, Order order)
        {
            Entity orderableEntity;
            game.GlobalManager.FindEntityByGuid (order.EntityForOrderReq, out orderableEntity);
            //TranslateOrderableDB db = orderableEntity.GetDataBlob<TranslateOrderableDB> ();
            EntityManager entityManager = orderableEntity.Manager;

            if (order is TranslationOrder)
            {
                TranslationOrder torder = (TranslationOrder)order;

                switch (torder.MoveOrderType)
                {
                    case TranslationOrder.MoveOrderTypeEnum.AddWaypoint:
                        AddToQueue(entityManager, orderableEntity, torder);
                        break;
                    case TranslationOrder.MoveOrderTypeEnum.ReplaceQueue:
                        ReplaceQueue(orderableEntity, torder);
                        break;
                }


            }
            else throw new Exception("Bad Order Type, must be a TranslateionOrder");
        }

        void AddToQueue (EntityManager manager, Entity entity, TranslationOrder order)
        {
            TranslateOrderableDB db = entity.GetDataBlob<TranslateOrderableDB>();
            if(db.waypointQueue.Count == 0) //then it's sitting there with no orders.
            {
                db.HelmState = TranslateOrderableDB.HelmStatus.Underway; //set status so helm processor knows to process next waypoint.
                foreach (var item in order.WaypointList)
                {
                    db.waypointQueue.Enqueue(item);
                }
                TranslationOrderableProcessor.ProcessSingleEntity(manager, entity, 0);//kick off the helm processor
            }
            else
                foreach (var item in order.WaypointList)
                {
                    db.waypointQueue.Enqueue(item);
                }
        }

        void ReplaceQueue(Entity entity, TranslationOrder order)
        {
            TranslateOrderableDB db = entity.GetDataBlob<TranslateOrderableDB> ();
            db.waypointQueue.Clear();
            foreach (var item in order.WaypointList)
            {
                db.waypointQueue.Enqueue(item);
            }
        }
    }

    public class  TranslationOrder : Order
    {
        public enum MoveOrderTypeEnum
        {
            AddWaypoint,
            ReplaceQueue,

        }

        public MoveOrderTypeEnum MoveOrderType;
        public List<TranslateOrderableDB.waypointOrderObj> WaypointList;
    }

    internal class TranslationOrderableProcessor
    {
        internal static void Process(EntityManager manager, int deltaSeconds)
        {
            foreach (Entity entityWithHelm in manager.GetAllEntitiesWithDataBlob<TranslateOrderableDB> ())
            {
                ProcessSingleEntity(manager, entityWithHelm, deltaSeconds);
            }
        }

        internal static void ProcessSingleEntity(EntityManager manager, Entity entityWithHelm, int deltaSeconds)
        {
            PositionDB positionDB = entityWithHelm.GetDataBlob<PositionDB>();
            PropulsionDB propulsionDB = entityWithHelm.GetDataBlob<PropulsionDB>();
            TranslateOrderableDB helm = entityWithHelm.GetDataBlob<TranslateOrderableDB>();
            Vector4 absolutePos = positionDB.AbsolutePosition;

            if(helm.HelmState == TranslateOrderableDB.HelmStatus.Orbiting || helm.HelmState == TranslateOrderableDB.HelmStatus.HoldingUnderPower) {
                if(propulsionDB.CurrentSpeed.Length() != 0) { propulsionDB.CurrentSpeed = new Vector4(0, 0, 0, 0); } //set speed to 0
                //Do nothing we're waiting for another order system to finish.
            }
            else if(helm.HelmState == TranslateOrderableDB.HelmStatus.Underway) //we've not started moving to the next waypoint.
            {

                Vector4 waypoint = helm.waypointQueue.Peek().waypoint;
                propulsionDB.CurrentSpeed = GMath.GetVector(absolutePos, waypoint, propulsionDB.MaximumSpeed);//Full steam ahead!
                DateTime currentTime = manager.Game.CurrentDateTime;
                DateTime estDT = currentTime.AddSeconds(ETA(propulsionDB.MaximumSpeed, absolutePos, waypoint));
                manager.ManagerSubpulses.AddEntityInterupt(estDT, PulseActionEnum.MoveOnlyProcessor, entityWithHelm);
                helm.EstTimeToWaypoint = estDT;

            }
            else if(helm.HelmState == TranslateOrderableDB.HelmStatus.Makingway)//we're on our way to the current waypoint
            {
                Vector4 waypoint = helm.waypointQueue.Peek().waypoint;
                double distanceToTarget = (absolutePos - waypoint).Length();
                if(absolutePos == waypoint) //we've reached our destination
                {
                    propulsionDB.CurrentSpeed = new Vector4(0, 0, 0, 0);  //set speed to 0
                    helm.HelmState = TranslateOrderableDB.HelmStatus.HoldingUnderPower; //TODO decide which of these we're doing, get this info from helm state
                    helm.HelmState = TranslateOrderableDB.HelmStatus.Orbiting;
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

    public class TranslateOrderableDB:BaseDataBlob
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

}
