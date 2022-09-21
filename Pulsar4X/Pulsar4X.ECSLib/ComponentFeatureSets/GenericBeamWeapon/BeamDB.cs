using System;
using System.Collections.Generic;
using System.Net;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.GenericBeamWeapon
{
    public class BeamWeapnProcessor : IHotloopProcessor
    {
        private static readonly int _beamInfoIndex = EntityManager.GetTypeIndex<BeamInfoDB>();
        public void Init(Game game)
        {
            //donothing
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            BeamMovePhysics(entity.GetDataBlob<BeamInfoDB>(_beamInfoIndex), deltaSeconds);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var dbs = manager.GetAllDataBlobsOfType<BeamInfoDB>(_beamInfoIndex);
            foreach (BeamInfoDB db in dbs)
            { 
                BeamMovePhysics(db, deltaSeconds);
            }

            return dbs.Count;
        }

        public TimeSpan RunFrequency { get; } = TimeSpan.FromSeconds(1);
        public TimeSpan FirstRunOffset { get; } = TimeSpan.FromSeconds(0);
        public Type GetParameterType { get; } = typeof(BeamInfoDB);

        public static void BeamMovePhysics(BeamInfoDB beamInfo, int seconds)
        {
            if (beamInfo.HitsTarget)
            {
                //adjust the vector to ensure the visuals line up with the target
                var state = (beamInfo.PosDB.AbsolutePosition, beamInfo.VelocityVector);
                var nowTime = beamInfo.OwningEntity.StarSysDateTime;
                var futurePosTime = PredictTgtPositionAndTime(state, nowTime, beamInfo.TargetEntity, beamInfo.VelocityVector.Length());
                var normVector = Vector3.Normalise(futurePosTime.pos - state.AbsolutePosition);
                var absVector =  normVector * beamInfo.VelocityVector.Length();
                
                beamInfo.VelocityVector = absVector;

                if (futurePosTime.seconds <= seconds)
                {
                    //var ralitivePos = state.AbsolutePosition_m - futurePosTime.pos;
                    var posRalitiveToTarget = futurePosTime.pos - state.AbsolutePosition;
                    var beamAngle = Math.Atan2(posRalitiveToTarget.Y, posRalitiveToTarget.X);
                    var shipFutureVel = beamInfo.TargetEntity.GetAbsoluteFutureVelocity(nowTime + TimeSpan.FromSeconds(futurePosTime.seconds));
                    var shipHeading = Math.Atan2(shipFutureVel.Y, shipFutureVel.X);
                    var hitAngle = beamAngle + shipHeading;
                    var ralitiveVel = shipFutureVel - beamInfo.VelocityVector;
                    var ralitiveSpeed = ralitiveVel.Length();
                    var freq = beamInfo.Frequency;
                    
                    DamageFragment damage = new DamageFragment()
                    {
                        Velocity = new Vector2( ralitiveVel.X, ralitiveVel.Y),
                        Position = ((int)posRalitiveToTarget.X, (int)posRalitiveToTarget.Y),
                        //Angle = hitAngle,
                        Mass = 0.000001f,
                        Density = 1000,
                        Momentum = (float)(UniversalConstants.Science.PlankConstant * freq),
                        Length = (float)(beamInfo.Positions[0] - beamInfo.Positions[1]).Length(),
                    };
                    var slides = DamageProcessor.OnTakingDamage(beamInfo.TargetEntity, damage);
                    beamInfo.OwningEntity.Destroy();
                }
                else
                {
                    beamInfo.PosDB.AbsolutePosition += beamInfo.VelocityVector * seconds;
                    for (int j = 0; j < beamInfo.Positions.Length; j++)
                    {
                        beamInfo.Positions[j] += beamInfo.VelocityVector * seconds;
                    }
                }
            }
            
            else
            {
                beamInfo.PosDB.AbsolutePosition += beamInfo.VelocityVector * seconds;
                for (int j = 0; j < beamInfo.Positions.Length; j++)
                {
                    beamInfo.Positions[j] += beamInfo.VelocityVector * seconds;
                }
            }
        }
        
        public static void FireBeamWeapon(Entity launchingEntity, Entity targetEntity, bool hitsTarget,double freqency, double beamVelocity, double beamLenInSeconds)
        {
            var nowTime = launchingEntity.StarSysDateTime;
            var ourState = launchingEntity.GetAbsoluteState();
            var futurePosTime = PredictTgtPositionAndTime(ourState, nowTime, targetEntity, beamVelocity);
            
            var ourAbsPos = launchingEntity.GetAbsoluteFuturePosition(nowTime);
            var normVector = Vector3.Normalise(futurePosTime.pos - ourAbsPos);
            var absVector =  normVector * beamVelocity;
            var startPos = (PositionDB)launchingEntity.GetDataBlob<PositionDB>().Clone();
            var beamInfo = new BeamInfoDB(launchingEntity.Guid, targetEntity, hitsTarget);
            var beamlenInMeters = beamLenInSeconds * 299792458;
            beamInfo.Positions = new Vector3[2];
            beamInfo.Positions[0] = startPos.AbsolutePosition ;
            beamInfo.Positions[1] = startPos.AbsolutePosition - normVector * beamlenInMeters;
            beamInfo.VelocityVector = absVector;
            beamInfo.Frequency = freqency;
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add(beamInfo);
            //dataBlobs.Add(new ComponentInstancesDB());
            dataBlobs.Add(startPos);
            //dataBlobs.Add(new NameDB("Beam", launchingEntity.FactionOwner, "Beam" ));

            var newbeam = Entity.Create(launchingEntity.Manager, launchingEntity.FactionOwnerID, dataBlobs);
        }

        public static (Vector3 pos, double seconds) PredictTgtPositionAndTime((Vector3 pos, Vector3 Velocity) ourState, DateTime atTime, Entity targetEntity, double beamVelocity)
        {
            
            var tgtState = targetEntity.GetAbsoluteState();
            Vector3 leadToTgt = (ourState.Velocity - tgtState.Velocity);
            Vector3 vectorToTgt = (ourState.pos -tgtState.pos);
            var distanceToTgt = vectorToTgt.Length();
            var timeToTarget = distanceToTgt / beamVelocity;
            var futureDate = atTime + TimeSpan.FromSeconds(timeToTarget);
            var futurePosition = targetEntity.GetAbsoluteFuturePosition(futureDate);
            return (futurePosition, timeToTarget);

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
                var futurePosition = targetEntity.GetRelativeFuturePosition(futureDate);
                    
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
        
        public static double TimeToTarget(double distanceToTgt, Vector3 ourVelocity, Vector3 targetVelocity)
        {
            Vector3 leadToTgt = targetVelocity - ourVelocity;
            var closingSpeed = leadToTgt.Length() ;
            var timeToTarget = distanceToTgt / closingSpeed;
            return timeToTarget;
        }
    }

    public class BeamInfoDB : BaseDataBlob
    {
        public double Frequency;
        public Guid FiredBy;
        public Vector3 VelocityVector;
        public Vector3[] Positions;
        public bool HitsTarget;
        public Entity TargetEntity;
        private PositionDB _posDB;
        public PositionDB PosDB {
            get
            {
                if (_posDB == null)
                    _posDB = OwningEntity.GetDataBlob<PositionDB>();
                return _posDB;
            }}

        public BeamInfoDB(Guid launchedBy, Entity targetEntity, bool hitsTarget)
        {
            FiredBy = launchedBy;
            TargetEntity = targetEntity;
            HitsTarget = hitsTarget;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}