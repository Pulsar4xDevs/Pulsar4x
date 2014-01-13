using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Entities
{
    public class ShipClassTN : GameEntity
    {
        #region Class Members
        /// <summary>
        /// Not using these yet, may not use some of them at all.
        /// </summary>


        [Browsable(false)]
        public Faction Faction { get; set; }

        [Browsable(false)]
        public int HullDescriptionId { get; set; }

        [DisplayName("Required Rank"),
        Category("Detials"),
        Description("Minium Rank needed to command this Class of ship."),
        Browsable(true),
        ReadOnly(true)]
        public int RequiredRank { get; set; }

        [Browsable(false)]
        public int MaxLifeSupport { get; set; } //not sure what life support is supposed to be? deployment time?

        /// <summary>
        /// Notes on this ship class, used by the UI/Player.
        /// </summary>
        [DisplayName("Notes"), 
        Category("Description"),
        Description("Notes on this ship class"),
        Browsable(true),
        ReadOnly(false)]
        public string Notes { get; set; }

        /// <summary>
        /// Class design summary.
        /// </summary>
        [DisplayName("Summary"),
        Category("Description"),
        Description("Class Summary"),
        Browsable(true),
        ReadOnly(false)]
        public String Summary { get; set; }

        [Browsable(false)]
        public BindingList<ShipTN> ShipsInClass { get; set; }

        /// <summary>
        /// Cost in BP of the ship, not its mineral cost, not doing that just yet.
        /// </summary>
        [DisplayName("Cost"), 
        Category("Detials"),
        Description("Cost in BP of the ship."),
        Browsable(true),
        ReadOnly(true)]
        public decimal BuildPointCost { get; set; }

        /// <summary>
        /// Size in tons.
        /// </summary>
        [DisplayName("Size in Tons"), 
        Category("Detials"),
        Description("Size in tons."),
        Browsable(true),
        ReadOnly(true)]
        public float SizeTons { get; set; }

        /// <summary>
        /// Size in tons / 50.0; 1HS = 50 tons.
        /// </summary>
        [DisplayName("Size in HS"), 
        Category("Detials"),
        Description("Size in Hull Sizes (HS), 1HS = 50 tons."),
        Browsable(true),
        ReadOnly(true)]
        public float SizeHS { get; set; }

        /// <summary>
        /// Total Internal Hit to kill of all components. components need not all be destroyed in order for a ship to be destroyed however.
        /// </summary>
        [DisplayName("Total HTK"), 
        Category("Detials"),
        Description("Total Internal Hit to kill of all components."),
        Browsable(true),
        ReadOnly(true)]
        public int TotalHTK { get; set; }

        /// <summary>
        /// Is this ship a tanker?
        /// </summary>
        [Browsable(false)]
        public bool IsTanker { get; set; }

        /// <summary>
        /// Is this ship a resupply vessel?
        /// </summary>
        [Browsable(false)]
        public bool IsSupply { get; set; }

        /// <summary>
        /// Is this ship an ammunition collier?
        /// </summary>
        [Browsable(false)]
        public bool IsCollier { get; set; }

        /// <summary>
        /// Is this ship a military or civilian flagged design?
        /// </summary>
        [Browsable(false)]
        public bool IsMilitary { get; set; }

        /// <summary>
        /// Has the user declared this design obsolete?
        /// </summary>
        [Browsable(false)]
        public bool IsObsolete { get; set; }
        
        /// <summary>
        /// Is this design locked to further changes?
        /// </summary>
        [Browsable(false)]
        public bool IsLocked { get; set; }

        /// <summary>
        /// How many military grade components are part of this ship?
        /// </summary>
        [Browsable(false)]
        public int MilitaryComponentCount { get; set; }


        /// <summary>
        /// Internal listing of component definitions for max repair calculation.
        /// </summary>
        [Browsable(false)]
        public BindingList<ComponentDefTN> ListOfComponentDefs { get; set; }

        /// <summary>
        /// Internal listing of component definition counts for max repair calculation.
        /// </summary>
        [Browsable(false)]
        public BindingList<short> ListOfComponentDefsCount { get; set; }

        /// <summary>
        /// List of where each component group falls on the ships hull distribution. size less than 1 components each have DAC of atleast 1.
        /// </summary>
        [DisplayName("Damage Allocation Chart"), 
        Category("Detials"),
        Description("Where components are in relation to potential internal component strikes."),
        Browsable(true),
        ReadOnly(true)]
        public Dictionary<ComponentDefTN,int> DamageAllocationChart { get; set; }

        [DisplayName("Electronic Damage Allocation Chart"),
        Category("Detials"),
        Description("Electronic only DAC for microwave hits."),
        Browsable(true),
        ReadOnly(true)]
        public Dictionary<ComponentDefTN, int> ElectronicDamageAllocationChart { get; set; }

        /// <summary>
        /// What is the perceived protection value this ship class provides to civilians.
        /// </summary>
        [DisplayName("Planetary Protection Value"), 
        Category("Detials"),
        Description("What is the perceived protection value this ship class provides to civilians."),
        Browsable(true),
        ReadOnly(true)]
        public int PlanetaryProtectionValue { get; set; }

        /// <summary>
        /// Armor statistics that matter to the class itself.
        /// </summary>
        [Browsable(false)]
        public ArmorDefTN ShipArmorDef { get; set; }

        /// <summary>
        /// Crew Quarters, Small Crew Quarters, Tiny Crew Quarters.
        /// Every ship has a required crew amount, though for some small sensor only craft it might be 0.
        /// </summary>
        [DisplayName("Crew Quarters"), 
        Category("Component Lists"),
        Description("List of Crew Quarters on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<GeneralComponentDefTN> CrewQuarters { get; set; }

        [DisplayName("Crew Quarters Count"),
        Category("Component Counts"),
        Description("Number of Crew Quarters on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> CrewQuartersCount { get; set; }
        public int TotalCrewQuarters { get; set; }
        public int TotalRequiredCrew { get; set; }
        public int SpareCrewQuarters { get; set; }
        public int SpareCryoBerths { get; set; }
        public int MaxDeploymentTime { get; set; }
        public float TonsPerMan { get; set; }
        public float CapPerHS { get; set; }
        public float AccomHSRequirement { get; set; }
        public float AccomHSAvailable { get; set; }


        /// <summary>
        /// Fuel Tanks, Small Fuel Tanks, Tiny Fuel Tanks, Large Fuel Tanks, Very Large Fuel Tanks, Ultra Large Fuel Tanks.
        /// Should fuel compression be a tank type or an option? Base fuel storage is Tons * 1000.
        /// </summary>
        [DisplayName("Fuel Tanks"),
        Category("Component Lists"),
        Description("List of Fuel Tanks on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<GeneralComponentDefTN> FuelTanks { get; set; }

        [DisplayName("Fuel Tanks Count"),
        Category("Component Counts"),
        Description("Number of Fuel Tanks on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> FuelTanksCount { get; set; }

        [DisplayName("Total Fuel Capacity"),
        Category("Detials"),
        Description("The Total Fuel Capacity of the ship."),
        Browsable(true),
        ReadOnly(true)]
        public float TotalFuelCapacity { get; set; }

        /// <summary>
        /// Engineering bay, Small Engineering Bay, Tiny Engineering Bay, Fighter Engineering Bay.
        /// Ebays give 3 benefits, they increase MSP, they give a minor boost to damage control and they reduce failure rate for military vessels.
        /// Base Ebay storage is handled in a bizarre manner. 4% hull space of ebays = normal failure rate, and enough parts in BP to cover 1/2 the cost of the ship.
        /// </summary>
        [DisplayName("Engineering Bays"),
        Category("Component Lists"),
        Description("List of Engineering Bays on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<GeneralComponentDefTN> EngineeringBays { get; set; }

        [DisplayName("Engineering Bays Count"),
        Category("Component Counts"),
        Description("Number of Engineering Bays on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> EngineeringBaysCount { get; set; }

        [DisplayName("Total MSP Capacity"),
        Category("Engineering Detials"),
        Description("The Total MSP Capacity of the ship."),
        Browsable(true),
        ReadOnly(true)]
        public int TotalMSPCapacity { get; set; }

        [DisplayName("Engineering HS"),
        Category("Engineering Detials"),
        Description("Total size, in HS, dedicated to Engineering bays."),
        Browsable(true),
        ReadOnly(true)]
        public float EngineeringHS { get; set; }

        [DisplayName("Maintenance Life"),
        Category("Engineering Detials"),
        Description("The Maintenance Life of the ship."),
        Browsable(true),
        ReadOnly(true)]
        public float MaintenanceLife { get; set; }

        [DisplayName("Annual Failure Rate"),
        Category("Engineering Detials"),
        Description("The estimated Annual Failure Rate of the ship."),
        Browsable(true),
        ReadOnly(true)]
        public float AnnualFailureRate { get; set; }

        [DisplayName("Initial Failure Rate"),
        Category("Engineering Detials"),
        Description("The estimated Initial Failure Rate of the ship, i.e. the Failure rate when the Ship has just left the yard."),
        Browsable(true),
        ReadOnly(true)]
        public float InitialFailureRate { get; set; }

        public float YearOneFailureTotal { get; set; }
        public float YearFiveFailureTotal { get; set; }
        public int MaxDamageControlRating { get; set; }
        public int MaxRepair { get; set; }

        /// <summary>
        /// Bridge, Flag Bridge, Damage Control, Improved Damage Control, Advanced Damage Control, Maintenance bay, Recreational Facility, Orbital Habitat.
        /// Each gives a fairly specialist benefit that not all ships will need, excepting the bridge of course.
        /// </summary>
        [DisplayName("Other Components"),
        Category("Component Lists"),
        Description("List of Other Components on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<GeneralComponentDefTN> OtherComponents { get; set; }

        [DisplayName("Other Componenmts Count"),
        Category("Component Counts"),
        Description("Number of Other Componenmts on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> OtherComponentsCount { get; set; }
        [Browsable(false)]
        public bool HasBridge { get; set; }

        #region Engine
        /// <summary>
        /// each ship class can only have one type of engine, though several copies may be present.
        /// </summary>
        [DisplayName("Ship Engine Model"),
        Category("Engine Stats"),
        Description("The model/type of this ships engines"),
        Browsable(true),
        ReadOnly(true)]
        public EngineDefTN ShipEngineDef { get; set; }

        [DisplayName("Max Fuel Usage"),
        Category("Engine Stats"),
        Description("The maximum fuel usage per hour of this ships engines"),
        Browsable(true),
        ReadOnly(true)]
        public float MaxFuelUsePerHour { get; set; }

        [DisplayName("Engine Count"),
        Category("Engine Stats"),
        Description("The number of engines in this ship."),
        Browsable(true),
        ReadOnly(true)]
        public ushort ShipEngineCount { get; set; }

        /// <summary>
        /// Ship class Engine statistics.
        /// </summary>
        [DisplayName("Max Engine Power"),
        Category("Engine Stats"),
        Description("The maximum power of this ships engines"),
        Browsable(true),
        ReadOnly(true)]
        public int MaxEnginePower { get; set; }

        [DisplayName("Max Thermal Signature"),
        Category("Engine Stats"),
        Description("The maximum Thermal Signature generated by this ships engines"),
        Browsable(true),
        ReadOnly(true)]
        public int MaxThermalSignature { get; set; }

        [DisplayName("Max Speed"),
        Category("Engine Stats"),
        Description("the Maximum speed of this ship"),
        Browsable(true),
        ReadOnly(true)]
        public int MaxSpeed { get; set; }
        #endregion

        #region Active and Passive Sensors
        /// <summary>
        /// List of passive sensor types, and how many of each that there are in this ship.
        /// Likewise the best possible sensors are stored.
        /// </summary>
        [DisplayName("Passive Sensors"), 
        Category("Component Lists"),
        Description("List of passive sensors on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<PassiveSensorDefTN> ShipPSensorDef { get; set; }

        [DisplayName("Passive Sensors Count"), 
        Category("Component Counts"),
        Description("Number of passive sensors on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipPSensorCount { get; set; }

        [DisplayName("Best Thermal Rating"), 
        Category("Detials"),
        Description("Best Thermal Rating that can be detected by this ships passive sensors."),
        Browsable(true),
        ReadOnly(true)]
        public int BestThermalRating { get; set; }

        [DisplayName("Best EM Rating"), 
        Category("Detials"),
        Description("Best EM Rating that can be detected by this ships passive sensors."),
        Browsable(true),
        ReadOnly(true)]
        public int BestEMRating { get; set; }

        /// <summary>
        /// List of active sensors, as well as the number of each, and the TCS and EM signatures of the craft.
        /// </summary>
        [DisplayName("Active Sensors"), 
        Category("Component Lists"),
        Description("List of active sensors on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ActiveSensorDefTN> ShipASensorDef { get; set; }

        /// <summary>
        /// Number of Active sensors of each type on this ship class.
        /// </summary>
        [DisplayName("Active Sensors Count"), 
        Category("Component Counts"),
        Description("Number of active sensors on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipASensorCount { get; set; }

        /// <summary>
        /// Total Cross Section is essentially the HS count in integer form for the sensor model.
        /// </summary>
        [DisplayName("Cross Cection"), 
        Category("Detials"),
        Description("Total Cross Section of this Class."),
        Browsable(true),
        ReadOnly(true)]
        public int TotalCrossSection { get; set; }

        /// <summary>
        /// Largest possible EM signature for this ship class.
        /// </summary>
        [DisplayName("Max EM Signature"), 
        Category("Detials"),
        Description("Max EM Signature of this class.."),
        Browsable(true),
        ReadOnly(true)]
        public int MaxEMSignature { get; set; }
        #endregion

        #region Cargo/Cryo/Troop loading and unloading
        /// <summary>
        /// List of Cargo hold definitions present on this ship class.
        /// </summary>
        [DisplayName("Cargo holds"),
        Category("Component Lists"),
        Description("List of Cargo holds on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<CargoDefTN> ShipCargoDef { get; set; }
        
        /// <summary>
        /// Counter for each Cargo hold definition.
        /// </summary>
        [DisplayName("Cargo Holds Count"),
        Category("Component Counts"),
        Description("Number of Cargo holds on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipCargoCount { get; set; }

        /// <summary>
        /// Sum of the cargo capacity of all ship holds.
        /// </summary>
        [DisplayName("Cargo Space"),
        Category("Detials"),
        Description("Amount of cargo, in tons, that this ship can carry."),
        Browsable(true),
        ReadOnly(true)]
        public int TotalCargoCapacity { get; set; }


        /// <summary>
        /// List of Colony bay definitions present on this ship class.
        /// </summary>
        [DisplayName("Cryo Storage Bays"),
        Category("Component Lists"),
        Description("List of Cryo Storage Bays on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ColonyDefTN> ShipColonyDef { get; set; }

        /// <summary>
        /// Counter for each colony bay definition.
        /// </summary>
        [DisplayName("Cryo storage Count"),
        Category("Component Counts"),
        Description("Number of Cryo storage bays on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipColonyCount { get; set; }


        /// <summary>
        /// Sum of the troop bay capacity for the ship..
        /// </summary>
        [DisplayName("Troop Bay Capacity"),
        Category("Detials"),
        Description("Number of troops that the combined troop bays on this ship can accomodate."),
        Browsable(true),
        ReadOnly(true)]
        public int TotalTroopCapacity { get; set; }


        /// <summary>
        /// List of Cargo handling system types on this ship. there is no reason to ever put more than one type on a ship however.
        /// </summary>
        [DisplayName("Cargo Handling Systems"),
        Category("Component Lists"),
        Description("List of Cargo handling systems on this ship class"),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<CargoHandlingDefTN> ShipCHSDef { get; set; }

        /// <summary>
        /// Count of each Cargo handling system type on this ship.
        /// </summary>
        [DisplayName("Cargo Handling Systems Counts"),
        Category("Component Counts"),
        Description("Number of Cargo handling systems on this ship class"),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipCHSCount { get; set; }

        /// <summary>
        /// Reduction of base load time for Cargo/cryo capacity.
        /// </summary>
        [DisplayName("Tractor Multiplier"),
        Category("Detials"),
        Description("Reduction modifier for base Cargo/Cryo load times onto the ship."),
        Browsable(true),
        ReadOnly(true)]
        public int TractorMultiplier { get; set; }


        /// <summary>
        /// Full cargo load time of the ship class, not counting logistics leadership or spaceports.
        /// </summary>
        [DisplayName("Cargo Load Time"),
        Category("Detials"),
        Description("Load time of the cargo holds on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public int CargoLoadTime { get; set; }

        /// <summary>
        /// Full cryo load time of the ship class, not counting logistics leadership or spaceports.
        /// </summary>
        [DisplayName("Cryo Load Time"),
        Category("Detials"),
        Description("Load time of the cryo storage on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public int CryoLoadTime { get; set; }

        /// <summary>
        /// Full Troop load time of the ship class, not counting logistics leadership or spaceports.
        /// </summary>
        [DisplayName("Troop Load Time"),
        Category("Detials"),
        Description("Load time of the troop bays on this ship."),
        Browsable(true),
        ReadOnly(true)]
        public int TroopLoadTime { get; set; }
        #endregion

        #region Beam & FC Info
        [DisplayName("Beam Fire Controls"),
        Category("Component Lists"),
        Description("List of Beam Fire Controls on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<BeamFireControlDefTN> ShipBFCDef { get; set; }

        [DisplayName("Beam Fire Control Counts"),
        Category("Component Counts"),
        Description("Count of Beam Fire Controls on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipBFCCount { get; set; }

        [DisplayName("Beam Weapons"),
        Category("Component Lists"),
        Description("List of Beam Weapons on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<BeamDefTN> ShipBeamDef { get; set; }

        [DisplayName("Beam Weapon Counts"),
        Category("Component Counts"),
        Description("Count of Beam Weapons on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipBeamCount { get; set; }

        [DisplayName("Reactors"),
        Category("Component Lists"),
        Description("List of Reactors on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ReactorDefTN> ShipReactorDef { get; set; }

        [DisplayName("Reactor Counts"),
        Category("Component Counts"),
        Description("Count of Reactors on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipReactorCount { get; set; }

        [DisplayName("Total Power Requirement"),
        Category("Detials"),
        Description("Power required by beam weapon capacitors on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public int TotalPowerRequirement { get; set; }

        [DisplayName("Total Power Generation"),
        Category("Detials"),
        Description("Power Generation of all reactors on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public int TotalPowerGeneration { get; set; }
        #endregion

        #region Shield Info
        /// <summary>
        /// Only 1 shield is allowed per class.
        /// </summary>
        [DisplayName("Ship Shield"),
        Category("Shield Stats"),
        Description("The Shield type present on this class"),
        Browsable(true),
        ReadOnly(true)]
        public ShieldDefTN ShipShieldDef { get; set; }

        [DisplayName("Shield Count"),
        Category("Component Counts"),
        Description("Count of Shields on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public ushort ShipShieldCount { get; set; }

        [DisplayName("Total Shield Pool"),
        Category("Detials"),
        Description("Shield Strength Total for every shield on this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public float TotalShieldPool { get; set; }

        [DisplayName("Total Shield Regeneration per Tick"),
        Category("Detials"),
        Description("Total Shield Regeneration of every shield on this ship class per tick."),
        Browsable(true),
        ReadOnly(true)]
        public float TotalShieldGenPerTick { get; set; }

        [DisplayName("Total Fuel Cost Per Tick"),
        Category("Detials"),
        Description("Total fuel consumed every tick to keep the shields operating."),
        Browsable(true),
        ReadOnly(true)]
        public float TotalShieldFuelCostPerTick { get; set; }
        #endregion

        #region Missile Components
        /// <summary>
        /// Ships need magazines(though launchers have some innate magazine capacity), launch tubes, and missile fire controls to fire missiles at targets.
        /// </summary>
        [DisplayName("Ship Missile Launchers"),
        Category("Component Lists"),
        Description("List of missile launchers present on this class"),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<MissileLauncherDefTN> ShipMLaunchDef { get; set; }

        [DisplayName("Ship Missile Launcher Count"),
        Category("Component Counts"),
        Description("Count of missile launchers present on this class"),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipMLaunchCount { get; set; }

        [DisplayName("Ship Magazines"),
        Category("Component Lists"),
        Description("List of Magazines present on this class"),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<MagazineDefTN> ShipMagazineDef { get; set; }

        [DisplayName("Ship Magazine Count"),
        Category("Component Counts"),
        Description("Count of Magazines present on this class"),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipMagazineCount { get; set; }

        [DisplayName("Ship Missile Fire Controls"),
        Category("Component Lists"),
        Description("List of MFCs present on this class"),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ActiveSensorDefTN> ShipMFCDef { get; set; }

        [DisplayName("Ship MFC Count"),
        Category("Component Counts"),
        Description("Count of MFCs present on this class"),
        Browsable(true),
        ReadOnly(true)]
        public BindingList<ushort> ShipMFCCount { get; set; }

        [DisplayName("Total Magazine space"),
        Category("Detials"),
        Description("Total ordnance carrying capacity of this vessel, this is launch tubes + magazine space."),
        Browsable(true),
        ReadOnly(true)]
        public int TotalMagazineCapacity { get; set; }

        [DisplayName("Ship Class Ordnance"),
        Category("Detials"),
        Description("Preferred Ordnance loadout for this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public Dictionary<OrdnanceDefTN, int> ShipClassOrdnance { get; set; }

        [DisplayName("Preferred Ordnance size"),
        Category("Detials"),
        Description("Preferred Ordnance loadout size for this ship class."),
        Browsable(true),
        ReadOnly(true)]
        public int PreferredOrdnanceSize { get; set; }
        #endregion


        #endregion

        #region Constructor
        /// <summary>
        /// This constructor will initialize the craft class to a default conventional armored 0 space ship, with a deployment time of 3 months and a name of title.
        /// </summary>
        /// <param name="Title">Class name</param>
        public ShipClassTN(string Title, Faction ShipClassFaction)
        {
            Name = Title;
            Faction = ShipClassFaction;

            ShipsInClass = new BindingList<ShipTN>();

            /// <summary>
            /// Sanity initializations
            /// </summary>
            BuildPointCost = 0.0m;
            SizeHS = 0.0f;
            SizeTons = 0.0f;
            TotalHTK = 0;
            IsMilitary = false;
            IsTanker = false;
            IsSupply = false;
            IsCollier = false;
            IsLocked = false;
            MilitaryComponentCount = 0;
            PlanetaryProtectionValue = 0;

            ListOfComponentDefs = new BindingList<ComponentDefTN>();
            ListOfComponentDefsCount = new BindingList<short>();
            DamageAllocationChart = new Dictionary<ComponentDefTN,int>();
            ElectronicDamageAllocationChart = new Dictionary<ComponentDefTN, int>();

            CrewQuarters = new BindingList<GeneralComponentDefTN>();
            CrewQuartersCount = new BindingList<ushort>();
            TotalCrewQuarters = 0;
            TotalRequiredCrew = 0;
            SpareCrewQuarters = 0;
            SpareCryoBerths = 0;
            MaxDeploymentTime = 3;
            TonsPerMan = (float)Math.Pow((double)MaxDeploymentTime, (1.0 / 3.0));
            CapPerHS = 50.0f / TonsPerMan;
            AccomHSRequirement = 0.0f;
            AccomHSAvailable = 0.0f;

            FuelTanks = new BindingList<GeneralComponentDefTN>();
            FuelTanksCount = new BindingList<ushort>();
            TotalFuelCapacity = 0.0f;

            EngineeringBays = new BindingList<GeneralComponentDefTN>();
            EngineeringBaysCount = new BindingList<ushort>();
            TotalMSPCapacity = 0;
            EngineeringHS = 0.0f;
            MaintenanceLife = 0.0f;
            AnnualFailureRate = 0.0f; 
            InitialFailureRate = 0.0f;
            YearOneFailureTotal = 0.0f;
            YearFiveFailureTotal = 0.0f;
            MaxDamageControlRating = 0;
            MaxRepair = 0;

            OtherComponents = new BindingList<GeneralComponentDefTN>();
            OtherComponentsCount = new BindingList<ushort>();
            HasBridge = false;


            MaxFuelUsePerHour = 0.0f;
            ShipEngineCount = 0;

            MaxEnginePower = 0;
            MaxThermalSignature = 0;
            MaxSpeed = 0;

            ShipCargoDef = new BindingList<CargoDefTN>();
            ShipCargoCount = new BindingList<ushort>();
            TotalCargoCapacity = 0;
            CargoLoadTime = 0;

            ShipCHSDef = new BindingList<CargoHandlingDefTN>();
            ShipCHSCount = new BindingList<ushort>();
            TractorMultiplier = 1;

            ShipColonyDef = new BindingList<ColonyDefTN>();
            ShipColonyCount = new BindingList<ushort>();
            CryoLoadTime = 0;

            TroopLoadTime = 0;


            ShipPSensorDef = new BindingList<PassiveSensorDefTN>();
            ShipPSensorCount = new BindingList<ushort>();
            BestThermalRating = 1;
            BestEMRating = 1;

            ShipASensorDef = new BindingList<ActiveSensorDefTN>();
            ShipASensorCount = new BindingList<ushort>();
            TotalCrossSection = 0;
            MaxEMSignature = 0;

            ShipBFCDef = new BindingList<BeamFireControlDefTN>();
            ShipBFCCount = new BindingList<ushort>();
            ShipBeamDef = new BindingList<BeamDefTN>();
            ShipBeamCount = new BindingList<ushort>();

            ShipReactorDef = new BindingList<ReactorDefTN>();
            ShipReactorCount = new BindingList<ushort>();

            TotalPowerGeneration = 0;
            TotalPowerRequirement = 0;

            ShipShieldDef = null;
            ShipShieldCount = 0;
            TotalShieldPool = 0.0f;
            TotalShieldFuelCostPerTick = 0.0f;
            TotalShieldGenPerTick = 0.0f;

            ShipMLaunchDef = new BindingList<MissileLauncherDefTN>();
            ShipMLaunchCount = new BindingList<ushort>();
            ShipMagazineDef = new BindingList<MagazineDefTN>();
            ShipMagazineCount = new BindingList<ushort>();
            ShipMFCDef = new BindingList<ActiveSensorDefTN>();
            ShipMFCCount = new BindingList<ushort>();
            TotalMagazineCapacity = 0;
            ShipClassOrdnance = new Dictionary<OrdnanceDefTN, int>();
            PreferredOrdnanceSize = 0;

            ShipArmorDef = new ArmorDefTN("Conventional");
            NewArmor("Conventional", 2, 1);

            BuildPointCost = ShipArmorDef.cost;
        }
        #endregion

        /// <summary>
        /// Sets deployment time and alters crew requirement statistics.
        /// </summary>
        /// <param name="months">Months of deployment tour.</param>
        public void SetDeploymentTime(int months)
        {
            MaxDeploymentTime = months;
            TonsPerMan = (float)Math.Pow((double)MaxDeploymentTime, (1.0 / 3.0));
            CapPerHS = 50.0f / TonsPerMan;
            AccomHSRequirement = (((float)TotalRequiredCrew * TonsPerMan) / 50.0f);

            TotalCrewQuarters = (int)Math.Floor((AccomHSAvailable * 50.0f) / TonsPerMan);

            SpareCrewQuarters = TotalCrewQuarters - TotalRequiredCrew;

            BuildClassSummary();
        }

        /// <summary>
        /// General Class update function, many things will change as a result of adding components, this function handles them all.
        /// I think this function might be enough to handle subtracting components as well. Update: now subtracting.
        /// </summary>
        /// <param name="Component">Basic abstract class definition of the added component.</param>
        /// <param name="increment">The number of new components to be added.</param>
        /// <param name="isElectronic">Whether or not this component should go in the EDAC</param>
        private void UpdateClass(ComponentDefTN Component, short increment)
        {
            int CIndex = ListOfComponentDefs.IndexOf(Component);
            if (CIndex != -1)
            {
                ListOfComponentDefsCount[CIndex] = (short)(ListOfComponentDefsCount[CIndex] + increment);
            }
            
            if (CIndex == -1 && increment >= 1)
            {
                /// <summary>
                /// The damage allocation chart values will be recalculated later in this very function. A DAC of -1 is obviously indicative of an error.
                /// </summary>
                ListOfComponentDefs.Add(Component);
                ListOfComponentDefsCount.Add(increment);
                DamageAllocationChart.Add(Component, -1);

                if (Component.isElectronic == true)
                {
                    ElectronicDamageAllocationChart.Add(Component, -1);
                }
            }
            else
            {
                if (CIndex != -1)
                {
                    if (ListOfComponentDefsCount[CIndex] <= 0)
                    {
                        DamageAllocationChart.Remove(ListOfComponentDefs[CIndex]);
                        if (Component.isElectronic == true)
                        {
                            ElectronicDamageAllocationChart.Remove(ListOfComponentDefs[CIndex]);
                        }
                        ListOfComponentDefsCount.RemoveAt(CIndex);
                        ListOfComponentDefs.RemoveAt(CIndex);  
                    }
                }
                else
                {

                    /// <summary>
                    /// This is an error condition that may need to be handled at some point.
                    /// </summary>
                    return;
                }
            }


            /// <summary>
            /// Size of the craft has to be adjusted
            /// </summary>
            SizeHS = SizeHS + (Component.size * (float)increment);
            SizeTons = (float)Math.Ceiling(SizeHS) * 50.0f;

            /// <summary>
            /// The ship has a new total required crew now.
            /// </summary>
            TotalRequiredCrew = TotalRequiredCrew + (Component.crew * increment);
            AccomHSRequirement = (((float)TotalRequiredCrew * TonsPerMan) / 50.0f);

            SpareCrewQuarters = TotalCrewQuarters - TotalRequiredCrew;

            /// <summary>
            /// The Cost of the ship has likewise gone up.
            /// </summary>
            BuildPointCost = BuildPointCost + (Component.cost * increment);

            /// <summary>
            /// Update TotalHTK
            /// </summary>
            TotalHTK = TotalHTK + (Component.htk * increment);

            /// <summary>
            /// Update Military status. Does this craft suffer maintenance failures?
            /// </summary>
            if (Component.isMilitary == true)
            {
                MilitaryComponentCount = MilitaryComponentCount + increment;

                if (MilitaryComponentCount > 0)
                    IsMilitary = true;
            }

            /// <summary>
            /// Armor requires that size and cost be subtracted from the ship before recalculation/readding.
            /// </summary>
            BuildPointCost = BuildPointCost - ShipArmorDef.cost;
            SizeHS = SizeHS - ShipArmorDef.size;

            ShipArmorDef.CalcArmor(ShipArmorDef.Name, ShipArmorDef.armorPerHS, SizeHS, ShipArmorDef.depth);

            BuildPointCost = BuildPointCost + ShipArmorDef.cost;
            SizeHS = SizeHS + ShipArmorDef.size;

            /// <summary>
            /// Due to the change in size the cross section will be different.
            /// </summary>
            TotalCrossSection = (int)Math.Ceiling(SizeHS);

            /// <summary>
            /// Likewise speed shall change.
            /// </summary>
            if (TotalCrossSection != 0)
            {
                if (ShipEngineDef != null)
                {
                    MaxSpeed = (int)((1000.0f / (float)TotalCrossSection) * (float)MaxEnginePower);
                }
                else
                    MaxSpeed = 1;
            }
            else
            {
                MaxSpeed = 0;
            }

            /// <summary>
            /// MSP capacity and later maintenance will change. ***The rest of maintenance is not yet finished***.
            /// </summary>
            if (EngineeringHS == 0.0f)
            {
                TotalMSPCapacity = 0;
            }
            else
            {
                TotalMSPCapacity = (int)((float)BuildPointCost * ((EngineeringHS / SizeHS) / 0.08f)); 
            }

            /// <summary>
            /// Max repair can change.
            /// </summary>
            if (Component.cost > MaxRepair && increment >= 1)
            {
                MaxRepair = (int)Component.cost;
            }
            else
            {
                CIndex = ListOfComponentDefs.IndexOf(Component);
                if (CIndex == -1)
                {
                    decimal tempCost = 0.0m;
                    for (int loop = 0; loop < ListOfComponentDefs.Count; loop++)
                    {
                        if (ListOfComponentDefs[loop].cost > tempCost)
                        {
                            tempCost = ListOfComponentDefs[loop].cost;
                        }
                    }

                    MaxRepair = (int)tempCost;
                }
            }

            CargoLoadTime = (TotalCargoCapacity * Constants.ShipTN.BaseCargoLoadTimePerTon) / TractorMultiplier;
            CryoLoadTime = (SpareCryoBerths * Constants.ShipTN.BaseCryoLoadTimePerPerson) / TractorMultiplier;
            TroopLoadTime = (TotalTroopCapacity * Constants.ShipTN.BaseTroopLoadTime) / TractorMultiplier;


            /// <summary>
            /// This code recalculates the entire DAC and EDAC every time a component is added or subtracted from the design.
            /// </summary>
            int DAC = 0;
            int EDAC = 0;
            for (int loop = 0; loop < ListOfComponentDefs.Count; loop++)
            {
                if (ListOfComponentDefs[loop].size < 1.0)
                {
                    int localDAC = ListOfComponentDefsCount[loop];

                    DamageAllocationChart[ListOfComponentDefs[loop]] = localDAC + DAC;
                    DAC = DAC + localDAC;

                    if (ListOfComponentDefs[loop].isElectronic == true)
                    {
                        ElectronicDamageAllocationChart[ListOfComponentDefs[loop]] = localDAC + EDAC;
                        EDAC = EDAC + localDAC;
                    }
                }
                else
                {
                    int localDAC = (int)(Math.Floor(ListOfComponentDefs[loop].size)) * ListOfComponentDefsCount[loop];

                    DamageAllocationChart[ListOfComponentDefs[loop]] = localDAC + DAC;
                    DAC = DAC + localDAC;

                    if (ListOfComponentDefs[loop].isElectronic == true)
                    {
                        ElectronicDamageAllocationChart[ListOfComponentDefs[loop]] = localDAC + EDAC;
                        EDAC = EDAC + localDAC;
                    }
                }
            }

            /// <summary>
            /// Ordnance Section
            /// </summary>
            if (TotalMagazineCapacity < PreferredOrdnanceSize)
            {
                int overrun = PreferredOrdnanceSize - TotalMagazineCapacity;
                foreach (KeyValuePair<OrdnanceDefTN, int> pair in ShipClassOrdnance)
                {
                    int SCOSize = ((int)pair.Key.size * pair.Value);
                    if (SCOSize <= overrun)
                    {
                        ShipClassOrdnance.Remove(pair.Key);
                        overrun = overrun - SCOSize;
                        PreferredOrdnanceSize = PreferredOrdnanceSize - SCOSize;
                    }
                    else
                    {
                        int reduction = (int)Math.Ceiling((float)(overrun) / pair.Key.size);
                        ShipClassOrdnance[pair.Key] = ShipClassOrdnance[pair.Key] - reduction;
                        PreferredOrdnanceSize = PreferredOrdnanceSize - reduction;
                        break;
                    }
                }
            }

            BuildClassSummary();
        }

        /// <summary>
        /// NewArmor updates the ships existing armor in the specified manner:
        /// </summary>
        /// <param name="Title">New Name of armor.</param>
        /// <param name="ArmorPHS">New armor type, conventional to collapsium in int form.</param>
        /// <param name="ArmorDepth">New depth of armor coverage.</param>
        public void NewArmor(string Title, ushort ArmorPHS, ushort ArmorDepth)
        {
            /// <summary>
            /// Armor requires that size and cost be subtracted from the ship before recalculation/readding.
            /// </summary>
            BuildPointCost = BuildPointCost - ShipArmorDef.cost;
            SizeHS = SizeHS - ShipArmorDef.size; 

            ShipArmorDef.CalcArmor(Title, ArmorPHS, SizeHS, ArmorDepth);

            BuildPointCost = BuildPointCost + ShipArmorDef.cost;
            SizeHS = SizeHS + ShipArmorDef.size;

            SizeTons = SizeHS * 50.0f;

            BuildClassSummary();

        }

        /// <summary>
        /// Adds the specified crew quarter component to the ship. Subtracts now.
        /// </summary>
        /// <param name="CrewQ">General Component Crew Quarter definition.</param>
        /// <param name="inc">Number of crew quarters to add, or subtract.</param>
        public void AddCrewQuarters(GeneralComponentDefTN CrewQ, short inc)
        {
            /// <summary>
            /// Wrong type of generalComponent def sent to add crew quarters. What error should be sent?
            /// </summary>
            if (CrewQ.componentType != ComponentTypeTN.Crew)
            {
                return;
            }

            int CrewIndex = CrewQuarters.IndexOf(CrewQ);
            if (CrewIndex != -1)
            {
                CrewQuartersCount[CrewIndex] = (ushort)((short)CrewQuartersCount[CrewIndex] + inc);
            }
            
            if (CrewIndex == -1 && inc >= 1)
            {
                CrewQuarters.Add(CrewQ);
                CrewQuartersCount.Add((ushort)inc);
            }
            else
            {
                if (CrewIndex != -1)
                {
                    if (CrewQuartersCount[CrewIndex] <= 0)
                    {
                        CrewQuartersCount.RemoveAt(CrewIndex);
                        CrewQuarters.RemoveAt(CrewIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Perhaps some kind of error should be noted here.
                    /// </summary>
                    return;
                }
            }

            /// <summary>
            /// The Size of the available accomodations for crew is increased by the size of this component.
            /// </summary>
            AccomHSAvailable = AccomHSAvailable + (CrewQ.size * (float)inc);

            /// <summary>
            /// Total Crew Berths. AccomHS / Tons per man.
            /// </summary>
            TotalCrewQuarters = (int)Math.Floor((AccomHSAvailable * 50.0f) / TonsPerMan);

            /// <summary>
            /// General Housekeeping
            /// </summary>
            UpdateClass(CrewQ, inc);
        }

        /// <summary>
        /// Adds the specified Fuel Tanks to the ship. Subtracts now.
        /// </summary>
        /// <param name="FuelT">Fuel tank definition</param>
        /// <param name="inc">number of fuel tanks.</param>
        public void AddFuelStorage(GeneralComponentDefTN FuelT, short inc)
        {
            /// <summary>
            /// Wrong type of generalComponent def sent to add Fuel Storage.
            /// </summary>
            if (FuelT.componentType != ComponentTypeTN.Fuel)
            {
                return;
            }

            int FuelIndex = FuelTanks.IndexOf(FuelT);
            if (FuelIndex != -1)
            {
                FuelTanksCount[FuelIndex] = (ushort)((short)FuelTanksCount[FuelIndex] + inc);
            }
            if (FuelIndex == -1 && inc >= 1)
            {
                FuelTanks.Add(FuelT);
                FuelTanksCount.Add((ushort)inc);
            }
            else
            {
                if (FuelIndex != -1)
                {
                    if (FuelTanksCount[FuelIndex] <= 0)
                    {
                        FuelTanksCount.RemoveAt(FuelIndex);
                        FuelTanks.RemoveAt(FuelIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Perhaps some kind of error should be noted here.
                    /// </summary>
                    return;
                }
            }

            TotalFuelCapacity = TotalFuelCapacity + (FuelT.size * (float)inc * 50000.0f);

            UpdateClass(FuelT, inc);
        }

        /// <summary>
        /// Adds engineering bays to the design. subtracts now.
        /// </summary>
        /// <param name="EBay">Component definition.</param>
        /// <param name="inc">Number of components.</param>
        public void AddEngineeringSpaces(GeneralComponentDefTN EBay, short inc)
        {
            /// <summary>
            /// Wrong type of generalComponent def sent to add Engineering Spaces.
            /// </summary>
            if (EBay.componentType != ComponentTypeTN.Engineering)
            {
                return;
            }

            int EBayIndex = EngineeringBays.IndexOf(EBay);

            if (EBayIndex != -1)
            {
                EngineeringBaysCount[EBayIndex] = (ushort)((short)EngineeringBaysCount[EBayIndex] + inc);
            }

            if (EBayIndex == -1 && inc >= 1)
            {
                EngineeringBays.Add(EBay);
                EngineeringBaysCount.Add((ushort)inc);
            }
            else
            {
                if (EBayIndex != -1)
                {
                    if (EngineeringBaysCount[EBayIndex] <= 0)
                    {
                        EngineeringBaysCount.RemoveAt(EBayIndex);
                        EngineeringBays.RemoveAt(EBayIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error message?
                    /// </summary>
                    return;
                }
            }

            EngineeringHS = EngineeringHS + (EBay.size * (float)inc);

            UpdateClass(EBay, inc);
        }

        /// <summary>
        /// AddOtherComponent handles specialty general components. To Subtract add a negative number of the component.
        /// </summary>
        /// <param name="Other">The Component Definition.</param>
        /// <param name="inc">The number of components.</param>
        public void AddOtherComponent(GeneralComponentDefTN Other, short inc)
        {
            /// <summary>
            /// Wrong type of generalComponent def sent to add Other Component.
            /// </summary>
            if (Other.componentType < ComponentTypeTN.Bridge)
            {
                return;
            }

            int OtherCompIndex = OtherComponents.IndexOf(Other);
            if (OtherCompIndex != -1)
            {
                OtherComponentsCount[OtherCompIndex] = (ushort)((short)OtherComponentsCount[OtherCompIndex] + inc);
            }
            if (OtherCompIndex == -1 && inc >= 1)
            {
                OtherComponents.Add(Other);
                OtherComponentsCount.Add((ushort)inc);
            }
            else
            {
                if (OtherCompIndex != -1)
                {
                    if (OtherComponentsCount[OtherCompIndex] <= 0)
                    {
                        OtherComponentsCount.RemoveAt(OtherCompIndex);
                        OtherComponents.RemoveAt(OtherCompIndex);
                    }

                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            if (Other.componentType == ComponentTypeTN.Bridge)
            {
                HasBridge = true;

                //***Fill in the rest of this if statement for other components, and subtracting components.
            }

            UpdateClass(Other, inc);
        }

        /// <summary>
        /// Right now all this function does is overwrite the previous engine entry if switching types.
        /// I think I'll add a static variable to ComponentDefTN to deal with that. Can subtract.
        /// </summary>
        /// <param name="Engine">Engine Definition</param>
        /// <param name="inc">Number of engines to be added.</param>
        public void AddEngine(EngineDefTN Engine, short inc)
        {
            ShipEngineDef = Engine;
            ShipEngineCount = (ushort)((short)ShipEngineCount + inc);

            if (ShipEngineCount > 0)
            {

                float EP = Engine.enginePower * ShipEngineCount;
                float TS = Engine.thermalSignature * ShipEngineCount;

                MaxEnginePower = (int)Math.Round(EP);
                if (MaxEnginePower == 0)
                    MaxEnginePower = 1;

                MaxThermalSignature = (int)Math.Round(TS);
                if (MaxThermalSignature == 0)
                    MaxThermalSignature = 1;

                MaxFuelUsePerHour = MaxFuelUsePerHour + (Engine.fuelUsePerHour * (float)inc);

                UpdateClass(Engine, inc);
            }
            else
            {
                ShipEngineDef = null;
                MaxEnginePower = 1;
                MaxThermalSignature = 1;

                /// <summary>
                /// Nav thrusters don't use fuel?
                /// </summary>
                MaxFuelUsePerHour = 0.0f;

                UpdateClass(Engine, inc);
            }
        }

        /// <summary>
        /// AddCargoHold adds the specifed hold to the ship in quantity inc. will attempt to subtract if inc is negative.
        /// </summary>
        /// <param name="Cargo">Hold definition.</param>
        /// <param name="inc">Amount to add or subtract.</param>
        public void AddCargoHold(CargoDefTN Cargo, short inc)
        {
            int CargoIndex = ShipCargoDef.IndexOf(Cargo);
            if (CargoIndex != -1)
            {
                ShipCargoCount[CargoIndex] = (ushort)((short)ShipCargoCount[CargoIndex] + inc);
            }
            if (CargoIndex == -1 && inc >= 1)
            {
                ShipCargoDef.Add(Cargo);
                ShipCargoCount.Add((ushort)inc);
            }
            else
            {
                if (CargoIndex != -1)
                {
                    if (ShipCargoCount[CargoIndex] <= 0)
                    {
                        ShipCargoCount.RemoveAt(CargoIndex);
                        ShipCargoDef.RemoveAt(CargoIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            TotalCargoCapacity = TotalCargoCapacity + (Cargo.cargoCapacity * inc);
            UpdateClass(Cargo, inc);
        }

        /// <summary>
        /// AddColonyBay adds the specified cryo storage definition to the ship class definition in quantity inc, or will subtract them.
        /// </summary>
        /// <param name="Colony">Definition to add.</param>
        /// <param name="inc">Count of colony bay definitions.</param>
        public void AddColonyBay(ColonyDefTN Colony, short inc)
        {
            int ColonyIndex = ShipColonyDef.IndexOf(Colony);
            if (ColonyIndex != -1)
            {
                ShipColonyCount[ColonyIndex] = (ushort)((short)ShipColonyCount[ColonyIndex] + inc);
            }
            if (ColonyIndex == -1 && inc >= 1)
            {
                ShipColonyDef.Add(Colony);
                ShipColonyCount.Add((ushort)inc);
            }
            else
            {
                if (ColonyIndex != -1)
                {
                    if (ShipColonyCount[ColonyIndex] <= 0)
                    {
                        ShipColonyCount.RemoveAt(ColonyIndex);
                        ShipColonyDef.RemoveAt(ColonyIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }
        
            SpareCryoBerths = SpareCryoBerths + (Colony.cryoBerths * inc);
            UpdateClass(Colony, inc);
        }

        /// <summary>
        /// Add CHS adds a cargo handling tractor beam to the ship which shortens ship loading times.
        /// the function should also be able to subtract them.
        /// </summary>
        /// <param name="CHS">Definition of the CHS to add or subtract.</param>
        /// <param name="inc">number to add or to subtract.</param>
        public void AddCargoHandlingSystem(CargoHandlingDefTN CHS, short inc)
        {
            int CHSIndex = ShipCHSDef.IndexOf(CHS);
            if (CHSIndex != -1)
            {
                ShipCHSCount[CHSIndex] = (ushort)((short)ShipCHSCount[CHSIndex] + inc);
            }
            if (CHSIndex == -1 && inc >= 1)
            {
                ShipCHSDef.Add(CHS);
                ShipCHSCount.Add((ushort)inc);
            }
            else
            {
                if (CHSIndex != -1)
                {
                    if (ShipCHSCount[CHSIndex] <= 0)
                    {
                        ShipCHSCount.RemoveAt(CHSIndex);
                        ShipCHSDef.RemoveAt(CHSIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            /// <summary>
            /// Tractor multiplier is always aleast 1, but not 1 + CHS value.
            /// so either adjust upwards from 1, or if at 0 due to subtraction, reset to 1.
            /// </summary>
            if (TractorMultiplier == 1)
            {
                TractorMultiplier = (CHS.tractorMultiplier * (int)inc);
            }
            else
            {
                TractorMultiplier = TractorMultiplier + (CHS.tractorMultiplier * (int)inc);

                if (TractorMultiplier == 0)
                    TractorMultiplier = 1;
            }
            UpdateClass(CHS, inc);
        }


        /// <summary>
        /// AddPassiveSensor adds the specified sensor in quantity inc. Can subtract.
        /// </summary>
        /// <param name="Sensor">Passive sensor definition</param>
        /// <param name="inc">Number of sensors to add.</param>
        public void AddPassiveSensor(PassiveSensorDefTN Sensor, short inc)
        {
            int SensorIndex = ShipPSensorDef.IndexOf(Sensor);
            if (SensorIndex != -1)
            {
                ShipPSensorCount[SensorIndex] = (ushort)((short)ShipPSensorCount[SensorIndex] + inc);
            }
            if (SensorIndex == -1 && inc >= 1)
            {
                ShipPSensorDef.Add(Sensor);
                ShipPSensorCount.Add((ushort)inc);
            }
            else
            {
                if (SensorIndex != -1)
                {
                    if (ShipPSensorCount[SensorIndex] <= 0)
                    {
                        ShipPSensorCount.RemoveAt(SensorIndex);
                        ShipPSensorDef.RemoveAt(SensorIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            if (Sensor.thermalOrEM == PassiveSensorType.Thermal)
            {
                if (Sensor.rating > BestThermalRating)
                {
                    BestThermalRating = (int)Sensor.rating;
                }
            }
            else if (Sensor.thermalOrEM == PassiveSensorType.EM)
            {
                if (Sensor.rating > BestEMRating)
                {
                    BestEMRating = (int)Sensor.rating;
                }
            }

            UpdateClass(Sensor, inc);
        }

        /// <summary>
        /// AddActiveSensor adds the specified sensor in quantity inc. Can Subtract.
        /// </summary>
        /// <param name="Sensor">Active sensor definition</param>
        /// <param name="inc">Number of sensors to add.</param>
        public void AddActiveSensor(ActiveSensorDefTN Sensor, short inc)
        {
            int SensorIndex = ShipASensorDef.IndexOf(Sensor);
            if (SensorIndex != -1)
            {
                ShipASensorCount[SensorIndex] = (ushort)((short)ShipASensorCount[SensorIndex] + inc);
            }
            if (SensorIndex == -1 && inc >= 1)
            {
                ShipASensorDef.Add(Sensor);
                ShipASensorCount.Add((ushort)inc);
            }
            else
            {
                if (SensorIndex != -1)
                {
                    if (ShipASensorCount[SensorIndex] <= 0)
                    {
                        ShipASensorCount.RemoveAt(SensorIndex);
                        ShipASensorDef.RemoveAt(SensorIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            MaxEMSignature = MaxEMSignature + (Sensor.gps * (int)inc);

            UpdateClass(Sensor, inc);
        }

        /// <summary>
        /// Add BFC adds or subtracts the selected fire control for beam weapons to the ship class.
        /// </summary>
        /// <param name="BFC">Beam fire control</param>
        /// <param name="inc">Number to add or subtract</param>
        public void AddBeamFireControl(BeamFireControlDefTN BFC, short inc)
        {
            int BFCIndex = ShipBFCDef.IndexOf(BFC);
            if (BFCIndex != -1)
            {
                ShipBFCCount[BFCIndex] = (ushort)((short)ShipBFCCount[BFCIndex] + inc);
            }
            
            if (BFCIndex == -1 && inc >= 1)
            {
                ShipBFCDef.Add(BFC);
                ShipBFCCount.Add((ushort)inc);
            }
            else
            {
                if (BFCIndex != -1)
                {
                    if (ShipBFCCount[BFCIndex] <= 0)
                    {
                        ShipBFCCount.RemoveAt(BFCIndex);
                        ShipBFCDef.RemoveAt(BFCIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            UpdateClass(BFC, inc);
        }

        /// <summary>
        /// Add Beam weapon adds or subtracts the specified beam weapon to the ship class in increment inc.
        /// </summary>
        /// <param name="Beam">Beam weapon</param>
        /// <param name="inc">increment to add or subtract</param>
        public void AddBeamWeapon(BeamDefTN Beam, short inc)
        {
            int BeamIndex = ShipBeamDef.IndexOf(Beam);
            if (BeamIndex != -1)
            {
                ShipBeamCount[BeamIndex] = (ushort)((short)ShipBeamCount[BeamIndex] + inc);
            }
            
            if (BeamIndex == -1 && inc >= 1)
            {
                ShipBeamDef.Add(Beam);
                ShipBeamCount.Add((ushort)inc);
            }
            else
            {
                if (BeamIndex != -1)
                {
                    if (ShipBeamCount[BeamIndex] <= 0)
                    {
                        ShipBeamCount.RemoveAt(BeamIndex);
                        ShipBeamDef.RemoveAt(BeamIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            TotalPowerRequirement = TotalPowerRequirement + (int)(Beam.powerRequirement * inc);
            UpdateClass(Beam, inc);
        }

        /// <summary>
        /// AddReactor puts a powerplant design into this shipclass.
        /// </summary>
        /// <param name="Reactor">Reactor definition</param>
        /// <param name="inc"># of reactors to put in or remove.</param>
        public void AddReactor(ReactorDefTN Reactor, short inc)
        {
            int ReactorIndex = ShipReactorDef.IndexOf(Reactor);
            if (ReactorIndex != -1)
            {
                ShipReactorCount[ReactorIndex] = (ushort)((short)ShipReactorCount[ReactorIndex] + inc);
            }
            
            if (ReactorIndex == -1 && inc >= 1)
            {
                ShipReactorDef.Add(Reactor);
                ShipReactorCount.Add((ushort)inc);
            }
            else
            {
                if (ReactorIndex != -1)
                {
                    if (ShipReactorCount[ReactorIndex] <= 0)
                    {
                        ShipReactorCount.RemoveAt(ReactorIndex);
                        ShipReactorDef.RemoveAt(ReactorIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            float PowerTemp = 0.0f;
            for (int loop = 0; loop < ShipReactorDef.Count; loop++)
            {
                PowerTemp = PowerTemp + (ShipReactorDef[loop].powerGen * ShipReactorCount[loop]);
            }

            TotalPowerGeneration = (int)Math.Round(PowerTemp);
            UpdateClass(Reactor, inc);
        }

        /// <summary>
        /// AddShield puts the specified shield onto this shipclass design.
        /// </summary>
        /// <param name="Shield">Shield definition</param>
        /// <param name="inc">Amount to add or remove</param>
        public void AddShield(ShieldDefTN Shield, short inc)
        {
            bool ShieldAllowed = true;
            /// <summary>
            /// Just blow away the previous definition.
            /// </summary>
            if (ShipShieldDef == Shield)
            {
                if (ShipShieldDef.componentType != ComponentTypeTN.AbsorptionShield)
                {
                    ShipShieldCount = (ushort)((short)ShipShieldCount + inc);

                    if (ShipShieldCount != 0)
                    {
                        TotalShieldPool = TotalShieldPool + (ShipShieldDef.shieldPool * inc);
                        TotalShieldGenPerTick = TotalShieldGenPerTick + (ShipShieldDef.shieldGenPerTick * inc);
                        TotalShieldFuelCostPerTick = TotalShieldFuelCostPerTick + ((ShipShieldDef.fuelCostPerHour / 720.0f) * inc);
                    }
                    else
                    {
                        TotalShieldPool = 0.0f;
                        TotalShieldGenPerTick = 0.0f;
                        TotalShieldFuelCostPerTick = 0.0f;
                        ShipShieldDef = null;
                    }
                }
                else
                {
                    if (ShipShieldCount == 1 && inc == -1)
                    {
                        ShipShieldCount = 0;
                        TotalShieldPool = 0.0f;
                        TotalShieldGenPerTick = 0.0f;
                        TotalShieldFuelCostPerTick = 0.0f;
                        ShipShieldDef = null;
                    }
                    else
                    {
                        /// <summary>
                        /// Multiple shields of this type are not allowed.
                        /// </summary>
                    }
                }
            }
            else if (ShipShieldDef == null)
            {
                ShipShieldDef = Shield;
                ShipShieldCount = (ushort)((short)ShipShieldCount + inc);
                TotalShieldPool = TotalShieldPool + (ShipShieldDef.shieldPool * inc);
                TotalShieldGenPerTick = TotalShieldGenPerTick + (ShipShieldDef.shieldGenPerTick * inc);
                TotalShieldFuelCostPerTick = TotalShieldFuelCostPerTick + ((ShipShieldDef.fuelCostPerHour / 720.0f) * inc);
            }
            else
            {
                /// <summary>
                /// Multiple shield types are not allowed.
                /// </summary>
                ShieldAllowed = false;
            }

            if (ShieldAllowed == true)
            {
                MaxEMSignature = MaxEMSignature + (int)(Shield.shieldPool * 30.0f * (float)inc);
                UpdateClass(Shield, inc);
            }
        }

        /// <summary>
        /// Add a missile launcher to this ship class
        /// </summary>
        /// <param name="Tube">Launch tube(Can also be a pdc silo no problem.)</param>
        /// <param name="inc">number to add or subtract.</param>
        public void AddLauncher(MissileLauncherDefTN Tube, short inc)
        {
            int TubeIndex = ShipMLaunchDef.IndexOf(Tube);
            if (TubeIndex != -1)
            {
                ShipMLaunchCount[TubeIndex] = (ushort)((short)ShipMLaunchCount[TubeIndex] + inc);
            }
            
            if (TubeIndex == -1 && inc >= 1)
            {
                ShipMLaunchDef.Add(Tube);
                ShipMLaunchCount.Add((ushort)inc);
            }
            else
            {
                if (TubeIndex != -1)
                {
                    if (ShipMLaunchCount[TubeIndex] <= 0)
                    {
                        ShipMLaunchCount.RemoveAt(TubeIndex);
                        ShipMLaunchDef.RemoveAt(TubeIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            TotalMagazineCapacity = TotalMagazineCapacity + ((int)Tube.launchMaxSize * inc);
            UpdateClass(Tube, inc);
        }

        /// <summary>
        /// Add a magazine to this ship class
        /// </summary>
        /// <param name="Mag">Magazine to add</param>
        /// <param name="inc">number to add or subtract.</param>
        public void AddMagazine(MagazineDefTN Mag, short inc)
        {
            int MagIndex = ShipMagazineDef.IndexOf(Mag);
            if (MagIndex != -1)
            {
                ShipMagazineCount[MagIndex] = (ushort)((short)ShipMagazineCount[MagIndex] + inc);
            }
            
            if (MagIndex == -1 && inc >= 1)
            {
                ShipMagazineDef.Add(Mag);
                ShipMagazineCount.Add((ushort)inc);
            }
            else
            {
                if (MagIndex != -1)
                {
                    if (ShipMagazineCount[MagIndex] <= 0)
                    {
                        ShipMagazineCount.RemoveAt(MagIndex);
                        ShipMagazineDef.RemoveAt(MagIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            TotalMagazineCapacity = TotalMagazineCapacity + ((int)Mag.capacity * inc);
            UpdateClass(Mag, inc);
        }

        /// <summary>
        /// Add a Missile Fire Control to this ship class
        /// </summary>
        /// <param name="MFC">MFC to add</param>
        /// <param name="inc">number to add or subtract.</param>
        public void AddMFC(ActiveSensorDefTN MFC, short inc)
        {
            int MFCIndex = ShipMFCDef.IndexOf(MFC);
            if (MFCIndex != -1)
            {
                ShipMFCCount[MFCIndex] = (ushort)((short)ShipMFCCount[MFCIndex] + inc);
            }
            
            if (MFCIndex == -1 && inc >= 1)
            {
                ShipMFCDef.Add(MFC);
                ShipMFCCount.Add((ushort)inc);
            }
            else
            {
                if (MFCIndex != -1)
                {
                    if (ShipMFCCount[MFCIndex] <= 0)
                    {
                        ShipMFCCount.RemoveAt(MFCIndex);
                        ShipMFCDef.RemoveAt(MFCIndex);
                    }
                }
                else
                {
                    /// <summary>
                    /// Error here so return.
                    /// </summary>
                    return;
                }
            }

            UpdateClass(MFC, inc);
        }


        /// <summary>
        /// Set preferred ordnance adds or subtracts missiles from the preferred ordnance list of this class.
        /// </summary>
        /// <param name="missile">Missile to be added or subtracted.</param>
        /// <param name="inc">Amount to add/remove.</param>
        public void SetPreferredOrdnance(OrdnanceDefTN missile, int inc)
        {
            int loadAmt = (int)missile.size * inc;

            if (inc > 0)
            {
                if (PreferredOrdnanceSize + loadAmt <= TotalMagazineCapacity)
                {
                    loadAmt = inc;
                }
                else
                {
                    if (PreferredOrdnanceSize == TotalMagazineCapacity)
                    {
                        return;
                    }
                    else
                    {
                        int capRemaining = TotalMagazineCapacity - PreferredOrdnanceSize;
                        loadAmt = (int)Math.Floor(((float)capRemaining / missile.size));
                    }
                }

                if (ShipClassOrdnance.ContainsKey(missile))
                {
                    ShipClassOrdnance[missile] = ShipClassOrdnance[missile] + loadAmt;
                    PreferredOrdnanceSize = PreferredOrdnanceSize + loadAmt;
                }
                else
                {
                    ShipClassOrdnance.Add(missile, loadAmt);
                    PreferredOrdnanceSize = PreferredOrdnanceSize + loadAmt;
                }
            }
            else
            {
                if (ShipClassOrdnance.ContainsKey(missile) == false)
                {
                    return;
                }
                else
                {
                    /// <summary>
                    /// Have to remember that inc is negative here.
                    /// </summary>
                    ShipClassOrdnance[missile] = ShipClassOrdnance[missile] + inc;
                    PreferredOrdnanceSize = PreferredOrdnanceSize + loadAmt;

                    if (ShipClassOrdnance[missile] == 0)
                    {
                        ShipClassOrdnance.Remove(missile);
                    }
                    else if (ShipClassOrdnance[missile] < 0)
                    {
                        /// <summary>
                        /// This can be bad.
                        /// </summary>
                        Console.WriteLine("ShipClassOrdnance underflow.");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Builds the class summary.
        /// Geo/Grav values not handled yet. or present for that matter.
        /// Missile related components not yet handled.
        /// many other components not handled either.
        /// Need to pass desired range/tracking for bfc adjustment.
        /// </summary>
        public void BuildClassSummary()
        {
            bool control = false;
            Summary = "N/A";

            /// <summary>
            /// if (isFighter)
            ///    Tons = SizeTons;
            /// </summary>
            float Tons = (float)Math.Ceiling(SizeHS) * 50.0f;

            String Entry = String.Format("{0} class Warship   {1} tons   {2} Crew   {3} BP   TCS {4} TH {5} EM {6}\n", Name, Tons.ToString(),
                                         TotalRequiredCrew.ToString(), Math.Floor(BuildPointCost).ToString(), TotalCrossSection.ToString(),
                                         MaxThermalSignature.ToString(), MaxEMSignature.ToString());
                
            Summary = String.Format("{0}",Entry);

            String ShieldR = "0";
            if (ShipShieldDef != null)
            {
                if (ShipShieldDef.shieldGen == ShipShieldDef.shieldPool)
                {
                    ShieldR = "300";
                }
                else
                {
                    float shield = (float)Math.Floor((ShipShieldDef.shieldPool / ShipShieldDef.shieldGen) * 300.0f);
                    ShieldR = shield.ToString();
                }
            }

            Entry = String.Format("{0} km/s   Armour {1}-{2}   Shields {3}-{4}   Sensors {5}/{6}/{7}/{8}   Damage Control Rating {9}  PPV {10}\n", MaxSpeed,
                                  ShipArmorDef.depth, ShipArmorDef.cNum, TotalShieldPool, ShieldR, BestThermalRating, BestEMRating, 0, 0, MaxDamageControlRating, 
                                  PlanetaryProtectionValue);

            Summary = String.Format("{0}{1}",Summary,Entry);

            Entry = String.Format("Maint Life {0} Years   MSP {1}   AFR {2}   IFR {3}   1YR {4}   5YR {5}   Max Repair {6} MSP\n", MaintenanceLife, TotalMSPCapacity, AnnualFailureRate, 
                                  InitialFailureRate, YearOneFailureTotal, YearFiveFailureTotal, MaxRepair);

            Summary = String.Format("{0}{1}",Summary,Entry);

            Entry = String.Format("Intended Deployment Time: {0} months   Spare Berths {1}\n", MaxDeploymentTime, SpareCrewQuarters);

            Summary = String.Format("{0}{1}",Summary,Entry);

            if (ShipMagazineDef.Count != 0)
            {
                Entry = String.Format("Magazine {0}\n", TotalMagazineCapacity);
                Summary = String.Format("{0}{1}", Summary, Entry);
            }

            Summary = String.Format("{0}\n",Summary);

            if (ShipEngineDef != null)
            {
                float fuelCon = (float)Math.Floor(ShipEngineDef.fuelConsumptionMod * 100.0f);
                String FuelString = String.Format("{0}", fuelCon);

                Entry = String.Format("{0} ({1})   Power {2}   Fuel Use {3}%    Signature {4}    Armour 0    Exp {5}%\n", ShipEngineDef.Name, ShipEngineCount,
                                                                   ShipEngineDef.enginePower, FuelString, ShipEngineDef.thermalSignature, ShipEngineDef.expRisk);

                Summary = String.Format("{0}{1}",Summary,Entry);
            }

            if (TotalFuelCapacity != 0)
            {
                String Range = "Range N/A";
                if (ShipEngineDef != null)
                {
                    String Time = "N/A";
                    float HoursOfFuel = TotalFuelCapacity / MaxFuelUsePerHour;


                    if (HoursOfFuel < 72)
                    {
                        Time = String.Format("({0} hours at full power)", Math.Floor(HoursOfFuel).ToString());
                    }
                    else
                    {
                        float DaysOfFuel = HoursOfFuel / 24.0f;
                        Time = String.Format("({0} days at full power)", Math.Floor(DaysOfFuel).ToString());
                    }

                    float SecondsPerBillion = 1000000000.0f / MaxSpeed;
                    float HoursPerBillion = SecondsPerBillion / 3600.0f;

                    float billions = HoursOfFuel / HoursPerBillion;

                    if (billions >= 0.1)
                    {
                        billions = (float)(Math.Floor(10.0 * billions) / 10.0f);
                        Range = String.Format("Range {0} B km {1}", billions, Time);
                    }
                    else
                    {
                        float millions = billions * 1000.0f;
                        millions = (float)(Math.Floor(10.0 * millions) / 10.0f);
                        Range = String.Format("Range {0} M km {1}", millions, Time);
                    }
                }

                Entry = String.Format("Fuel Capacity {0} Litres   {1}\n", TotalFuelCapacity, Range);

                Summary = String.Format("{0}{1}",Summary,Entry);
            }

            if (ShipShieldDef != null)
            {
                float FuelCostPerHour = ShipShieldDef.fuelCostPerHour * ShipShieldCount;
                float FuelCostPerDay = FuelCostPerHour * 24.0f;

                Entry = String.Format("{0} ({1})   Total Fuel Cost  {2} Litres per hour  ({3} per day)\n", ShipShieldDef, ShipShieldCount,
                                                                             FuelCostPerHour, FuelCostPerDay);

                Summary = String.Format("{0}{1}",Summary,Entry);
            }

            Entry = "\n";
            Summary = String.Format("{0}{1}",Summary,Entry);

            for (int loop = 0; loop < ShipBeamDef.Count; loop++)
            {
                String Range = "N/A";

                float MaxRange = 0;

                for (int loop2 = 0; loop2 < ShipBFCDef.Count; loop2++)
                {
                    if (ShipBFCDef[loop2].range > MaxRange)
                        MaxRange = ShipBFCDef[loop2].range;
                }

                if (MaxRange > ShipBeamDef[loop].range)
                {
                    Range = String.Format("Range {0}km", ShipBeamDef[loop].range);
                }
                else
                {
                    Range = String.Format("Range {0}km", MaxRange);
                }

                String Tracking = "N/A";
                if (MaxSpeed > Faction.BaseTracking)
                {
                    Tracking = String.Format("TS: {0} km/s", MaxSpeed);
                }
                else
                {
                    Tracking = String.Format("TS: {0} km/s", Faction.BaseTracking);
                }

                String Power = "N/A";
                if (ShipBeamDef[loop].componentType == ComponentTypeTN.Gauss)
                {
                    Power = "0-0";
                }
                else
                {
                    Power = String.Format("{0}-{1}", ShipBeamDef[loop].powerRequirement, ShipBeamDef[loop].weaponCapacitor);
                }

                float ROF = (float)Math.Ceiling(ShipBeamDef[loop].powerRequirement / ShipBeamDef[loop].weaponCapacitor) * 5.0f;

                if (ROF < 5)
                    ROF = 5;
                String DamageString = ShipBeamDef[loop].damage[0].ToString();

                for (int loop2 = 1; loop2 < 10; loop2++)
                {
                    int value = -1;
                    if (loop2 >= ShipBeamDef[loop].damage.Count)
                    {
                        value = 0;
                    }
                    else
                    {
                        value = ShipBeamDef[loop].damage[loop2];
                    }
                    DamageString = String.Format("{0} {1}", DamageString, value);
                }

                Entry = String.Format("{0} ({1})   {2}   {3}   Power {4}   RM {5}   ROF {6}   {7}\n",
                                          ShipBeamDef[loop].Name, ShipBeamCount[loop], Range, Tracking, Power,
                                          (ShipBeamDef[loop].damage.Count - 1), ROF, DamageString);

                Summary = String.Format("{0}{1}",Summary,Entry);
                control = true;
            }

            for (int loop = 0; loop < ShipBFCDef.Count; loop++)
            {
                String AccString = String.Format("{0}", Math.Floor(ShipBFCDef[loop].rangeAccuracyTable[0] * 100.0f));

                for (int loop2 = 1; loop2 < 10; loop2++)
                {
                    if (loop2 < ShipBFCDef[loop].rangeAccuracyTable.Count)
                    {
                        AccString = String.Format("{0} {1}", AccString, Math.Floor(ShipBFCDef[loop].rangeAccuracyTable[loop2] * 100.0f));
                    }
                    else
                    {
                        AccString = String.Format("{0} 0", AccString);
                    }
                }


                Entry = String.Format("{0} ({1})   Max Range: {2} km   TS: {3} km/s   {4}\n", ShipBFCDef[loop].Name, ShipBFCCount[loop], ShipBFCDef[loop].range,
                                      ShipBFCDef[loop].tracking, AccString);

                Summary = String.Format("{0}{1}",Summary,Entry);

                control = true;
            }

            for (int loop = 0; loop < ShipReactorDef.Count; loop++)
            {
                float TPO = ShipReactorDef[loop].powerGen * ShipReactorCount[loop];

                /// <summary>
                /// probably a better way to format this.
                /// </summary>
                TPO = TPO * 10.0f;
                TPO = (float)Math.Round(TPO);
                TPO = TPO / 10.0f;

                Entry = String.Format("{0} ({1})   Total Power Output {2}   Armour 0    Exp {3}%\n", ShipReactorDef[loop].Name,
                                          ShipReactorCount[loop], TPO, ShipReactorDef[loop].expRisk);

                Summary = String.Format("{0}{1}",Summary,Entry);

                control = true;
            }

            if (control == true)
            {
                Entry = "\n";
                Summary = String.Format("{0}{1}", Summary, Entry);
            }

            control = false;

            for (int loop = 0; loop < ShipMLaunchDef.Count; loop++)
            {
                Entry = String.Format("{0} ({1})    Missile Size {2}    Rate of Fire {3}\n",ShipMLaunchDef[loop].Name,ShipMLaunchCount[loop],ShipMLaunchDef[loop].launchMaxSize,
                                                                                          ShipMLaunchDef[loop].rateOfFire);
                Summary = String.Format("{0}{1}",Summary,Entry);
                control = true;
            }

            for (int loop = 0; loop < ShipMFCDef.Count; loop++)
            {
                String RangeString = "-4.2m";

                if (ShipMFCDef[loop].maxRange >= 100000)
                {
                    float RangeB = (float)Math.Floor((double)ShipMFCDef[loop].maxRange / 10000.0) / 10.0f;

                    RangeString = String.Format("{0}B", RangeB);
                }
                else if (ShipMFCDef[loop].maxRange >= 100)
                {
                    float RangeM = (float)Math.Floor((double)ShipMFCDef[loop].maxRange / 10.0) / 10.0f;

                    RangeString = String.Format("{0}M", RangeM);
                }
                else
                {
                    RangeString = String.Format("{0}K", ((float)Math.Floor((double)ShipMFCDef[loop].maxRange) * 10.0f));
                }

                String MCRString = " ";

                if (ShipMFCDef[loop].resolution == 1)
                {
                    int minRange = ShipMFCDef[loop].lookUpMT[0];

                    if (minRange >= 100000)
                    {
                        float RangeB = (float)Math.Floor((double)minRange / 10000.0) / 10.0f;
                        MCRString = String.Format(" MCR {0}B km   ", RangeB);
                    }
                    else if (minRange >= 100)
                    {
                        float RangeM = (float)Math.Floor((double)minRange / 10.0) / 10.0f;
                        MCRString = String.Format(" MCR {0}M km   ", RangeM);
                    }
                    else
                    {
                        MCRString = String.Format(" MCR {0}K km   ", ((float)Math.Floor((double)minRange) * 10.0f));
                    }
                }

                Entry = String.Format("{0} ({1})     Range {2} km  {3}Resolution {4}\n", ShipMFCDef[loop].Name, ShipMFCCount[loop], RangeString, MCRString, ShipMFCDef[loop].resolution);
                Summary = String.Format("{0}{1}", Summary, Entry);
                control = true;
            }

            foreach (KeyValuePair<OrdnanceDefTN, int> pair in ShipClassOrdnance)
            {
                Entry = String.Format("{0} ({1})",pair.Key.Name,pair.Value);
                control = true;
            }

            if (control == true)
            {
                Entry = "\n";
                Summary = String.Format("{0}{1}", Summary, Entry);
            }

            control = false;

            for (int loop = 0; loop < ShipASensorDef.Count; loop++)
            {
                String RangeString = "-4.2m";

                if (ShipASensorDef[loop].maxRange >= 100000)
                {
                    float RangeB = (float)Math.Floor((double)ShipASensorDef[loop].maxRange / 10000.0) / 10.0f;

                    RangeString = String.Format("{0}B", RangeB);
                }
                else if (ShipASensorDef[loop].maxRange >= 100)
                {
                    float RangeM = (float)Math.Floor((double)ShipASensorDef[loop].maxRange / 10.0) / 10.0f;

                    RangeString = String.Format("{0}M", RangeM);
                }
                else
                {
                    RangeString = String.Format("{0}K", ((float)Math.Floor((double)ShipASensorDef[loop].maxRange) * 10.0f));
                }

                String MCRString = " ";

                if (ShipASensorDef[loop].resolution == 1)
                {
                    int minRange = ShipASensorDef[loop].lookUpMT[0];

                    if (minRange >= 100000)
                    {
                        float RangeB = (float)Math.Floor((double)minRange / 10000.0) / 10.0f;
                        MCRString = String.Format(" MCR {0}B km   ", RangeB);
                    }
                    else if (minRange >= 100)
                    {
                        float RangeM = (float)Math.Floor((double)minRange / 10.0) / 10.0f;
                        MCRString = String.Format(" MCR {0}M km   ", RangeM);
                    }
                    else
                    {
                        MCRString = String.Format(" MCR {0}K km   ", ((float)Math.Floor((double)minRange) * 10.0f));
                    }
                }


                Entry = String.Format("{0} ({1})   GPS {2}   Range {3} km  {4}Resolution {5}\n", ShipASensorDef[loop].Name,
                                          ShipASensorCount[loop], ShipASensorDef[loop].gps, RangeString, MCRString,
                                          ShipASensorDef[loop].resolution);

                Summary = String.Format("{0}{1}",Summary,Entry);

                control = true;
            }

            for (int loop = 0; loop < ShipPSensorDef.Count; loop++)
            {
                String RangeString = "20m";

                int range = ShipPSensorDef[loop].range;

                if (range >= 100000)
                {
                    float RangeB = (float)Math.Floor((double)range / 10000.0) / 10.0f;
                    RangeString = String.Format("{0}B", RangeB);
                }
                else
                {
                    float RangeM = (float)Math.Floor((double)range / 10.0) / 10.0f;
                    RangeString = String.Format("{0}M", RangeM);
                }

                Entry = String.Format("{0} ({1})     Sensitivity {2}     Detect Sig Strength 1000:  {3} km\n", ShipPSensorDef[loop].Name,
                                          ShipPSensorCount[loop], ShipPSensorDef[loop].rating, RangeString);

                Summary = String.Format("{0}{1}",Summary,Entry);

                control = true;
            }

            if (control == true)
            {
                Entry = "\n";
                Summary = String.Format("{0}{1}", Summary, Entry);
            }

            if (IsMilitary == true)
            {
                Entry = "This design is classed as a Military Vessel for maintenance purposes\n";
            }
            else
            {
                Entry = "This design is classed as a Commercial Vessel for maintenance purposes\n";
            }

            Summary = String.Format("{0}{1}",Summary,Entry);
        }
    }
}
