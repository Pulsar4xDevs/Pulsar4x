using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.ComponentModel;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Parent class for planetary build queue items
    /// </summary>
    public abstract class BuildQueueItem : GameEntity
    {
        /// <summary>
        /// How many of this item should be built?
        /// </summary>
        private float m_NumToBuild;
        public float numToBuild
        {
            get { return m_NumToBuild; }
            set { m_NumToBuild = value; }
        }

        /// <summary>
        /// How much of planetary industry in percentage terms is devoted to this item(must be validated)
        /// </summary>
        private float m_BuildCapcity;
        public float buildCapacity
        {
            get { return m_BuildCapcity; }
            set { m_BuildCapcity = value; }
        }

        /// <summary>
        /// How many planetary buildpoints per annum are devoted to this build order.
        /// </summary>
        private float m_ProductionRate;
        public float productionRate
        {
            get { return m_ProductionRate; }
            set { m_ProductionRate = value; }
        }

        /// <summary>
        /// How much does each item cost?
        /// </summary>
        private decimal m_CostPerItem;
        public decimal costPerItem
        {
            get { return m_CostPerItem; }
            set { m_CostPerItem = value; }
        }

        /// <summary>
        /// When will this item finish? This is an estimate, actual time completion is based on game ticks(seconds).
        /// </summary>
        private DateTime m_CompletionDate;
        public DateTime completionDate
        {
            get { return m_CompletionDate; }
            set { m_CompletionDate = value; }
        }

        /// <summary>
        /// Is this item currently being built or is construction paused? queued items can be paused. false = paused.
        /// </summary>
        private bool m_InProduction;
        public bool inProduction
        {
            get { return m_InProduction; }
            set { m_InProduction = value; }
        }

        /// <summary>
        /// Update the build queue information for this item. should it be in production? how many should be built, how much industry should be put in?
        /// </summary>
        /// <param name="BuildNum">Number to build</param>
        /// <param name="BuildPercent">Percent of industry to devote to production.</param>
        /// <param name="Production">Is this item paused?</param>
        public void UpdateBuildQueueInfo(float BuildNum, float BuildPercent, bool Production, decimal Cost)
        {
            m_NumToBuild = BuildNum;
            m_BuildCapcity = BuildPercent;
            m_InProduction = true;

            float BPRequirement = (float)Math.Floor(m_NumToBuild) * (float)Cost;
            float DaysInYear = (float)Constants.TimeInSeconds.RealYear / (float)Constants.TimeInSeconds.Day;
            float YearsOfProduction = (BPRequirement / m_BuildCapcity);
            int TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);

            /// <summary>
            /// YearsOfProduction here being greater than 5475852 means that it will take more than 2 Billion days, or around the 32 bit limit. so don't bother calculating time in that case.
            /// </summary>
            if (m_BuildCapcity != 0.0f && YearsOfProduction < Constants.Colony.TimerYearMax)
            {
                DateTime EstTime = GameState.Instance.GameDateTime;
                TimeSpan TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                EstTime = EstTime.Add(TS);
                m_CompletionDate = EstTime;
            }
        }
    }

    public class ConstructionBuildQueueItem : BuildQueueItem
    {
        public enum CBType
        {
            /// <summary>
            /// Planetary Installation
            /// </summary>
            PlanetaryInstallation,
            /// <summary>
            /// Ship Component
            /// </summary>
            ShipComponent,
            /// <summary>
            /// Building a PDC from scratch
            /// </summary>
            PDCConstruction,
            /// <summary>
            /// Building the PDC prefab parts. 100% of cost for parts that speed construction by 90%.
            /// </summary>
            PDCPrefab,
            /// <summary>
            /// Build PDC from prefabbed parts. 10% of cost + prefabbed parts.
            /// </summary>
            PDCAssembly,
            /// <summary>
            /// Refit existing PDC
            /// </summary>
            PDCRefit,
            MaintenanceSupplies,
            Count
        }
        /// <summary>
        /// type of construction to build.
        /// </summary>
        private CBType m_BuildType;
        public CBType buildType
        {
            get { return m_BuildType; }
        }

        /// <summary>
        /// Installation that this build item will construct.
        /// </summary>
        private Installation m_InstallationBuild;
        public Installation installationBuild
        {
            get { return m_InstallationBuild; }
        }

        /// <summary>
        /// Component that this build item will construct
        /// </summary>
        private ComponentDefTN m_ComponentBuild;
        public ComponentDefTN componentBuild
        {
            get { return m_ComponentBuild; }
        }


        /// <summary>
        /// Constructor for Installations.
        /// </summary>
        /// <param name="InstallationToBuild">Installation to build</param>
        public ConstructionBuildQueueItem(Installation InstallationToBuild)
            : base()
        {
            Name = InstallationToBuild.Name;
            numToBuild = 0.0f;
            buildCapacity = 0.0f;
            productionRate = 0.0f;
            costPerItem = InstallationToBuild.Cost;

            m_BuildType = CBType.PlanetaryInstallation;
            m_InstallationBuild = InstallationToBuild;
        }

        /// <summary>
        /// Constructor for ship components.
        /// </summary>
        /// <param name="ComponentToBuild">Ship Component to build</param>
        public ConstructionBuildQueueItem(ComponentDefTN ComponentToBuild)
            : base()
        {
            Name = ComponentToBuild.Name;
            numToBuild = 0.0f;
            buildCapacity = 0.0f;
            productionRate = 0.0f;
            costPerItem = ComponentToBuild.cost;

            m_BuildType = CBType.ShipComponent;
            m_ComponentBuild = ComponentToBuild;
        }

        /// <summary>
        /// Maintenance supplies build queue constructor
        /// </summary>
        public ConstructionBuildQueueItem()
            : base()
        {
            Name = "Maintenance Supplies";
            numToBuild = 0.0f;
            buildCapacity = 0.0f;
            productionRate = 0.0f;
            costPerItem = 0.25m;

            m_BuildType = CBType.MaintenanceSupplies;
        }
    }

    /// <summary>
    /// Missile Build Queue.
    /// </summary>
    public class MissileBuildQueueItem : BuildQueueItem
    {
        /// <summary>
        /// Missile to build.
        /// </summary>
        private OrdnanceDefTN m_OrdanceDef;
        public OrdnanceDefTN ordnanceDef
        {
            get { return m_OrdanceDef; }
        }

        /// <summary>
        /// Constructor for Missile Build Queue Items
        /// </summary>
        /// <param name="Definition"></param>
        public MissileBuildQueueItem(OrdnanceDefTN Definition)
            : base()
        {
            numToBuild = 0.0f;
            buildCapacity = 0.0f;
            productionRate = 0.0f;
            costPerItem = Definition.cost;

            inProduction = false;

            m_OrdanceDef = Definition;
        }
    }

    /// <summary>
    /// Fighter Build Queue
    /// </summary>
    public class FighterBuildQueueItem : BuildQueueItem
    {
        /// <summary>
        /// Fighter to build.
        /// </summary>
        private ShipClassTN m_ShipClassDef;
        public ShipClassTN shipClassDef
        {
            get { return m_ShipClassDef; }
        }

        /// <summary>
        /// Constructor for Missile Build Queue Items
        /// </summary>
        /// <param name="Definition"></param>
        public FighterBuildQueueItem(ShipClassTN Definition)
            : base()
        {
            numToBuild = 0.0f;
            buildCapacity = 0.0f;
            productionRate = 0.0f;
            costPerItem = Definition.BuildPointCost;

            m_ShipClassDef = Definition;
        }
    }

    public class Population : StarSystemEntity
    {

        /// <summary>
        /// What is the political situation on this colony? how productive is it and how much of a military presence is needed to hold control of it.
        /// </summary>
        public enum PoliticalStatus
        {
            Conquered,
            Subjugated,
            Occupied,
            Candidate,
            Imperial,
            Count
        }
        #region Properties

        /// <summary>
        /// Which faction owns this population?
        /// </summary>
        public Faction Faction { get; set; }

        /// <summary>
        /// Which species is on this planet.
        /// </summary>
        public Species Species { get; set; }

        /// <summary>
        /// SystemBody the population is attached to
        /// </summary>
        public SystemBody Planet { get; set; }

        /// <summary>
        /// Does this pop have an assigned governor?
        /// </summary>
        public bool GovernorPresent { get; set; }

        /// <summary>
        /// If so who is he?
        /// </summary>
        public Commander PopulationGovernor { get; set; }

        /// <summary>
        /// How skilled in administration should the Planetary Governor be?
        /// </summary>
        public int AdminRating { get; set; }

        /// <summary>
        /// The contact that this population is associated with.
        /// </summary>
        public SystemContact Contact { get; set; }

        /// <summary>
        /// Which factions have detected a thermal sig from this population?
        /// </summary>
        public BindingList<int> ThermalDetection { get; set; }

        /// <summary>
        /// Which factions have detected an EM signature?
        /// </summary>
        public BindingList<int> EMDetection { get; set; }

        /// <summary>
        /// Any active sensor in range detects a planet.
        /// </summary>
        public BindingList<int> ActiveDetection { get; set; }

        /// <summary>
        /// Populations with structures tend to emit a thermal signature. 5 per installation I believe.
        /// </summary>
        public int ThermalSignature { get; set; }

        /// <summary>
        /// How many orbital terraforming modules are in orbit around this population? (NOTE: orbital terraformers should always have an assigned population, and hence this variable is here and not on
        /// planet)
        /// </summary>
        public float _OrbitalTerraformModules { get; set; }


        public float CivilianPopulation { get; set; }
        public float PopulationGrowthRate { get; set; }
        public float FuelStockpile { get; set; }
        public float MaintenanceSupplies { get; set; }

        /// <summary>
        /// What is the situation of this colony.
        /// </summary>
        public PoliticalStatus PoliticalPopStatus { get; set; }


        public float PopulationWorkingInAgriAndEnviro
        {
            get
            {
                // 5% of civi pop

                //5 + 5 * ColonyCost
                float Agriculture = 0.05f + (0.05f * (float)Species.ColonyCost(Planet));
                return CivilianPopulation * Agriculture;
            }
        }

        public float PopulationWorkingInServiceIndustries
        {
            get
            {
                // 75% of Civi Pop
                //ServicePercent = Sqr(Sqr(TotalPop * 100000)) / 100
                float ServicePercent = (float)(Math.Sqrt(Math.Sqrt((double)CivilianPopulation * 100000.0)) / 100.0);
                if (ServicePercent > 0.75f)
                    ServicePercent = 0.75f;

                float pop = CivilianPopulation - PopulationWorkingInAgriAndEnviro;
                return ServicePercent * pop;
            }
        }

        public float PopulationWorkingInManufacturing
        {
            get
            {
                // 20% of civi pop
                return CivilianPopulation - (PopulationWorkingInAgriAndEnviro + PopulationWorkingInServiceIndustries);
            }
        }

        /// <summary>
        /// EM Signature is related to population.
        /// </summary>
        public int EMSignature { get; set; }


        /// <summary>
        /// Mineral stockpile for this population
        /// </summary>
        float[] m_aiMinerials;
        public float[] Minerials
        {
            get
            {
                return m_aiMinerials;
            }
            set
            {
                m_aiMinerials = value;
            }
        }

        Installation[] m_aoInstallations;
        public Installation[] Installations
        {
            get
            {
                return m_aoInstallations;
            }
            set
            {
                m_aoInstallations = value;
            }
        }



        public float ModifierEconomicProduction { get; set; }
        public float ModifierManfacturing { get; set; }
        public float ModifierProduction { get; set; }
        public float ModifierWealthAndTrade { get; set; }
        public float ModifierPoliticalStability { get; set; }


        /// <summary>
        /// This population's stored TN components. 
        /// </summary>
        public BindingList<ComponentDefTN> ComponentStockpile { get; set; }

        /// <summary>
        /// The number of each component.
        /// </summary>
        public BindingList<float> ComponentStockpileCount { get; set; }

        /// <summary>
        /// Where in the stockpile any particular component is. guid = the guid of the componentdef and int is the array location.
        /// </summary>
        public Dictionary<Guid, int> ComponentStockpileLookup { get; set; }

        /// <summary>
        /// Missiles at this colony
        /// </summary>
        public Dictionary<OrdnanceDefTN, float> MissileStockpile { get; set; }

        /// <summary>
        /// Build queue for construction factories.
        /// </summary>
        public BindingList<ConstructionBuildQueueItem> ConstructionBuildQueue { get; set; }

        /// <summary>
        /// Build queue for ordnance factories.
        /// </summary>
        public BindingList<MissileBuildQueueItem> MissileBuildQueue { get; set; }

        /// <summary>
        /// Build Queue for fighter factories
        /// </summary>
        public BindingList<FighterBuildQueueItem> FighterBuildQueue { get; set; }

        /// <summary>
        /// Does this planet refine sorium into fuel?
        /// </summary>
        public bool IsRefining { get; set; }

        /// <summary>
        /// List of every task at every shipyard at this population center.
        /// </summary>
        public Dictionary<Installation.ShipyardInformation.ShipyardTask, Installation.ShipyardInformation> ShipyardTasks { get; set; }

        /// <summary>
        /// Populations can't be "destroyed" by enemy action, but they can be deleted
        /// </summary>
        public BindingList<ShipTN> ShipsTargetting { get; set; }

        /// <summary>
        /// Missiles targeted on this population.
        /// </summary>
        public BindingList<OrdnanceGroupTN> MissilesInFlight { get; set; }

        /// <summary>
        /// Has this population changes its sensor coverage? Building/moving(getting and taking)/destroying DSTS will cause this to increment. This is totally handled here in this function.
        /// </summary>
        private uint _SensorUpdateAck;
        public uint _sensorUpdateAck
        { 
            get { return _SensorUpdateAck; }
            set { _SensorUpdateAck = value; }
        }

        /// <summary>
        /// True = Add gas, False = Subtract gas.
        /// </summary>
        public bool _GasAddSubtract { get; set; }
        /// <summary>
        /// How much Gas should be added or subtracted?
        /// </summary>
        public float _GasAmt { get; set; }

        /// <summary>
        /// What gas should be altered on this world by terraforming?
        /// </summary>
        public AtmosphericGas _GasToAdd { get; set; }
        #endregion

        /// <summary>
        /// Constructor for population.
        /// </summary>
        /// <param name="a_oPlanet">Planet this population is on</param>
        /// <param name="a_oFaction">Faction this population belongs to</param>
        /// <param name="CurrentTimeSlice">Tick this population was created</param>
        /// <param name="a_oName">Name of the population</param>
        /// <param name="a_oSpecies">Species that will reside on this population.</param>
        public Population(SystemBody a_oPlanet, Faction a_oFaction, int CurrentTimeSlice, String a_oName = "Earth", Species a_oSpecies = null)
        {
            Id = Guid.NewGuid();
            // initialise minerials:
            m_aiMinerials = new float[Constants.Minerals.NO_OF_MINERIALS];
            for (int i = 0; i < Constants.Minerals.NO_OF_MINERIALS; ++i)
            {
                m_aiMinerials[i] = 0;
            }

            m_aoInstallations = new Installation[Installation.NO_OF_INSTALLATIONS];
            for (int i = 0; i < Installation.NO_OF_INSTALLATIONS; ++i)
            {
                m_aoInstallations[i] = new Installation((Installation.InstallationType)i);
            }

            CivilianPopulation = 0.0f;
            PopulationGrowthRate = 0.1f;
            FuelStockpile = 0;
            MaintenanceSupplies = 0;
            ModifierEconomicProduction = 1.0f;
            ModifierManfacturing = 1.0f;
            ModifierPoliticalStability = 1.0f;
            ModifierProduction = 1.0f;
            ModifierWealthAndTrade = 1.0f;

            Name = a_oName;  // just a default Value!

            Faction = a_oFaction;
            Planet = a_oPlanet;


            if (a_oSpecies == null)
            {
                Species = Faction.Species;
            }
            else
            {
                Species = a_oSpecies;
            }

            SSEntity = StarSystemEntityType.Population;

            Planet.Populations.Add(this); // add us to the list of pops on the planet!
            Planet.Position.System.Populations.Add(this);
            Contact = new SystemContact(Faction, this);
            Contact.Position.System = Planet.Position.System;
            Contact.Position.X = Planet.Position.X;
            Contact.Position.Y = Planet.Position.Y;
            Planet.Position.System.SystemContactList.Add(Contact);

            GovernorPresent = false;
            AdminRating = 0;

            ComponentStockpile = new BindingList<ComponentDefTN>();
            ComponentStockpileCount = new BindingList<float>();
            ComponentStockpileLookup = new Dictionary<Guid, int>();
            MissileStockpile = new Dictionary<OrdnanceDefTN, float>();

            _OrbitalTerraformModules = 0.0f;

            PoliticalPopStatus = PoliticalStatus.Imperial;

            for (int InstallationIterator = 0; InstallationIterator < (int)Installation.InstallationType.InstallationCount; InstallationIterator++)
            {
                Installations[InstallationIterator].Number = 0.0f;
            }

            FuelStockpile = 0.0f;
            MaintenanceSupplies = 0.0f;
            EMSignature = 0;
            ThermalSignature = 0;
            ModifierEconomicProduction = 1.0f;
            ModifierManfacturing = 1.0f;
            ModifierProduction = 1.0f;
            ModifierWealthAndTrade = 1.0f;
            ModifierPoliticalStability = 1.0f;

            ConstructionBuildQueue = new BindingList<ConstructionBuildQueueItem>();
            MissileBuildQueue = new BindingList<MissileBuildQueueItem>();
            FighterBuildQueue = new BindingList<FighterBuildQueueItem>();

            IsRefining = false;

            ShipyardTasks = new Dictionary<Installation.ShipyardInformation.ShipyardTask, Installation.ShipyardInformation>();

            ThermalDetection = new BindingList<int>();
            EMDetection = new BindingList<int>();
            ActiveDetection = new BindingList<int>();

            for (int loop = 0; loop < Constants.Faction.FactionMax; loop++)
            {
                ThermalDetection.Add(CurrentTimeSlice);
                EMDetection.Add(CurrentTimeSlice);
                ActiveDetection.Add(CurrentTimeSlice);
            }

            ShipsTargetting = new BindingList<ShipTN>();
            MissilesInFlight = new BindingList<OrdnanceGroupTN>();

            _SensorUpdateAck = 0;

            /// <summary>
            /// Terraforming Section:
            /// </summary>
            _GasAddSubtract = false;
            _GasAmt = 0.0f;
            _GasToAdd = null;
        }

        public override List<Constants.ShipTN.OrderType> LegalOrders(Faction faction)
        {
            List<Constants.ShipTN.OrderType> legalOrders = new List<Constants.ShipTN.OrderType>();
            legalOrders.AddRange(_legalOrders);
            if (faction == this.Faction)
            {
                legalOrders.Add(Constants.ShipTN.OrderType.LoadCrewFromColony);
                if (this.FuelStockpile > 0)
                    legalOrders.Add(Constants.ShipTN.OrderType.RefuelFromColony);
                if (this.MaintenanceSupplies > 0)
                    legalOrders.Add(Constants.ShipTN.OrderType.ResupplyFromColony);
                if (Array.Exists(this.Installations, x => x.Type == Installation.InstallationType.MaintenanceFacility))
                    legalOrders.Add(Constants.ShipTN.OrderType.BeginOverhaul);
                if (this.Installations.Count() > 0)
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadInstallation);
                if (this.ComponentStockpile.Count() > 0)
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadShipComponent);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadAllMinerals);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadAllMinerals);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadMineral);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadMineralWhenX);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadMineral);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadOrUnloadMineralsToReserve);
                if (this.CivilianPopulation > 0)
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadColonists);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadColonists);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadFuelToPlanet);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadSuppliesToPlanet);
                if (Array.Exists(this.Installations, x => x.Type == Installation.InstallationType.OrdnanceFactory) || this.MissileStockpile.Count > 0)
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadMineral);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadOrdnanceFromColony);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadOrdnanceToColony);
            }
            return legalOrders;
        }

        #region starting options and debug
        /// <summary>
        /// start without TN technology.
        /// </summary>
        public void ConventionalStart()
        {
            Installations[(int)Installation.InstallationType.ConventionalIndustry].Number = 1000.0f;
            Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number = 1.0f;
            Installations[(int)Installation.InstallationType.MilitaryAcademy].Number = 1.0f;
            Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number = 1.0f;

            Installation.ShipyardInformation SYI = new Installation.ShipyardInformation(Faction, Constants.ShipyardInfo.SYType.Naval, 1);

            Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo.Add(SYI);

            Faction.AddNewTaskGroup("Shipyard TG", Planet, Planet.Position.System);

            Installations[(int)Installation.InstallationType.MaintenanceFacility].Number = 5.0f;
            Installations[(int)Installation.InstallationType.ResearchLab].Number = 5.0f;

            FuelStockpile = 0.0f;
            MaintenanceSupplies = 2000.0f;

            CivilianPopulation = 500.0f;

            IsRefining = true;

            /// <summary>
            /// A DSTS was given to this population, so tell the UI to display it.
            /// </summary>
            _SensorUpdateAck++; 
        }

        /// <summary>
        /// Start a transnewtonian empire. not yet implemented.
        /// </summary>
        public void TNStart()
        {
            CivilianPopulation = 500.0f;

        }

        #endregion

        /// <summary>
        /// I am not sure if this will be necessary but since the population has detection statistics it should have a contact with an accessible
        /// location to the SystemContactList.
        /// </summary>
        public void UpdateLocation()
        {
            Contact.UpdateLocationInSystem(Planet.Position.X, Planet.Position.Y);
        }

        /// <summary>
        /// How long does it take to load or unload from this population?
        /// </summary>
        /// <param name="TaskGroupTime">Time that the taskgroup will take barring any planetary modifiers. this is calculated beforehand.</param>
        /// <returns>Time in seconds.</returns>
        public int CalculateLoadTime(int TaskGroupTime)
        {
            float NumStarports = m_aoInstallations[(int)Installation.InstallationType.Spaceport].Number;

            int TotalTime = TaskGroupTime;

            if (GovernorPresent)
                TotalTime = (int)((float)TaskGroupTime / ((NumStarports + 1.0f) * PopulationGovernor.LogisticsBonus));
            else
                TotalTime = (int)((float)TaskGroupTime / (NumStarports + 1.0f));

            return TotalTime;
        }


        /// <summary>
        /// Add Components to stockpile places increment number of componentDefs into the planetary stockpile.
        /// </summary>
        /// <param name="ComponentDef">Component to be added. This is the class all components inherit from, not any particular type of component.</param>
        /// <param name="increment">Number to add to the stockpile.</param>
        public void AddComponentsToStockpile(ComponentDefTN ComponentDef, float increment)
        {
            if (ComponentStockpileLookup.ContainsKey(ComponentDef.Id) == true)
            {
                ComponentStockpileCount[ComponentStockpileLookup[ComponentDef.Id]] = ComponentStockpileCount[ComponentStockpileLookup[ComponentDef.Id]] + increment;
            }
            else
            {
                ComponentStockpile.Add(ComponentDef);
                ComponentStockpileCount.Add(increment);
                ComponentStockpileLookup.Add(ComponentDef.Id, ComponentStockpile.IndexOf(ComponentDef));
            }
        }

        /// <summary>
        /// Constructs TN facilities at this population center.
        /// </summary>
        /// <param name="Inst">Installation to be built</param>
        /// <param name="increment">Amount of said installation to be built</param>
        public void AddInstallation(Installation Inst, float increment)
        {
            int Index = (int)Inst.Type;
            switch (Inst.Type)
            {
                    /// <summary>
                    /// Conversions must be converted to the installation we are going to add. Subtraction should have been handled elsewhere.
                    /// </summary>
                case Installation.InstallationType.ConvertCIToConstructionFactory:
                    Index = (int)Installation.InstallationType.ConstructionFactory;
                    break;
                case Installation.InstallationType.ConvertCIToFighterFactory:
                    Index = (int)Installation.InstallationType.FighterFactory;
                    break;
                case Installation.InstallationType.ConvertCIToFuelRefinery:
                    Index = (int)Installation.InstallationType.FuelRefinery;
                    break;
                case Installation.InstallationType.ConvertCIToMine:
                    Index = (int)Installation.InstallationType.Mine;
                    break;
                case Installation.InstallationType.ConvertCIToOrdnanceFactory:
                    Index = (int)Installation.InstallationType.OrdnanceFactory;
                    break;
                case Installation.InstallationType.ConvertMineToAutomated:
                    Index = (int)Installation.InstallationType.AutomatedMine;
                    break;


                case Installation.InstallationType.NavalShipyardComplex:
                    /// <summary>
                    /// Shipyards must have a shipyard info created for them.
                    /// </summary>
                    float Adjustment = (float)Math.Floor(Installations[Index].Number + increment) - (float)Math.Floor(Installations[Index].Number);
                    int Number = (int)Math.Floor(Installations[Index].Number);
                    if (Number == 0)
                        Number++;
                    while (Adjustment >= 1.0f)
                    {
                        Installation.ShipyardInformation SYI = new Installation.ShipyardInformation(Faction, Constants.ShipyardInfo.SYType.Naval, Number);
                        Number++;
                        
                        Installations[Index].SYInfo.Add(SYI);
                        Adjustment = Adjustment - 1.0f;
                    }
                    break;
                case Installation.InstallationType.CommercialShipyard:
                    /// <summary>
                    /// Shipyards must have a shipyard info created for them.
                    /// </summary>
                    Adjustment = (float)Math.Floor(Installations[Index].Number + increment) - (float)Math.Floor(Installations[Index].Number);
                    Number = (int)Math.Floor(Installations[Index].Number);
                    if (Number == 0)
                        Number++;
                    while (Adjustment >= 1.0f)
                    {
                        Installation.ShipyardInformation SYI = new Installation.ShipyardInformation(Faction, Constants.ShipyardInfo.SYType.Commercial, Number);
                        Number++;

                        Installations[Index].SYInfo.Add(SYI);
                        Adjustment = Adjustment - 1.0f;
                    }
                    break;
                case Installation.InstallationType.DeepSpaceTrackingStation:
                    /// <summary>
                    /// A DSTS was given to this population, so tell the UI to display it.
                    /// </summary>
                    _SensorUpdateAck++; 
                    break;
            }
            Installations[Index].Number = Installations[Index].Number + increment;
        }

        /// <summary>
        /// On loading an installation decrement the installation[].number, and handle other conditions such as potential sensor UI updates.
        /// </summary>
        /// <param name="iType">Type of installation/</param>
        /// <param name="massToLoad">Total mass of the installation of type iType to take from the planet.</param>
        public void LoadInstallation(Installation.InstallationType iType, int massToLoad)
        {
            Installations[(int)iType].Number =Installations[(int)iType].Number - (float)(massToLoad / Faction.InstallationTypes[(int)iType].Mass);
            switch (iType)
            {
                case Installation.InstallationType.DeepSpaceTrackingStation:
                    /// <summary>
                    /// A DSTS was given to this population, so tell the UI to display it.
                    /// </summary>
                    _SensorUpdateAck++;
                    break;
            }
        }

        /// <summary>
        /// When an installation is unloaded to this population run this function to handle incremented installation[].number and other conditions.
        /// </summary>
        /// <param name="iType">Type of installation</param>
        /// <param name="massToUnload">Total mass of said installation to unload. this can result in fractional changes to installation[].number</param>
        public void UnloadInstallation(Installation.InstallationType iType, int massToUnload)
        {
            Installations[(int)iType].Number = Installations[(int)iType].Number + (float)(massToUnload / Faction.InstallationTypes[(int)iType].Mass);
            switch(iType)
            {
                case Installation.InstallationType.DeepSpaceTrackingStation:
                    /// <summary>
                    /// A DSTS was given to this population, so tell the UI to display it.
                    /// </summary>
                    _SensorUpdateAck++;
                break;
            }
        }

        /// <summary>
        /// Constructs maintenance supply parts at this population.
        /// </summary>
        /// <param name="increment">number to build.</param>
        public void AddMSP(float increment)
        {
            MaintenanceSupplies = MaintenanceSupplies + increment;
        }

        /// <summary>
        /// TakeComponents from Stockpile takes the specified number of components out of the stockpile, and returns how many were subtracted.
        /// </summary>
        /// <param name="ComponentDef">Component def to be removed.</param>
        /// <param name="decrement">number to remove</param>
        /// <returns>number that were removed.</returns>
        public float TakeComponentsFromStockpile(ComponentDefTN ComponentDef, float decrement)
        {
            float Components = 0.0f;
            if (ComponentStockpileLookup.ContainsKey(ComponentDef.Id) == true)
            {
                Components = ComponentStockpileCount[ComponentStockpileLookup[ComponentDef.Id]];

                if (Components - decrement <= 0.0f)
                {
                    ComponentStockpile.RemoveAt(ComponentStockpileLookup[ComponentDef.Id]);
                    ComponentStockpileCount.RemoveAt(ComponentStockpileLookup[ComponentDef.Id]);
                    ComponentStockpileLookup.Remove(ComponentDef.Id);

                    return Components;
                }
                else
                {
                    Components = Components - decrement;
                    ComponentStockpileCount[ComponentStockpileLookup[ComponentDef.Id]] = Components;
                }
            }
            else
            {
                /// <summary>
                /// Invalid remove request sent from somewhere. Error reporting? logs?
                /// </summary>
                return -1.0f;
            }

            return decrement;
        }

        /// <summary>
        /// Loads or unloads missiles from a population.
        /// </summary>
        /// <param name="Missile">Ordnance type to be loaded or unloaded.</param>
        /// <param name="inc">Amount to load or unload.</param>
        /// <returns>Missiles placed into stockpile or taken out of it.</returns>
        public float LoadMissileToStockpile(OrdnanceDefTN Missile, float inc)
        {
            if (inc > 0)
            {
                if (MissileStockpile.ContainsKey(Missile))
                {
                    MissileStockpile[Missile] = MissileStockpile[Missile] + inc;
                }
                else
                {
                    MissileStockpile.Add(Missile, inc);
                }
                return inc;
            }
            else
            {
                if (MissileStockpile.ContainsKey(Missile) == false)
                {
                    return 0;
                }
                else
                {
                    /// <summary>
                    /// Inc is negative here.
                    /// </summary>
                    float retVal = MissileStockpile[Missile];
                    MissileStockpile[Missile] = MissileStockpile[Missile] + inc;

                    if (MissileStockpile[Missile] <= 0)
                    {
                        MissileStockpile.Remove(Missile);

                        return retVal;
                    }

                    return inc;
                }
            }
        }

        #region Sensor Characteristcs
        /// <summary>
        /// Calculate the thermal signature of this colony
        /// </summary>
        /// <returns>Thermal Signature</returns>
        public int CalcThermalSignature()
        {
            int signature = (int)Math.Round(CivilianPopulation * Constants.Colony.CivilianThermalSignature);
            foreach (Installation Inst in m_aoInstallations)
            {
                if (Inst.Type == Installation.InstallationType.CommercialShipyard)
                {
                    int ThermalBase = (int)Inst.ThermalSignature;
                    int SYCount = (int)Math.Floor(Inst.Number);
                    for (int SYIterator = 0; SYIterator < SYCount; SYIterator++)
                    {
                        int totalTons = Inst.SYInfo[SYIterator].Tonnage * Inst.SYInfo[SYIterator].Slipways;
                        signature = signature + ThermalBase + (int)Math.Round((float)totalTons / Constants.Colony.CommercialShipyardTonnageDivisor);
                    }
                }
                else if (Inst.Type == Installation.InstallationType.NavalShipyardComplex)
                {
                    int ThermalBase = (int)Inst.ThermalSignature;
                    int SYCount = (int)Math.Floor(Inst.Number);
                    for (int SYIterator = 0; SYIterator < SYCount; SYIterator++)
                    {
                        int totalTons = Inst.SYInfo[SYIterator].Tonnage * Inst.SYInfo[SYIterator].Slipways;
                        signature = signature + ThermalBase + (int)Math.Round((float)totalTons / Constants.Colony.NavalShipyardTonnageDivisor);
                    }
                }
                else
                {
                    signature = signature + (int)Math.Round(Inst.ThermalSignature * Math.Floor(Inst.Number));
                }
            }
            ThermalSignature = signature;
            return signature;
        }

        /// <summary>
        /// Calculate the EM signature of this colony
        /// </summary>
        /// <returns>EM Signature</returns>
        public int CalcEMSignature()
        {
            int signature = (int)Math.Round(CivilianPopulation * Constants.Colony.CivilianEMSignature);
            foreach (Installation Inst in m_aoInstallations)
            {
                if (Inst.Type == Installation.InstallationType.CommercialShipyard)
                {
                    int EMBase = (int)Inst.EMSignature;
                    int SYCount = (int)Math.Floor(Inst.Number);
                    for (int SYIterator = 0; SYIterator < SYCount; SYIterator++)
                    {
                        int totalTons = Inst.SYInfo[SYIterator].Tonnage * Inst.SYInfo[SYIterator].Slipways;
                        signature = signature + EMBase + (int)Math.Round((float)totalTons / Constants.Colony.CommercialShipyardTonnageDivisor);
                    }
                }
                else if (Inst.Type == Installation.InstallationType.NavalShipyardComplex)
                {
                    int EMBase = (int)Inst.EMSignature;
                    int SYCount = (int)Math.Floor(Inst.Number);
                    for (int SYIterator = 0; SYIterator < SYCount; SYIterator++)
                    {
                        int totalTons = Inst.SYInfo[SYIterator].Tonnage * Inst.SYInfo[SYIterator].Slipways;
                        signature = signature + EMBase + (int)Math.Round((float)totalTons / Constants.Colony.NavalShipyardTonnageDivisor);
                    }
                }
                else
                {
                    signature = signature + (int)Math.Round(Inst.EMSignature * Math.Floor(Inst.Number));
                }
            }
            EMSignature = signature;
            return signature;
        }
        #endregion

        #region Build Queue
        /// <summary>
        /// Add an installation to the build queue.
        /// </summary>
        /// <param name="Install">Installation to add.</param>
        /// <param name="BuildAmt">number of such installations to construct.</param>
        /// <param name="RequestedBuildPercentage">Percent of construction factories, conventional industry, engineering teams to devote to construction.</param>
        public void BuildQueueAddInstallation(Installation Install, float BuildAmt, float RequestedBuildPercentage)
        {
            ConstructionBuildQueueItem NewCBQItem = new ConstructionBuildQueueItem(Install);
            NewCBQItem.UpdateBuildQueueInfo(BuildAmt, RequestedBuildPercentage, true,Install.Cost);

            ConstructionBuildQueue.Add(NewCBQItem);
        }

        /// <summary>
        /// Add a component to the build queue.
        /// </summary>
        /// <param name="ComponentDef">Component to add.</param>
        /// <param name="BuildAmt">number of components to construct.</param>
        /// <param name="RequestedBuildPercentage">Percent of construction factories, conventional industry, engineering teams to devote to construction.</param>
        public void BuildQueueAddComponent(ComponentDefTN ComponentDef, float BuildAmt, float RequestedBuildPercentage)
        {
            ConstructionBuildQueueItem NewCBQItem = new ConstructionBuildQueueItem(ComponentDef);
            NewCBQItem.UpdateBuildQueueInfo(BuildAmt, RequestedBuildPercentage, true,ComponentDef.cost);

            ConstructionBuildQueue.Add(NewCBQItem);
        }

        /// <summary>
        /// Add MSP to the build queue.
        /// </summary>
        /// <param name="BuildAmt">number of MSP to construct.</param>
        /// <param name="RequestedBuildPercentage">Percent of construction factories, conventional industry, engineering teams to devote to construction.</param>
        public void BuildQueueAddMSP(float BuildAmt, float RequestedBuildPercentage)
        {
            ConstructionBuildQueueItem NewCBQItem = new ConstructionBuildQueueItem();
            NewCBQItem.UpdateBuildQueueInfo(BuildAmt, RequestedBuildPercentage, true, Constants.Colony.MaintenanceSupplyCost);

            ConstructionBuildQueue.Add(NewCBQItem);
        }

        /// <summary>
        /// Add a missile to the missile build queue.
        /// </summary>
        /// <param name="MissileDef">Missile to add</param>
        /// <param name="BuildAmt">missile build count</param>
        /// <param name="RequestedBuildPercentage">percentage of Ordnance factories to devote to construction.</param>
        public void BuildQueueAddMissile(OrdnanceDefTN MissileDef, float BuildAmt, float RequestedBuildPercentage)
        {
            MissileBuildQueueItem NewMBQItem = new MissileBuildQueueItem(MissileDef);
            NewMBQItem.UpdateBuildQueueInfo(BuildAmt, RequestedBuildPercentage, true, MissileDef.cost);

            MissileBuildQueue.Add(NewMBQItem);
        }

        #region production calculation: Construction, ordnance, fighters, mining, refining and others TBD. not all modifiers in place.
        /// <summary>
        /// Add Construction factories, engineering squads, and conventional industry, then modify by construction technology, governor bonus, sector bonus.
        /// </summary>
        /// <returns>total annual industrial production</returns>
        public float CalcTotalIndustry()
        {
#warning No Governor,Sector, Tech bonuses, and no engineering squad additions. likewise activation and deactivation of industry should be handled. also efficiencies. also for OF and FF, mining and refining.
#warning implement radiation in addition to governor/sector bonuses. industrial penalty is Rad / 100(45 = -0.45%)
            float BP = (float)Math.Floor(Installations[(int)Installation.InstallationType.ConstructionFactory].Number) * 10.0f + (float)Math.Floor(Installations[(int)Installation.InstallationType.ConventionalIndustry].Number);
            return BP;
        }

        /// <summary>
        /// Add ordnance factories and modify by tech, efficiency, governor,etc
        /// </summary>
        /// <returns></returns>
        public float CalcTotalOrdnanceIndustry()
        {
            float BP = (float)(Math.Floor(Installations[(int)Installation.InstallationType.OrdnanceFactory].Number) * 10.0f);
            return BP;
        }

        /// <summary>
        /// Add Fighter factories, tech, efficiency, governor,etc to produce total fighter factory industry.
        /// </summary>
        /// <returns></returns>
        public float CalcTotalFighterIndustry()
        {
            float BP = (float)(Math.Floor(Installations[(int)Installation.InstallationType.FighterFactory].Number) * 10.0f);
            return BP;
        }

        /// <summary>
        /// Add mines, automines, conventional industry
        /// </summary>
        /// <returns></returns>
        public float CalcTotalMining()
        {
            float MP = (float)(Math.Floor(Installations[(int)Installation.InstallationType.Mine].Number) * 10.0f) + (float)(Math.Floor(Installations[(int)Installation.InstallationType.AutomatedMine].Number) * 10.0f)
                              + (float)(Math.Floor(Installations[(int)Installation.InstallationType.ConventionalIndustry].Number));

            return MP;
        }

        /// <summary>
        /// Add refineries and CI to get total refining.
        /// </summary>
        /// <returns></returns>
        public float CalcTotalRefining()
        {
            float BP = (float)(Math.Floor(Installations[(int)Installation.InstallationType.FuelRefinery].Number) * Constants.Colony.SoriumToFuel * 10.0f) +
                       (float)Math.Floor(Installations[(int)Installation.InstallationType.ConventionalIndustry].Number * Constants.Colony.SoriumToFuel);
            return BP;
        }
        
        /// <summary>
        /// Add Terraforming installations to orbital terraforming modules.
        /// </summary>
        /// <returns></returns>
        public float CalcTotalTerraforming()
        {
            int modules = (int)Math.Floor(_OrbitalTerraformModules);
            float TP = (float)((int)Math.Floor(Installations[(int)Installation.InstallationType.TerraformingInstallation].Number) + modules) * Constants.Colony.TerraformRate[0];

            return TP;
        }

        /// <summary>
        /// Calculates the population growth of this colony
        /// </summary>
        /// <returns></returns>
        public float CalcPopulationGrowth()
        {
            /// <summary>
            /// Don't want a divide by zero from this.
            /// </summary>
            if (CivilianPopulation == 0.0f)
                return 0.0f;

            /// <summary>
            /// This is the AuroraTN population growth rate formula: 20 / cubed root(Population)
            /// </summary>
            float AnnualColonyGrowthRate = 20.0f / (float)Math.Pow((double)CivilianPopulation, (1.0 / 3.0));

            float RadGrowthAdjust = Planet.RadiationLevel / 400.0f;

            AnnualColonyGrowthRate = AnnualColonyGrowthRate - RadGrowthAdjust;

            if (AnnualColonyGrowthRate > 10.0f)
                AnnualColonyGrowthRate = 10.0f;

#warning sector bonuses come after cap for pop growth.


            /// <summary>
            /// Set the population growth rate here.
            /// </summary>
            PopulationGrowthRate = AnnualColonyGrowthRate;

            return AnnualColonyGrowthRate;
        }
        #endregion


        /// <summary>
        /// CIRequirement checks to see if this population center has enough Conventional industry to perform conversions.
        /// </summary>
        /// <param name="CIReq">Number to convert</param>
        /// <returns>Whether enough CI is present.</returns>
        public bool CIRequirement(float CIReq)
        {
            bool ret = false;
            if (Installations[(int)Installation.InstallationType.ConventionalIndustry].Number >= CIReq)
            {
                ret = true;
            }
            return ret;
        }

        /// <summary>
        /// MineRequirement checks to see if enough mines are present to convert to automines.
        /// </summary>
        /// <param name="MineReq">Number to convert</param>
        /// <returns>Are enough present?</returns>
        public bool MineRequirement(float MineReq)
        {
            bool ret = false;
            if (Installations[(int)Installation.InstallationType.Mine].Number >= MineReq)
            {
                ret = true;
            }
            return ret;
        }

        /// <summary>
        /// Mineral Requirement checks to see if this population has enough of the specified mineral to commence building.
        /// </summary>
        /// <param name="MineralCost">Cost in minerals of this project.</param>
        /// <param name="Completion">Completed items</param>
        /// <returns>Whether enough of said mineral is present.</returns>
        public bool MineralRequirement(decimal[] MineralCost, float Completion)
        {
            bool ret = true;
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                if (MineralCost[(int)mineralIterator] != 0.0m)
                {
                    if (m_aiMinerials[mineralIterator] >= ((float)MineralCost[(int)mineralIterator] * Completion))
                    {
                        ret = true;
                    }
                    else
                    {
                        ret = false;
                        break;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// HandleBuildItemCost decrements the mineral count by the cost of the item.
        /// </summary>
        /// <param name="Item Cost">Wealth cost of item</param>
        /// <param name="MineralCost">Mineral Requirement</param>
        /// <param name="Completion">number of item to be built. This will be multiplied against wealth and minerals.</param>
        /// <param name="CIConvReq">Will CI be required?</param>
        /// <param name="MineConvReq">Will a mine be required?</param>
        public void HandleBuildItemCost(decimal ItemCost, decimal[] MineralCost, float Completion, bool CIConvReq = false, bool MineConvReq = false)
        {
            /// <summary>
            /// Wealth cost adjustment. maybe these conversions can be handled better.
            /// </summary>
            Faction.FactionWealth = Faction.FactionWealth - (decimal)((float)ItemCost * Completion);

            /// <summary>
            /// Mineral Cost adjustment.
            /// </summary>
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                if (MineralCost[mineralIterator] != 0.0m)
                {
                    m_aiMinerials[mineralIterator] = m_aiMinerials[mineralIterator] - ((float)MineralCost[mineralIterator] * Completion);
                }
            }

            /// <summary>
            /// CI Installation requirement adjustment. if CIConvAmt is -1 then no CI are required.
            /// </summary>
            if (CIConvReq == true)
            {
                Installations[(int)Installation.InstallationType.ConventionalIndustry].Number = Installations[(int)Installation.InstallationType.ConventionalIndustry].Number - Completion;
            }

            /// <summary>
            /// Mine conversion adjustment. if MineConvAmt is -1 then no CI are required.
            /// </summary>
            if (MineConvReq == true)
            {
                Installations[(int)Installation.InstallationType.Mine].Number = Installations[(int)Installation.InstallationType.Mine].Number - Completion;
            }
        }

        /// <summary>
        /// HandleShipyardCost will process buildcosts from shipyard items. This is a separate function because shipyard costs may be a little different.
        /// No installations will be required for one thing.
        /// </summary>
        /// <param name="ItemCost">Total Cost of the shipyard activity or task</param>
        /// <param name="MineralCost">Total cost in minerals of the shipyards work</param>
        public void HandleShipyardCost(decimal ItemCost, decimal[] MineralCost, float Completion)
        {
            /// <summary>
            /// Wealth cost adjustment. maybe these conversions can be handled better.
            /// </summary>
            Faction.FactionWealth = Faction.FactionWealth - (decimal)((float)ItemCost * Completion);

            /// <summary>
            /// Mineral Cost adjustment.
            /// </summary>
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                if (MineralCost[mineralIterator] != 0.0m)
                {
                    m_aiMinerials[mineralIterator] = m_aiMinerials[mineralIterator] - ((float)MineralCost[mineralIterator] * Completion);
                }
            }
        }

        /// <summary>
        /// Changes the population and handles anything resulting from that for the colony.
        /// </summary>
        /// <param name="Growth">Amount of population to add/Subtract to this colony.</param>
        public void AddPopulation(float Growth)
        {
            CivilianPopulation = CivilianPopulation + Growth;
        }
        #endregion

        #region Population damage due to combat
        public bool OnDamaged(DamageTypeTN TypeOfDamage, ushort Value, ShipTN FiringShip, int RadLevel = 0)
        {
            /// <summary>
            /// Check damage type to see if atmosphere blocks it.
            /// Populations are damaged in several ways.
            /// Civilian Population will die off, about 50k per point of damage.
            /// Atmospheric dust will be kicked into the atmosphere(regardless of whether or not there is an atmosphere) lowering temperature for a while.
            /// Some beam weapons are blocked(partially or in whole) by atmosphere(Lasers,Gauss,Railguns,particle beams, plasma), some are not(Mesons), and some have no effect(microwaves) on populations.
            /// PDCs will be similarly defended by this atmospheric blocking but are vulnerable to microwaves.
            /// Missiles will increase the radiation value of the colony. Enhanced Radiation warheads will add more for less overall damage done. Radiation is of course harmful to life on the world. but
            /// not immediately so.
            /// Installations will have a chance at being destroyed. bigger installations should be more resilient to damage, but even infrastructure should have a chance to survive.
            /// Shipyards must be targetted in orbit around the colony. Special handling will be required for that.
            /// </summary>

            ushort ActualDamage;
            switch (TypeOfDamage)
            {
                /// <summary>
                /// Neither missile nor meson damage is effected by atmospheric pressure.
                /// </summary>
                case DamageTypeTN.Missile:
                case DamageTypeTN.Meson:
                    ActualDamage = Value;
                break;
                /// <summary>
                /// All other damage types must be adjusted by atmospheric pressure.
                /// </summary>
                default:
                    ActualDamage = (ushort)Math.Round((float)Value * Planet.Atmosphere.Pressure);
                break;
            }

            /// <summary>
            /// No damage was done. either all damage was absorbed by the atmosphere or the missile had no warhead. Missiles with no warhead should "probably" be sensor missiles that loiter in orbit
            /// until their fuel is gone.
            /// </summary>
            if (ActualDamage == 0)
            {
                return false;
            }

            /// <summary>
            /// Each point of damage kills off 50,000 people, or 0.05f as 1.0f = 1M people.
            /// </summary>
            float PopulationDamage = 0.05f * ActualDamage;
            CivilianPopulation = CivilianPopulation - PopulationDamage;

            /// <summary>
            /// Increase the atmospheric dust and radiation of the planet.
            /// </summary>
            Planet.AtmosphericDust = Planet.AtmosphericDust + ActualDamage;
            Planet.RadiationLevel = Planet.RadiationLevel + RadLevel;

            if (GameState.Instance.DamagedPlanets.Contains(Planet) == false)
                GameState.Instance.DamagedPlanets.Add(Planet);

            String IndustrialDamage = "Industrial Damage:";
            while (ActualDamage > 0)
            {
                ActualDamage = (ushort)(ActualDamage - 5);
                /// <summary>
                /// Installation destruction will be naive. pick an installation at random.
                /// </summary>
                int Inst = GameState.RNG.Next(0, (int)Installation.InstallationType.InstallationCount);
                if (Inst == (int)Installation.InstallationType.CommercialShipyard || Inst == (int)Installation.InstallationType.NavalShipyardComplex)
                {
                    /// <summary>
                    /// Damage was done, but installations escaped unharmed. Shipyards must be damaged from orbit.
                    /// </summary>
                    continue;
                }
                else if (Inst >= (int)Installation.InstallationType.ConvertCIToConstructionFactory && Inst <= (int)Installation.InstallationType.ConvertMineToAutomated)
                {
                    /// <summary>
                    /// These "installations" can't be damaged, so again, lucky planet.
                    /// </summary>
                    continue;
                }
                else
                {
                    int InstCount = (int)Math.Floor(Installations[Inst].Number);

                    /// <summary>
                    /// Luckily for the planet it had none of the installations that just got targetted.
                    /// </summary>
                    if (InstCount == 0)
                    {
                        continue;
                    }

                    switch ((Installation.InstallationType)Inst)
                    {
                        case Installation.InstallationType.DeepSpaceTrackingStation:
                            /// <summary>
                            /// A DSTS was destroyed at this population, inform the UI to update the display.
                            /// </summary>
                            _SensorUpdateAck++; 
                            break;
                    }

                    /// <summary>
                    /// Installation destroyed.
                    /// </summary>
                    Installations[Inst].Number = Installations[Inst].Number - 1.0f;

#warning Industry damage should be reworked to have differing resilience ratings, and logging should compress industrial damage.
                    IndustrialDamage = String.Format("{0} {1}: {2}", IndustrialDamage, Installations[Inst].Name, 1);
                }
            }

            String Entry = String.Format("{0} hit by {1} points of damage. Casualties: {2}{3}Environment Update: Atmospheric Dust:{4}, Radiation:{5}", Name,ActualDamage,PopulationDamage,
                                         IndustrialDamage,Planet.AtmosphericDust, Planet.RadiationLevel);
            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.PopulationDamage, Planet.Position.System, Contact,
            GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Entry);
            Faction.MessageLog.Add(NMsg);

            return false;
        }
        #endregion
    }
}
