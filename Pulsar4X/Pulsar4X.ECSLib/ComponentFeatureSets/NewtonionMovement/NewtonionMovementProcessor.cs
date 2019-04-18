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
