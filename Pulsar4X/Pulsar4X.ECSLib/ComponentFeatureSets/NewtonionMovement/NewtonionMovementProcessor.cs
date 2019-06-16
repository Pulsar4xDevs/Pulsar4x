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
                double speed_kms = newtonMoveDB.CurrentVector_kms.Length();


                //double timeStep = Math.Max(secondsToItterate / speed_kms, 1);
                //timeStep = Math.Min(timeStep, secondsToItterate);
                double timeStep = 1;//because the above seems unstable and looses energy. 
                double distanceToParent_m = Distance.AuToMt(positionDB.GetDistanceTo(newtonMoveDB.SOIParent.GetDataBlob<PositionDB>()));

                distanceToParent_m = Math.Max(distanceToParent_m, 0.1); //don't let the distance be 0 (once collision is in this will likely never happen anyway)

                double gravForce = GameConstants.Science.GravitationalConstant * (Mass_Kg * ParentMass_kg / Math.Pow(distanceToParent_m, 2));
                Vector4 gravForceVector = gravForce * -Vector4.Normalise(positionDB.RelativePosition_AU);
                double distance = Distance.AuToKm(positionDB.RelativePosition_AU).Length();
                Vector4 totalForce = gravForceVector + newtonMoveDB.ThrustVector;

                Vector4 acceleration_mps = totalForce / Mass_Kg;
                Vector4 newVelocity = (acceleration_mps * timeStep * 0.001) + newtonMoveDB.CurrentVector_kms;

                newtonMoveDB.CurrentVector_kms = newVelocity;
                Vector4 deltaPos = (newtonMoveDB.CurrentVector_kms + newVelocity) / 2 * timeStep;
                //Vector4 deltaPos = newtonMoveDB.CurrentVector_kms * timeStep;

                positionDB.RelativePosition_AU += Distance.KmToAU(deltaPos);

                double sOIRadius_AU = OrbitProcessor.GetSOI(newtonMoveDB.SOIParent);

                if (positionDB.RelativePosition_AU.Length() >= sOIRadius_AU)
                {
                    Entity newParent;
                    Vector4 parentRalitiveVector;
                    //if our parent is a regular kepler object (normaly this is the case)
                    if (newtonMoveDB.SOIParent.HasDataBlob<OrbitDB>())
                    {
                        var orbitDB = newtonMoveDB.SOIParent.GetDataBlob<OrbitDB>();
                        newParent = orbitDB.Parent;
                        var parentVelocity = OrbitProcessor.InstantaneousOrbitalVelocityVector(orbitDB, entity.Manager.ManagerSubpulses.StarSysDateTime);
                        parentRalitiveVector = Distance.KmToAU(newtonMoveDB.CurrentVector_kms) + parentVelocity;
                        var pvlen = Distance.AuToKm( parentVelocity.Length());
                        var vlen = newtonMoveDB.CurrentVector_kms.Length();
                        var rvlen = Distance.AuToKm( parentRalitiveVector.Length());
                    }
                    else //if (newtonMoveDB.SOIParent.HasDataBlob<NewtonMoveDB>())
                    {   //this will pretty much never happen.
                        newParent = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().SOIParent;
                        var parentVelocity = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().CurrentVector_kms;
                        parentRalitiveVector = Distance.KmToAU(newtonMoveDB.CurrentVector_kms + parentVelocity);
                    }
                    double newParentMass = newParent.GetDataBlob<MassVolumeDB>().Mass;
                    double sgp = GameConstants.Science.GravitationalConstant * (newParentMass + Mass_Kg) / 3.347928976e33;
                    Vector4 posRalitiveToNewParent = positionDB.AbsolutePosition_AU - newParent.GetDataBlob<PositionDB>().AbsolutePosition_AU;

                    var dateTime = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds - secondsToItterate);
                    var kE = OrbitMath.KeplerFromPositionAndVelocity(sgp, posRalitiveToNewParent, parentRalitiveVector, dateTime);

                    if (kE.Eccentricity < 1) //if we're going to end up in a regular orbit around our new parent
                    {
                        /*
                        var newOrbit = OrbitDB.FromKeplerElements(
                            newParent, 
                            newParentMass, 
                            Mass_Kg, 
                            kE,
                            dateTime);
                            */
                        var newOrbit = OrbitDB.FromVector(newParent, entity, parentRalitiveVector, dateTime);
                        entity.RemoveDataBlob<NewtonMoveDB>();
                        entity.SetDataBlob(newOrbit);
                        positionDB.SetParent(newParent);
                        var currentPos = Distance.AuToKm(positionDB.RelativePosition_AU);
                        var newPos = OrbitProcessor.GetPosition_AU(newOrbit, dateTime);
                        var newPosKM = Distance.AuToKm(newPos);
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
