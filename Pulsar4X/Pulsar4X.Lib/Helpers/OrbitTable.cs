using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Entities;

namespace Pulsar4X.Lib
{
    public class OrbitTable
    {
        private static OrbitTable m_oInstance;
        public static OrbitTable Instance
        {
            get
            {
                if (m_oInstance == null)
                {
                    m_oInstance = new OrbitTable();
                }
                return m_oInstance;
            }
        }

        public void FindPolarPosition(OrbitingEntity theOrbit, long DaysSinceEpoch, out double angle, out double radius)
        {

            long orbitPeriod = (long)(theOrbit.OrbitalPeriod);


            /// <summary>
            /// orbitFraction is essentially mean Anomaly
            /// </summary>
            //double orbitFraction = 1.0 * ((DaysSinceEpoch + theOrbit.TimeSinceApogee) % orbitPeriod) / orbitPeriod;
            double orbitFraction = 1.0 * theOrbit.TimeSinceApogee / orbitPeriod;
            double orbitFractionRemainder = 1.0 * ((float)(theOrbit.TimeSinceApogeeRemainder / (float)Constants.TimeInSeconds.Day) / (float)orbitPeriod);

            orbitFraction = orbitFraction + orbitFractionRemainder;

#warning how can this orbit code be made more efficient?

            /// <summary>
            /// Eccentric anomaly Calculation code. I hope. this is copied from http://www.jgiesen.de/ 's Solving Kepler's Equation
            /// </summary>
            int iteration = 0;
            int maxIteration = 30;
            double delta = Math.Pow(10, -10);
            double radian = Math.PI / 180.0;
            double meanAnomaly = orbitFraction * 360.0;
            double eccentricAnomaly = Math.PI;
            if (theOrbit.Eccentricity < 0.8)
                eccentricAnomaly = meanAnomaly;

            /// <summary>
            /// I don't know any more descriptive name for this than partial, I'm not sure what it represents.
            /// </summary>
            double partial = eccentricAnomaly - theOrbit.Eccentricity * Math.Sin((meanAnomaly * radian)) - meanAnomaly;

            while ((Math.Abs(partial) > delta) && (iteration < maxIteration))
            {
                eccentricAnomaly = eccentricAnomaly - partial / (1.0 - theOrbit.Eccentricity * Math.Cos((eccentricAnomaly * radian)));
                partial = eccentricAnomaly - theOrbit.Eccentricity * Math.Sin((eccentricAnomaly * radian)) - meanAnomaly;
                iteration = iteration + 1;
            }

            eccentricAnomaly = eccentricAnomaly * radian;


            /// <summary>
            /// True Anomaly Calculation code, from same place as above.
            /// </summary>

            double SinV = Math.Sin(eccentricAnomaly);
            double CosV = Math.Cos(eccentricAnomaly);

            /// <summary>
            /// I called this Numerator because that is what this particular section is in many functions. Otherwise I'm not sure if it has a more descriptive name.
            /// </summary>
            double Numerator = Math.Sqrt(1.0 - (theOrbit.Eccentricity * theOrbit.Eccentricity));
            double trueAnomaly = Math.Atan2((Numerator * SinV), (CosV - theOrbit.Eccentricity)) /*/ radian*/;

            angle = trueAnomaly;

            theOrbit.TrueAnomaly = trueAnomaly;

            radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(trueAnomaly));
        }

        public void UpdatePosition(OrbitingEntity theOrbit, long deltaSeconds)
        {
            double x, y;

            /// <summary>
            /// Increment the second remainder counter, and then check to see if timeSinceApogee(days) should be incremented.
            /// </summary>
            theOrbit.TimeSinceApogeeRemainder = theOrbit.TimeSinceApogeeRemainder + deltaSeconds;

            while (theOrbit.TimeSinceApogeeRemainder > Constants.TimeInSeconds.Day)
            {
                theOrbit.TimeSinceApogee = theOrbit.TimeSinceApogee + 1;
                theOrbit.TimeSinceApogeeRemainder = theOrbit.TimeSinceApogeeRemainder - Constants.TimeInSeconds.Day;
            }

            /// <summary>
            /// if timeSinceApogee is greater than the orbital period then the orbital period should be safe to subtract without changing anything about how the orbit works.
            /// Orbital Period needs to be converted to seconds.
            /// </summary>
            while (theOrbit.TimeSinceApogee > (long)(theOrbit.OrbitalPeriod))
            {
                theOrbit.TimeSinceApogee = theOrbit.TimeSinceApogee - (long)(theOrbit.OrbitalPeriod);
            }

            /// <summary>
            /// now get the position.
            /// </summary>
            FindCartesianPosition(theOrbit, theOrbit.TimeSinceApogee, out x, out y);
            theOrbit.Position.X = x;
            theOrbit.Position.Y = y;
        }

        public void FindCartesianPosition(OrbitingEntity theOrbit, long DaysSinceEpoch, out double x, out double y)
        {
            double angle, radius;
            FindPolarPosition(theOrbit, DaysSinceEpoch, out angle, out radius);
            x = -1 * radius * Math.Sin(angle);
            y = radius * Math.Cos(angle);
        }

        public double FindRadiusFromAngle(OrbitingEntity theOrbit, double angle)
        {

            double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle));
            return radius;
        }

        public void FindCordsFromAngle(OrbitingEntity theOrbit, double angle, out double x, out double y)
        {

            double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle));
            x = -1 * radius * Math.Sin(angle);
            y = radius * Math.Cos(angle);
        }

    }

}

