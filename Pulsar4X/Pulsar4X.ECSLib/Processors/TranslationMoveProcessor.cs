using System;
using System.Collections;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// Handels the order from the messagePump.
    /// </summary>
    public class TranslationOrderProcessor : IOrderableProcessor
    {
        public void ProcessOrder(BaseAction order)
        {
            Entity orderableEntity = order.ThisEntity;
            EntityManager entityManager = orderableEntity.Manager;

            if (order is TranslationOrder)
            {
                TranslationOrder torder = (TranslationOrder)order;

                switch (torder.HelmOrderType)
                {
                    case TranslationOrder.HelmOrderTypeEnum.OrbitTarget:
                        SetOrbitTarget(order.ThisEntity, order.TargetEntity, order);
                        break;
                    case TranslationOrder.HelmOrderTypeEnum.InterceptTarget:
                        SetCurrentVector(orderableEntity, order.TargetEntity);
                        break;
                    case TranslationOrder.HelmOrderTypeEnum.MatchTarget:
                        //ReplaceQueue(orderableEntity, torder);
                        break;
                }
            }
            else throw new Exception("Bad Order Type, must be a TranslationOrder");
        }



        public void FirstProcess(BaseAction order)
        {
            Entity thisEntity = order.ThisEntity;
            TranslateOrderableDB translateOrderableDB = thisEntity.GetDataBlob<TranslateOrderableDB>();
            //translateOrderableDB.CurrentOrder = (TranslationOrder)order;
            translateOrderableDB.LastRunDate = thisEntity.Manager.ManagerSubpulses.SystemLocalDateTime;

            var position = thisEntity.GetDataBlob<PositionDB>();
            var propulsion = thisEntity.GetDataBlob<PropulsionDB>();

            Vector4 targetPos = order.TargetEntity.GetDataBlob<PositionDB>().AbsolutePosition;

            SetNextInterupt(thisEntity.Manager.Game, thisEntity, position.AbsolutePosition, targetPos, propulsion.MaximumSpeed);
        }

        private void SetCurrentVector(Entity thisEntity, Entity targetEntity)
        {
            var position = thisEntity.GetDataBlob<PositionDB>();
            var propulsion = thisEntity.GetDataBlob<PropulsionDB>();

            SetCurrentVector(propulsion, position, targetEntity);

        }

        private void SetCurrentVector(PropulsionDB propulsion, PositionDB position, Entity targetEntity)
        {
            int speed = propulsion.MaximumSpeed; //TODO allow speed changes.
            PositionDB targetPosition = targetEntity.GetDataBlob<PositionDB>();
            if (targetEntity.HasDataBlob<OrbitDB>())
            {
                //TODO figure out how to calculate an intercept vector.
            }

            propulsion.CurrentSpeed = GMath.GetVector(position.AbsolutePosition, targetPosition.AbsolutePosition, speed);
        }

        private void SetOrbitTarget(Entity thisEntity, Entity targetEntity, BaseAction order)
        {
            MassVolumeDB targetMassDB = targetEntity.GetDataBlob<MassVolumeDB>();
            MassVolumeDB thisMass = thisEntity.GetDataBlob<MassVolumeDB>();
            OrbitDB targetOrbit = targetEntity.GetDataBlob<OrbitDB>();
            PositionDB targetPosition = targetEntity.GetDataBlob<PositionDB>();
            PositionDB thisPosition = thisEntity.GetDataBlob<PositionDB>();
            double distance = thisPosition.GetDistanceTo(targetPosition);


            if(targetOrbit.SphereOfInfluince > targetMassDB.Radius * 10) //ok I'm spitballing here, maybe increase this
            {
                if (targetOrbit.SphereOfInfluince > distance) //then we're within the SOI,we can orbit here.
                {
                    double parentMass = targetMassDB.Mass;
                    double myMass = thisMass.Mass;
                    double semiMajAxis = distance;
                    double eccentricity = 0.0; //TODO allow eccentricity input
                    double inclination = 0.0;
                    double loAN = 0.0;
                    double aoP = 0.0;
                    double meanLong = 0.0;
                    DateTime j2000 = new DateTime(2000, 1, 1, 12, 0, 0);
                    OrbitDB newOrbitDB =OrbitDB.FromMajorPlanetFormat(targetEntity, parentMass, myMass, semiMajAxis,
                                                                      eccentricity, inclination, loAN,
                                                                      aoP, meanLong, j2000);
                    thisEntity.GetDataBlob<TranslateOrderableDB>().HelmState = TranslateOrderableDB.HelmStatus.Orbiting;

                    order.IsFinished = true;
                }
                else
                {
                    //TODO: set a moveto order to get within soi
                }
            }
            else
            {
                //target not massy enough to orbit.
                //TODO: MatchOrbit
            }
        }

        public BaseAction GetCurrentOrder(BaseAction order)
        {
            TranslateOrderableDB translateOrderableDB = order.ThisEntity.GetDataBlob<TranslateOrderableDB>();
            throw new NotImplementedException();
            //return translateOrderableDB.CurrentOrder = (TranslationOrder)order;

        }

        public PercentValue GetPercentComplete(BaseAction order)
        {
            throw new NotImplementedException();
        }

        private void SetNextInterupt(Game game, Entity entity, Vector4 from, Vector4 too, int atSpeed)
        {
            DateTime currentTime = game.CurrentDateTime;
            DateTime estDT = currentTime.AddSeconds(ETA(atSpeed, from, too));
            entity.Manager.ManagerSubpulses.AddEntityInterupt(estDT, PulseActionEnum.MoveOnlyProcessor, entity);
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

    
    public class  TranslationOrder : BaseAction
    {
        public enum HelmOrderTypeEnum
        {
            InterceptTarget, //move to the target, other orders should initiate this first if they're not close enough.
            MatchTarget, //close and orbit the targets parent if orbiting, else close and match the targets speed.
            OrbitTarget, //orbit the target
        }

        public HelmOrderTypeEnum HelmOrderType { get; set; }
        public double TargetDistance { get; set; } //this is the distance we want to Match or orbit at.


        public TranslationOrder(Entity entityGuid, Entity factionID) : base(1, true, entityGuid, factionID)
        {
            OrderableProcessor = new TranslationOrderProcessor();
        }

        public TranslationOrder(IOrderableProcessor processor, Entity entityGuid, Entity factionID, Entity targetGuid) : base(1, true, entityGuid, factionID, targetGuid)
        {
            OrderableProcessor = new TranslationOrderProcessor();
        }

    }
}
