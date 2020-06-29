using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.GenericBeamWeapon
{
    public class BeamWeapnProcessor : IHotloopProcessor
    {
        public void Init(Game game)
        {
            //dotnothing
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            BeamMovePhysics(entity.GetDataBlob<BeamInfoDB>(), deltaSeconds);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var dbs = manager.GetAllDataBlobsOfType<BeamInfoDB>();
            foreach (BeamInfoDB db in dbs)
            { 
                BeamMovePhysics(db, deltaSeconds);
            }
        }

        public TimeSpan RunFrequency { get; } = TimeSpan.FromSeconds(1);
        public TimeSpan FirstRunOffset { get; } = TimeSpan.FromSeconds(0);
        public Type GetParameterType { get; } = typeof(BeamInfoDB);

        public static void BeamMovePhysics(BeamInfoDB beamInfo, int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                beamInfo.PosDB.AbsolutePosition_m += beamInfo.VelocityVector;
                for (int j = 0; j < beamInfo.Positions.Length; j++)
                {
                    beamInfo.Positions[j] += beamInfo.VelocityVector;
                }
            }
        }
        
        public static void FireBeamWeapon(Entity launchingEntity, Entity targetEntity, double beamVelocity, double beamLenInSeconds)
        {
            var ourState = Entity.GetRalitiveState(launchingEntity);
            var tgtState = Entity.GetRalitiveState(targetEntity);
            
            Vector3 leadToTgt = (tgtState.Velocity - ourState.Velocity);
            Vector3 vectorToTgt = (tgtState.pos = ourState.pos);
            var distanceToTgt = vectorToTgt.Length();
            var timeToTarget = distanceToTgt / beamVelocity;
            var futureDate = launchingEntity.StarSysDateTime + TimeSpan.FromSeconds(timeToTarget);
            var futurePosition = Entity.GetAbsoluteFuturePosition(targetEntity, futureDate);
            var ourAbsPos = Entity.GetAbsoluteFuturePosition(launchingEntity, futureDate);
            var normVector = Vector3.Normalise(futurePosition - ourAbsPos);
            var absVector =  normVector * beamVelocity;
            var startPos = (PositionDB)launchingEntity.GetDataBlob<PositionDB>().Clone();
            var beamInfo = new BeamInfoDB(launchingEntity.Guid);
            var beamlenInMeters = beamLenInSeconds * 299792458;
            beamInfo.Positions = new Vector3[2];
            beamInfo.Positions[0] = startPos.AbsolutePosition_m ;
            beamInfo.Positions[1] = startPos.AbsolutePosition_m - normVector * beamlenInMeters;
            beamInfo.VelocityVector = absVector;
            
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add(beamInfo);
            //dataBlobs.Add(new ComponentInstancesDB());
            dataBlobs.Add(startPos);
            //dataBlobs.Add(new NameDB("Beam", launchingEntity.FactionOwner, "Beam" ));

            var newbeam = Entity.Create(launchingEntity.Manager, launchingEntity.FactionOwner, dataBlobs);
        }
        
        Vector3 LeadVector(
            double dvToUse, 
            double burnTime, 
            Entity targetEntity,
            (Vector3 pos, Vector3 Velocity) ourState, 
            (Vector3 pos, Vector3 Velocity) tgtState, 
            DateTime atDateTime )
        {
            var distanceToTgt = (ourState.pos - tgtState.pos).Length();
            var tgtBearing = tgtState.pos - ourState.pos;
            
            Vector3 leadToTgt = tgtState.Velocity - ourState.Velocity;
            var closingSpeed = leadToTgt.Length() ;
            double newttt = distanceToTgt / closingSpeed;
            double oldttt = 0;
            int itterations = 0;
            
            while (Math.Abs(newttt - oldttt) > 1) //itterate till we get a solution that's less than a second difference from last.
            {
                oldttt = newttt;

                TimeSpan timespanToIntercept = TimeSpan.MaxValue;
                if (newttt * 10000000 <= long.MaxValue)
                {
                    timespanToIntercept = TimeSpan.FromSeconds(newttt);
                }
                DateTime futureDate = atDateTime + timespanToIntercept;
                var futurePosition = Entity.GetRalitiveFuturePosition(targetEntity, futureDate);
                    
                tgtBearing = futurePosition - ourState.pos;
                distanceToTgt = (tgtBearing).Length();

                leadToTgt = tgtState.Velocity - ourState.Velocity;
                closingSpeed = leadToTgt.Length() ;
                newttt = distanceToTgt / closingSpeed;
                
                itterations++;

            }
            
            var vectorToTgt = Vector3.Normalise(tgtBearing);
            var deltaVVector = vectorToTgt * dvToUse;
            
            return vectorToTgt * dvToUse;
        }
        
        double TimeToTarget(double distanceToTgt, Vector3 ourVelocity, Vector3 targetVelocity)
        {
            Vector3 leadToTgt = targetVelocity - ourVelocity;
            var closingSpeed = leadToTgt.Length() ;
            var timeToTarget = distanceToTgt / closingSpeed;
            return timeToTarget;
        }
    }

    public class BeamInfoDB : BaseDataBlob
    {
        public Guid FiredBy;
        public Vector3 VelocityVector;
        public Vector3[] Positions;
        private PositionDB _posDB;
        public PositionDB PosDB {
            get
            {
                if (_posDB == null)
                    _posDB = OwningEntity.GetDataBlob<PositionDB>();
                return _posDB;
            }}

        public BeamInfoDB(Guid launchedBy)
        {
            FiredBy = launchedBy;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}