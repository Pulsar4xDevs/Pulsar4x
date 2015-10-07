using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
        public int Amount { get; internal set; }
        public int HalfOriginalAmount { get; internal set; }
        public double Accessibility { get; internal set; }
    }

    public class SystemBodyDB : BaseDataBlob
    {
        [PublicAPI]
        public BodyType Type
        {
            get { return _type; }
            internal set { _type = value; }
        }

        /// <summary>
        /// Plate techtonics. Ammount of activity depends on age vs mass.
        /// Influences magnitic field. maybe this should be in the processor?
        /// </summary>
        [PublicAPI]
        public TectonicActivity Tectonics
        {
            get { return _tectonics; }
            internal set { _tectonics = value; }
        }

        /// <summary>
        /// The Axial Tilt of this body.
        /// Measured in degrees.
        /// </summary>
        [PublicAPI]
        public float AxialTilt
        {
            get { return _axialTilt; }
            internal set { _axialTilt = value; }
        }

        /// <summary>
        /// Magnetic field of the body. It is important as it affects how much atmosphere a body will have.
        /// In Microtesla (uT)
        /// </summary>
        [PublicAPI]
        public float MagneticField
        {
            get { return _magneticField; }
            internal set { _magneticField = value; }
        }

        /// <summary>
        /// Temperature of the planet BEFORE greenhouse effects are taken into consideration. 
        /// This is mostly a factor of how much light reaches the planet nad is calculated at generation time.
        /// In Degrees C.
        /// </summary>
        [PublicAPI]
        public float BaseTemperature
        {
            get { return _baseTemperature; }
            internal set { _baseTemperature = value; }
        }

        // the following will be used for ground combat effects:
        /// <summary>
        /// Todo: Decide if we want RadiationLevel and AtmosphericDust game play features.
        /// arnt these going to affect how many Infrastructure or colony cost?
        /// </summary>
        [PublicAPI]
        public float RadiationLevel
        {
            get { return _radiationLevel; }
            internal set { _radiationLevel = value; }
        }

        [PublicAPI]
        public float AtmosphericDust
        {
            get { return _atmosphericDust; }
            internal set { _atmosphericDust = value; }
        }

        /// <summary>
        /// Indicates if the system body supports populations and can be settled by Players/NPRs..
        /// </summary>
        [PublicAPI]
        public bool SupportsPopulations
        {
            get { return _supportsPopulations; }
            internal set { _supportsPopulations = value; }
        }


            
        [PublicAPI]
        public List<Entity> Colonies {get{return _colonies;} internal set { _colonies = value; } }

        [PublicAPI]
        public TimeSpan LengthOfDay
        {
            get { return _lengthOfDay; }
            internal set { _lengthOfDay = value; }
        }

        /// <summary>
        /// Stores the amount of the variopus minerials. the guid can be used to lookup the
        /// minerial definition (MineralSD) from the StaticDataStore.
        /// </summary>
        [PublicAPI]
        public JDictionary<Guid, MineralDepositInfo> Minerals
        {
            get { return _minerals;}
            internal set { _minerals = value; }
        }

        [JsonProperty]
        private JDictionary<Guid, MineralDepositInfo> _minerals;
        [JsonProperty]
        private BodyType _type;
        [JsonProperty]
        private TectonicActivity _tectonics;
        [JsonProperty]
        private float _axialTilt;
        [JsonProperty]
        private float _magneticField;
        [JsonProperty]
        private float _baseTemperature;
        [JsonProperty]
        private float _radiationLevel;
        [JsonProperty]
        private float _atmosphericDust;
        [JsonProperty]
        private bool _supportsPopulations;
        [JsonProperty]
        private List<Entity> _colonies;
        [JsonProperty]
        private TimeSpan _lengthOfDay;

        public SystemBodyDB()
        {
            _minerals = new JDictionary<Guid, MineralDepositInfo>();
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
            _minerals = new JDictionary<Guid, MineralDepositInfo>(systemBodyDB.Minerals);
        }

        public override object Clone()
        {
            return new SystemBodyDB(this);
        }
    }
}
