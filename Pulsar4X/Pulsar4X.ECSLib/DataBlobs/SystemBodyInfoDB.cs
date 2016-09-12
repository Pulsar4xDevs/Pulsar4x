using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
    public class MineralDepositInfo
    {
        [JsonProperty]
        public int Amount { get; internal set; }
        [JsonProperty]
        public int HalfOriginalAmount { get; internal set; }
        [JsonProperty]
        public double Accessibility { get; internal set; }
    }

    public class SystemBodyInfoDB : BaseDataBlob
    {
        [PublicAPI]
        [JsonProperty]
        public BodyType BodyType { get; internal set; }

        /// <summary>
        /// Plate techtonics. Ammount of activity depends on age vs mass.
        /// Influences magnitic field. maybe this should be in the processor?
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public TectonicActivity Tectonics { get; internal set; }

        /// <summary>
        /// The Axial Tilt of this body.
        /// Measured in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float AxialTilt { get; internal set; }

        /// <summary>
        /// Magnetic field of the body. It is important as it affects how much atmosphere a body will have.
        /// In Microtesla (uT)
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float MagneticField { get; internal set; }

        /// <summary>
        /// Temperature of the planet BEFORE greenhouse effects are taken into consideration. 
        /// This is mostly a factor of how much light reaches the planet nad is calculated at generation time.
        /// In Degrees C.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float BaseTemperature { get; internal set; }

        // the following will be used for ground combat effects:
        /// <summary>
        /// Todo: Decide if we want RadiationLevel and AtmosphericDust game play features.
        /// arnt these going to affect how many Infrastructure or colony cost?
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float RadiationLevel { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public float AtmosphericDust { get; internal set; }

        /// <summary>
        /// Indicates if the system body supports populations and can be settled by Players/NPRs..
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public bool SupportsPopulations { get; internal set; }
        
        [PublicAPI]
        [JsonProperty]
        public List<Entity> Colonies { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public TimeSpan LengthOfDay { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double Gravity { get; internal set; }


        /// <summary>
        /// Stores the amount of the variopus minerials. the guid can be used to lookup the
        /// minerial definition (MineralSD) from the StaticDataStore.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public Dictionary<Guid, MineralDepositInfo> Minerals { get; internal set; }

        public SystemBodyInfoDB()
        {
            Minerals = new Dictionary<Guid, MineralDepositInfo>();
        }

        public SystemBodyInfoDB(SystemBodyInfoDB systemBodyDB)
        {
            BodyType = systemBodyDB.BodyType;
            Tectonics = systemBodyDB.Tectonics;
            AxialTilt = systemBodyDB.AxialTilt;
            MagneticField = systemBodyDB.MagneticField;
            BaseTemperature = systemBodyDB.BaseTemperature;
            RadiationLevel = systemBodyDB.RadiationLevel;
            AtmosphericDust = systemBodyDB.AtmosphericDust;
            SupportsPopulations = systemBodyDB.SupportsPopulations;
            LengthOfDay = systemBodyDB.LengthOfDay;
            Gravity = systemBodyDB.Gravity;
            Minerals = new Dictionary<Guid, MineralDepositInfo>(systemBodyDB.Minerals);
        }

        public override object Clone()
        {
            return new SystemBodyInfoDB(this);
        }
    }
}
