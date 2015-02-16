using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Newtonsoft.Json;
using Pulsar4X.Lib;

namespace Pulsar4X.Entities
{
    public abstract class OrbitingEntity : StarSystemEntity
    {
        public Orbit Orbit { get; set; }

        /// <summary>
        /// The Parent Orbiting Body, for Planets and stars this is the same as Primary, for moons it will be a planet.
        /// </summary>
        public OrbitingEntity Parent { get; set; }

        /// <summary>
        /// equitorial radius (in AU)
        /// </summary>
        public double Radius { get; set; }
        
        /// <summary>
        /// Mass in KG
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        /// Indicates weither the system body supports populations and can be settled by Plaerys/NPRs..
        /// </summary>
        public bool SupportsPopulations { get; set; }

        public OrbitingEntity()
            : base()
        {
            Orbit = new Orbit();
        }
    }
}