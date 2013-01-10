using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class Waypoint : StarSystemEntity
    {
        /// <summary>
        /// Starsystem this waypoint resides in.
        /// </summary>
        StarSystem System;

        /// <summary>
        /// Mass is totally unnecessary here.
        /// </summary>
        public override double Mass 
        { 
            get { return 0.0; } 
            set { value = 0.0; } 
        }

        public Waypoint(StarSystem Sys,double X, double Y)
        {
            System = Sys;
            XSystem = X;
            YSystem = Y;
            ZSystem = 0.0;

            SSEntity = StarSystemEntityType.Waypoint;
        }
    }
}
