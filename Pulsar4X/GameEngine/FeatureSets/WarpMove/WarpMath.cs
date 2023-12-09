using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;

namespace GameEngine.WarpMove;

public static class WarpMath
{
    /// <summary>
    /// recalculates a shipsMaxSpeed.
    /// </summary>
    /// <param name="ship"></param>
    public static void CalcMaxWarpAndEnergyUsage(Entity ship)
    {
        Dictionary<string, double> totalFuelUsage = new Dictionary<string, double>();
        var instancesDB = ship.GetDataBlob<ComponentInstancesDB>();
        int totalEnginePower = instancesDB.GetTotalEnginePower(out totalFuelUsage);
            
        //Note: TN aurora uses the TCS for max speed calcs. 
        WarpAbilityDB warpDB = ship.GetDataBlob<WarpAbilityDB>();
        warpDB.TotalWarpPower = totalEnginePower;
        //propulsionDB.FuelUsePerKM = totalFuelUsage;

        var mass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
        var maxSpeed = MaxSpeedCalc(totalEnginePower, mass);
        warpDB.MaxSpeed = maxSpeed;
            
    }
    
    /// <summary>
    /// Calculates max ship speed based on engine power and ship mass
    /// </summary>
    /// <param name="power">TotalEnginePower</param>
    /// <param name="tonage">HullSize</param>
    /// <returns>Max speed in km/s</returns>
    public static int MaxSpeedCalc(double power, double tonage)
    {
        // From Aurora4x wiki:  Speed = (Total Engine Power / Total Class Size in HS) * 1000 km/s
        return (int)((power / tonage) * 1000);
    }
    
    struct Orbit
    {
        public Vector3 position;
        public double T;
    }
    
    /// <summary>
    /// Calculates a cartisian position for an intercept for a ship and an target's orbit using warp.
    /// </summary>
    /// <returns>The intercept position and DateTime</returns>
    /// <param name="mover">The entity that is trying to intercept a target.</param>
    /// <param name="targetOrbit">Target orbit.</param>
    /// <param name="atDateTime">Datetime of transit start</param>
    public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition(Entity mover, OrbitDB targetOrbit, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
    {
        Vector3 moverPos = mover.GetAbsoluteFuturePosition(atDateTime);
        double spd_m = mover.GetDataBlob<WarpAbilityDB>().MaxSpeed;
        return WarpMath.GetInterceptPosition_m(moverPos, spd_m, targetOrbit, atDateTime, offsetPosition);
    }
    /// <summary>
    /// Calculates a cartisian position for an intercept for a ship and an target's orbit using warp.
    /// </summary>
    /// <param name="moverAbsolutePos"></param>
    /// <param name="speed"></param>
    /// <param name="targetOrbit"></param>
    /// <param name="atDateTime"></param>
    /// <param name="offsetPosition">position relative to the target object we wish to stop warp.</param>
    /// <returns></returns>
    public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition_m(Vector3 moverAbsolutePos, double speed, OrbitDB targetOrbit, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
    {

        var pos = moverAbsolutePos;
        double tim = 0;

        var pl = new Orbit()
        {
            position = moverAbsolutePos,
            T = targetOrbit.OrbitalPeriod.TotalSeconds,
        };

        double a = targetOrbit.SemiMajorAxis * 2;

        Vector3 p;
        int i;
        double tt, t, dt, a0, a1, T;
        // find orbital position with min error (coarse)
        a1 = -1.0;
        dt = 0.01 * pl.T;


        for (t=0; t< pl.T; t+=dt)
        {
            p = targetOrbit.GetAbsolutePosition_m(atDateTime + TimeSpan.FromSeconds(t));  //pl.position(sim_t + t);                     // try time t
            p += offsetPosition;
            tt = (p - pos).Length() / speed;  //length(p - pos) / speed;
            a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
            a0 /= pl.T;                                   // remove full periods from the difference
            a0 -= Math.Floor(a0);
            a0 *= pl.T;
            if ((a0 < a1) || (a1 < 0.0))
            {
                a1 = a0;
                tim = tt;
            }   // remember best option
        }
        // find orbital position with min error (fine)
        for (i = 0; i < 10; i++)                               // recursive increase of accuracy
            for (a1 = -1.0, t = tim - dt, T = tim + dt, dt *= 0.1; t < T; t += dt)
            {
                p = targetOrbit.GetAbsolutePosition_m(atDateTime + TimeSpan.FromSeconds(t));  //p = pl.position(sim_t + t);                     // try time t
                p += offsetPosition;
                tt = (p - pos).Length() / speed;  //tt = length(p - pos) / speed;
                a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
                a0 /= pl.T;                                   // remove full periods from the difference
                a0 -= Math.Floor(a0);
                a0 *= pl.T;
                if ((a0 < a1) || (a1 < 0.0))
                {
                    a1 = a0;
                tim = tt;
                }   // remember best option
            }
        // direction
        p = targetOrbit.GetAbsolutePosition_m(atDateTime + TimeSpan.FromSeconds(tim));//pl.position(sim_t + tim);
        p += offsetPosition;
        //dir = normalize(p - pos);
        return (p, atDateTime + TimeSpan.FromSeconds(tim));
    }

}