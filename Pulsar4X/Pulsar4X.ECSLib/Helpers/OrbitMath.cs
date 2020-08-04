using System;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Orbit math.
    /// note multiple simular functions for doing the same thing, some of these are untested.
    /// Take care when using unless the function has a decent test in the tests project. 
    /// Some simular functions with simular inputs left in for future performance testing (ie one of the two might be slightly more performant).
    /// </summary>
    public class OrbitMath : OrbitalMath
    {
        #region VelocityAndSpeed;

        /// <summary>
        /// Calculates distance/s on an orbit by calculating positions now and second in the future. 
        /// Fairly slow and inefficent. 
        /// </summary>
        /// <returns>the distance traveled in a second</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDatetime">At datetime.</param>
        public static double Hackspeed(OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDatetime);
            var pos2 = OrbitProcessor.GetPosition_AU(orbit, atDatetime + TimeSpan.FromSeconds(1));

            return Distance.DistanceBetween(pos1, pos2);
        }

        public static double HackVelocityHeading(OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDatetime);
            var pos2 = OrbitProcessor.GetPosition_AU(orbit, atDatetime + TimeSpan.FromSeconds(1));

            Vector3 vector = pos2 - pos1;
            double heading = Math.Atan2(vector.Y, vector.X);
            return heading;
        }

        public static Vector3 HackVelocityVector(OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDatetime);
            var pos2 = OrbitProcessor.GetPosition_AU(orbit, atDatetime + TimeSpan.FromSeconds(1));
            //double speed = Distance.DistanceBetween(pos1, pos2);
            return pos2 - pos1;
        }

        /// <summary>
        /// This is an aproximation of the mean velocity of an orbit. 
        /// </summary>
        /// <returns>The orbital velocity in au.</returns>
        /// <param name="orbit">Orbit.</param>
        public static double MeanOrbitalVelocityInAU(OrbitDB orbit)
        {
            double a = orbit.SemiMajorAxis_AU;
            double b = EllipseMath.SemiMinorAxis(a, orbit.Eccentricity);
            double orbitalPerodSeconds = orbit.OrbitalPeriod.TotalSeconds;
            double peremeter = Math.PI * (3* (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
            return peremeter  / orbitalPerodSeconds;
        }

        public static double MeanOrbitalVelocityInm(OrbitDB orbit)
        {
            double a = orbit.SemiMajorAxis;
            double b = EllipseMath.SemiMinorAxis(a, orbit.Eccentricity);
            double orbitalPerodSeconds = orbit.OrbitalPeriod.TotalSeconds;
            double peremeter = Math.PI * (3* (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
            return peremeter  / orbitalPerodSeconds;
        }

        #endregion

        #region Time

        /// <summary>
        /// Incorrect/Incomplete Unfinished DONOTUSE
        /// </summary>
        /// <returns>The to radius from periapsis.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="radiusAU">Radius au.</param>
        public static double TimeToRadiusFromPeriapsis(OrbitDB orbit, double radiusAU)
        {
            throw new NotImplementedException();
            var a = orbit.SemiMajorAxis_AU;
            var e = orbit.Eccentricity;
            var p = EllipseMath.SemiLatusRectum(a, e);
            var angle = AngleAtRadus(radiusAU, p, e);
            //var meanAnomaly = CurrentMeanAnomaly(orbit.MeanAnomalyAtEpoch, meanMotion, )
            return TimeFromPeriapsis(a, orbit.GravitationalParameterAU, orbit.MeanAnomalyAtEpoch_Degrees);
        }

        #endregion


        /// <summary>
        /// Currently this only calculates the change in velocity from 0 to planet radius +* 0.33333.
        /// TODO: add gravity drag and atmosphere drag, and tech improvements for such.  
        /// </summary>
        /// <param name="planetEntity"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static double FuelCostToLowOrbit(Entity planetEntity, double payload)
        {
            var lowOrbit = LowOrbitRadius(planetEntity);
            
            var exaustVelocity = 275;
            var sgp = OrbitMath.CalculateStandardGravityParameterInKm3S2(payload, planetEntity.GetDataBlob<MassVolumeDB>().MassDry);
            Vector3 pos = new Vector3(lowOrbit, 0, 0);
            
            var vel = OrbitMath.ObjectLocalVelocityPolar(sgp, pos, lowOrbit, 0, 0, 0);
            var fuelCost = OrbitMath.TsiolkovskyFuelCost(payload, exaustVelocity, vel.speed);
            return fuelCost;
        }

        public static double LowOrbitRadius(Entity planetEntity)
        {
            var prad = planetEntity.GetDataBlob<MassVolumeDB>().RadiusInM;
            double alt = prad * 0.33333;
            var lowOrbit = prad + alt;
            return lowOrbit;
        }



        struct orbit
        {
            public Vector3 position;
            public double T;
        }
        
        /// <summary>
        /// 
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
            
            var pl = new orbit()
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
                p = OrbitProcessor.GetAbsolutePosition_m(targetOrbit, atDateTime + TimeSpan.FromSeconds(t));  //pl.position(sim_t + t);                     // try time t
                p += offsetPosition;
                tt = Vector3.Magnitude(p - pos) / speed;  //length(p - pos) / speed;
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
                    p = OrbitProcessor.GetAbsolutePosition_m(targetOrbit, atDateTime + TimeSpan.FromSeconds(t));  //p = pl.position(sim_t + t);                     // try time t
                    p += offsetPosition;    
                    tt = Vector3.Magnitude(p - pos) / speed;  //tt = length(p - pos) / speed;
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
            p = OrbitProcessor.GetAbsolutePosition_m(targetOrbit, atDateTime + TimeSpan.FromSeconds(tim));//pl.position(sim_t + tim);
            p += offsetPosition;
            //dir = normalize(p - pos);
            return (p, atDateTime + TimeSpan.FromSeconds(tim));
        }

        
    }
}
