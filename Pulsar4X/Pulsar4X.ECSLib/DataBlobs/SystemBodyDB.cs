using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public enum BodyType : byte
    {
        Terrestrial,    // Like Earth/Mars/Venus/etc.
        GasGiant,       // Like Jupiter/Saturn
        IceGiant,       // Like Uranus/Neptune
        DwarfPlanet,    // Pluto!
        GasDwarf,       // What you'd get is Jupiter and Saturn ever had a baby.
        ///< @todo Add more planet types like Ice Planets (bigger Plutos), carbon planet (http://en.wikipedia.org/wiki/Carbon_planet), Iron SystemBody (http://en.wikipedia.org/wiki/Iron_planet) or Lava Planets (http://en.wikipedia.org/wiki/Lava_planet). (more: http://en.wikipedia.org/wiki/List_of_planet_types).
        Moon,
        Asteroid,
        Comet
    }

    public enum TectonicActivity : byte
    {
        Dead,
        Minor,
        EarthLike,
        Major,
        NA
    }

    /// <summary>
    /// Small struct to store specifics of a minerial deposit.
    /// </summary>
    public struct MineralDepositInfo
    {
        public int Amount;
        public int HalfOriginalAmount;
        public double Accessibility;
    }

    public class SystemBodyDB : BaseDataBlob
    {
        public BodyType Type;

        /// Plate techtonics. Ammount of activity depends on age vs mass.
        /// Influences magnitic field. maybe this should be in the processor?
        /// </summary>
        public TectonicActivity Tectonics;

        /// <summary>
        /// The Axial Tilt of this body.
        /// Measured in degrees.
        /// </summary>
        public float AxialTilt;

        /// <summary>
        /// Magnetic field of the body. It is important as it affects how much atmosphere a body will have.
        /// In Microtesla (uT)
        /// </summary>
        public float MagneticField;

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

        /// <summary>
        /// Indicates if the system body supports populations and can be settled by Players/NPRs..
        /// </summary>
        public bool SupportsPopulations { get; set; }

        public TimeSpan LengthOfDay { get; set; }

        /// <summary>
        /// Stores the amount of the variopus minerials. the guid can be used to lookup the
        /// minerial definition (MineralSD) from the StaticDataStore.
        /// </summary>
        public JDictionary<Guid, MineralDepositInfo> Minerals;

        public SystemBodyDB()
        {
            Minerals = new JDictionary<Guid, MineralDepositInfo>();
        }

        public SystemBodyDB(SystemBodyDB systemBodyDB)
        {
            Type = systemBodyDB.Type;
            Tectonics = systemBodyDB.Tectonics;
            AxialTilt = systemBodyDB.AxialTilt;
            MagneticField = systemBodyDB.MagneticField;
            BaseTemperature = systemBodyDB.BaseTemperature;
            RadiationLevel = systemBodyDB.RadiationLevel;
            AtmosphericDust = systemBodyDB.AtmosphericDust;
            SupportsPopulations = systemBodyDB.SupportsPopulations;
            LengthOfDay = systemBodyDB.LengthOfDay;
            Minerals = new JDictionary<Guid, MineralDepositInfo>(systemBodyDB.Minerals);
        }

        public override object Clone()
        {
            return new SystemBodyDB(this);
        }
    }
}
