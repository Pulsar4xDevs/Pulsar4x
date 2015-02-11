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

        private static uint m_oNodes = 100;		//Each table point reprsents 1/(2n)th of an orbital period
        private static int m_oOrbits = 40;	//Each each orbital excentricy has a different profile.

        /// <summary>
        /// This is a table of eccentric orbits, with each eccentricity from  0.0 to 1.0 in increments of 0.025, with 100 nodes for each orbit of that eccentricity 
        /// </summary>
        private static double[,] m_lTable = new double[m_oOrbits + 1, m_oNodes + 1];

        private OrbitTable()
        {
            //generate lookup table
            int j;
            for (j = 0; j < m_oOrbits; j++)
            {
                double excentricy = 1.0 * j / m_oOrbits;
                double angle = 0;
                m_lTable[j, 0] = 0;

                int k;


                /// <summary>
                /// Total number of increments of the following loop. More gives a more precise location for each table entry.
                /// </summary>
                uint StepFactor = 4;
                uint TotalSteps = m_oNodes * StepFactor;

                for (k = 0; k < TotalSteps; k++)
                {
                    /// <summary>
                    /// Stepfactor: the '4' reflects solving for 4x as many points as table elements to improve accuracy.
                    /// </summary>
                    if (k % StepFactor == 0)
                        m_lTable[j, (int)(k / StepFactor)] = angle;

                    //Secant Predition-Correction Method. 
                    //Pretty good accuracy when excentricy is small. For accuracy with large excentricy smaller steps are required.
                    double iAngle1 = angle + Math.PI * 2 / (2.0 * TotalSteps) * Math.Pow(1 - excentricy * Math.Cos(angle), 2.0) / Math.Pow(1 - excentricy * excentricy, 1.5);
                    double iAngle2 = angle + Math.PI * 2 / (2.0 * TotalSteps) * Math.Pow(1 - excentricy * Math.Cos(iAngle1), 2.0) / Math.Pow(1 - excentricy * excentricy, 1.5);
                    angle = 0.5 * (iAngle1 + iAngle2);
                }
                m_lTable[j, m_oNodes] = angle;
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
            //angle += theOrbit.LongitudeOfApogee;
            //if(angle > Math.PI * 2)
            //	angle -= Math.PI * 2;

            //double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle+theOrbit.LongitudeOfApogee));

            double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle/* + theOrbit.TrueAnomaly*/));
            return radius;
        }

        public void FindCordsFromAngle(OrbitingEntity theOrbit, double angle, out double x, out double y)
        {
            //angle += theOrbit.LongitudeOfApogee;
            //if(angle > Math.PI * 2)
            //angle -= Math.PI * 2;

            //double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle+theOrbit.LongitudeOfApogee));

            double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle/* + theOrbit.TrueAnomaly*/));
            x = -1 * radius * Math.Sin(angle);
            y = radius * Math.Cos(angle);
        }

    }

}

