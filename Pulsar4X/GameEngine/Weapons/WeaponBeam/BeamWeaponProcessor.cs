using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Engine;

namespace Pulsar4X.Weapons;

public class BeamWeaponProcessor : IHotloopProcessor
{
    public void Init(Game game) { }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        UpdateBeam(entity.GetDataBlob<BeamInfoDB>(), deltaSeconds);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var dbs = manager.GetAllDataBlobsOfType<BeamInfoDB>();
        foreach (BeamInfoDB db in dbs)
        {
            UpdateBeam(db, deltaSeconds);
        }

        return dbs.Count;
    }

    public TimeSpan RunFrequency { get; } = TimeSpan.FromSeconds(1);
    public TimeSpan FirstRunOffset { get; } = TimeSpan.FromSeconds(0);
    public Type GetParameterType { get; } = typeof(BeamInfoDB);



    public static void UpdateBeam(BeamInfoDB beamInfo, int seconds)
    {
        //adjust the vector to ensure the visuals line up with the target

        var state = (beamInfo.PosDB.AbsolutePosition, beamInfo.VelocityVector);
        var targetState = beamInfo.TargetEntity.GetAbsoluteState();
        var vectorToTarget = state.AbsolutePosition - targetState.pos;
        var timeToTarget = WeaponUtils.TimeToTarget(vectorToTarget, beamInfo.VelocityVector.Length());

        // we don't really need this, visuals are close enough for most things
        // beamInfo.VelocityVector = absVector;

        // TODO: we should update the physics and check for a collision?
        // If the beam hits this update
        if (timeToTarget <= seconds)
        {
            var nowTime = beamInfo.OwningEntity.StarSysDateTime;
            var futurePosTime = WeaponUtils.PredictTargetPositionAndTime(timeToTarget, nowTime, beamInfo.TargetEntity);
            OnPotentialHit(beamInfo, state, nowTime, futurePosTime);
        }
        else
        {
            UpdatePhysics(beamInfo, seconds);
        }
    }

    private static void OnPotentialHit(BeamInfoDB beamInfo, (Vector3 AbsolutePosition, Vector3 VelocityVector) state, DateTime nowTime, (Vector3 pos, double seconds) futurePosTime)
    {
        // FIXME: fix the base 95% chance to hit
        var tohit = ToHitChance(beamInfo.LaunchPosition, futurePosTime.pos, beamInfo.VelocityVector.Length(), 0.95);
        var hitsTarget = (beamInfo.OwningEntity.Manager as StarSystem).RNGNextBool(tohit);

        if(hitsTarget)
        {
            var posRelativeToTarget = futurePosTime.pos - state.AbsolutePosition;
            var shipFutureVel = beamInfo.TargetEntity.GetAbsoluteFutureVelocity(nowTime + TimeSpan.FromSeconds(futurePosTime.seconds));
            var relativeVelocity = shipFutureVel - beamInfo.VelocityVector;
            var freq = beamInfo.Frequency;

            DamageFragment damage = new DamageFragment()
            {
                Velocity = new Vector2(relativeVelocity.X, relativeVelocity.Y),
                Position = ((int)posRelativeToTarget.X, (int)posRelativeToTarget.Y),
                Mass = 0.000001f,
                Density = 1000,
                Momentum = (float)(UniversalConstants.Science.PlankConstant * freq),
                Length = (float)(beamInfo.Positions[0] - beamInfo.Positions[1]).Length(),
                Energy = beamInfo.Energy,
            };
            DamageProcessor.OnTakingDamage(beamInfo.TargetEntity, damage);
        }

        // FIXME: beam should continue on and dissipate on a miss
        beamInfo.OwningEntity.Destroy();
    }

    private static double ToHitChance(Vector3 launchPosition, Vector3 targetPosition, double beamSpeed, double baseHitChance)
    {
        double range = launchPosition.GetDistanceTo_m(targetPosition);

        //var ttt = BeamWeapnProcessor.TimeToTarget(range, launchingEntity.))
        //tempory timetotarget
        double ttt = range / beamSpeed; //this should be the closing speed (ie the velocity of the two, the beam speed and the range)
        double missChance = ttt * ( 1 - baseHitChance);
        return Math.Max(0, 1 - missChance); // avoid negative hit chances
    }

    private static void UpdatePhysics(BeamInfoDB beamInfo, int seconds)
    {
        beamInfo.PosDB.AbsolutePosition += beamInfo.VelocityVector * seconds;
        for (int j = 0; j < beamInfo.Positions.Length; j++)
        {
            beamInfo.Positions[j] += beamInfo.VelocityVector * seconds;
        }
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
            Positions = [startPos.AbsolutePosition, startPos.AbsolutePosition + normVector * beamlenInMeters],
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