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
        private static OrbitTable instance;
        public static OrbitTable Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OrbitTable();
                }
                return instance;
            }
        }

		private static uint nodes = 100;		//Each table point reprsents 1/(2n)th of an orbital period
		private static int N_orbits = 40;	//Each each orbital excentricy has a different profile.
		private static double[,] table = new double[N_orbits+1, nodes];

		private OrbitTable ()
		{
			//generate lookup table
			int j;
			for (j = 0; j < N_orbits; j++) {
				double excentricy = 1.0 * j / N_orbits;
				double angle = 0;
				table[j, 0] = 0;

				int k;
				for (k = 0; k < nodes * 4; k++) {
					if (k % 4 == 0)	// the '4' reflects solving for 4x as many points as table elements to improve accuracy.
                        table[j, (int)(k / 4)] = angle;

					//Secant Predition-Correction Method. 
					//Pretty good accuracy when excentricy is small. For accuracy with large excentricy smaller steps are required.
					double iAngle1 = angle + Math.PI * 2 / (2.0 * nodes * 4) * Math.Pow (1 - excentricy * Math.Cos (angle), 2.0) / Math.Pow (1 - excentricy * excentricy, 1.5);
					double iAngle2 = angle + Math.PI * 2 / (2.0 * nodes * 4) * Math.Pow (1 - excentricy * Math.Cos (iAngle1), 2.0) / Math.Pow (1 - excentricy * excentricy, 1.5);
					angle = 0.5 * (iAngle1 + iAngle2);
				}
				table [j, nodes] = angle;
			}
		}


		public void FindPolarPosition(OrbitingEntity theOrbit, long secondsSinceEpoch, out double angle, out double radius)
		{
			long orbitPeriod = (long) (Math.PI * 2 * Math.Sqrt( theOrbit.SemiMajorAxis * theOrbit.SemiMajorAxis * theOrbit.SemiMajorAxis / (theOrbit.Mass*Constants.Units.GRAV_CONSTANT)));
            double orbitFraction = 1.0 * ((secondsSinceEpoch + theOrbit.TimeSinceApogee) % orbitPeriod) / orbitPeriod;
			bool mirrorSide = false;
			if( orbitFraction >= 0.5)
			{
				mirrorSide = true;
				orbitFraction = 1.0 - orbitFraction;
			}

			int lowerA = (int) (orbitFraction * nodes * 2);
			int upperA = (int) Math.Ceiling(orbitFraction * nodes * 2);
			int lowerE = (int) theOrbit.Eccentricity * N_orbits;
			int upperE = (int) Math.Ceiling(theOrbit.Eccentricity * N_orbits);

			double lowEA = table[lowerE, lowerA] + (table[lowerE, upperA] - table[lowerE, lowerA]) * (orbitFraction * nodes * 2.0 - lowerA);
			double highEA = table[upperE, lowerA] + (table[upperE, upperA] - table[upperE, lowerA]) * (orbitFraction * nodes * 2.0 - lowerA);

			angle = lowEA + (highEA - lowEA) * (theOrbit.Eccentricity * N_orbits - lowerE);

			if(mirrorSide)
				angle = Math.PI * 2 - angle;

			angle += theOrbit.LongitudeOfApogee;
			if(angle > Math.PI * 2)
				angle -= Math.PI * 2;

			radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 - theOrbit.Eccentricity * Math.Cos(angle));

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
			angle += theOrbit.LongitudeOfApogee;
			if(angle > Math.PI * 2)
				angle -= Math.PI * 2;
			
			double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 - theOrbit.Eccentricity * Math.Cos(angle));
			return radius;
		}

		public void FindCordsFromAngle(OrbitingEntity theOrbit, double angle, out double x, out double y)
		{
			angle += theOrbit.LongitudeOfApogee;
			if(angle > Math.PI * 2)
				angle -= Math.PI * 2;
			
			double radius = theOrbit.SemiMajorAxis * (1 - theOrbit.Eccentricity * theOrbit.Eccentricity) / (1 - theOrbit.Eccentricity * Math.Cos(angle));
			x = -1 * radius * Math.Sin(angle);
			y = radius * Math.Cos(angle);	
		}

	}

}

