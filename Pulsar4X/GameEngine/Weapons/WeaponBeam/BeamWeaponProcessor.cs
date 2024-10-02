using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Engine;
using Pulsar4X.Damage;
using Pulsar4X.Events;

namespace Pulsar4X.Weapons;

public class BeamWeaponProcessor : IHotloopProcessor
{
    public void Init(Game game) { }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        if(entity.IsValid)
            UpdateBeam(entity.GetDataBlob<BeamInfoDB>(), deltaSeconds);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var dbs = manager.GetAllDataBlobsOfType<BeamInfoDB>();
        foreach (BeamInfoDB db in dbs)
        {
            if(db.OwningEntity.IsValid)
                UpdateBeam(db, deltaSeconds);
        }

        return dbs.Count;
    }

    public TimeSpan RunFrequency { get; } = TimeSpan.FromSeconds(1);
    public TimeSpan FirstRunOffset { get; } = TimeSpan.FromSeconds(0);
    public Type GetParameterType { get; } = typeof(BeamInfoDB);



    public static void UpdateBeam(BeamInfoDB beamInfo, int seconds)
    {
        if(!beamInfo.TargetEntity.IsValid)
        {
            // FIXME: beam should probably continue on for a bit and dissipate instead of abrupting removing itself from the game
            beamInfo.OwningEntity.Destroy();
            return;
        }

        var nowTime = beamInfo.OwningEntity.StarSysDateTime;
        var state = (beamInfo.PosDB.AbsolutePosition, beamInfo.VelocityVector);
        (Vector3 pos, double seconds) futurePosTime;

        switch(beamInfo.BeamState)
        {
            case BeamInfoDB.BeamStates.Fired:
                var targetState = beamInfo.TargetEntity.GetAbsoluteState();
                var vectorToTarget = state.AbsolutePosition - targetState.pos;
                var timeToTarget = WeaponUtils.TimeToTarget(vectorToTarget, beamInfo.VelocityVector.Length());

                // we don't really need this, visuals are close enough for most things
                // beamInfo.VelocityVector = absVector;

                // TODO: we should update the physics and check for a collision?
                // If the beam hits this update
                if (timeToTarget <= seconds)
                {
                    beamInfo.BeamState = BeamInfoDB.BeamStates.AtTarget;
                    
                    futurePosTime = WeaponUtils.PredictTargetPositionAndTime(timeToTarget, nowTime, beamInfo.TargetEntity);
                    beamInfo.Positions.Item1 = beamInfo.Positions.Item2;
                    beamInfo.Positions.Item2 = futurePosTime.pos;
                }
                else
                {
                    UpdatePhysics(beamInfo, seconds);
                }
                break;
            case BeamInfoDB.BeamStates.AtTarget:
                    futurePosTime = WeaponUtils.PredictTargetPositionAndTime(0.0, nowTime, beamInfo.TargetEntity);
                    OnPotentialHit(beamInfo, state, nowTime, futurePosTime);
                break;
        }        
    }

    private static void OnPotentialHit(BeamInfoDB beamInfo, (Vector3 AbsolutePosition, Vector3 VelocityVector) state, DateTime nowTime, (Vector3 pos, double seconds) futurePosTime)
    {
        // FIXME: fix the base 95% chance to hit
        var tohit = WeaponUtils.ToHitChance(beamInfo.LaunchPosition, futurePosTime.pos, beamInfo.VelocityVector.Length(), 0.95);
        var hitsTarget = (beamInfo.OwningEntity.Manager as StarSystem).RNGNextBool(tohit);

        if(hitsTarget)
        {
            // var posRelativeToTarget = futurePosTime.pos - state.AbsolutePosition;
            // var shipFutureVel = beamInfo.TargetEntity.GetAbsoluteFutureVelocity(nowTime + TimeSpan.FromSeconds(futurePosTime.seconds));
            // var relativeVelocity = shipFutureVel - beamInfo.VelocityVector;
            // var freq = beamInfo.Frequency;

            // DamageFragment damage = new DamageFragment()
            // {
            //     Velocity = new Vector2(relativeVelocity.X, relativeVelocity.Y),
            //     Position = ((int)posRelativeToTarget.X, (int)posRelativeToTarget.Y),
            //     Mass = 0.000001f,
            //     Density = 1000,
            //     Momentum = (float)(UniversalConstants.Science.PlankConstant * freq),
            //     Length = (float)(beamInfo.Positions[0] - beamInfo.Positions[1]).Length(),
            //     Energy = beamInfo.Energy,
            // };
            // DamageProcessor.OnTakingDamage(beamInfo.TargetEntity, damage);

            var damageResult = SimpleDamage.OnTakingDamage(beamInfo.TargetEntity, 100, 500);

            if(damageResult.Destroyed)
            {
                // Target was destroyed
                EventManager.Instance.Publish(
                    Event.Create(
                        EventType.TargetDestroyed,
                        nowTime,
                        "Target has been destroyed",
                        beamInfo.OwningEntity.FactionOwnerID,
                        beamInfo.OwningEntity.Manager.ManagerID,
                        beamInfo.TargetEntity.Id,
                        new List<int>()
                        {
                            beamInfo.OwningEntity.FactionOwnerID,
                            beamInfo.TargetEntity.FactionOwnerID
                        }));
            }
            else if(damageResult.Damage > 0)
            {
                // Target took damage
                EventManager.Instance.Publish(
                    Event.Create(
                        EventType.TargetHit,
                        nowTime,
                        $"Target hit for {damageResult.Damage} damage",
                        beamInfo.OwningEntity.FactionOwnerID,
                        beamInfo.OwningEntity.Manager.ManagerID,
                        beamInfo.TargetEntity.Id,
                        new List<int>()
                        {
                            beamInfo.OwningEntity.FactionOwnerID,
                            beamInfo.TargetEntity.FactionOwnerID
                        }));
            }
        }
        else
        {
            // Target was missed
            EventManager.Instance.Publish(
                Event.Create(
                    EventType.TargetMissed,
                    nowTime,
                    $"Missed target",
                    beamInfo.OwningEntity.FactionOwnerID,
                    beamInfo.OwningEntity.Manager.ManagerID,
                    beamInfo.TargetEntity.Id,
                    new List<int>()
                    {
                        beamInfo.OwningEntity.FactionOwnerID,
                        beamInfo.TargetEntity.FactionOwnerID
                    }));
        }

        // FIXME: beam should continue on and dissipate on a miss
        beamInfo.OwningEntity.Destroy();
    }    

    private static void UpdatePhysics(BeamInfoDB beamInfo, int seconds)
    {
        beamInfo.PosDB.AbsolutePosition += beamInfo.VelocityVector * seconds;
        beamInfo.Positions.Item1 = beamInfo.Positions.Item2;
        beamInfo.Positions.Item2 = beamInfo.PosDB.AbsolutePosition;
    }

    public static void FireBeamWeapon(Entity launchingEntity, Entity targetEntity, bool hitsTarget, double energy, double wavelen, double beamVelocity, double beamLenInSeconds)
    {
        var nowTime = launchingEntity.StarSysDateTime;
        var ourState = launchingEntity.GetAbsoluteState();
        var targetFuturePosTime = WeaponUtils.PredictTargetPositionAndTime(ourState, nowTime, targetEntity, beamVelocity);

        var ourAbsPos = launchingEntity.GetAbsoluteFuturePosition(nowTime);
        var normVector = Vector3.Normalise(targetFuturePosTime.pos - ourAbsPos);
        var absVector =  normVector * beamVelocity;
        var startPos = (PositionDB)launchingEntity.GetDataBlob<PositionDB>().Clone();
        var beamlenInMeters = beamLenInSeconds * UniversalConstants.Units.SpeedOfLightInMetresPerSecond;

        // Setup the beam entity
        var beamInfo = new BeamInfoDB(launchingEntity.Id, targetEntity, hitsTarget)
        {
            Positions = (startPos.AbsolutePosition, startPos.AbsolutePosition),
            LaunchPosition = startPos.AbsolutePosition,
            VelocityVector = absVector,
            Frequency = wavelen,
            Energy = energy
        };

        var dataBlobs = new List<BaseDataBlob>()
        {
            beamInfo,
            startPos
        };

        var newbeam = Entity.Create(launchingEntity.FactionOwnerID);

        // Add the beam to the game
        launchingEntity.Manager.AddEntity(newbeam, dataBlobs);
    }
}