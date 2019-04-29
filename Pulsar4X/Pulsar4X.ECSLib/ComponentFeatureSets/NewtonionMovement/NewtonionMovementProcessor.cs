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


        public static void NewtonMove(Entity entity, int deltaSeconds)
        {
            double timeStep = deltaSeconds; 
            NewtonMoveDB newtonMoveDB = entity.GetDataBlob<NewtonMoveDB>();
            PositionDB positionDB = entity.GetDataBlob<PositionDB>();
            double Mass_Kg = entity.GetDataBlob<MassVolumeDB>().Mass;
            double ParentMass_kg = newtonMoveDB.ParentMass;

            double secondsToItterate = deltaSeconds;
            while (secondsToItterate > 0) 
            {
                double speed_kms = newtonMoveDB.CurrentVector_kms.Length();

                timeStep = GMath.Clamp(secondsToItterate / speed_kms, new MinMaxStruct() { Min = 1, Max = secondsToItterate });

                double distanceToParent_m = Distance.AuToMt(positionDB.GetDistanceTo(newtonMoveDB.SOIParent.GetDataBlob<PositionDB>()));

                distanceToParent_m = Math.Max(distanceToParent_m, 0.1); //don't let the distance be 0 (once collision is in this will likely never happen anyway)

                double gravForce = GameConstants.Science.GravitationalConstant * (Mass_Kg * ParentMass_kg / Math.Pow(distanceToParent_m, 2));
                Vector4 gravForceVector = gravForce * -Vector4.Normalise(positionDB.RelativePosition_AU);

                var totalForce = gravForceVector + newtonMoveDB.ThrustVector;

                Vector4 totalAcceleration = totalForce / Mass_Kg; 
                newtonMoveDB.CurrentVector_kms += totalAcceleration * 0.01; //convert m/s to km/s

                var DistanceToMove_Km = newtonMoveDB.CurrentVector_kms * timeStep;

                positionDB.RelativePosition_AU += Distance.KmToAU(DistanceToMove_Km);
                double sOIRadius_AU = OrbitProcessor.GetSOI(newtonMoveDB.SOIParent);
                if (positionDB.RelativePosition_AU.Length() >= sOIRadius_AU)
                {
                    Entity newParent;
                    Vector4 parentRalitiveVector;
                    if (newtonMoveDB.SOIParent.HasDataBlob<OrbitDB>())
                    {
                        var orbitDB = newtonMoveDB.SOIParent.GetDataBlob<OrbitDB>();
                        newParent = orbitDB.Parent;
                        var parentVelocity = OrbitProcessor.PreciseOrbitalVelocityVector(orbitDB, entity.Manager.ManagerSubpulses.StarSysDateTime);
                        parentRalitiveVector = Distance.KmToAU(newtonMoveDB.CurrentVector_kms) + parentVelocity;
                        var pvlen = Distance.AuToKm( parentVelocity.Length());
                        var vlen = newtonMoveDB.CurrentVector_kms.Length();
                        var rvlen = Distance.AuToKm( parentRalitiveVector.Length());
                    }
                    else //if (newtonMoveDB.SOIParent.HasDataBlob<NewtonMoveDB>())
                    { 
                        newParent = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().SOIParent;
                        var parentVelocity = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().CurrentVector_kms;
                        parentRalitiveVector = Distance.KmToAU(newtonMoveDB.CurrentVector_kms + parentVelocity);
                    }
                    double newParentMass = newParent.GetDataBlob<MassVolumeDB>().Mass;
                    double sgp = GameConstants.Science.GravitationalConstant * (newParentMass + Mass_Kg) / 3.347928976e33;
                    Vector4 posRalitiveToNewParent = positionDB.AbsolutePosition_AU - newParent.GetDataBlob<PositionDB>().AbsolutePosition_AU;
                     
                    var kE = OrbitMath.KeplerFromPositionAndVelocity(sgp, posRalitiveToNewParent, parentRalitiveVector);
                    var dateTime = entity.Manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds - secondsToItterate);
                    if (kE.Eccentricity < 1)
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
                    else
                    {
                        positionDB.SetParent(newParent);
                        newtonMoveDB.ParentMass = newParentMass;
                        newtonMoveDB.SOIParent = newParent;
                        
                    }

                }
                secondsToItterate -= timeStep;
            }
        }
    }

    public class NewtonMoveDB : BaseDataBlob
    {
        //internal DateTime TimeSinceLastRun { get; set; }
        public Vector4 ThrustVector { get; internal set; } = Vector4.Zero;
        public Vector4 CurrentVector_kms { get; internal set; }

        public Entity SOIParent { get; internal set; }
        public double ParentMass { get; internal set; }

        public NewtonMoveDB() { }

        public NewtonMoveDB(Entity sphereOfInfluenceParent)         
        {
            SOIParent = sphereOfInfluenceParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().Mass;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
