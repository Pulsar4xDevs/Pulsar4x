using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{

    public class NewtonionMovementProcessor : IHotloopProcessor
    {
        public NewtonionMovementProcessor()
        {
        }

        public TimeSpan RunFrequency => TimeSpan.FromMinutes(5);

        public TimeSpan FirstRunOffset => TimeSpan.FromSeconds(0);

        public Type GetParameterType => typeof(NewtonMoveDB);

        public void Init(Game game)
        {

        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            NewtonMove(entity, deltaSeconds);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entites = manager.GetAllEntitiesWithDataBlob<NewtonMoveDB>();
            foreach (var entity in entites)
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }

        /// <summary>
        /// This was designed so that fast moving objects will get interpolated a lot more than slow moving objects
        /// so fast moving objects shouldn't loose positional acuracy when close to a planet, 
        /// and slow moving objects won't have processor time wasted on them by calulcating too often. 
        /// However this seems to be unstable and looses energy, unsure why. currently set it to just itterate/interpolate every second.
        /// so currently will be using more time to get through this than neccisary. 
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="deltaSeconds">Delta seconds.</param>
        public static void NewtonMove(Entity entity, int deltaSeconds)
        {

            NewtonMoveDB newtonMoveDB = entity.GetDataBlob<NewtonMoveDB>();
            PositionDB positionDB = entity.GetDataBlob<PositionDB>();
            double Mass_Kg = entity.GetDataBlob<MassVolumeDB>().Mass;
            double ParentMass_kg = newtonMoveDB.ParentMass;

            var manager = entity.Manager;
            DateTime dateTimeFrom = newtonMoveDB.LastProcessDateTime;
            DateTime dateTimeNow = manager.ManagerSubpulses.StarSysDateTime;
            DateTime dateTimeFuture = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds);
            double deltaT = (dateTimeFuture - dateTimeFrom).TotalSeconds;

            double secondsToItterate = deltaT;
            while (secondsToItterate > 0) 
            {


                //double timeStep = Math.Max(secondsToItterate / speed_kms, 1);
                //timeStep = Math.Min(timeStep, secondsToItterate);
                double timeStep = 1;//because the above seems unstable and looses energy. 
                double distanceToParent_m = positionDB.GetDistanceTo_m(newtonMoveDB.SOIParent.GetDataBlob<PositionDB>());

                distanceToParent_m = Math.Max(distanceToParent_m, 0.1); //don't let the distance be 0 (once collision is in this will likely never happen anyway)

                double gravForce = GameConstants.Science.GravitationalConstant * (Mass_Kg * ParentMass_kg / Math.Pow(distanceToParent_m, 2));
                Vector3 gravForceVector = gravForce * -Vector3.Normalise(positionDB.RelativePosition_m);
           
                Vector3 totalForce = gravForceVector + newtonMoveDB.ThrustVector;

                Vector3 acceleration_mps = totalForce / Mass_Kg;
                Vector3 newVelocity = (acceleration_mps * timeStep) + newtonMoveDB.CurrentVector_ms;

                newtonMoveDB.CurrentVector_ms = newVelocity;
                Vector3 deltaPos = (newtonMoveDB.CurrentVector_ms + newVelocity) / 2 * timeStep;

                positionDB.RelativePosition_m += deltaPos;

                double sOIRadius = OrbitProcessor.GetSOI_m(newtonMoveDB.SOIParent);

                if (positionDB.RelativePosition_m.Length() >= sOIRadius)
                {
                    Entity newParent;
                    Vector3 parentRalitiveVector;
                    //if our parent is a regular kepler object (normaly this is the case)
                    if (newtonMoveDB.SOIParent.HasDataBlob<OrbitDB>())
                    {
                        var orbitDB = newtonMoveDB.SOIParent.GetDataBlob<OrbitDB>();
                        newParent = orbitDB.Parent;
                        var parentVelocity = OrbitProcessor.InstantaneousOrbitalVelocityVector(orbitDB, entity.Manager.ManagerSubpulses.StarSysDateTime);
                        parentVelocity = Distance.AuToMt(parentVelocity);
                        parentRalitiveVector = newtonMoveDB.CurrentVector_ms + parentVelocity;
           
                    }
                    else //if (newtonMoveDB.SOIParent.HasDataBlob<NewtonMoveDB>())
                    {   //this will pretty much never happen.
                        newParent = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().SOIParent;
                        var parentVelocity = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().CurrentVector_ms;
                        parentRalitiveVector = newtonMoveDB.CurrentVector_ms + parentVelocity;
                    }
                    double newParentMass = newParent.GetDataBlob<MassVolumeDB>().Mass;
                    
                    Vector3 posRalitiveToNewParent = positionDB.AbsolutePosition_m - newParent.GetDataBlob<PositionDB>().AbsolutePosition_m;

                    var dateTime = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds - secondsToItterate);
                    double sgp = GMath.StandardGravitationalParameter(newParentMass + Mass_Kg);
                    var kE = OrbitMath.KeplerFromPositionAndVelocity(sgp, posRalitiveToNewParent, parentRalitiveVector, dateTime);

                    if (kE.Eccentricity < 1) //if we're going to end up in a regular orbit around our new parent
                    {
                        
                        var newOrbit = OrbitDB.FromKeplerElements(
                            newParent,
                            Mass_Kg, 
                            kE,
                            dateTime);
                        entity.RemoveDataBlob<NewtonMoveDB>();
                        entity.SetDataBlob(newOrbit);
                        positionDB.SetParent(newParent);
                        var newPos = OrbitProcessor.GetPosition_AU(newOrbit, dateTime);
                        positionDB.RelativePosition_AU = newPos;
                        break;
                    }
                    else //else we're in a hyperbolic trajectory around our new parent, so just coninue the newtonion move. 
                    {
                        positionDB.SetParent(newParent);
                        newtonMoveDB.ParentMass = newParentMass;
                        newtonMoveDB.SOIParent = newParent;    
                    }

                }
                secondsToItterate -= timeStep;
            }
            newtonMoveDB.LastProcessDateTime = dateTimeFuture;
        }
    }
}
