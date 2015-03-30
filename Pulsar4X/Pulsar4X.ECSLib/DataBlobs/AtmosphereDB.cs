using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class AtmosphereDB : BaseDataBlob
    {
        /// <summary>
        /// Atmospheric Presure
        /// In Earth Atmospheres (atm).
        /// </summary>
        public float Pressure;

        /// <summary>
        /// Weather or not the planet has abundent water.
        /// </summary>
        public bool Hydrosphere;

        /// <summary>
        /// The percentage of the bodies sureface covered by water.
        /// </summary>
        public short HydrosphereExtent;

        /// <summary>
        /// A measure of the greenhouse factor provided by this Atmosphere.
        /// </summary>
        public float GreenhouseFactor;

        /// <summary>
        /// Pressure (in atm) of greenhouse gasses. but not really.
        /// to get this figure for a given gass toy would take its pressure 
        /// in the atmosphere and times it by the gasses GreenhouseEffect 
        /// which is a number between 1 and -1 normally.
        /// </summary>
        public float GreenhousePressure;

        /// <summary>
        /// How much light the body reflects. Affects temp.
        /// a number from 0 to 1.
        /// </summary>
        public float Albedo;

        /// <summary>
        /// Temperature of the planet AFTER greenhouse effects are taken into considuration. 
        /// This is a factor of the base temp and Green House effects.
        /// In Degrees C.
        /// </summary>
        public float SurfaceTemperature;

         //<summary>
         //The composition of the atmosphere, i.e. what gases make it up and in what ammounts.
         //In Earth Atmospheres (atm).
         //</summary>
        public Dictionary<AtmosphericGasDB, float> Composition;

        /// <summary>
        /// Constructor for AtmosphereDataBlob. 
        /// </summary>
        /// <param name="pressure">In Earth Atmospheres (atm).</param>
        /// <param name="hydrosphere">Weather or not the planet has abundent water.</param>
        /// <param name="hydroExtent">The percentage of the bodies sureface covered by water.</param>
        /// <param name="greenhouseFactor">Greenhouse factor provided by this Atmosphere.</param>
        /// <param name="greenhousePressue"></param>
        /// <param name="albedo">from 0 to 1.</param>
        /// <param name="surfaceTemp">AFTER greenhouse effects, In Degrees C.</param>
        public AtmosphereDB(float pressure, bool hydrosphere, short hydroExtent, float greenhouseFactor, float greenhousePressue, float albedo, float surfaceTemp, Dictionary<AtmosphericGasDB,float> composition)
            : base()
        {
            Pressure = pressure;
            Hydrosphere = hydrosphere;
            HydrosphereExtent = hydroExtent;
            GreenhouseFactor = greenhouseFactor;
            GreenhousePressure = greenhousePressue;
            Albedo = albedo;
            SurfaceTemperature = surfaceTemp;
            Composition = composition;

        }
    }
}
