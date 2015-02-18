using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    
    /// <summary>
    /// The Atmosphere of a Planet or Moon.
    /// @todo Make this a generic component.
    /// </summary>
    class Atmosphere
    {
        /// <summary>
        /// Atmospheric Presure
        /// In Earth Atmospheres (atm).
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

        /// <summary>
        /// How much light the body reflects. Affects temp.
        /// @todo found out what units this should be in and how to calculate it.
        /// </summary>
        public float Albedo { get; set; }

        /// <summary>
        /// Temperature of the planet AFTER greenhouse effects are taken into considuration. 
        /// This is a factor of the base temp and Green House effects.
        /// In Degrees C.
        /// </summary>
        public float SurfaceTemperature { get; set; }

        private Dictionary<AtmosphericGas, float> _composition = new Dictionary<AtmosphericGas, float>();
        /// <summary>
        /// The composition of the atmosphere, i.e. what gases make it up and in what ammounts.
        /// In Earth Atmospheres (atm).
        /// </summary>
        public Dictionary<AtmosphericGas, float> Composition { get { return _composition; } }

        /// <summary>
        /// Returns true if The atmosphere exists (i.e. there are any gases in it), else it return false.
        /// </summary>
        public bool Exists 
        {
            get
            {
                if (_composition.Count > 0)
                    return true;

                return false;
            } 
        }

        public Atmosphere()
        {

        }
    }
}
