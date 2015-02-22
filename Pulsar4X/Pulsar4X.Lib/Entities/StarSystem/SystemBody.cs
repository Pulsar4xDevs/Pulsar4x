using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

#if LOG4NET_ENABLED
using log4net;
#endif

namespace Pulsar4X.Entities
{
    [TypeDescriptionProvider(typeof(PlanetTypeDescriptionProvider))]
    public class SystemBody : OrbitingEntity
    {
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(SystemBody));
#endif

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
            Asteriod,
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

        public PlanetType Type { get; set; }

        /// <summary>
        /// dictionary listing of which faction has surveyed this planetary body. dictionary might be superfluous, and this could merely be
        /// accomplished by a bindinglist of factions, since only factions present will have surveyed the body.
        /// < @todo: Get this moved to faction code, where it belongs.
        /// </summary>
        public Dictionary<Faction, bool> GeoSurveyList { get; set; }

        /// <summary>
        /// @todo From the Aurora Wiki:
        /// The colony cost is calculated in the following way. The five checks below this paragraph are made. Whichever results in the highest colony cost, that will be the colony cost for the planet. You can see these factors in the Colony Cost Factors section in the lower left of the F9 view for the currently selected planet. 
        /// 1.) If the atmosphere is not breathable, the colony cost is 2.0. 
        /// 2.) If there are toxic gases in the atmosphere then the colony cost will be 2.0 for some gases and 3.0 for others. 
        /// 3.) If the pressure is too high, the colony cost will be equal to the Atmospheric Pressure divided by the species maximum pressure with a minimum of 2.0 
        /// 4.) If the oxygen percentage is above 30%, the colony cost will be 2.0 
        /// 5.) The colony cost for a temperature outside the range is Temperature Difference / Temperature Deviation. So if the deviation was 22 and the temperature was 48 degrees below the minimum, the colony cost would be 48/22 = 2.18 
        /// Some (or all) of these need to be implimented.
        /// </summary>
        public float ColonyCost { get; set; }

        /// <summary>
        /// The Atmosphere of the planet.
        /// </summary>
        public Atmosphere Atmosphere { get; set; }

        /// <summary>
        /// Measure on the gravity of a planet at its surface.
        /// In Earth Gravities (Gs).
        /// </summary>
        public float SurfaceGravity { get; set; }

        /// <summary>
        /// The density of the body in g/cm^3
        /// </summary>
        public double Density { get; set; }

        public TimeSpan LengthOfDay { get; set; }

        /// <summary>
        /// The Axial Tilt of this body.
        /// Measured in degrees.
        /// </summary>
        public float AxialTilt { get; set; }

        /// <summary>
        /// Plate techtonics. Ammount of activity depends on age vs mass.
        /// Influences magnitic feild.
        /// </summary>
        public TectonicActivity Tectonics { get; set; }

        /// <summary>
        /// Magnetic feild of the body. It is important as it affects how much atmosphere a body will have.
        /// In Microtesla (uT)
        /// </summary>
        public float MagneticFeild { get; set; }        

        /// <summary>
        /// Temperature of the planet BEFORE greenhouse effects are taken into considuration. 
        /// This is mostly a factor of how much light reaches the planet nad is calculated at generation time.
        /// In Degrees C.
        /// </summary>
        public float BaseTemperature { get; set; }

        // the following will be used for ground combat effects:
        /// <summary>
        /// < @todo Decide if we want RadiationLevel and AtmosphericDust game play features.
        /// </summary>
        public float RadiationLevel { get; set; }
        public float AtmosphericDust { get; set; }

        public BindingList<SystemBody> Moons { get; set; } //moons orbiting the planet
        public BindingList<Population> Populations { get; set; } // list of Populations (colonies) on this planet.

        /// <summary>
        /// Are any taskgroups orbiting with this body?
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroupsInOrbit { get; set; }

        /// <summary>
        /// Entry for whether or not this planet has ruins on it.
        /// </summary>
        public Ruins PlanetaryRuins { get; set; }

        ///< @todo Add Research Anomalies.

        /// <summary>
        /// What mineral resources does this planet have to be mined?
        /// </summary>
        float[] m_aiMinerialReserves;
        public float[] MinerialReserves
        {
            get
            {
                return m_aiMinerialReserves;
            }
        }

        /// <summary>
        /// What is the accessibility of this mineral?
        /// </summary>
        private float[] m_aiMinerialAccessibility;
        public float[] MinerialAccessibility
        {
            get
            {
                return m_aiMinerialAccessibility;
            }
        }

        public SystemBody(OrbitingEntity parent)
            : base()
        {
            /// <summary>
            /// create these or else anything that relies on a unique global id will break.
            /// </summary>
            Id = Guid.NewGuid();

            Moons = new BindingList<SystemBody>();
            Populations = new BindingList<Population>();

            SSEntity = StarSystemEntityType.Body;

            Parent = parent;
            Position = parent.Position;

            TaskGroupsInOrbit = new BindingList<TaskGroupTN>();

            GeoSurveyList = new Dictionary<Faction, bool>();

            /// <summary>
            /// Default mineral amount is zero.
            /// do mineral generation elsewhere.
            /// </summary>
            m_aiMinerialReserves = new float[Constants.Minerals.NO_OF_MINERIALS];
            m_aiMinerialAccessibility = new float[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                m_aiMinerialReserves[mineralIterator] = 0.0f;
                m_aiMinerialAccessibility[mineralIterator] = 0.0f;
            }
        }


        public override List<Constants.ShipTN.OrderType> LegalOrders(Faction faction)
        {
            List<Constants.ShipTN.OrderType> legalOrders = new List<Constants.ShipTN.OrderType>();
            legalOrders.AddRange(_legalOrders);
            if (this.GeoSurveyList.ContainsKey(faction) == true)
            {
                if (this.GeoSurveyList[faction] == false)
                    legalOrders.Add(Constants.ShipTN.OrderType.GeoSurvey);
            }
            return legalOrders;
        }

        /// <summary>
        /// Update the planet's position, Parent positions must be updated in sequence obviously.
        /// </summary>
        /// <param name="tickValue"></param>
        public void UpdatePosition(int tickValue)
        {
            double x, y;
            Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Position.X = x;
            Position.Y = y;

            if (Parent != null)
            {
                // Position is absolute system coordinates, while
                // coordinates returned from GetPosition are relative to it's parent.
                Position.X += Parent.Position.X;
                Position.Y += Parent.Position.Y;
            }

            /// <summary>
            /// Update all the moons.
            /// </summary>
            foreach (SystemBody CurrentMoon in Moons)
            {
                CurrentMoon.UpdatePosition(tickValue);
            }

            ///<summary>
            ///Update taskgroup positions.
            ///</summary>
            foreach (TaskGroupTN TaskGroup in TaskGroupsInOrbit)
            {
                TaskGroup.Contact.Position.X = Position.X;
                TaskGroup.Contact.Position.Y = Position.Y;
            }
        }

        /// <summary>
        /// This generates the rich assortment of all minerals for a homeworld. non-hw planets have less, or even no resources.
        /// should some resources be scarcer than others?
        /// </summary>
        public void HomeworldMineralGeneration()
        {
            m_aiMinerialReserves[0] = 150000.0f + (100000.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
            m_aiMinerialAccessibility[0] = 1.0f;
            for (int mineralIterator = 1; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                m_aiMinerialReserves[mineralIterator] = 50000.0f + (70000.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
                m_aiMinerialAccessibility[mineralIterator] = 1.0f * ((float)GameState.RNG.Next(2, 10) / 10.0f);
            }
        }
    }

    #region Data Binding

    /// <summary>
    /// Used for databinding, see here: http://blogs.msdn.com/b/msdnts/archive/2007/01/19/how-to-bind-a-datagridview-column-to-a-second-level-property-of-a-data-source.aspx
    /// </summary>
    public class PlanetTypeDescriptionProvider : TypeDescriptionProvider
    {
        private ICustomTypeDescriptor td;

        public PlanetTypeDescriptionProvider()
            : this(TypeDescriptor.GetProvider(typeof(SystemBody)))
        { }

        public PlanetTypeDescriptionProvider(TypeDescriptionProvider parent)
            : base(parent)
        { }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            if (td == null)
            {
                td = base.GetTypeDescriptor(objectType, instance);
                td = new AtmosphereTypeDescriptor(td);
                td = new OrbitTypeDescriptor(td);
            }

            return td;
        }
    }

    #endregion
}
