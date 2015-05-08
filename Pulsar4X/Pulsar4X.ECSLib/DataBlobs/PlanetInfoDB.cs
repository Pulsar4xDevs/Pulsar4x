using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class PlanetInfoDB : BaseDataBlob
    {
        public enum PlanetType
        {
            Terrestrial,    // Like Earth/Mars/Venus/etc.
            GasGiant,       // Like Jupiter/Saturn
            IceGiant,       // Like Uranus/Neptune
            DwarfPlanet,    // Pluto!
            GasDwarf,       // What you'd get is Jupiter and Saturn ever had a baby.
            ///< @todo Add more planet types like Ice Planets (bigger Plutos), carbon planet (http://en.wikipedia.org/wiki/Carbon_planet), Iron SystemBody (http://en.wikipedia.org/wiki/Iron_planet) or Lava Planets (http://en.wikipedia.org/wiki/Lava_planet). (more: http://en.wikipedia.org/wiki/List_of_planet_types).
            Moon,
            IceMoon,
            Asteroid,
            Comet
        }

        public enum TectonicActivity
        {
            Dead,
            Minor,
            EarthLike,
            Major,
            NA
        }

        public PlanetType Type;

        /// Plate techtonics. Ammount of activity depends on age vs mass.
        /// Influences magnitic feild. maybe this should be in the processor?
        /// </summary>
        public TectonicActivity Tectonics;

        /// <summary>
        /// Measure on the gravity of a planet at its surface.
        /// In Earth Gravities (Gs).
        /// </summary>
        public float SurfaceGravity;

        /// <summary>
        /// The density of the body in g/cm^3
        /// </summary> 
        public double Density;

        /// <summary>
        /// The Axial Tilt of this body.
        /// Measured in degrees.
        /// </summary>
        public float AxialTilt;

        /// <summary>
        /// Magnetic feild of the body. It is important as it affects how much atmosphere a body will have.
        /// In Microtesla (uT)
        /// </summary>
        public float MagneticFeild;

        /// <summary>
        /// Temperature of the planet BEFORE greenhouse effects are taken into considuration. 
        /// This is mostly a factor of how much light reaches the planet nad is calculated at generation time.
        /// In Degrees C.
        /// </summary>
        public float BaseTemperature;

        // the following will be used for ground combat effects:
        /// <summary>
        /// < @todo Decide if we want RadiationLevel and AtmosphericDust game play features.
        /// arnt these going to affect how many Infrastructure or colony cost?
        /// </summary>
        public float RadiationLevel;
        public float AtmosphericDust;

        public AtmosphereDB Atmosphere;

        /// <summary>
        /// Constructor for PlanetInfoDB
        /// </summary>
        /// <param name="type">Type of planet</param>
        /// <param name="tectonics">Plate techtonics. Ammount of activity depends on age vs mass.</param>
        /// <param name="surfaceGrav">Gravity of a planet at its surface. In Earth Gravities (Gs).</param>
        /// <param name="density">In g/cm^3</param>
        /// <param name="axialTilt">in degrees.</param>
        /// <param name="magField">In Microtesla (uT)</param>
        /// <param name="baseTemp">Degrees C.</param>
        /// <param name="radLevel">not yet used?</param>
        /// <param name="atmoDust">not yet used?</param>
        /// <param name="atmosphere">atmosphere</param>
        public PlanetInfoDB(PlanetType type, TectonicActivity tectonics, float surfaceGrav, double density, float axialTilt, 
                            float magField, float baseTemp, float radLevel, float atmoDust, AtmosphereDB atmosphere)
        {
            Type = type;
            Tectonics = tectonics;
            SurfaceGravity = surfaceGrav;
            Density = density;
            AxialTilt = axialTilt;
            MagneticFeild = magField;
            BaseTemperature = baseTemp;
            RadiationLevel = radLevel;
            AtmosphericDust = atmoDust;
            Atmosphere = atmosphere;
        }

    }
}
