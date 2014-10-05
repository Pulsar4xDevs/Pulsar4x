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

		private OrbitTable ()
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


		public void FindPolarPosition(OrbitingEntity theOrbit, long secondsSinceEpoch, out double angle, out double radius)
		{   
            long orbitPeriod = (long)Math.Floor(theOrbit.OrbitalPeriod * Constants.TimeInSeconds.Day);

            double orbitFraction = 1.0 * ((secondsSinceEpoch + theOrbit.TimeSinceApogee) % orbitPeriod) / orbitPeriod;
			bool mirrorSide = false;
			if( orbitFraction >= 0.5)
			{
				mirrorSide = true;
				orbitFraction = 1.0 - orbitFraction;
			}

            /// <summary>
            /// Floor and Ceiling of orbitFraction * node Count * 2. orbit fraction appears to be between 0.0 and 0.5
            /// </summary>
            int lowerA = (int)(orbitFraction * m_oNodes * 2);
            int upperA = (int)Math.Ceiling(orbitFraction * m_oNodes * 2);

            /// <summary>
            /// Floor and ceiling of eccentricity * number of eccentricity steps. 
            /// </summar>
            int lowerE = (int)theOrbit.Eccentricity * m_oOrbits;
            int upperE = (int)Math.Ceiling(theOrbit.Eccentricity * m_oOrbits);

            double lowEA = m_lTable[lowerE, lowerA] + (m_lTable[lowerE, upperA] - m_lTable[lowerE, lowerA]) * (orbitFraction * m_oNodes * 2.0 - lowerA);
            double highEA = m_lTable[upperE, lowerA] + (m_lTable[upperE, upperA] - m_lTable[upperE, lowerA]) * (orbitFraction * m_oNodes * 2.0 - lowerA);

            angle = lowEA + (highEA - lowEA) * (theOrbit.Eccentricity * m_oOrbits - lowerE);

			if(mirrorSide)
				angle = Math.PI * 2 - angle;

			//angle += theOrbit.LongitudeOfApogee;
			//if(angle > Math.PI * 2)
			//	angle -= Math.PI * 2;

            /// <summary>
            /// This is radius from true anomoaly, found a bug in the / part, changed 1 - theOrbit.Eccentricity to 1 + theOrbit.Eccentricity
            /// r = a(1 – e^2)/(1 + e cos (phi)) phi being true anomaly in this
            /// If we had the eccentric anomaly the equation would be r = a * (1 - (e * cos E)) E being the eccentric anomaly and e being eccentricity
            /// (angle+theOrbit.LongitudeOfApogee) appears to be true anomaly
            /// <summary>
#warning did we mean / (1 - theOrbit.Eccentricity despite that being incorrect? also corrected all the ones further down.
			radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle+theOrbit.LongitudeOfApogee));

		}

        public void UpdatePosition(OrbitingEntity theOrbit, long delta)
        {
            double x, y;
#warning How can I tell when the apogee was, to safely prevent this from overflowing? does it matter?
            theOrbit.TimeSinceApogee+=delta;

            /// <summary>
            /// if timeSinceApogee is greater than the orbital period then the orbital period should be safe to subtract without changing anything about how the orbit works.
            /// Orbital Period needs to be converted to seconds.
            /// </summary>
            if (theOrbit.TimeSinceApogee > (long)(theOrbit.OrbitalPeriod * Constants.TimeInSeconds.Day))
            {
                theOrbit.TimeSinceApogee = theOrbit.TimeSinceApogee - (long)(theOrbit.OrbitalPeriod * Constants.TimeInSeconds.Day);
            }

            FindCartesianPosition(theOrbit, theOrbit.TimeSinceApogee, out x, out y);
            theOrbit.XSystem = x;
            theOrbit.YSystem = y;
        }

		public void FindCartesianPosition(OrbitingEntity theOrbit, long secondsSinceEpoch, out double x, out double y)
		{
			double angle, radius;
			FindPolarPosition(theOrbit, secondsSinceEpoch, out angle, out radius);
			x = -1 * radius * Math.Sin(angle);
			y = radius * Math.Cos(angle);
		}

		public double FindRadiusFromAngle(OrbitingEntity theOrbit, double angle)
		{
			//angle += theOrbit.LongitudeOfApogee;
			//if(angle > Math.PI * 2)
			//	angle -= Math.PI * 2;
			
			double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle+theOrbit.LongitudeOfApogee));
			return radius;
		}

		public void FindCordsFromAngle(OrbitingEntity theOrbit, double angle, out double x, out double y)
		{
			//angle += theOrbit.LongitudeOfApogee;
			//if(angle > Math.PI * 2)
				//angle -= Math.PI * 2;
			
			double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 + theOrbit.Eccentricity * Math.Cos(angle+theOrbit.LongitudeOfApogee));
			x = -1 * radius * Math.Sin(angle);
			y = radius * Math.Cos(angle);	
		}

	}

}

