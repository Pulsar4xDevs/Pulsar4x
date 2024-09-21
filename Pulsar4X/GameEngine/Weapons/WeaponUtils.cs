using System;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;

namespace Pulsar4X.Weapons;

public class WeaponUtils
{
    /// <summary>
    /// Returns seconds to target
    /// </summary>
    /// <param name="distanceToTarget">Distance to target (in meters)</param>
    /// <param name="ourVelocity">Source velocity</param>
    /// <param name="targetVelocity">Target velocity</param>
    /// <returns></returns>
    public static double TimeToTarget(double distanceToTarget, Vector3 ourVelocity, Vector3 targetVelocity)
    {
        return distanceToTarget / (targetVelocity - ourVelocity).Length();
    }

    public static double TimeToTarget(Vector3 vectorToTarget, double weaponVelocity)
    {
        return vectorToTarget.Length() / weaponVelocity;
    }

    public static (Vector3 pos, double seconds) PredictTargetPositionAndTime((Vector3 pos, Vector3 Velocity) ourState, DateTime atTime, Entity targetEntity, double weaponVelocity)
    {
        var targetState = targetEntity.GetAbsoluteState();
        var vectorToTarget = ourState.pos - targetState.pos;
        var timeToTarget = TimeToTarget(vectorToTarget, weaponVelocity);
        var futureDate = atTime + TimeSpan.FromSeconds(timeToTarget);
        var futurePosition = targetEntity.GetAbsoluteFuturePosition(futureDate);
        return (futurePosition, timeToTarget);
    }

    public static (Vector3 pos, double seconds) PredictTargetPositionAndTime(double timeToTarget, DateTime atTime, Entity targetEntity)
    {
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
}