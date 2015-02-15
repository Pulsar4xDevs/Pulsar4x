using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X
{
    /// <summary>
    /// Galaxy Gen holds some information used by the SystemGen Class when generating Stars.
    /// It does not actually Generate anything on its own, but rahter guids the generation of each star system to make a consistand Galaxy.
    /// </summary>
    public static class GalaxyGen
    {
        /// <summary>
        /// RNG used to generate seeds for a star system if none are provided.
        /// </summary>
        public  static Random SeedRNG = new Random();

        /// <summary>
        /// indicates weither We shoudl generate a Real Star System or a more gamey one.
        /// </summary>
        public static bool RealStarSystems = true;
    }
}
