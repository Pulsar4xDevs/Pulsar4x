using System.Collections.Generic;
using System.Linq;
using System;

namespace Pulsar4X.ECSLib
{
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
                    //TODO: use fuel.
                }
            }
        }

        /// <summary>
        /// process PropulsionDB movement for a single system
        /// </summary>
        /// <param name="system">the system to process</param>
        /// <param name="deltaSeconds">amount of time in seconds</param>
        internal static void Process(StarSystem system, int deltaSeconds)
        {
            OrderProcessor.ProcessSystem(system);
            foreach (Entity shipEntity in system.SystemManager.GetAllEntitiesWithDataBlob<PropulsionDB>())
            {
                //TODO: do we need to check if the ship has an orbitDB?
                Queue < BaseOrder > orders = shipEntity.GetDataBlob<ShipInfoDB>().Orders;

                if(orders.Count > 0)
                {

                    if (orders.Peek().OrderType == orderType.MOVETO)
                    {

                        // Check to see if we will overtake the target

                        MoveOrder order = (MoveOrder)orders.Peek();
                        Vector4 shipPos = shipEntity.GetDataBlob<PositionDB>().AbsolutePosition;
                        Vector4 targetPos;
                        Vector4 newPos;
                        Vector4 deltaVec;
                        Vector4 currentSpeed = shipEntity.GetDataBlob<PropulsionDB>().CurrentSpeed;
                        double distanceToTarget;
                        double distanceToNewTarget;

                        double speedDelta;
                        double distanceDelta;
                        double newDistanceDelta;

                        double currentSpeedLength = currentSpeed.Length();

                        if (order.PositionTarget == null)
                            targetPos = order.Target.GetDataBlob<PositionDB>().AbsolutePosition;
                        else
                            targetPos = order.PositionTarget.AbsolutePosition;

                        deltaVec = shipPos - targetPos;

                        distanceToTarget = deltaVec.Length();

                        newPos = shipPos + (currentSpeed * deltaSeconds);

                        deltaVec = shipPos - newPos;
                        distanceToNewTarget = deltaVec.Length();

                        if (distanceToTarget < distanceToNewTarget) // moving would overtake target, just go directly to target
                        {
                            shipEntity.GetDataBlob<PropulsionDB>().CurrentSpeed = new Vector4(0, 0, 0, 0);
                            if (order.Target != null && order.Target.HasDataBlob<SystemBodyDB>())
                                shipEntity.GetDataBlob<PositionDB>().SetParent(order.Target);
                            if (order.Target != null)
                            {
                                if (order.Target.HasDataBlob<SystemBodyDB>())  // Set position to the target body
                                {
                                    shipEntity.GetDataBlob<PositionDB>().SetParent(order.Target);
                                    shipEntity.GetDataBlob<PositionDB>().AbsolutePosition = targetPos;
                                }
                                    
                                else
                                    shipEntity.GetDataBlob<PositionDB>().AbsolutePosition = targetPos;
                            }
                                
                            else // We arrived, get rid of the order
                            {
                                shipEntity.GetDataBlob<PositionDB>().AbsolutePosition = targetPos;
                                shipEntity.GetDataBlob<ShipInfoDB>().Orders.Dequeue();
                            }
                                
                        }
                    }

                }

                shipEntity.GetDataBlob<PositionDB>().AbsolutePosition += (shipEntity.GetDataBlob<PropulsionDB>().CurrentSpeed * deltaSeconds);
                //TODO: use fuel.
            }
            
        }


        /// <summary>
        /// recalculates a shipsMaxSpeed.
        /// </summary>
        /// <param name="ship"></param>
        public static void CalcMaxSpeed(Entity ship)
        {
            int totalEnginePower = 0;

            List<KeyValuePair<Entity,List<Entity>>> engineEntities = ship.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<EnginePowerAtbDB>()).ToList();
            foreach (var engineDesign in engineEntities)
            {
                foreach (var engineInstance in engineDesign.Value)
                {
                    //todo check if it's damaged
                    totalEnginePower += engineDesign.Key.GetDataBlob<EnginePowerAtbDB>().EnginePower;
                }
            }

            //Note: TN aurora uses the TCS for max speed calcs. 
            ship.GetDataBlob<PropulsionDB>().TotalEnginePower = totalEnginePower;
            ship.GetDataBlob<PropulsionDB>().MaximumSpeed = MaxSpeedCalc(totalEnginePower,  ship.GetDataBlob<ShipInfoDB>().Tonnage);
        }

        public static int MaxSpeedCalc(int power, float tonage)
        {
            // From Aurora4x wiki:  Speed = (Total Engine Power / Total Class Size in HS) * 1000 km/s
            // 1 HS = 50 tons

            return (int)(power / tonage) * 20;
        }
    }
}