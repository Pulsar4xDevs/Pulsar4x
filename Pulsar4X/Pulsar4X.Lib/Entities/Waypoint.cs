using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    class Waypoint : StarSystemEntity
    {
        /// <summary>
        /// Mass is totally unnecessary here.
        /// </summary>
        public override double Mass 
        { 
            get { return 0.0; } 
            set { value = 0.0; } 
        }

        public Waypoint(double X, double Y)
        {
            XSystem = X;
            YSystem = Y;
            ZSystem = 0.0;

            SSEntity = StarSystemEntityType.Waypoint;
        }
    }
}
