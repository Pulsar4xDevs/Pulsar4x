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

        public enum MineralType
        {
            NotGenerated,
            NoMinerals,        //Nothing.
            Asteroid,          //500-10k of each mineral, above 1.0 accessibility? 1-5 minerals
            Comet,             //10-100k 6-10 minerals high accessibility.
            FewGood,           //Rich world with few but decent quality mineral reserves. 500k-4M range, 2-4 minerals.
            ManyGood,          //Rich world with many high quality mineral deposits, but not truly massive deposits 500k-4M range 4-8 minerals
            MassiveReserves,   //Gargantuan amount of resources, low accessibility. 10-150M 8-11 minerals.
            Homeworld,         //50k-150k of every resource in good amounts. Everything a starting faction will need.
            GasGiant,          //500k to 50M Sorium only, varying accessibility but not extremely low.
            Count,
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
        /// Update: All done in species now, each species has its own colony cost, so this may not be appropriate for SystemBody(Planet), which means that SystemGenandDisplay should be reworked.
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
        /// </summary>
        /// Radiation Level adversely effects population growth and industrial production: Each point of radiation decreases industrial production by 0.01% and growth by 0.0025%
        /// radiation decreases at a baseline 1 per 5 days.
        /// </summary>
#warning Implement RadiationLevel, Atmospheric dust in reduced population growth, industrial percentage, and lowered planetary temperature.
        public float RadiationLevel { get; set; }

        /// <summary>
        /// Atmospheric dust decreases temperature by (dust / 100) degrees C. dust decreases at a baseline 3 points per 5 days.
        /// </summary>
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

        /// <summary>
        /// Has mineral generation been run for this world?
        /// </summary>
        private bool _MineralsGenerated;
        public bool _mineralsGenerated
        {
            get { return _MineralsGenerated; }
        }

        /// <summary>
        /// What type of minerals should be generated for this world?
        /// </summary>
        private MineralType _BodyMineralType;
        public MineralType _bodyMineralType
        {
            get { return _BodyMineralType; }
        }

        public SystemBody(OrbitingEntity parent, PlanetType type)
            : base()
        {
            /// <summary>
            /// create these or else anything that relies on a unique global id will break.
            /// </summary>
            Id = Guid.NewGuid();

            Type = type; // set the type ASAP in case anthing needs to know it.

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
            _MineralsGenerated = false;
            _BodyMineralType = MineralType.NotGenerated;

#warning planet generation needs minerals, anomalies, and ruins generation.
            PlanetaryRuins = new Ruins();


            Atmosphere = new Atmosphere(this);

            if(Type != PlanetType.GasDwarf && Type != PlanetType.GasGiant && Type != PlanetType.IceGiant)
                SupportsPopulations = true;
        }


        public override List<Constants.ShipTN.OrderType> LegalOrders(Faction faction)
        {
            List<Constants.ShipTN.OrderType> legalOrders = new List<Constants.ShipTN.OrderType>();
            legalOrders.AddRange(_legalOrders);
            if (this.GeoSurveyList.ContainsKey(faction) == false)
            {
                legalOrders.Add(Constants.ShipTN.OrderType.GeoSurvey);
            }
            else if (this.GeoSurveyList.ContainsKey(faction) == true)
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

            /// <summary>
            /// update every population on this planet.
            /// </summary>
            foreach (Population Pops in Populations)
            {
                Pops.Contact.Position.X = Position.X;
                Pops.Contact.Position.Y = Position.Y;
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
#warning More of a bellcurve distribution of minerals would be better, with the lowest and highest results relatively rare. Accessibility, and even mineral type should be the same.
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

        /// <summary>
        /// Asteroids have a small amount of a few types of minerals. but accessibility will be very high.
        /// </summary>
        public void AsteroidMineralGeneration()
        {
            int mCount = 1 + GameState.RNG.Next(4);
            while (mCount != 0)
            {
                for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                {
                    if (GameState.RNG.Next(10) == 1)
                    {
                        m_aiMinerialReserves[mineralIterator] = 500.0f + (9500.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
                        m_aiMinerialAccessibility[mineralIterator] = 1.0f * ((float)GameState.RNG.Next(8, 10) / 10.0f);

                        mCount--;
                    }
                }
            }
        }

        /// <summary>
        /// Comets are fairly rich and have a good assortment of minerals
        /// </summary>
        public void CometMineralGeneration()
        {
            int mCount = 6 + GameState.RNG.Next(4);
            while (mCount != 0)
            {
                for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                {
                    if (GameState.RNG.Next(10) == 1)
                    {
                        m_aiMinerialReserves[mineralIterator] = 10000.0f + (90000.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
                        m_aiMinerialAccessibility[mineralIterator] = 1.0f * ((float)GameState.RNG.Next(4, 10) / 10.0f);

                        mCount--;
                    }
                }
            }
        }

        /// <summary>
        /// Low Planet means that this world should have only a few deposits, though there should be a good amount of resources on them.
        /// </summary>
        public void LowPlanetMineralGeneration()
        {
            int mCount = 2 + GameState.RNG.Next(2);
            while (mCount != 0)
            {
                for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                {
                    if (GameState.RNG.Next(10) == 1)
                    {
                        m_aiMinerialReserves[mineralIterator] = 500000.0f + (3500000.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
                        m_aiMinerialAccessibility[mineralIterator] = 1.0f * ((float)GameState.RNG.Next(2, 10) / 10.0f);

                        mCount--;
                    }
                }
            }
        }

        /// <summary>
        /// High planet indicates that this will be a fairly desirable mining spot.
        /// </summary>
        public void HighPlanetMineralGeneration()
        {
            int mCount = 4 + GameState.RNG.Next(4);
            while (mCount != 0)
            {
                for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                {
                    if (GameState.RNG.Next(10) == 1)
                    {
                        m_aiMinerialReserves[mineralIterator] = 500000.0f + (3500000.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
                        m_aiMinerialAccessibility[mineralIterator] = 1.0f * ((float)GameState.RNG.Next(2, 10) / 10.0f);

                        mCount--;
                    }
                }
            }
        }

        /// <summary>
        /// Planets with truly stupendous amounts of resources, but at low accessibility.
        /// </summary>
        public void MassiveMineralGeneration()
        {
            int mCount = 8 + GameState.RNG.Next(3);
            while (mCount != 0)
            {
                for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                {
                    if (GameState.RNG.Next(10) == 1)
                    {
                        m_aiMinerialReserves[mineralIterator] = 10000000.0f + (140000000.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
                        m_aiMinerialAccessibility[mineralIterator] = 0.1f;

                        mCount--;
                    }
                }
            }
        }

        public void GasGiantMineralGeneration()
        {
            m_aiMinerialReserves[(int)Constants.Minerals.MinerialNames.Sorium] = 500000.0f + (49500000.0f * ((float)GameState.RNG.Next(0,100000) / 100000.0f));
            m_aiMinerialAccessibility[(int)Constants.Minerals.MinerialNames.Sorium] = 0.4f + ((float)GameState.RNG.Next(0, 6) / 10.0f);
        }

        /// <summary>
        /// determine what type of world this is with regards to mineral generation and generate those minerals.
        /// </summary>
        /// <param name="isHomeworld">Should this world have homeworld mineral generation?</param>
        public void GenerateMinerals(bool isHomeWorld = false)
        {

            MineralType MType = MineralType.Homeworld;



            if (isHomeWorld == false)
            {
                int MineralTest = GameState.RNG.Next(0,100);
                MType = MineralType.NoMinerals;

                /// <summary>
                /// Minerals should be generated.
                /// </summary>
                if(MineralTest > 60)
                {
                    if (Type == PlanetType.GasGiant || Type == PlanetType.IceGiant || Type == PlanetType.GasDwarf)
                    {
                        MType = SystemBody.MineralType.GasGiant;
                    }
                    else if (Type == PlanetType.Comet)
                    {
                        MType = SystemBody.MineralType.Comet;
                    }
                    else if (Type == PlanetType.Asteroid)
                    {
                        MType = MineralType.Asteroid;
                    }
                    else
                    {
                        MType = (MineralType)GameState.RNG.Next((int)SystemBody.MineralType.FewGood, (int)SystemBody.MineralType.Homeworld);
                    }
                }
            }

            switch (MType)
            {
                case SystemBody.MineralType.NoMinerals:
                    break;
                case SystemBody.MineralType.Asteroid:
                    AsteroidMineralGeneration();
                    break;
                case SystemBody.MineralType.Comet:
                    CometMineralGeneration();
                    break;
                case SystemBody.MineralType.FewGood:
                    LowPlanetMineralGeneration();
                    break;
                case SystemBody.MineralType.ManyGood:
                    HighPlanetMineralGeneration();
                    break;
                case SystemBody.MineralType.MassiveReserves:
                    MassiveMineralGeneration();
                    break;
                case SystemBody.MineralType.Homeworld:
                    HomeworldMineralGeneration();
                    break;
                case SystemBody.MineralType.GasGiant:
                    GasGiantMineralGeneration();
                    break;
            }
            _MineralsGenerated = true;
        }

        /// <summary>
        /// Survey cost for bodies is based on their radius relative to that of Earth.
        /// </summary>
        /// <returns>Cost in geopoints to survey this world.</returns>
        public int GetSurveyCost()
        {
            return (int)Math.Floor((float)Constants.SensorTN.EarthSurvey * ((float)Radius / Constants.SensorTN.EarthRadius));
        }


        /// <summary>
        /// Add a population if possible for the selected faction of species CurrentSpecies.
        /// </summary>
        /// <param name="PopFaction">Faction of population.</param>
        /// <param name="TimeSlice">Sensor info needs timeslice I think</param>
        /// <param name="CurrentSpecies">Species of this population.</param>
        public void AddPopulation(Faction PopFaction, int TimeSlice, Species CurrentSpecies)
        {
            String Entry = String.Format("AddPopulation Run {0} {1}",SupportsPopulations, Populations.Count);
            MessageEntry NMG = new MessageEntry(MessageEntry.MessageType.Count, null, null, GameState.Instance.CurrentDate, GameState.Instance.CurrentSecond, Entry);
            GameState.Instance.Factions[0].MessageLog.Add(NMG);
            /// <summary>
            /// Some planet types can't host populations. Gas Giants for example.
            /// </summary>
            if (SupportsPopulations == false)
                return;

            /// <summary>
            /// If such a population already exists do not allow duplicates. this will crash if a duplicate is allowed, and it also potentially perverts gameplay mechanics.
            /// </summary>
            foreach (Population CurrentPop in Populations)
            {
                if (CurrentPop.Faction == PopFaction && CurrentPop.Species == CurrentSpecies)
                    return;
            }

            /// <summary>
            /// Assuming that we got this far, add the population.
            /// </summary>
            Population NewPopulation = new Population(this, PopFaction, TimeSlice, Name, CurrentSpecies);
            Populations.Add(NewPopulation);
            PopFaction.Populations.Add(NewPopulation);
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
