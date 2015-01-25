using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Lib
{
    public class Orbit
    {
        // TODO: Delete this

        //Orbit assumed to travel counter-clockwise from viewer

        /// <summary>
        /// (1) The semi-major axis a, half the greatest width of the orbital ellipse, which gives the size of the orbit.
        /// </summary>
        public double SemiMajorAxis { get; set; }		//in km

        /// <summary>
        /// 2) The eccentricity e, a number from 0 to 1, giving the shape of the orbit. For a circle e = 0, larger values give progressively more flattened circles, 
        /// up to e = 1 where the ellipse stretches to infinity and becomes a parabola. The orbits of all large planets are rather close to circles: that of Earth, for instance, has e=0.0167
        /// </summary>
        public double Eccentricity { get; set; }
        public double TrueAnomaly { get; set; } 		//angle in radians couterclockwise from Perigee at Epoch
        public double LongitudeOfApogee { get; set; } //angle counterclockwise from system 'north' to SemiMajorAxis at Apogee
        public double StandGrav { get; set; } 		//Standard Gravitational Parameter of Gravity Well in km^3*s^-2
        public double TimeSinceApogee { get; set; } // added by Greg as orbitTable is expecting it... 

        public Orbit()
        {
        }

        public Orbit(Orbit toCopy)
        {
            SemiMajorAxis = toCopy.SemiMajorAxis;
            Eccentricity = toCopy.Eccentricity;
            TrueAnomaly = toCopy.TrueAnomaly;
            LongitudeOfApogee = toCopy.LongitudeOfApogee;
            StandGrav = toCopy.StandGrav;
        }
    }
}

