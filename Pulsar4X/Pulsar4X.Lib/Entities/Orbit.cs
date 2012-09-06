using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Lib
{
	public class Orbit
	{
		//Orbit assumed to travel counter-clockwise from viewer
		public double SemiMajorAxis {get; set;}		//in km
		public double Eccentricity {get; set;} 
		public double TrueAnomaly {get; set;} 		//angle in radians couterclockwise from Perigee at Epoch
		public double LongitudeOfApogee {get; set;} //angle counterclockwise from system 'north' to SemiMajorAxis at Apogee
		public double StandGrav {get; set;} 		//Standard Gravitational Parameter of Gravity Well in km^3*s^-2

		public Orbit ()
		{
		}

		public Orbit (Orbit toCopy)
		{
			SemiMajorAxis = toCopy.SemiMajorAxis;
			Eccentricity = toCopy.Eccentricity;
			TrueAnomaly = toCopy.TrueAnomaly;
			LongitudeOfApogee = toCopy.LongitudeOfApogee;
			StandGrav = toCopy.StandGrav;
		}
	}
}

