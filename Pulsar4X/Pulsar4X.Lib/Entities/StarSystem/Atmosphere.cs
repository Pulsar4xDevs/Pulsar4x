using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.StarSystem
{
    
    /// <summary>
    /// The Atmosphere of a Planet or Moon.
    /// @todo Make this a generic component.
    /// </summary>
    class Atmosphere
    {
        /// <summary>
        /// Atmospheric Presure
        /// Units to be decided.
        /// </summary>
        public float Pressure { get; set; }
        
        /// <summary>
        /// Weather or not the planet has abundent water.
        /// </summary>
        public bool Hydrosphere { get; set; }

        /// <summary>
        /// The percentage of the bodies sureface covered by water.
        /// </summary>
        public short HydrosphereExtent { get; set; }

        /// <summary>
        /// A measure of the greenhouse factor provided by this Atmosphere.
        /// </summary>
        public float GreenhouseFactor { get; set; }
    }
}
