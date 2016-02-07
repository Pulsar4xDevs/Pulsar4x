using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;
using Pulsar4X.Helpers.GameMath;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif


namespace Pulsar4X.Entities
{
    public class ShipTN : StarSystemEntity
    {
        /// <summary>
        /// Ships may be standard crewed vessels, organic lifeforms, or completely machine run solid state vessels.
        /// </summary>
        public enum ShipType
        {
            Standard,
            Organic,
            SolidState,
            Count
        }

#if LOG4NET_ENABLED
        /// <summary>
        /// The logger for this class
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(ShipTN));
#endif


        /// <summary>
        /// Class of this ship.
        /// </summary>
        public ShipClassTN ShipClass { get; set; }


        /// <summary>
        /// What type of ship is this?
        /// </summary>
        public ShipType TypeOf { get; set; }

        /// <summary>
        /// Taskgroup the ship is part of.
        /// </summary>
        public TaskGroupTN ShipsTaskGroup { get; set; }

        /// <summary>
        /// Faction this Ship belongs to.
        /// </summary>
        public Faction ShipsFaction { get; set; }

        /// <summary>
        /// Unused at the moment.
        /// </summary>
        public string ClassNotes { get; set; }
        public string Notes { get; set; }

        public int MaxLifeSupport { get; set; } //here is life support again.

        /// <summary>
        /// Does this ship have a commander assigned?
        /// </summary>
        public bool ShipCommanded { get; set; }

        /// <summary>
        /// Who is the commander of the ship.
        /// </summary>
        public Commander ShipCommander { get; set; }


        /// <summary>
        /// The ship will have an armor layering.
        /// </summary>
        public ArmorTN ShipArmor { get; set; }

        /// <summary>
        /// Crew Quarters, Small Crew Quarters, Tiny Crew Quarters.
        /// </summary>
        public BindingList<GeneralComponentTN> CrewQuarters { get; set; }

        /// <summary>
        /// How many crew are on the ship. Crew aren't necessarily in their quarters when those are destroyed, though the crew requirement for quarters will be at risk.
        /// </summary>
        public int CurrentCrew { get; set; }

        /// <summary>
        /// How many additional berths this vessel has. negative numbers indicate overloaded crew quarters and chance of environmental failures.
        /// </summary>
        public int SpareBerths { get; set; }

        /// <summary>
        /// How many crew/POWs are in cryo stasis.
        /// </summary>
        public int CurrentCryoStorage { get; set; }

        /// <summary>
        /// How long has the ship been out on patrol. 1.0 = Max deployment time.
        /// </summary>
        public float CurrentDeployment { get; set; }

        /// <summary>
        /// What is the ship's morale, this is Current Deployment / Max Deployment but in steps from 100% to 25%.
        /// </summary>
        public float Morale { get; set; }

        /// <summary>
        /// Modifier to tohit, certain things increase.
        /// </summary>
        public float ShipGrade { get; set; }

        /// <summary>
        /// Response time to orders. semi random, up to 5 Minutes I believe.
        /// </summary>
        public float TFTraining { get; set; }

        /// <summary>
        /// Fuel Tanks, Small Fuel Tanks, Tiny Fuel Tanks, Large Fuel Tanks, Very Large Fuel Tanks, Ultra Large Fuel Tanks.
        /// </summary>
        public BindingList<GeneralComponentTN> FuelTanks { get; set; }

        /// <summary>
        /// How much fuel in total does the ship have. it will be divided equally among all tanks for convenience.
        /// </summary>
        public float CurrentFuel { get; set; }

        /// <summary>
        /// How much fuel can the ship carry?
        /// </summary>
        public float CurrentFuelCapacity { get; set; }

        /// <summary>
        /// Ship statistics track fuel use by the hour, but I need some way of determining if an engine has been running that long, hence this.
        /// </summary>
        public int FuelCounter { get; set; }

        /// <summary>
        /// Engineering bay, Small Engineering Bay, Tiny Engineering Bay, Fighter Engineering Bay.
        /// </summary>
        public BindingList<GeneralComponentTN> EngineeringBays { get; set; }

        /// <summary>
        /// How much maintenance supply is available to this ship, as with fuel it is divided equally among all bays.
        /// </summary>
        public int CurrentMSP { get; set; }

        /// <summary>
        /// How much Maintenance supply can be carried by this ship?
        /// </summary>
        public int CurrentMSPCapacity { get; set; }

        /// <summary>
        /// How long has this ship been away from a maintenance facility.
        /// </summary>
        public float MaintenanceClock { get; set; }

        /// <summary>
        /// Bridge, Flag Bridge, Damage Control, Improved Damage Control, Advanced Damage Control, Maintenance bay, Recreational Facility, Orbital Habitat.
        /// </summary>
        public BindingList<GeneralComponentTN> OtherComponents { get; set; }

        /// <summary>
        /// What is this ship's current damage control rating. Ebays and damage control both contribute to this value.
        /// </summary>
        public int CurrentDamageControlRating { get; set; }

        /// <summary>
        /// Ships can potentially have multiple engines, though they must all be of the same type.
        /// </summary>
        public BindingList<EngineTN> ShipEngine { get; set; }

        /// <summary>
        /// Engine related ship statistics:
        /// Engine Power: Current and current max(for damage purposes) power with which the engine can push the ship forward.
        /// ThermalSignature: Current and Current max signature the ship generates when operating its engines.
        /// Speed: Speed the ship is travelling at, and current max attainable speed.
        /// FuelusePerHour: Litres of fuel the engine consumes per hour, along with the maximum amount that can be consumed.
        /// </summary>
        public int CurrentEnginePower { get; set; }
        public int CurrentMaxEnginePower { get; set; }
        public int CurrentThermalSignature { get; set; }
        public int CurrentMaxThermalSignature { get; set; }
        public int CurrentSpeed { get; set; }
        public int CurrentMaxSpeed { get; set; }
        public float CurrentFuelUsePerHour { get; set; }
        public float CurrentMaxFuelUsePerHour { get; set; }

        /// <summary>
        /// Ships can have several types of cargo holds and multiple of each.
        /// </summary>
        public BindingList<CargoTN> ShipCargo { get; set; }

        /// <summary>
        /// Just how much is this specific ship holding.
        /// </summary>
        public int CurrentCargoTonnage { get; set; }

        /// <summary>
        /// List of installations in the cargo hold.
        /// </summary>
        public Dictionary<Installation.InstallationType, CargoListEntryTN> CargoList { get; set; }

        /// <summary>
        /// List of all components in the cargo holds.
        /// </summary>
        public Dictionary<ComponentDefTN, CargoListEntryTN> CargoComponentList { get; set; }

        /// <summary>
        /// Ships can also have several cryo storage bays and bay types.
        /// </summary>
        public BindingList<ColonyTN> ShipColony { get; set; }

        /// <summary>
        /// Ships with any kind of load capability will have a cargo handling system, or more.
        /// </summary>
        public BindingList<CargoHandlingTN> ShipCHS { get; set; }

        /// <summary>
        /// Ships with CHS systems have a tractor multiplier that reduces load time for cargo, but these can be damaged.
        /// </summary>
        public int CurrentTractorMultiplier { get; set; }

        /// <summary>
        /// List of ship components for DAC/OnDamage/Wreck functionality
        /// </summary>
        public BindingList<ComponentTN> ShipComponents { get; set; }

        /// <summary>
        /// List of FCs for the UI.
        /// </summary>
        public BindingList<ComponentTN> ShipFireControls { get; set; }

        /// <summary>
        /// In shipclass is a member ListOfComponentDefs. This will store the starting index of each of these in ShipComponents.
        /// </summary>
        public BindingList<ushort> ComponentDefIndex { get; set; }

        /// <summary>
        /// List of destroyed component indexes for damage control.
        /// </summary>
        public BindingList<ushort> DestroyedComponents { get; set; }

        /// <summary>
        /// Type of component represented by DestroyedComponents, which indexes the general component list, rather than the specific ones.
        /// </summary>
        public BindingList<ComponentTypeTN> DestroyedComponentsType { get; set; }

        /// <summary>
        /// Remaining hit to kill of the ship.
        /// </summary>
        public int ShipHTK { get; set; }

        /// <summary>
        /// Component currently being repaired by damage control.
        /// </summary>
        public short DamageControlTarget { get; set; }

        /// <summary>
        /// List of component indexes to be repaired by damage control in the order specified by player. 0 first, count last.
        /// </summary>
        public BindingList<ushort> DamageControlQue { get; set; }

        /// <summary>
        /// List of passive sensors that this craft will have.
        /// every ship has a base sensitivity 1 thermal and EM sensor, those won't be in this list however.
        /// Best ratings store the best currently working sensor detection, these are where that default will be.
        /// </summary>
        public BindingList<PassiveSensorTN> ShipPSensor { get; set; }
        public int BestThermalRating { get; set; }
        public int BestEMRating { get; set; }

        /// <summary>
        /// List of the actual active sensors, which store whether or not they are active, and if they are destroyed.
        /// </summary>
        public BindingList<ActiveSensorTN> ShipASensor { get; set; }
        public int TotalCrossSection { get; set; }
        public int CurrentEMSignature { get; set; }

        /// <summary>
        /// These lists will store timestamps for whenever this ship is detected. Example:
        /// Faction 0 detects this craft via thermal on tick 102, so ThermalDetection[0] = 102.
        /// On tick 103, the craft is still detected, so ThermalDetection[0] is updated to 103.
        /// on 104, the ship is no longer detected so no update is made.
        /// What this all means is that on any given tick it is possible to quickly determine whether or not a ship has been detected by a faction.
        /// I am thinking that ticks will be counted in 5 second intervals, there should not be any issue with this for my code.
        /// </summary>
        public BindingList<int> ThermalDetection { get; set; }
        public BindingList<int> EMDetection { get; set; }
        public BindingList<int> ActiveDetection { get; set; }

        /// <summary>
        /// change to how tick works means that year must also be recorded.
        /// </summary>
        private BindingList<int> ThermalYearDetection { get; set; }
        private BindingList<int> EMYearDetection { get; set; }
        private BindingList<int> ActiveYearDetection { get; set; }

        /// <summary>
        /// Each ship will store its placement in the overall taskgroup.
        /// </summary>
        public LinkedListNode<int> ThermalList;
        public LinkedListNode<int> EMList;
        public LinkedListNode<int> ActiveList;

        /// <summary>
        /// Any ship with a beam weapon will need atleast one  fire control for that weapon.
        /// </summary>
        public BindingList<BeamFireControlTN> ShipBFC { get; set; }

        /// <summary>
        /// Certain military warships will have beam weapons.
        /// </summary>
        public BindingList<BeamTN> ShipBeam { get; set; }


        /// <summary>
        /// All beam weapon ships excepting gauss equipped vessels will require reactors.
        /// </summary>
        public BindingList<ReactorTN> ShipReactor { get; set; }

        /// <summary>
        /// Total power generation of all shipboard reactors.
        /// </summary>
        public int CurrentPowerGen { get; set; }

        #region Shield Info
        /// <summary>
        /// All shields on this ship.
        /// </summary>
        public BindingList<ShieldTN> ShipShield { get; set; }

        /// <summary>
        /// Current shield strength value.
        /// </summary>
        public float CurrentShieldPool { get; set; }

        /// <summary>
        /// Current maximum shield strength value.
        /// </summary>
        public float CurrentShieldPoolMax { get; set; }

        /// <summary>
        /// Current shield regeneration per tick(5 seconds).
        /// </summary>
        public float CurrentShieldGen { get; set; }

        /// <summary>
        /// Current Shield fuel use per tick(5 seconds).
        /// </summary>
        public float CurrentShieldFuelUse { get; set; }

        /// <summary>
        /// Are the ships shields active?
        /// </summary>
        public bool ShieldIsActive { get; set; }
        #endregion

        #region Missile Info
        /// <summary>
        /// Missile Launchers on this ship.
        /// </summary>
        public BindingList<MissileLauncherTN> ShipMLaunchers { get; set; }

        /// <summary>
        /// Magazines on this ship.
        /// </summary>
        public BindingList<MagazineTN> ShipMagazines { get; set; }

        /// <summary>
        /// Missile fire controls on this ship.
        /// </summary>
        public BindingList<MissileFireControlTN> ShipMFC { get; set; }

        /// <summary>
        /// Missile types this ship is carrying.
        /// </summary>
        public Dictionary<OrdnanceDefTN, int> ShipOrdnance { get; set; }

        /// <summary>
        /// Ordnance currently on this ship.
        /// </summary>
        public int CurrentMagazineCapacity { get; set; }

        /// <summary>
        /// Current Max ordnance carrying capacity of this Ship.
        /// </summary>
        public int CurrentMagazineCapacityMax { get; set; }

        /// <summary>
        /// Breakdown of current magazine capacity due to launch tubes
        /// </summary>
        public int CurrentLauncherMagCapacityMax { get; set; }

        /// <summary>
        /// All current magazine capacity due to magazines.
        /// </summary>
        public int CurrentMagazineMagCapacityMax { get; set; }
        #endregion

        #region CIWS and Turrets
        /// <summary>
        /// Close in weapon systems on this ship.
        /// </summary>
        public BindingList<CIWSTN> ShipCIWS { get; set; }

        /// <summary>
        /// For point defense purposes, which CIWS is currently to be tested against?
        /// </summary>
        public int ShipCIWSIndex { get; set; }

        /// <summary>
        /// Turrets on this ship.
        /// </summary>
        public BindingList<TurretTN> ShipTurret { get; set; }
        #endregion

        /// <summary>
        /// All of the jump engines on this ship.
        /// </summary>
        public BindingList<JumpEngineTN> ShipJumpEngine { get; set; }

        /// <summary>
        /// If this ship has just transitted then it will be unable to use its sensors or weapons for a period of time.
        /// </summary>
        public int JumpSickness { get; set; }

        #region Survey Sensor Info
        /// <summary>
        /// All of the survey sensors on this ship.
        /// </summary>
        public BindingList<SurveySensorTN> ShipSurvey { get; set; }

        /// <summary>
        /// What is the current ability of this ship to perform geosurveys?
        /// </summary>
        public float CurrentGeoSurveyStrength { get; set; }

        /// <summary>
        /// What is the current ability of this ship to perform gravsurveys?
        /// </summary>
        public float CurrentGravSurveyStrength { get; set; }
        #endregion

        /// <summary>
        /// If this ship has been destroyed. this will need more sophisticated handling.
        /// </summary>
        public bool IsDestroyed { get; set; }

        /// <summary>
        /// List of ships targeted on this vessel. On ship destruction this is needed for cleanup.
        /// </summary>
        public BindingList<ShipTN> ShipsTargetting { get; set; }

        /// <summary>
        /// Taskgroups with orders to this ship. On ship destruction this is needed for cleanup.
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroupsOrdered { get; set; }


        /// <summary>
        /// ShipTN creates a ship of classDefinition in Index ShipIndex for the taskgroup ship list.
        /// </summary>
        /// <param name="ClassDefinition">Definition of the ship.</param>
        /// <param name="ShipIndex">Its index within the shiplist of the taskgroup.</param>
        /// <param name="CurrentTimeSlice">tick when this ship is created.</param>
        /// <param name="ShipTG">TG this ship belongs to.</param>
        /// <param name="ShipFact">Faction this ship belongs to.</param>
        /// <param name="Title">Name of the ship.</param>
        public ShipTN(ShipClassTN ClassDefinition, int ShipIndex, int CurrentTimeSlice, TaskGroupTN ShipTG, Faction ShipFact, String Title)
        {
            int index;
            Name = Title;

            /// <summary>
            /// create these or else anything that relies on a unique global id will break.
            /// </summary>
            Id = Guid.NewGuid();

            /// <summary>
            /// Set the class definition
            /// </summary>
            ShipClass = ClassDefinition;

            /// <summary>
            /// Inform the class that it has a new member.
            /// </summary>
            ClassDefinition.ShipsInClass.Add(this);

            /// <summary>
            /// Ships are standard crewed vessels for now.
            /// </summary>
            TypeOf = ShipType.Standard;

            /// <summary>
            /// Tell the Ship which TG it is a part of.
            /// </summary>
            ShipsTaskGroup = ShipTG;

            /// <summary>
            /// Likewise for Faction.
            /// </summary>
            ShipsFaction = ShipFact;

            /// <summary>
            /// Add this ship to the overall faction ship list.
            /// </summary>
            ShipsFaction.Ships.Add(this);

            /// <summary>
            /// Make sure to initialize this important variable that everything uses.
            /// </summary>
            ShipComponents = new BindingList<ComponentTN>();

            /// <summary>
            /// Initialize the list of Ship fire controls.
            /// </summary>
            ShipFireControls = new BindingList<ComponentTN>();

            /// <summary>
            /// Likewise the ListOfComponentDefs counterpart here is important.
            /// </summary>
            ComponentDefIndex = new BindingList<ushort>();
            for (int loop = 0; loop < ClassDefinition.ListOfComponentDefs.Count; loop++)
            {
                ComponentDefIndex.Add(0);
            }

            /// <summary>
            /// List of components that have been destroyed.
            /// </summary>
            DestroyedComponents = new BindingList<ushort>();

            DestroyedComponentsType = new BindingList<ComponentTypeTN>();

            /// <summary>
            /// How much damage can the ship take?
            /// </summary>
            ShipHTK = ShipClass.TotalHTK;

            /// <summary>
            /// When the destroyed components list is populated it can be selected from to put components here to be repaired.
            /// </summary>
            DamageControlQue = new BindingList<ushort>();

            /// <summary>
            /// Not yet set.
            /// </summary>
            DamageControlTarget = -1;


            /// <summary>
            /// All ships will have armor, and all ship defs should have armor before this point.
            /// </summary.
            ShipArmor = new ArmorTN(ClassDefinition.ShipArmorDef);

            /// <summary>
            /// Crew Quarters don't strictly have to be present, but will be in almost all designs.
            /// </summary>
            CrewQuarters = new BindingList<GeneralComponentTN>();
            AddComponents(CrewQuarters, ClassDefinition.CrewQuarters, ClassDefinition.CrewQuartersCount);

            /// <summary>
            /// Subtract crew from crew pool if ship is not conscript staffed Here:
            /// </summary>

            CurrentCrew = 0;
            SpareBerths = ShipClass.TotalCrewQuarters;
            CurrentCryoStorage = 0;
            CurrentDeployment = 0.0f;
            Morale = 100.0f;
            ShipGrade = 0.0f;
            TFTraining = 0.0f;

            /// <summary>
            ///Fuel Tanks don't have to be present, but will be in most designs.
            /// </summary>
            FuelTanks = new BindingList<GeneralComponentTN>();
            AddComponents(FuelTanks, ClassDefinition.FuelTanks, ClassDefinition.FuelTanksCount);
            CurrentFuel = 0.0f;
            CurrentFuelCapacity = ShipClass.TotalFuelCapacity;
            FuelCounter = 0;

            /// <summary>
            /// Engineering spaces must be on civ designs(atleast 1), but can be absent from military designs.
            /// </summary>
            EngineeringBays = new BindingList<GeneralComponentTN>();
            AddComponents(EngineeringBays, ClassDefinition.EngineeringBays, ClassDefinition.EngineeringBaysCount);
            CurrentDamageControlRating = ClassDefinition.MaxDamageControlRating;
            CurrentMSP = ShipClass.TotalMSPCapacity;
            CurrentMSPCapacity = ShipClass.TotalMSPCapacity;
            MaintenanceClock = 0.0f;

            /// <summary>
            /// All remaining components that are of a more specialized nature. These do not have to be present, except bridges on ships bigger than 1K tons.
            /// </summary>
            OtherComponents = new BindingList<GeneralComponentTN>();
            AddComponents(OtherComponents, ClassDefinition.OtherComponents, ClassDefinition.OtherComponentsCount);

            /// <summary>
            /// All mobile ships need engines, orbitals and PDCs don't however.
            /// </summary>
            ShipEngine = new BindingList<EngineTN>();
            index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipEngineDef);
            if (index != -1)
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;

            for (int loop = 0; loop < ClassDefinition.ShipEngineCount; loop++)
            {
                EngineTN Engine = new EngineTN(ClassDefinition.ShipEngineDef);
                Engine.componentIndex = ShipEngine.Count;
                ShipEngine.Add(Engine);
                ShipComponents.Add(Engine);
            }
            CurrentEnginePower = ClassDefinition.MaxEnginePower;
            CurrentMaxEnginePower = CurrentEnginePower;
            CurrentThermalSignature = ClassDefinition.MaxThermalSignature;
            CurrentMaxThermalSignature = CurrentThermalSignature;
            CurrentSpeed = ClassDefinition.MaxSpeed;
            CurrentMaxSpeed = CurrentSpeed;
            CurrentFuelUsePerHour = ClassDefinition.MaxFuelUsePerHour;
            CurrentMaxFuelUsePerHour = CurrentFuelUsePerHour;


            /// <summary>
            /// Usually only cargo ships and salvagers will have cargo holds.
            /// </summary>
            ShipCargo = new BindingList<CargoTN>();
            for (int loop = 0; loop < ClassDefinition.ShipCargoDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipCargoDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipCargoCount[loop]; loop2++)
                {
                    CargoTN cargo = new CargoTN(ClassDefinition.ShipCargoDef[loop]);
                    cargo.componentIndex = ShipCargo.Count;
                    ShipCargo.Add(cargo);
                    ShipComponents.Add(cargo);
                }
            }
            CurrentCargoTonnage = 0;
            CargoList = new Dictionary<Installation.InstallationType, CargoListEntryTN>();
            CargoComponentList = new Dictionary<ComponentDefTN, CargoListEntryTN>();

            /// <summary>
            /// While only colonyships will have the major bays, just about any craft can have an emergency cryo bay.
            /// </summary>
            ShipColony = new BindingList<ColonyTN>();
            for (int loop = 0; loop < ClassDefinition.ShipColonyDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipColonyDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipColonyCount[loop]; loop2++)
                {
                    ColonyTN colony = new ColonyTN(ClassDefinition.ShipColonyDef[loop]);
                    colony.componentIndex = ShipColony.Count;
                    ShipColony.Add(colony);
                    ShipComponents.Add(colony);
                }
            }
            CurrentCryoStorage = 0;

            /// <summary>
            /// Any ship with cargo holds, troop bays, cryo berths, or drop pods will benefit from a cargohandling system. though droppods benefit from the CHSes on other vessels as well.
            /// </summary>
            ShipCHS = new BindingList<CargoHandlingTN>();
            for (int loop = 0; loop < ClassDefinition.ShipCHSDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipCHSDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipCHSCount[loop]; loop2++)
                {
                    CargoHandlingTN CHS = new CargoHandlingTN(ClassDefinition.ShipCHSDef[loop]);
                    CHS.componentIndex = ShipCHS.Count;
                    ShipCHS.Add(CHS);
                    ShipComponents.Add(CHS);
                }
            }
            CurrentTractorMultiplier = ShipClass.TractorMultiplier;

            /// <summary>
            /// Every ship will have a passive sensor rating, but very few will have specialized passive sensors.
            /// </summary>
            ShipPSensor = new BindingList<PassiveSensorTN>();
            for (int loop = 0; loop < ClassDefinition.ShipPSensorDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipPSensorDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipPSensorCount[loop]; loop2++)
                {
                    PassiveSensorTN PSensor = new PassiveSensorTN(ClassDefinition.ShipPSensorDef[loop]);
                    PSensor.componentIndex = ShipPSensor.Count;
                    ShipPSensor.Add(PSensor);
                    ShipComponents.Add(PSensor);
                }
            }

            /// <summary>
            /// These two can and will change if the ship takes damage to its sensors.
            /// </summary>
            BestThermalRating = ClassDefinition.BestThermalRating;
            BestEMRating = ClassDefinition.BestEMRating;

            /// <summary>
            /// Active Sensors will be probably rarer than passive sensors, as they betray their location to any listener in range.
            /// And the listener may be far enough away that the active will not ping him.
            /// </summary>
            ShipASensor = new BindingList<ActiveSensorTN>();
            for (int loop = 0; loop < ClassDefinition.ShipASensorDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipASensorDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipASensorCount[loop]; loop2++)
                {
                    ActiveSensorTN ASensor = new ActiveSensorTN(ClassDefinition.ShipASensorDef[loop]);
                    ASensor.componentIndex = ShipASensor.Count;

                    int ASIndex = loop2 + 1;
                    ASensor.Name = ASensor.aSensorDef.Name + " #" + ASIndex.ToString();

                    ShipASensor.Add(ASensor);
                    ShipComponents.Add(ASensor);
                }
            }

            /// <summary>
            /// This won't change, but it should be here for convenience during sensor sweeps.
            /// </summary>
            TotalCrossSection = ClassDefinition.TotalCrossSection;
            CurrentEMSignature = 0;

            /// <summary>
            /// Detection Statistics initialization:
            /// </summary>
            ThermalList = new LinkedListNode<int>(ShipIndex);
            EMList = new LinkedListNode<int>(ShipIndex);
            ActiveList = new LinkedListNode<int>(ShipIndex);

            ThermalDetection = new BindingList<int>();
            EMDetection = new BindingList<int>();
            ActiveDetection = new BindingList<int>();

            ThermalYearDetection = new BindingList<int>();
            EMYearDetection = new BindingList<int>();
            ActiveYearDetection = new BindingList<int>();

            for (int loop = 0; loop < Constants.Faction.FactionMax; loop++)
            {
                ThermalDetection.Add(CurrentTimeSlice);
                EMDetection.Add(CurrentTimeSlice);
                ActiveDetection.Add(CurrentTimeSlice);
                ThermalYearDetection.Add(GameState.Instance.CurrentYear);
                EMYearDetection.Add(GameState.Instance.CurrentYear);
                ActiveYearDetection.Add(GameState.Instance.CurrentYear);
            }

            ShipCommanded = false;

            ShipBFC = new BindingList<BeamFireControlTN>();
            for (int loop = 0; loop < ClassDefinition.ShipBFCDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipBFCDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipBFCCount[loop]; loop2++)
                {
                    BeamFireControlTN BFC = new BeamFireControlTN(ClassDefinition.ShipBFCDef[loop]);
                    BFC.componentIndex = ShipBFC.Count;

                    int BFCIndex = loop2 + 1;
                    BFC.Name = BFC.beamFireControlDef.Name + " #" + BFCIndex.ToString();

                    ShipBFC.Add(BFC);
                    ShipComponents.Add(BFC);
                    ShipFireControls.Add(BFC);
                }
            }

            ShipBeam = new BindingList<BeamTN>();
            for (int loop = 0; loop < ClassDefinition.ShipBeamDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipBeamDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipBeamCount[loop]; loop2++)
                {
                    BeamTN Beam = new BeamTN(ClassDefinition.ShipBeamDef[loop]);
                    Beam.componentIndex = ShipBeam.Count;

                    int BeamIndex = loop2 + 1;
                    Beam.Name = Beam.beamDef.Name + " #" + BeamIndex.ToString();

                    ShipBeam.Add(Beam);
                    ShipComponents.Add(Beam);
                }
            }

            ShipReactor = new BindingList<ReactorTN>();
            for (int loop = 0; loop < ClassDefinition.ShipReactorDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipReactorDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipReactorCount[loop]; loop2++)
                {
                    ReactorTN Reactor = new ReactorTN(ClassDefinition.ShipReactorDef[loop]);
                    Reactor.componentIndex = ShipReactor.Count;
                    ShipReactor.Add(Reactor);
                    ShipComponents.Add(Reactor);
                }
            }
            CurrentPowerGen = ClassDefinition.TotalPowerGeneration;

            ShipShield = new BindingList<ShieldTN>();
            index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipShieldDef);
            if (index != -1)
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;

            for (int loop = 0; loop < ClassDefinition.ShipShieldCount; loop++)
            {
                ShieldTN Shield = new ShieldTN(ClassDefinition.ShipShieldDef);
                Shield.componentIndex = ShipShield.Count;
                ShipShield.Add(Shield);
                ShipComponents.Add(Shield);
            }

            CurrentShieldPool = 0.0f;
            CurrentShieldPoolMax = ClassDefinition.TotalShieldPool;
            CurrentShieldGen = ClassDefinition.TotalShieldGenPerTick;
            CurrentShieldFuelUse = ClassDefinition.TotalShieldFuelCostPerTick;
            ShieldIsActive = false;


            ShipMLaunchers = new BindingList<MissileLauncherTN>();
            for (int loop = 0; loop < ClassDefinition.ShipMLaunchDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipMLaunchDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipMLaunchCount[loop]; loop2++)
                {
                    MissileLauncherTN Tube = new MissileLauncherTN(ClassDefinition.ShipMLaunchDef[loop]);
                    Tube.componentIndex = ShipMLaunchers.Count;

                    int TubeIndex = loop2 + 1;
                    Tube.Name = Tube.missileLauncherDef.Name + " #" + TubeIndex.ToString();

                    ShipMLaunchers.Add(Tube);
                    ShipComponents.Add(Tube);
                }
            }

            ShipMagazines = new BindingList<MagazineTN>();
            for (int loop = 0; loop < ClassDefinition.ShipMagazineDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipMagazineDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipMagazineCount[loop]; loop2++)
                {
                    MagazineTN Mag = new MagazineTN(ClassDefinition.ShipMagazineDef[loop]);
                    Mag.componentIndex = ShipMagazines.Count;
                    ShipMagazines.Add(Mag);
                    ShipComponents.Add(Mag);
                }
            }

            ShipMFC = new BindingList<MissileFireControlTN>();
            for (int loop = 0; loop < ClassDefinition.ShipMFCDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipMFCDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipMFCCount[loop]; loop2++)
                {
                    MissileFireControlTN MFC = new MissileFireControlTN(ClassDefinition.ShipMFCDef[loop]);
                    MFC.componentIndex = ShipMFC.Count;

                    int MFCIndex = loop2 + 1;
                    MFC.Name = MFC.mFCSensorDef.Name + " #" + MFCIndex.ToString();

                    ShipMFC.Add(MFC);
                    ShipComponents.Add(MFC);
                    ShipFireControls.Add(MFC);
                }
            }

            ShipOrdnance = new Dictionary<OrdnanceDefTN, int>();

            CurrentMagazineCapacity = 0;
            CurrentMagazineCapacityMax = ClassDefinition.TotalMagazineCapacity;
            CurrentLauncherMagCapacityMax = ClassDefinition.LauncherMagSpace;
            CurrentMagazineMagCapacityMax = ClassDefinition.MagazineMagSpace;

            ShipCIWS = new BindingList<CIWSTN>();
            for (int loop = 0; loop < ClassDefinition.ShipCIWSDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipCIWSDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipCIWSCount[loop]; loop2++)
                {
                    CIWSTN CIWS = new CIWSTN(ClassDefinition.ShipCIWSDef[loop]);
                    CIWS.componentIndex = ShipCIWS.Count;

                    int CIWSIndex = loop2 + 1;
                    CIWS.Name = CIWS.cIWSDef.Name + " #" + CIWSIndex.ToString();

                    ShipCIWS.Add(CIWS);
                    ShipComponents.Add(CIWS);
                }
            }
            ShipCIWSIndex = 0;

            ShipTurret = new BindingList<TurretTN>();
            for (int loop = 0; loop < ClassDefinition.ShipTurretDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipTurretDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipTurretCount[loop]; loop2++)
                {
                    TurretTN Turret = new TurretTN(ClassDefinition.ShipTurretDef[loop]);
                    Turret.componentIndex = ShipTurret.Count;

                    int TurretIndex = loop2 + 1;
                    Turret.Name = Turret.turretDef.Name + " #" + TurretIndex.ToString();

                    ShipTurret.Add(Turret);
                    ShipComponents.Add(Turret);
                }
            }

            ShipJumpEngine = new BindingList<JumpEngineTN>();
            for (int loop = 0; loop < ClassDefinition.ShipJumpEngineDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipJumpEngineDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipJumpEngineCount[loop]; loop2++)
                {
                    JumpEngineTN JumpEngine = new JumpEngineTN(ClassDefinition.ShipJumpEngineDef[loop]);
                    JumpEngine.componentIndex = ShipJumpEngine.Count;

                    int JumpEngineIndex = loop2 + 1;
                    JumpEngine.Name = JumpEngine.jumpEngineDef.Name + " #" + JumpEngineIndex.ToString();

                    ShipJumpEngine.Add(JumpEngine);
                    ShipComponents.Add(JumpEngine);
                }
            }
            JumpSickness = 0;

            ShipSurvey = new BindingList<SurveySensorTN>();
            for (int loop = 0; loop < ClassDefinition.ShipSurveyDef.Count; loop++)
            {
                index = ClassDefinition.ListOfComponentDefs.IndexOf(ClassDefinition.ShipSurveyDef[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < ClassDefinition.ShipSurveyCount[loop]; loop2++)
                {
                    SurveySensorTN Survey = new SurveySensorTN(ClassDefinition.ShipSurveyDef[loop]);
                    Survey.componentIndex = ShipSurvey.Count;

                    int SurveyIndex = loop2 + 1;
                    Survey.Name = Survey.surveyDef.Name + " #" + SurveyIndex.ToString();

                    ShipSurvey.Add(Survey);
                    ShipComponents.Add(Survey);
                }
            }
            CurrentGeoSurveyStrength = ShipClass.ShipGeoSurveyStrength;
            CurrentGravSurveyStrength = ShipClass.ShipGravSurveyStrength;

            IsDestroyed = false;

            ShipsTargetting = new BindingList<ShipTN>();

            TaskGroupsOrdered = new BindingList<TaskGroupTN>();
        }

        /// <summary>
        /// AddComponents is a generalized component adder for GeneralComponentTN
        /// </summary>
        /// <param name="AddList">List the component will be added to.</param>
        /// <param name="fromList">List the definition for the component will be derived from.</param>
        /// <param name="countList">Number of components to add.</param>
        public void AddComponents(BindingList<GeneralComponentTN> AddList, BindingList<GeneralComponentDefTN> fromList, BindingList<ushort> countList)
        {
            for (int loop = 0; loop < fromList.Count; loop++)
            {
                int index = ShipClass.ListOfComponentDefs.IndexOf(fromList[loop]);
                ComponentDefIndex[index] = (ushort)ShipComponents.Count;
                for (int loop2 = 0; loop2 < countList[loop]; loop2++)
                {
                    GeneralComponentTN NewComponent = new GeneralComponentTN(fromList[loop]);
                    NewComponent.componentIndex = AddList.Count;

                    AddList.Add(NewComponent);
                    ShipComponents.Add(NewComponent);
                }
            }
        }

        /// <summary>
        /// Recrew ship puts replacement crew on a ship from a source of crew.
        /// </summary>
        /// <param name="CrewAvailable">Crew ready to be assigned to ship.</param>
        /// <returns>Crew left over to source.</returns>
        public int Recrew(int CrewAvailable)
        {
            int CrewRequired = ShipClass.TotalRequiredCrew - CurrentCrew;
            int CrewRemaining = CrewAvailable - CrewRequired;

            if (CrewRequired <= CrewAvailable)
            {
                CurrentCrew = CurrentCrew + CrewRequired;
                SpareBerths = ShipClass.TotalCrewQuarters - CurrentCrew;
            }
            else
            {
                CurrentCrew = CurrentCrew + CrewAvailable;
                SpareBerths = ShipClass.TotalCrewQuarters - CurrentCrew;
                CrewRemaining = 0;
            }

            return CrewRemaining;
        }


        /// <summary>
        /// Refuel Ship refuels the specfied ship as far as possible, but will never draw away more fuel than a source has.
        /// </summary>
        /// <param name="FuelAvailable">Amount of Fuel that refueling source possesses.</param>
        /// <returns>Fuel left over to source after refueling.</returns>
        public float Refuel(float FuelAvailable)
        {
            float FuelRequired = ShipClass.TotalFuelCapacity - CurrentFuel;
            float FuelRemaining = FuelAvailable - FuelRequired;

            if (FuelRequired <= FuelAvailable)
            {
                CurrentFuel = CurrentFuel + FuelRequired;
            }
            else
            {
                CurrentFuel = CurrentFuel + FuelAvailable;
                FuelRemaining = 0;
            }

            return FuelRemaining;
        }

        /// <summary>
        /// Resupplies ship from source of MSP.
        /// </summary>
        /// <param name="MSPAvailable">Available Maintenance supply points.</param>
        /// <returns>Left over MSP at source.</returns>
        public int Resupply(int MSPAvailable)
        {
            int MSPRequired = ShipClass.TotalMSPCapacity - CurrentMSP;
            int MSPRemaining = MSPAvailable - MSPRequired;

            if (MSPRequired <= MSPAvailable)
            {
                CurrentMSP = CurrentMSP + MSPRequired;
            }
            else
            {
                CurrentMSP = CurrentMSP + MSPAvailable;
                MSPRemaining = 0;
            }

            return MSPRemaining;
        }


        /// <summary>
        /// This function sets the specified active sensor to on or off. It is intended to be called at the Taskgroup level, as the sensor model has pertinent functionality there.
        /// This is akin to a housekeeping function as ships will never operate on their own, only via Taskgroups.
        /// </summary>
        /// <param name="Sensor">Sensor to be set.</param>
        /// <param name="active">On or off.</param>
        public void SetSensor(ActiveSensorTN Sensor, bool active)
        {
            if (Sensor.isActive == true && Sensor.isDestroyed == false && active == false)
            {
                CurrentEMSignature = CurrentEMSignature - Sensor.aSensorDef.gps;
            }
            else if (Sensor.isActive == false && Sensor.isDestroyed == false && active == true)
            {
                CurrentEMSignature = CurrentEMSignature + Sensor.aSensorDef.gps;
            }
            Sensor.isActive = active;
        }

        /// <summary>
        /// Another house keeping function, this will be called at the TG level, but sets several ship statistics that are important.
        /// </summary>
        /// <param name="Speed">New speed which the craft should attempt to meet.</param>
        /// <returns>Speed the ship is set to.</returns>
        public int SetSpeed(int Speed)
        {
            if (CurrentMaxSpeed < Speed)
                CurrentSpeed = CurrentMaxSpeed;
            else
                CurrentSpeed = Speed;

            float fraction = (float)CurrentSpeed / (float)CurrentMaxSpeed;

            CurrentEnginePower = (int)((float)CurrentMaxEnginePower * fraction);
            CurrentThermalSignature = (int)((float)CurrentMaxThermalSignature * fraction);
            CurrentFuelUsePerHour = CurrentMaxFuelUsePerHour * fraction;

            return CurrentSpeed;
        }

        #region Weapons and Damage Lines
        /// <summary>
        /// Damage goes through a 3 part process, 1st shields subtract damage, then armor blocks damage, then internals take the hits.
        /// if 20 rolls happen without an internal in the list being targeted then call OnDestroyed(); Mesons skip to the internal damage section.
        /// Microwaves do shield damage, then move to the special electronic only DAC.
        /// </summary>
        /// <param name="Type">Type of damage, for armor penetration.</param>
        /// <param name="Value">How much damage is being done.</param>
        /// <param name="HitLocation">Where Armor damage is inflicted. Temporary argument for the time being. remove these when rngs are resolved.</param>
        /// <returns>Whether or not the ship was destroyed as a result of this action.</returns>
        public bool OnDamaged(DamageTypeTN Type, ushort Value, ushort HitLocation, ShipTN FiringShip)
        {
            ushort Damage = Value;
            ushort internalDamage = 0;
            ushort startDamage = Value;
            bool ColumnPenetration = false;
            int LastColumnValue = ShipArmor.armorDef.depth;

            if (Type != DamageTypeTN.Meson)
            {

                /// <summary>
                /// Handle Shield Damage.
                /// Microwaves do 3 damage to shields. Make them do 3xPowerReq?
                /// </summary>

                if (Type == DamageTypeTN.Microwave)
                {
                    if (CurrentShieldPool >= 3.0f)
                    {
                        CurrentShieldPool = CurrentShieldPool - 3.0f;
                        Damage = 0;
                    }
                    else if (CurrentShieldPool < 1.0f)
                    {
                        CurrentShieldPool = 0.0f;
                    }
                    else
                    {
                        /// <summary>
                        /// Microwaves only do 1 damage to internals, so take away the bonus damage to shields here.
                        /// </summary>
                        Damage = 1;
                        CurrentShieldPool = 0.0f;
                    }
                }
                else
                {
                    if (CurrentShieldPool >= Damage)
                    {
                        CurrentShieldPool = CurrentShieldPool - Damage;
                        Damage = 0;
                    }
                    else if (CurrentShieldPool < 1.0f)
                    {
                        CurrentShieldPool = 0.0f;
                    }
                    else
                    {
                        Damage = (ushort)(Damage - (ushort)Math.Floor(CurrentShieldPool));

                        CurrentShieldPool = 0.0f;
                    }
                }

                /// <summary>
                /// Shields absorbed all damage.
                /// </summary>
                if (Damage == 0)
                {
                    String DamageString = String.Format("All damage to {0} absorbed by shields", Name);
                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                         GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                    ShipsFaction.MessageLog.Add(NMsg);

                    return false;
                }
                else
                {

                    if ((startDamage - Damage) > 0)
                    {
                        String DamageString = String.Format("{0} damage to {1} absorbed by shields", (startDamage - Damage), Name);
                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                        ShipsFaction.MessageLog.Add(NMsg);
                    }
                }

                startDamage = Damage;

                if (Type != DamageTypeTN.Microwave)
                {
                    /// <summary>
                    /// Shock Damage:
                    /// </summary>
                    Random ShockRand = new Random((int)startDamage);
                    float ShockChance = (float)Math.Floor(Math.Pow(startDamage, 1.3));
                    int shockTest = ShockRand.Next(0, 100);

                    /// <summary>
                    /// There is a chance for shock damage to occur
                    /// </summary>
                    if (shockTest > ShockChance)
                    {
                        float sTest = (float)ShockRand.Next(0, 100) / 100.0f;
                        internalDamage = (ushort)Math.Floor(((startDamage / 3.0f) * sTest));

                        if (internalDamage != 0)
                        {
                            String DamageString = String.Format("{0} Shock Damage to {1}", (internalDamage), Name);
                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                            ShipsFaction.MessageLog.Add(NMsg);
                        }
                    }

                    /// <summary>
                    /// Armor Penetration.
                    /// </summary>
                    ushort Columns = ShipArmor.armorDef.cNum;
                    short left, right;

                    ushort ImpactLevel = ShipArmor.armorDef.depth;
                    if (ShipArmor.isDamaged == true)
                        ImpactLevel = ShipArmor.armorColumns[HitLocation];

                    DamageTableTN Table;
                    switch (Type)
                    {
                        case DamageTypeTN.Beam: Table = DamageValuesTN.EnergyTable[Damage - 1];
                            break;
                        case DamageTypeTN.Kinetic: Table = DamageValuesTN.KineticTable[Damage - 1];
                            break;
                        case DamageTypeTN.Missile: Table = DamageValuesTN.MissileTable[Damage - 1];
                            break;
                        case DamageTypeTN.Plasma: Table = DamageValuesTN.PlasmaTable[Damage - 1];
                            break;
                        default:
                            Table = DamageValuesTN.MissileTable[Damage - 1];
                            break;
                    }
                    left = (short)(HitLocation - 1);
                    right = (short)(HitLocation + 1);

                    /// <summary>
                    /// What is the column damage level at Hit Location?
                    /// </summary>
                    if (ShipArmor.isDamaged == true)
                    {
                        LastColumnValue = ShipArmor.armorColumns[HitLocation];
                    }

                    /// <summary>
                    /// internalDamage is all damage that passed through the armour.
                    /// </summary>
                    internalDamage = (ushort)ShipArmor.SetDamage(Columns, ShipArmor.armorDef.depth, HitLocation, Table.damageTemplate[Table.hitPoint]);

                    /// <summary>
                    /// If this is a new penetration note this fact.
                    /// </summary>
                    if (LastColumnValue != 0 && internalDamage != 0)
                    {
                        ColumnPenetration = true;
                    }

                    /// <summary>
                    /// The plasma template is a little wierd and requires handling this condition. basically it has two maximum strength penetration attacks.
                    /// </summary>
                    if (Type == DamageTypeTN.Plasma && Table.hitPoint + 1 < Table.damageTemplate.Count)
                    {

                        if (ShipArmor.isDamaged == true)
                        {
                            LastColumnValue = ShipArmor.armorColumns[(HitLocation + 1)];
                        }

                        internalDamage = (ushort)((ushort)internalDamage + (ushort)ShipArmor.SetDamage(Columns, ShipArmor.armorDef.depth, (ushort)(HitLocation + 1), Table.damageTemplate[Table.hitPoint + 1]));


                        if (LastColumnValue != 0 && internalDamage != 0)
                        {
                            ColumnPenetration = true;
                        }

                        right++;
                    }

                    /// <summary>
                    /// Calculate the armour damage to the left and right of the hitLocation.
                    /// </summary>
                    for (int loop = 1; loop <= Table.halfSpread; loop++)
                    {
                        if (left < 0)
                        {
                            left = (short)(Columns - 1);
                        }
                        if (right >= Columns)
                        {
                            right = 0;
                        }

                        /// <summary>
                        /// side impact damage doesn't always reduce armor, the principle hitpoint should be the site of the deepest armor penetration. Damage can be wasted in this manner.
                        /// </summary>
                        if (Table.hitPoint - loop >= 0)
                        {
                            if (ShipArmor.isDamaged == true)
                            {
                                LastColumnValue = ShipArmor.armorColumns[left];
                            }

                            if (ImpactLevel - Table.damageTemplate[Table.hitPoint - loop] < ShipArmor.armorColumns[left])
                                internalDamage = (ushort)((ushort)internalDamage + (ushort)ShipArmor.SetDamage(Columns, ShipArmor.armorDef.depth, (ushort)left, Table.damageTemplate[Table.hitPoint - loop]));

                            if (LastColumnValue != 0 && internalDamage != 0)
                            {
                                ColumnPenetration = true;
                            }

                        }

                        if (Table.hitPoint + loop < Table.damageTemplate.Count)
                        {
                            if (ShipArmor.isDamaged == true)
                            {
                                LastColumnValue = ShipArmor.armorColumns[right];
                            }

                            if (ImpactLevel - Table.damageTemplate[Table.hitPoint + loop] < ShipArmor.armorColumns[right])
                                internalDamage = (ushort)((ushort)internalDamage + (ushort)ShipArmor.SetDamage(Columns, ShipArmor.armorDef.depth, (ushort)right, Table.damageTemplate[Table.hitPoint + loop]));

                            if (LastColumnValue != 0 && internalDamage != 0)
                            {
                                ColumnPenetration = true;
                            }

                        }

                        left--;
                        right++;
                    }

                    if ((startDamage - internalDamage) > 0)
                    {

                        String DamageString = String.Format("{0} damage to {1} absorbed by Armour", (startDamage - internalDamage), Name);
                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                        ShipsFaction.MessageLog.Add(NMsg);
                    }

                    if (ColumnPenetration == true)
                    {

                        String DamageString = "N/A";

                        /// <summary>
                        /// Need a switch here for organic or solid state ships to change or remove this message.
                        /// </summary>
                        switch (TypeOf)
                        {
                            case ShipType.Standard:
                                DamageString = String.Format("{0} is streaming atmosphere", Name);
                                break;
                            case ShipType.Organic:
                                DamageString = String.Format("{0} is streaming fluid", Name);
                                break;
                        }

                        if (TypeOf == ShipType.Standard || TypeOf == ShipType.Organic)
                        {

                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamageReport, FiringShip.ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                            FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                        }
                    }

                }
                else
                {
                    /// <summary>
                    /// This is a microwave strike.
                    /// </summary>
                    internalDamage = 1;
                }
            }
            else
            {
                /// <summary>
                /// This is a meson strike.
                /// </summary>
                internalDamage = 1;
            }

            /// <summary>
            /// Internal Component Damage. Each component with an HTK >0 can take atleast 1 hit. a random number is rolled over the entire dac. the selected component's HTK
            /// is tested against the internal damage value, and if greater than the damage value the component has a chance of surviving. otherwise, the component is destroyed, damage
            /// is reduced, and the next component is chosen.
            /// DAC Should be redone as a binary tree at some later date.
            /// 
            /// The Electronic DAC should be used for microwave hits, ships can't be destroyed due to microwave strikes however.
            /// </summary>
            int Attempts = 0;
            Random DacRNG = new Random(HitLocation);

            if (Type != DamageTypeTN.Microwave)
            {
                /// <summary>
                /// If 20 attempts to damage a component are made unsuccessfully the ship is considered destroyed. Does this scale well with larger ships?
                /// </summary>
                while (Attempts < 20 && internalDamage > 0)
                {
                    int DACHit = DacRNG.Next(1, ShipClass.DamageAllocationChart[ShipClass.ListOfComponentDefs[ShipClass.ListOfComponentDefs.Count - 1]]);

                    int localDAC = 1;
                    int previousDAC = 1;
                    int destroy = -1;
                    for (int loop = 0; loop < ShipClass.ListOfComponentDefs.Count; loop++)
                    {
                        localDAC = ShipClass.DamageAllocationChart[ShipClass.ListOfComponentDefs[loop]];
                        if (DACHit <= localDAC)
                        {
                            float size = ShipClass.ListOfComponentDefs[loop].size;
                            if (size < 1.0)
                                size = 1.0f;

                            destroy = (int)Math.Floor(((float)(DACHit - previousDAC) / (float)size));

                            /// <summary>
                            /// By this point total should definitely be >= destroy. destroy is the HS of the group being hit.
                            /// Should I try to find the exact component hit, or merely loop through all of them?
                            /// internalDamage: Damage done to all internals
                            /// destroy: component to destroy from shipClass.ListOfComponentDefs
                            /// ComponentDefIndex[loop] where in ShipComponents this definition group is.
                            /// </summary>

                            int DamageDone = DestroyComponent(ShipClass.ListOfComponentDefs[loop].componentType, loop, internalDamage, destroy, DacRNG);

                            if (DamageDone != -1)
                            {
                                int ID = ComponentDefIndex[loop] + destroy;

                                if (ShipComponents[ID].isDestroyed == true)
                                {
                                    String DamageString = String.Format("{0} hit by {1} damage and was destroyed", ShipComponents[ID].Name, DamageDone);
                                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                                         GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                                    ShipsFaction.MessageLog.Add(NMsg);
                                }
                                else
                                {
                                    String DamageString = String.Format("{0} Absorbed {1} damage", ShipComponents[ID].Name, DamageDone);
                                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                                         GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                                    ShipsFaction.MessageLog.Add(NMsg);
                                }
                            }

                            /// <summary>
                            /// No components are left to destroy, so short circuit the loops,destroy the ship, and create a wreck.
                            /// </summary>
                            if (DestroyedComponents.Count == ShipComponents.Count)
                            {
                                Attempts = 20;
                                internalDamage = 0;
                                break;
                            }


                            if (DamageDone == -1)
                            {
                                Attempts++;
                                if (Attempts == 20)
                                {
                                    internalDamage = 0;
                                }
                                break;
                            }
                            else if (DamageDone == -2)
                            {
                                Attempts = 20;
                                break;
                            }
                            else
                            {
                                internalDamage = (ushort)(internalDamage - (ushort)DamageDone);
                                break;
                            }
                        }
                        previousDAC = localDAC + 1;
                    }
                }

                if (Attempts == 20)
                {
                    String DamageString = String.Format("{0} Destroyed", Name);
                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                         GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                    ShipsFaction.MessageLog.Add(NMsg);
                    FiringShip.ShipsFaction.MessageLog.Add(NMsg);


                    IsDestroyed = true;
                    return true;
                }
            }
            else
            {
                /// <summary>
                /// Electronic damage can never destroy a craft, only wreck its sensors, so we'll cut short the attempts to damage components to only 5.
                /// There is no list of only electronic components, and only destroyed electronic components so this will have to do for now.
                /// Having those would improve performance slightly however.
                /// </summary>
                while (Attempts < 5 && internalDamage > 0)
                {
                    ComponentDefTN last = ShipClass.ElectronicDamageAllocationChart.Keys.Last();
                    int DACHit = DacRNG.Next(1, ShipClass.ElectronicDamageAllocationChart[last]);

                    int localDAC = 1;
                    int previousDAC = 1;
                    int destroy = -1;

                    foreach (KeyValuePair<ComponentDefTN, int> list in ShipClass.ElectronicDamageAllocationChart)
                    {
                        localDAC = ShipClass.ElectronicDamageAllocationChart[list.Key];

                        if (DACHit <= localDAC)
                        {
                            float size = list.Key.size;
                            if (size < 1.0)
                                size = 1.0f;

                            /// <summary>
                            /// Electronic component to attempt to destroy:
                            /// </summary>
                            destroy = (int)Math.Floor(((float)(DACHit - previousDAC) / (float)size));

                            /// <summary>
                            /// Actually destroy the component.
                            /// Store EDAC index values somewhere for speed?
                            /// </summary>

                            int CI = ShipClass.ListOfComponentDefs.IndexOf(list.Key);

                            int ComponentIndex = ShipComponents[ComponentDefIndex[CI] + destroy].componentIndex;

                            float hardCheck = (float)DacRNG.Next(1, 100);
                            float hardValue = -1.0f;

                            switch (list.Key.componentType)
                            {
                                case ComponentTypeTN.ActiveSensor:
                                    hardValue = ShipASensor[ComponentIndex].aSensorDef.hardening * 100.0f;
                                    break;
                                case ComponentTypeTN.PassiveSensor:
                                    hardValue = ShipPSensor[ComponentIndex].pSensorDef.hardening * 100.0f;
                                    break;
                                case ComponentTypeTN.BeamFireControl:
                                    hardValue = ShipBFC[ComponentIndex].beamFireControlDef.hardening * 100.0f;
                                    break;
                                case ComponentTypeTN.MissileFireControl:
                                    hardValue = ShipMFC[ComponentIndex].mFCSensorDef.hardening * 100.0f;
                                    break;
                            }

                            int DamageDone = -1;

                            if (hardValue == -1)
                            {
                                /// <summary>
                                /// This is an error condition obviously. I likely forgot to put the component in above however.
                                /// </summary>
                                String ErrorString = String.Format("Unidentified electronic component in onDamaged() Type:{0}.", list.Key.componentType);
                                MessageEntry EMsg = new MessageEntry(MessageEntry.MessageType.Error, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                    GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorString);
                                ShipsFaction.MessageLog.Add(EMsg);
                            }
                            else
                            {

                                if (hardCheck < hardValue)
                                {
                                    DamageDone = DestroyComponent(list.Key.componentType, CI, internalDamage, destroy, DacRNG);

                                    if (DamageDone != -1)
                                    {
                                        int ID = ComponentDefIndex[CI] + destroy;

                                        if (ShipComponents[ID].isDestroyed == true)
                                        {
                                            String DamageString = String.Format("{0} hit by {1} damage and was destroyed", ShipComponents[ID].Name, DamageDone);
                                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                                            ShipsFaction.MessageLog.Add(NMsg);
                                        }
                                        else
                                        {
                                            String DamageString = String.Format("{0} Absorbed {1} damage. Electronic Components shouldn't resist damage like this", ShipComponents[ID].Name, DamageDone);
                                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                                            ShipsFaction.MessageLog.Add(NMsg);
                                        }
                                    }
                                }
                                else
                                {

                                    int ID = ComponentDefIndex[CI] + destroy;
                                    String DamageString = String.Format("{0} Absorbed {1} damage", ShipComponents[ID].Name, DamageDone);
                                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShipDamage, ShipsTaskGroup.Contact.Position.System, ShipsTaskGroup.Contact,
                                                                         GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, DamageString);

                                    ShipsFaction.MessageLog.Add(NMsg);

                                    DamageDone = 0;
                                }
                            }



                            if (DamageDone == -1)
                            {
                                Attempts++;
                                if (Attempts == 5)
                                {
                                    internalDamage = 0;
                                }
                                break;
                            }
                            else
                            {
                                /// <summary>
                                /// Electronic damage should always be only 1.
                                /// </summary>
                                internalDamage = 0;
                                break;
                            }
                        }
                        previousDAC = localDAC + 1;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Destroy component handles destroying individual components of a ship, updating the ships data, and taskgroup data as needed.
        /// crew destruction and logging remain to be done.
        /// </summary>
        /// <param name="Type">Type of component destroyed</param>
        /// <param name="ComponentListDefIndex">Where in the shipClass.ListOfComponentDefs this component resides</param>
        /// <param name="Damage">How much damage is to be applied in the attempt to destroy the component</param>
        /// <param name="ComponentIndex">Which component specifically.</param>
        /// <param name="DacRNG">Current RNG kludge</param>
        /// <returns>Damage remaining after component destruction, or special message indicating no component to destroy(-1)/ship is totally destroyed(-2)</returns>
        public int DestroyComponent(ComponentTypeTN Type, int ComponentListDefIndex, int Damage, int ComponentIndex, Random DacRNG)
        {
            if (Damage >= ShipHTK)
                return -2;

            int ID = ComponentDefIndex[ComponentListDefIndex] + ComponentIndex;

            if (ShipComponents[ID].isDestroyed == true)
            {
                return -1;
            }

            int DamageReturn = -1;

            if (ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk == 0)
            {
                ShipComponents[ID].isDestroyed = true;
                DamageReturn = Damage;
            }
            else if (ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk != 0)
            {
                if (ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk <= Damage)
                {
                    ShipComponents[ID].isDestroyed = true;
                    DamageReturn = ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk;
                }
                else
                {
                    int htkTest = DacRNG.Next(1, ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk);

                    if (htkTest == ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk)
                    {
                        ShipComponents[ID].isDestroyed = true;
                        DamageReturn = Damage;
                    }
                    else
                    {
                        /// <summary>
                        /// All damage was absorbed by the component without it being destroyed.
                        /// </summary>

                        DamageReturn = Damage;
                        return DamageReturn;
                    }
                }
            }

            /// <summary>
            /// Copy the component over to the destroyed components list and decrement the ships remaining HTK value.
            DestroyedComponents.Add((ushort)ID);
            DestroyedComponentsType.Add(Type);
            ShipHTK = ShipHTK - ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk;

            /// <summary>
            /// Do Crew destruction here at some point.
            /// </summary>


            switch (Type)
            {
                /// <summary>
                /// To Do:
                /// put everything in the log.
                /// </summary>
                case ComponentTypeTN.Crew:
                    SpareBerths = SpareBerths - (int)(CrewQuarters[ShipComponents[ID].componentIndex].genCompDef.size / ShipClass.TonsPerMan);
                    break;

                case ComponentTypeTN.Fuel:
                    float Fuel = FuelTanks[ShipComponents[ID].componentIndex].genCompDef.size * 50000.0f;
                    float FuelPercentage = Fuel / ShipClass.TotalFuelCapacity;
                    float FuelLoss = FuelPercentage * CurrentFuel;
                    CurrentFuel = CurrentFuel - FuelLoss;
                    CurrentFuelCapacity = CurrentFuelCapacity - Fuel;
                    break;

                case ComponentTypeTN.Engineering:
                    float MSP = EngineeringBays[ShipComponents[ID].componentIndex].genCompDef.size;
                    float MSPPercentage = MSP / ShipClass.TotalMSPCapacity;
                    float MSPLoss = MSPPercentage * CurrentMSP;
                    CurrentMSP = CurrentMSP - (int)MSPLoss;
                    CurrentMSPCapacity = CurrentMSPCapacity - (int)((float)ShipClass.BuildPointCost * ((MSP / ShipClass.SizeHS) / 0.08f));
                    CurrentDamageControlRating = CurrentDamageControlRating - 1;
                    break;

                /// <summary>
                /// Nothing special for these yet.
                /// </summary>
                case ComponentTypeTN.Bridge:
                    break;
                case ComponentTypeTN.MaintenanceBay:
                    break;
                case ComponentTypeTN.FlagBridge:
                    break;
                case ComponentTypeTN.DamageControl:
                    break;
                case ComponentTypeTN.OrbitalHabitat:
                    break;
                case ComponentTypeTN.RecFacility:
                    break;

                case ComponentTypeTN.Engine:
                    /// <summary>
                    /// All engines have to be the same, so engine 0 is used for these for convienience.
                    /// </summary>
                    CurrentMaxEnginePower = CurrentMaxEnginePower - (int)Math.Round(ShipEngine[0].engineDef.enginePower);
                    CurrentMaxThermalSignature = CurrentMaxThermalSignature - (int)Math.Round(ShipEngine[0].engineDef.thermalSignature);
                    CurrentMaxFuelUsePerHour = CurrentMaxFuelUsePerHour - ShipEngine[0].engineDef.fuelUsePerHour;

                    if (CurrentMaxEnginePower != 0)
                        CurrentMaxSpeed = (int)((1000.0f / ((float)ShipClass.TotalCrossSection) * (float)CurrentMaxEnginePower));
                    else
                    {
                        CurrentMaxSpeed = 1;
                        CurrentMaxThermalSignature = 1; //it shouldn't be 0 either.
                    }

                    int oldThermal = CurrentThermalSignature;

                    if (ShipsTaskGroup.CurrentSpeed > CurrentMaxSpeed)
                    {
                        ShipsTaskGroup.CurrentSpeed = CurrentMaxSpeed;
                        for (int loop = 0; loop < ShipsTaskGroup.Ships.Count; loop++)
                        {
                            ShipsTaskGroup.Ships[loop].SetSpeed(ShipsTaskGroup.CurrentSpeed);
                        }
                    }

                    if (oldThermal != CurrentThermalSignature)
                        ShipsTaskGroup.SortShipBySignature(ThermalList, ShipsTaskGroup.ThermalSortList, 0);

                    int ExpTest = DacRNG.Next(1, 100);

                    if (ExpTest < ShipEngine[0].engineDef.expRisk)
                    {
                        /// <summary>
                        /// *** Do secondary damage here. ***
                        /// </summary>
                        /// SecondaryExplosion(SecondaryType.Engine,ShipEngine[0].engineDef.enginePower);
                    }
                    break;

                case ComponentTypeTN.PassiveSensor:
                    /// <summary>
                    /// Performance could be improved here by storing a sorted linked list of all passive sensors if need be.
                    /// I don't believe that sensor destruction events will be common enough to necessitate that however.
                    /// </summary>
                    if (ShipPSensor[ShipComponents[ID].componentIndex].pSensorDef.thermalOrEM == PassiveSensorType.EM)
                    {
                        if (ShipPSensor[ShipComponents[ID].componentIndex].pSensorDef.rating == ShipsTaskGroup.BestEM.pSensorDef.rating)
                        {
                            ShipsTaskGroup.BestEMCount--;

                            if (ShipsTaskGroup.BestEMCount == 0)
                            {
                                for (int loop = 0; loop < ShipsTaskGroup.Ships.Count; loop++)
                                {
                                    for (int loop2 = 0; loop2 < ShipsTaskGroup.Ships[loop].ShipPSensor.Count; loop2++)
                                    {
                                        if (ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].pSensorDef.thermalOrEM == PassiveSensorType.EM &&
                                            ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].isDestroyed == false)
                                        {
                                            if (ShipsTaskGroup.BestEMCount == 0 || ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].pSensorDef.rating > ShipsTaskGroup.BestEM.pSensorDef.rating)
                                            {
                                                ShipsTaskGroup.BestEM = ShipsTaskGroup.Ships[loop].ShipPSensor[loop2];
                                                ShipsTaskGroup.BestEMCount = 1;
                                            }
                                            else if (ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].pSensorDef.rating == ShipsTaskGroup.BestEM.pSensorDef.rating)
                                            {
                                                ShipsTaskGroup.BestEMCount++;
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (ShipPSensor[ShipComponents[ID].componentIndex].pSensorDef.rating == ShipsTaskGroup.BestThermal.pSensorDef.rating)
                        {
                            ShipsTaskGroup.BestThermalCount--;

                            if (ShipsTaskGroup.BestThermalCount == 0)
                            {
                                for (int loop = 0; loop < ShipsTaskGroup.Ships.Count; loop++)
                                {
                                    for (int loop2 = 0; loop2 < ShipsTaskGroup.Ships[loop].ShipPSensor.Count; loop2++)
                                    {
                                        if (ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].pSensorDef.thermalOrEM == PassiveSensorType.Thermal &&
                                            ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].isDestroyed == false)
                                        {
                                            if (ShipsTaskGroup.BestThermalCount == 0 || ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].pSensorDef.rating > ShipsTaskGroup.BestThermal.pSensorDef.rating)
                                            {
                                                ShipsTaskGroup.BestThermal = ShipsTaskGroup.Ships[loop].ShipPSensor[loop2];
                                                ShipsTaskGroup.BestThermalCount = 1;
                                            }
                                            else if (ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].pSensorDef.rating == ShipsTaskGroup.BestThermal.pSensorDef.rating)
                                            {
                                                ShipsTaskGroup.BestThermalCount++;
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                    break;
                case ComponentTypeTN.ActiveSensor:
                    /// <summary>
                    /// Luckily this function already takes care of active sensors being deactivated and removed from the sort lists.
                    /// </summary>
                    ShipsTaskGroup.SetActiveSensor(ShipsTaskGroup.Ships.IndexOf(this), ShipComponents[ID].componentIndex, false);
                    break;

                case ComponentTypeTN.CargoHold:
                    /// <summary>
                    /// Cargo should be destroyed here.
                    /// </summary>
                    break;

                case ComponentTypeTN.CargoHandlingSystem:
                    CurrentTractorMultiplier = CurrentTractorMultiplier - ShipCHS[ShipComponents[ID].componentIndex].cargoHandleDef.tractorMultiplier;
                    break;
                case ComponentTypeTN.CryoStorage:
                    /// <summary>
                    /// Colonists in stasis or lifeboat rescuees should be destroyed here.
                    /// </summary>
                    break;


                /// <summary>
                /// The fire control open fire list has to be updated.
                /// </summary>
                case ComponentTypeTN.BeamFireControl:
                    UnlinkAllWeapons(ShipBFC[ShipComponents[ID].componentIndex]);
                    ShipBFC[ShipComponents[ID].componentIndex].openFire = false;
                    if (ShipsFaction.OpenFireFC.ContainsKey(ShipComponents[ID]) == true)
                    {
                        ShipsFaction.OpenFireFC.Remove(ShipComponents[ID]);
                        ShipsFaction.OpenFireFCType.Remove(ShipComponents[ID]);
                    }
                    break;
                case ComponentTypeTN.Rail:
                case ComponentTypeTN.Gauss:
                case ComponentTypeTN.Plasma:
                case ComponentTypeTN.Laser:
                case ComponentTypeTN.Meson:
                case ComponentTypeTN.Microwave:
                case ComponentTypeTN.Particle:
                case ComponentTypeTN.AdvRail:
                case ComponentTypeTN.AdvLaser:
                case ComponentTypeTN.AdvPlasma:
                case ComponentTypeTN.AdvParticle:
                    UnlinkWeapon(ShipBeam[ShipComponents[ID].componentIndex]);
                    ShipBeam[ShipComponents[ID].componentIndex].currentCapacitor = 0;
                    break;

                case ComponentTypeTN.Reactor:

                    CurrentPowerGen = CurrentPowerGen - (int)(Math.Round(ShipReactor[ShipComponents[ID].componentIndex].reactorDef.powerGen));

                    ExpTest = DacRNG.Next(1, 100);

                    if (ExpTest < ShipReactor[ShipComponents[ID].componentIndex].reactorDef.expRisk)
                    {
                        /// <summary>
                        /// *** Do secondary damage here. ***
                        /// </summary>
                        /// SecondaryExplosion(SecondaryType.Reactor,ShipReactor[ShipComponents[ID].componentIndex].reactorDef.powerGen);
                    }
                    break;

                /// <summary>
                /// For shields I will preserve ShieldIsActive as is, but set the other values down on component destruction.
                /// </summary.
                case ComponentTypeTN.Shield:

                    CurrentShieldPoolMax = CurrentShieldPoolMax - ShipShield[ShipComponents[ID].componentIndex].shieldDef.shieldPool;
                    CurrentShieldGen = CurrentShieldGen - ShipShield[ShipComponents[ID].componentIndex].shieldDef.shieldGenPerTick;
                    CurrentShieldFuelUse = CurrentShieldFuelUse - (ShipShield[ShipComponents[ID].componentIndex].shieldDef.fuelCostPerHour / 720.0f);

                    /// <summary>
                    /// In the event of meson damage/mixed meson and non-meson damage:
                    /// </summary>
                    if (CurrentShieldPool != 0.0f && CurrentShieldPool > CurrentShieldPoolMax)
                        CurrentShieldPool = CurrentShieldPoolMax;

                    if (ShieldIsActive == true)
                    {
                        CurrentEMSignature = CurrentEMSignature - (int)(ShipShield[ShipComponents[ID].componentIndex].shieldDef.shieldPool * 30.0f);
                        ShipsTaskGroup.SortShipBySignature(EMList, ShipsTaskGroup.EMSortList, 1);
                    }
                    break;

                case ComponentTypeTN.AbsorptionShield:
                    CurrentShieldPool = 0.0f;
                    CurrentShieldPoolMax = 0.0f;
                    CurrentShieldGen = 0.0f;
                    CurrentShieldFuelUse = 0.0f;

                    if (ShieldIsActive == true)
                    {
                        CurrentEMSignature = CurrentEMSignature - (int)(ShipShield[ShipComponents[ID].componentIndex].shieldDef.shieldPool * 30.0f);
                        ShipsTaskGroup.SortShipBySignature(EMList, ShipsTaskGroup.EMSortList, 1);
                    }
                    break;

                case ComponentTypeTN.MissileLauncher:

                    /// <summary>
                    /// If the ship has loaded ordnance then figuring out which one to destroy is simple:
                    /// </summary>
                    if (ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance != null)
                    {
                        if (ShipOrdnance.ContainsKey(ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance) == true)
                        {

                            ShipOrdnance[ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance] = ShipOrdnance[ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance] - 1;

                            if (ShipOrdnance[ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance] == 0)
                            {
                                ShipOrdnance.Remove(ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance);
                            }
                            else if (ShipOrdnance[ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance] < 0)
                            {
#if LOG4NET_ENABLED
#warning faction messagelog this?
                                String Entry = String.Format("Ship ordnance subtraction below 0 on ship {0} due to launcher destruction.", this.Name);
                                logger.Debug(Entry);
#endif
                            }
                        }
                        else
                        {
                            /// <summary>
                            /// This condition could arise because all such missiles have been fired. Check the total ship ordnance vs magazine capacity to decide if another missile should be destroyed.
                            /// </summary>

                            if (CurrentMagazineCapacity > CurrentMagazineMagCapacityMax)
                            {
                                foreach (KeyValuePair<OrdnanceDefTN, int> pair in ShipOrdnance)
                                {
                                    if (ShipOrdnance[pair.Key] > 0)
                                    {
                                        ShipOrdnance[pair.Key] = ShipOrdnance[pair.Key] - 1;

                                        if (ShipOrdnance[pair.Key] == 0)
                                        {
                                            ShipOrdnance.Remove(pair.Key);
                                        }
                                        break;
                                    }
                                    else
                                    {
#if LOG4NET_ENABLED
#warning faction messagelog this?
                                        String Entry = String.Format("Ship {0} has ship ordnance in quantities of zero from somewhere.", this.Name);
                                        logger.Debug(Entry);
#endif
                                    }
                                }
                            }
                        }

                        ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance = null;
                    }
                    else
                    {
                        /// <summary>
                        /// This condition could arise because all such missiles have been fired. Check the total ship ordnance vs magazine capacity to decide if another missile should be destroyed.
                        /// This else is included twice due to paranoia over the possibility that loadedOrdnance = null
                        /// </summary>

                        if (CurrentMagazineCapacity > CurrentMagazineMagCapacityMax)
                        {
                            foreach (KeyValuePair<OrdnanceDefTN, int> pair in ShipOrdnance)
                            {
                                if (ShipOrdnance[pair.Key] > 0)
                                {
                                    ShipOrdnance[pair.Key] = ShipOrdnance[pair.Key] - 1;

                                    if (ShipOrdnance[pair.Key] == 0)
                                    {
                                        ShipOrdnance.Remove(pair.Key);
                                    }
                                    break;
                                }
                                else
                                {
#if LOG4NET_ENABLED
#warning faction messagelog this?
                                    String Entry = String.Format("Ship {0} has ship ordnance in quantities of zero from somewhere.", this.Name);
                                    logger.Debug(Entry);
#endif
                                }
                            }
                        }
                    }

                    CurrentLauncherMagCapacityMax = CurrentLauncherMagCapacityMax - ShipMLaunchers[ShipComponents[ID].componentIndex].missileLauncherDef.launchMaxSize;
                    CurrentMagazineCapacityMax = CurrentMagazineCapacityMax - ShipMLaunchers[ShipComponents[ID].componentIndex].missileLauncherDef.launchMaxSize;
                    CurrentMagazineCapacity = CurrentMagazineCapacity - (int)Math.Ceiling(ShipMLaunchers[ShipComponents[ID].componentIndex].loadedOrdnance.size);
                    ShipMLaunchers[ShipComponents[ID].componentIndex].ClearMFC();
                    break;

                case ComponentTypeTN.Magazine:
                    /// <summary>
                    /// There must be missiles in the magazines rather than all of them being in the launch tubes, or the mags could be totally empty
                    /// </summary>
                    int WarheadTotal = 0;
                    if (CurrentMagazineCapacity > CurrentLauncherMagCapacityMax)
                    {
                        /// <summary>
                        /// Total percentage of all magazine space divided by all space total on the ship for missiles(includes launch tubes).
                        /// </summary>
                        float MagPercentage = (CurrentMagazineMagCapacityMax / CurrentMagazineCapacityMax);

                        /// <summary>
                        /// Percentage of total magazine space this one magazine represents.
                        /// </summary>
                        float ThisMagPercentage = (ShipMagazines[ShipComponents[ID].componentIndex].magazineDef.capacity / CurrentMagazineMagCapacityMax) * MagPercentage;

                        /// <summary>
                        /// Counter for missiles destroyed.
                        /// </summary>
                        int TempCapTotal = 0;



                        /// <summary>
                        /// Temporary key list, will be removing these from shipordnance when done.
                        /// </summary>
                        Dictionary<OrdnanceDefTN,int> TempKeyList = new Dictionary<OrdnanceDefTN,int>();

                        /// <summary>
                        /// loop through all ordnance and determine the loadbalanced count of missiles that would be in this magazine.
                        /// if ShipOrdnance is empty, this does not run.
                        /// </summary>
                        foreach (KeyValuePair<OrdnanceDefTN, int> pair in ShipOrdnance)
                        {
                            int Total = (int)Math.Ceiling(pair.Key.size) * pair.Value;
                            int AmtInThisMag = (int)Math.Ceiling(ThisMagPercentage * Total);
                            TempCapTotal = TempCapTotal + AmtInThisMag;

                            TempKeyList.Add(pair.Key, AmtInThisMag);

                            WarheadTotal = WarheadTotal + pair.Key.warhead;

                            /// <summary>
                            /// Some mags will have a little more than total cap, and some will have a little less than total cap because of how this could work out.
                            /// </summary>
                            if (TempCapTotal >= ShipMagazines[ShipComponents[ID].componentIndex].magazineDef.capacity)
                                break;
                        }

                        /// <summary>
                        /// I have to subtract values elsewhere because it was giving me some crap about "Collection was modified; enumeration operation may not execute"
                        /// Atleast I hope this fixes the issue.
                        /// </summary>
                        if (TempKeyList.Count != 0)
                        {
                            foreach (KeyValuePair<OrdnanceDefTN,int> pair in TempKeyList)
                            {
                                ShipOrdnance[pair.Key] = ShipOrdnance[pair.Key] - pair.Value;
                                if (ShipOrdnance[pair.Key] <= 0)
                                {
                                    if (ShipOrdnance[pair.Key] < 0)
                                    {
#if LOG4NET_ENABLED
#warning faction messagelog this?
                                        logger.Debug("Ship ordnance key value inexplicably reduced below zero on magazine destruction.");
#endif
                                    }
                                    ShipOrdnance.Remove(pair.Key);
                                }
                                
                            }
                        }
                    }

                    CurrentMagazineCapacityMax = CurrentMagazineCapacityMax - ShipMagazines[ShipComponents[ID].componentIndex].magazineDef.capacity;
                    CurrentMagazineMagCapacityMax = CurrentMagazineMagCapacityMax - ShipMagazines[ShipComponents[ID].componentIndex].magazineDef.capacity;

                    ExpTest = DacRNG.Next(1, 100);

                    if (ExpTest < ShipMagazines[ShipComponents[ID].componentIndex].magazineDef.expRisk)
                    {
                        /// <summary>
                        /// *** Do secondary damage here. ***
                        /// </summary>
                        /// SecondaryExplosion(SecondaryType.Magazine,WarheadTotal);
                    }
                    break;

                case ComponentTypeTN.MissileFireControl:
                    ShipMFC[ShipComponents[ID].componentIndex].ClearAllWeapons();
                    ShipMFC[ShipComponents[ID].componentIndex].ClearAllMissiles();
                    ShipMFC[ShipComponents[ID].componentIndex].openFire = false;
                    if (ShipsFaction.OpenFireFC.ContainsKey(ShipComponents[ID]) == true)
                    {
                        ShipsFaction.OpenFireFC.Remove(ShipComponents[ID]);
                        ShipsFaction.OpenFireFCType.Remove(ShipComponents[ID]);
                    }
                    break;

                case ComponentTypeTN.CIWS:
                    /// <summary>
                    /// Do nothing for CIWS.
                    /// </summary>
                    break;

                case ComponentTypeTN.Turret:
                    UnlinkWeapon(ShipTurret[ShipComponents[ID].componentIndex]);
                    ShipTurret[ShipComponents[ID].componentIndex].currentCapacitor = 0;
                    break;

                case ComponentTypeTN.JumpEngine:
                    /// <summary>
                    /// Nothing special needs to be done to ship in this case.
                    /// </summary>
                    break;

                case ComponentTypeTN.SurveySensor:
                    SurveySensorDefTN sDef = ShipSurvey[ShipComponents[ID].componentIndex].surveyDef;
                    if (sDef.sensorType == SurveySensorDefTN.SurveySensorType.Geological)
                    {
                        CurrentGeoSurveyStrength = CurrentGeoSurveyStrength - sDef.sensorStrength;
                        if (CurrentGeoSurveyStrength < 0)
                            CurrentGeoSurveyStrength = 0;
                    }
                    else if (sDef.sensorType == SurveySensorDefTN.SurveySensorType.Gravitational)
                    {
                        CurrentGravSurveyStrength = CurrentGravSurveyStrength - sDef.sensorStrength;
                        if (CurrentGravSurveyStrength < 0)
                            CurrentGravSurveyStrength = 0;
                    }
                    break;
            }
            return DamageReturn;
        }

        public void RepairComponent(ComponentTypeTN Type, int ComponentIndex)
        {
            /// <summary>
            /// Subtract MSPs in damage control function, not necessary here.
            /// </summary>

            ShipComponents[ComponentIndex].isDestroyed = false;
            DestroyedComponents.Remove((ushort)ComponentIndex);

            switch (Type)
            {
                case ComponentTypeTN.Crew:
                    SpareBerths = SpareBerths + (int)(CrewQuarters[ShipComponents[ComponentIndex].componentIndex].genCompDef.size / ShipClass.TonsPerMan);
                    break;

                case ComponentTypeTN.Fuel:
                    CurrentFuelCapacity = CurrentFuelCapacity + (FuelTanks[ShipComponents[ComponentIndex].componentIndex].genCompDef.size * 50000.0f);
                    break;

                case ComponentTypeTN.Engineering:
                    float MSP = EngineeringBays[ShipComponents[ComponentIndex].componentIndex].genCompDef.size;
                    CurrentMSPCapacity = CurrentMSPCapacity + (int)((float)ShipClass.BuildPointCost * ((MSP / ShipClass.SizeHS) / 0.08f));
                    CurrentDamageControlRating = CurrentDamageControlRating + 1;
                    break;

                /// <summary>
                /// Nothing special is done for these yet.
                /// </summary>
                case ComponentTypeTN.Bridge:
                    break;
                case ComponentTypeTN.MaintenanceBay:
                    break;
                case ComponentTypeTN.FlagBridge:
                    break;
                case ComponentTypeTN.DamageControl:
                    break;
                case ComponentTypeTN.OrbitalHabitat:
                    break;
                case ComponentTypeTN.RecFacility:
                    break;

                case ComponentTypeTN.Engine:
                    /// <summary>
                    /// All engines have to be the same, so engine 0 is used for these for convienience.
                    /// </summary>
                    CurrentMaxEnginePower = CurrentMaxEnginePower + (int)Math.Round(ShipEngine[0].engineDef.enginePower);
                    CurrentMaxThermalSignature = CurrentMaxThermalSignature + (int)Math.Round(ShipEngine[0].engineDef.thermalSignature);
                    CurrentMaxFuelUsePerHour = CurrentMaxFuelUsePerHour + ShipEngine[0].engineDef.fuelUsePerHour;

                    if (CurrentMaxEnginePower == 0)
                    {
                        /// <summary>
                        /// This is a very bad error I think.
                        /// hopefully it should never happen.
                        /// </summary>
                        CurrentMaxSpeed = 1;
                        CurrentMaxThermalSignature = 1;
#if LOG4NET_ENABLED
#warning faction messagelog this?
                        logger.Debug("CurrentMaxEnginePower was 0 AFTER engine repair. oops. see Ship.cs RepairComponent()");
#endif
                    }
                    else
                        CurrentMaxSpeed = (int)((1000.0f / (float)ShipClass.TotalCrossSection) * (float)CurrentMaxEnginePower);

                    int speedMin = 0;
                    for (int loop = 0; loop < ShipsTaskGroup.Ships.Count; loop++)
                    {
                        if (ShipsTaskGroup.Ships[loop].CurrentMaxSpeed > speedMin)
                            speedMin = CurrentMaxSpeed;
                    }

                    int oldThermal = CurrentThermalSignature;

                    if (speedMin > ShipsTaskGroup.CurrentSpeed)
                    {
                        ShipsTaskGroup.CurrentSpeed = speedMin;
                        for (int loop = 0; loop < ShipsTaskGroup.Ships.Count; loop++)
                        {
                            ShipsTaskGroup.Ships[loop].SetSpeed(ShipsTaskGroup.CurrentSpeed);
                        }
                    }

                    if (oldThermal != CurrentThermalSignature)
                        ShipsTaskGroup.SortShipBySignature(ThermalList, ShipsTaskGroup.ThermalSortList, 0);

                    break;
                case ComponentTypeTN.PassiveSensor:
                    if (ShipPSensor[ShipComponents[ComponentIndex].componentIndex].pSensorDef.thermalOrEM == PassiveSensorType.EM)
                    {
                        if (ShipPSensor[ShipComponents[ComponentIndex].componentIndex].pSensorDef.rating > ShipsTaskGroup.BestEM.pSensorDef.rating)
                        {
                            ShipsTaskGroup.BestEM = ShipPSensor[ShipComponents[ComponentIndex].componentIndex];
                            ShipsTaskGroup.BestEMCount = 1;
                        }
                        else if (ShipPSensor[ShipComponents[ComponentIndex].componentIndex].pSensorDef.rating == ShipsTaskGroup.BestEM.pSensorDef.rating)
                        {
                            ShipsTaskGroup.BestEMCount++;
                        }
                    }
                    else
                    {
                        if (ShipPSensor[ShipComponents[ComponentIndex].componentIndex].pSensorDef.rating > ShipsTaskGroup.BestThermal.pSensorDef.rating)
                        {
                            ShipsTaskGroup.BestThermal = ShipPSensor[ShipComponents[ComponentIndex].componentIndex];
                            ShipsTaskGroup.BestThermalCount = 1;
                        }
                        else if (ShipPSensor[ShipComponents[ComponentIndex].componentIndex].pSensorDef.rating == ShipsTaskGroup.BestThermal.pSensorDef.rating)
                        {
                            ShipsTaskGroup.BestThermalCount++;
                        }
                    }
                    break;

                /// <summary>
                /// Nothing special for these yet.
                /// </summary>
                case ComponentTypeTN.ActiveSensor:
                    break;
                case ComponentTypeTN.CargoHold:
                    break;

                case ComponentTypeTN.CargoHandlingSystem:
                    CurrentTractorMultiplier = CurrentTractorMultiplier + ShipCHS[ShipComponents[ComponentIndex].componentIndex].cargoHandleDef.tractorMultiplier;
                    break;

                /// <summary>
                /// Nothing special for these yet. Weapon links won't be restored.
                /// </summary>
                case ComponentTypeTN.CryoStorage:
                    break;
                case ComponentTypeTN.BeamFireControl:
                    break;
                case ComponentTypeTN.Rail:
                case ComponentTypeTN.Gauss:
                case ComponentTypeTN.Plasma:
                case ComponentTypeTN.Laser:
                case ComponentTypeTN.Meson:
                case ComponentTypeTN.Microwave:
                case ComponentTypeTN.Particle:
                case ComponentTypeTN.AdvRail:
                case ComponentTypeTN.AdvLaser:
                case ComponentTypeTN.AdvPlasma:
                case ComponentTypeTN.AdvParticle:
                    break;

                case ComponentTypeTN.Reactor:
                    CurrentPowerGen = CurrentPowerGen + (int)(Math.Round(ShipReactor[ShipComponents[ComponentIndex].componentIndex].reactorDef.powerGen));
                    break;

                /// <summary>
                /// For shields I will preserve ShieldIsActive as is, but set the other values up on component repair.
                /// </summary.
                case ComponentTypeTN.Shield:
                    CurrentShieldPoolMax = CurrentShieldPoolMax + ShipShield[ShipComponents[ComponentIndex].componentIndex].shieldDef.shieldPool;
                    CurrentShieldGen = CurrentShieldGen + ShipShield[ShipComponents[ComponentIndex].componentIndex].shieldDef.shieldGenPerTick;
                    CurrentShieldFuelUse = CurrentShieldFuelUse + (ShipShield[ShipComponents[ComponentIndex].componentIndex].shieldDef.fuelCostPerHour / 720.0f);

                    if (ShieldIsActive == true)
                    {
                        CurrentEMSignature = CurrentEMSignature + (int)(ShipShield[ShipComponents[ComponentIndex].componentIndex].shieldDef.shieldPool * 30.0f);
                        ShipsTaskGroup.SortShipBySignature(EMList, ShipsTaskGroup.EMSortList, 1);
                    }
                    break;

                case ComponentTypeTN.AbsorptionShield:
                    CurrentShieldPoolMax = ShipClass.TotalShieldPool;
                    CurrentShieldGen = ShipClass.TotalShieldGenPerTick;
                    CurrentShieldFuelUse = ShipClass.TotalShieldFuelCostPerTick;

                    if (ShieldIsActive == true)
                    {
                        CurrentEMSignature = CurrentEMSignature + (int)(ShipShield[ShipComponents[ComponentIndex].componentIndex].shieldDef.shieldPool * 30.0f);
                        ShipsTaskGroup.SortShipBySignature(EMList, ShipsTaskGroup.EMSortList, 1);
                    }
                    break;

                case ComponentTypeTN.MissileLauncher:
                    CurrentLauncherMagCapacityMax = CurrentLauncherMagCapacityMax + ShipMLaunchers[ShipComponents[ComponentIndex].componentIndex].missileLauncherDef.launchMaxSize;
                    CurrentMagazineCapacityMax = CurrentMagazineCapacityMax + ShipMLaunchers[ShipComponents[ComponentIndex].componentIndex].missileLauncherDef.launchMaxSize;
                    break;

                case ComponentTypeTN.Magazine:
                    CurrentMagazineMagCapacityMax = CurrentMagazineMagCapacityMax + ShipMagazines[ShipComponents[ComponentIndex].componentIndex].magazineDef.capacity;
                    CurrentMagazineCapacityMax = CurrentMagazineCapacityMax + ShipMagazines[ShipComponents[ComponentIndex].componentIndex].magazineDef.capacity;
                    break;

                case ComponentTypeTN.MissileFireControl:
                    break;

                case ComponentTypeTN.CIWS:
                    break;

                case ComponentTypeTN.Turret:
                    break;

                case ComponentTypeTN.JumpEngine:
                    break;

                case ComponentTypeTN.SurveySensor:
                    SurveySensorDefTN sDef = ShipSurvey[ShipComponents[ComponentIndex].componentIndex].surveyDef;
                    if (sDef.sensorType == SurveySensorDefTN.SurveySensorType.Geological)
                    {
                        CurrentGeoSurveyStrength = CurrentGeoSurveyStrength + sDef.sensorStrength;
                    }
                    else if (sDef.sensorType == SurveySensorDefTN.SurveySensorType.Gravitational)
                    {
                        CurrentGravSurveyStrength = CurrentGravSurveyStrength + sDef.sensorStrength;
                    }
                    break;
            }
        }

        /// <summary>
        /// This function returns the cost of repairing the specified component based on the data given to the damage control queue.
        /// </summary>
        /// <param name="ID">Id of component in general ship component list.</param>
        /// <param name="CType">type of component.</param>
        /// <returns>cost of said component.</returns>
        public decimal GetDamagedComponentsRepairCost(int ID, ComponentTypeTN CType)
        {
            switch (CType)
            {
                case ComponentTypeTN.Crew:
                       return CrewQuarters[ShipComponents[ID].componentIndex].genCompDef.cost;

                case ComponentTypeTN.Fuel:
                    return FuelTanks[ShipComponents[ID].componentIndex].genCompDef.cost;

                case ComponentTypeTN.Engineering:
                    return EngineeringBays[ShipComponents[ID].componentIndex].genCompDef.cost;

                case ComponentTypeTN.Bridge:
                case ComponentTypeTN.MaintenanceBay:
                case ComponentTypeTN.FlagBridge:
                case ComponentTypeTN.DamageControl:
                case ComponentTypeTN.OrbitalHabitat:
                case ComponentTypeTN.RecFacility:
                    return OtherComponents[ShipComponents[ID].componentIndex].genCompDef.cost;

                case ComponentTypeTN.Engine:
                    /// <summary>
                    /// All engines have to be the same, so engine 0 is used for these for convienience.
                    /// </summary>
                    return ShipEngine[0].engineDef.cost;

                case ComponentTypeTN.PassiveSensor:
                    return ShipPSensor[ShipComponents[ID].componentIndex].pSensorDef.cost;

                case ComponentTypeTN.ActiveSensor:
                    return ShipASensor[ShipComponents[ID].componentIndex].aSensorDef.cost;

                case ComponentTypeTN.CargoHold:
                    return ShipCargo[ShipComponents[ID].componentIndex].cargoDef.cost;

                case ComponentTypeTN.CargoHandlingSystem:
                    return ShipCHS[ShipComponents[ID].componentIndex].cargoHandleDef.cost;

                case ComponentTypeTN.CryoStorage:
                    return ShipColony[ShipComponents[ID].componentIndex].colonyDef.cost;

                case ComponentTypeTN.BeamFireControl:
                    return ShipBFC[ShipComponents[ID].componentIndex].beamFireControlDef.cost;

                case ComponentTypeTN.Rail:
                case ComponentTypeTN.Gauss:
                case ComponentTypeTN.Plasma:
                case ComponentTypeTN.Laser:
                case ComponentTypeTN.Meson:
                case ComponentTypeTN.Microwave:
                case ComponentTypeTN.Particle:
                case ComponentTypeTN.AdvRail:
                case ComponentTypeTN.AdvLaser:
                case ComponentTypeTN.AdvPlasma:
                case ComponentTypeTN.AdvParticle:
                    return ShipBeam[ShipComponents[ID].componentIndex].beamDef.cost;


                case ComponentTypeTN.Reactor:
                    return ShipReactor[ShipComponents[ID].componentIndex].reactorDef.cost;

                case ComponentTypeTN.Shield:
                    return ShipShield[ShipComponents[ID].componentIndex].shieldDef.cost;

                case ComponentTypeTN.AbsorptionShield:
                    return ShipShield[ShipComponents[ID].componentIndex].shieldDef.cost;

                case ComponentTypeTN.MissileLauncher:
                    return ShipMLaunchers[ShipComponents[ID].componentIndex].missileLauncherDef.cost;

                case ComponentTypeTN.Magazine:
                    return ShipMagazines[ShipComponents[ID].componentIndex].magazineDef.cost;

                case ComponentTypeTN.MissileFireControl:
                    return ShipMFC[ShipComponents[ID].componentIndex].mFCSensorDef.cost;

                case ComponentTypeTN.CIWS:
                    return ShipCIWS[ShipComponents[ID].componentIndex].cIWSDef.cost;

                case ComponentTypeTN.Turret:
                    return ShipTurret[ShipComponents[ID].componentIndex].turretDef.cost;

                case ComponentTypeTN.JumpEngine:
                    return ShipJumpEngine[ShipComponents[ID].componentIndex].jumpEngineDef.cost;

                case ComponentTypeTN.SurveySensor:
                    return ShipSurvey[ShipComponents[ID].componentIndex].surveyDef.cost;
            }

            return 0.0m;
        }

        /// <summary>
        /// Handle the consequences of a ship destruction in game mechanics. C# loses its cookies if I try to actually delete anything here.
        /// still many things need to be cleaned up when a ship is destroyed outright.
        /// </summary>
        /// <returns>Whether LinkedListNodes have been removed.</returns>
        public bool OnDestroyed()
        {
            /// <summary>
            /// TG specific handling needs to be done here. first, the nodes have to be removed from the linked lists for each detection method.
            /// Next the shipId's of the surviving ships have to be adjusted downwards to match the new ship count.
            /// </summary>

            return ShipsTaskGroup.RemoveShipFromTaskGroup(this);

            /// <summary>
            /// A new wreck needs to be created with the surviving components, if any, and some fraction of the cost of the ship.
            /// </summary>
        }


        /// <summary>
        /// Links specified weapon to the selected BFC. Sanity check to make sure both are on the same ship?
        /// </summary>
        /// <param name="BFC">Beam Fire Controller</param>
        /// <param name="Weapon">Beam Weapon</param>
        public void LinkWeaponToBeamFC(BeamFireControlTN BFC, BeamTN Weapon)
        {
            BFC.linkWeapon(Weapon);
        }

        /// <summary>
        /// Links specified weapon to the selected BFC. Sanity check to make sure both are on the same ship?
        /// </summary>
        /// <param name="BFC">Beam Fire Controller</param>
        /// <param name="Weapon">Beam Weapon</param>
        public void LinkWeaponToBeamFC(BeamFireControlTN BFC, TurretTN Weapon)
        {
            BFC.linkWeapon(Weapon);
        }

        /// <summary>
        /// Links specified Tube to MFC.
        /// </summary>
        /// <param name="MFC">missile fire control</param>
        /// <param name="Tube">Launch tube</param>
        public void LinkTubeToMFC(MissileFireControlTN MFC, MissileLauncherTN Tube)
        {
            MFC.assignLaunchTube(Tube);
        }

        /// <summary>
        /// unlinks this tube from any MFC.
        /// </summary>
        /// <param name="Tube">Launch tube to be disconnected.</param>
        public void UnlinkTube(MissileLauncherTN Tube)
        {
            Tube.ClearMFC();
        }

        /// <summary>
        /// Unlinks the specified beam weapon from its fire controller.
        /// </summary>
        /// <param name="Weapon">beam weapon to be cleared.</param>
        public void UnlinkWeapon(BeamTN Weapon)
        {
            if (Weapon.fireController != null)
            {
                BeamFireControlTN BFC = Weapon.fireController;
                BFC.unlinkWeapon(Weapon);
            }
        }

        /// <summary>
        /// Unlinks the specified beam weapon from its fire controller.
        /// </summary>
        /// <param name="Weapon">beam weapon to be cleared.</param>
        public void UnlinkWeapon(TurretTN Weapon)
        {
            if (Weapon.fireController != null)
            {
                BeamFireControlTN BFC = Weapon.fireController;
                BFC.unlinkWeapon(Weapon);
            }
        }

        /// <summary>
        /// Removes all weapon links to ths specified MFC
        /// </summary>
        /// <param name="MFC"></param>
        public void UnlinkAllTubes(MissileFireControlTN MFC)
        {
            MFC.linkedWeapons.Clear();
        }

        /// <summary>
        /// Removes all weapon links to the specified BFC
        /// </summary>
        /// <param name="BFC">Beam fire Control to be cleared.</param>
        public void UnlinkAllWeapons(BeamFireControlTN BFC)
        {
            BFC.clearWeapons();
        }

        /// <summary>
        /// Reloads missile tubes, function is based on time alone for the most part. Launch tubes with no ordnance selected will "reload" as well. not going to demand absolutely meticulous 
        /// ordnance management from the player.
        /// </summary>
        /// <param name="tick">time increment that the sim is advanced by. 1 day = 86400 seconds, smallest practical value is 5.</param>
        /// <returns> Whether all tubes have been loaded or not.</returns>
        public bool ReloadLaunchTubes(uint tick)
        {
            bool allTubesLoaded = true;
            for (int loop = 0; loop < ShipMLaunchers.Count; loop++)
            {
                if (ShipMLaunchers[loop].loadTime > 0)
                {
                    allTubesLoaded = false;
                    ShipMLaunchers[loop].loadTime = ShipMLaunchers[loop].loadTime - (int)tick;

                    if (ShipMLaunchers[loop].loadTime < 0)
                    {
                        ShipMLaunchers[loop].loadTime = 0;
                    }

                    if (ShipMLaunchers[loop].loadedOrdnance != null)
                    {
                        if (ShipOrdnance.ContainsKey(ShipMLaunchers[loop].loadedOrdnance) == false)
                        {
                            String NoOrdnance = String.Format("No ordnance of type {0} remains on {1}. This will adversely affect turn processing(very slightly) if left unfixed.", ShipMLaunchers[loop].loadedOrdnance.Name, Name);
                            MessageEntry NewMessage = new MessageEntry(MessageEntry.MessageType.LaunchTubeNoOrdnanceToReload,
                                                                                        ShipsTaskGroup.Contact.Position.System,
                                                                                                      ShipsTaskGroup.Contact,
                                                                                             GameState.Instance.GameDateTime,
                                                                          GameState.Instance.LastTimestep,
                                                                                                                  NoOrdnance);
                            ShipsFaction.MessageLog.Add(NewMessage);
                            ShipMLaunchers[loop].loadTime = -1;
                        }
                    }
                }
                /// <summary>
                /// Ships with no loaded ordnance will remain in the weapon recharge list due to this, either the AI or the player should fix this.
                /// </summary>
                else if (ShipMLaunchers[loop].loadTime == -1)
                {
                    allTubesLoaded = false;
                    if (ShipMLaunchers[loop].loadedOrdnance == null)
                    {
                        ShipMLaunchers[loop].loadTime = 0;
                    }
                    else
                    {
                        if (ShipOrdnance.ContainsKey(ShipMLaunchers[loop].loadedOrdnance) == false && ShipMLaunchers[loop].mFC.openFire == true)
                        {
                            String NoOrdnance = String.Format("No ordnance of type {0} remains on {1}, cannot fire. This will adversely affect turn processing(very slightly) if left unfixed.", ShipMLaunchers[loop].loadedOrdnance.Name, Name);
                            MessageEntry NewMessage = new MessageEntry(MessageEntry.MessageType.LaunchTubeNoOrdnanceToReload,
                                                                                        ShipsTaskGroup.Contact.Position.System,
                                                                                                      ShipsTaskGroup.Contact,
                                                                                             GameState.Instance.GameDateTime,
                                                                          GameState.Instance.LastTimestep,
                                                                                                                  NoOrdnance);
                            ShipsFaction.MessageLog.Add(NewMessage);
                        }
                        else if (ShipOrdnance.ContainsKey(ShipMLaunchers[loop].loadedOrdnance) == true)
                        {
                            ShipMLaunchers[loop].loadTime = 0;
                        }
                    }
                }
            }
            return allTubesLoaded;
        }

        /// <summary>
        /// Recharges energyweapons to currentPowerGeneration of the ship.
        /// </summary>
        /// <param name="tick">Tick is the value in seconds the sim is being advanced by. 1 day = 86400 seconds. smallest practical value is 5.</param>
        /// <returns>Power not used. if nonzero then this ship no longer needs to be in the recharge list.</returns>
        public int RechargeBeamWeapons(uint tick, out int ShotsExpended)
        {
            ushort amt = (ushort)(Math.Floor((float)tick / 5.0f));
            float PowerRecharge = CurrentPowerGen * amt;
            ShotsExpended = 0;


            for (int loop = 0; loop < ShipBeam.Count; loop++)
            {
                /// <summary>
                /// Reset the shots expended for these beam weapons. the railguns still can't fire until they get power however.
                /// </summary>
                if (ShipBeam[loop].beamDef.componentType == ComponentTypeTN.Gauss || ShipBeam[loop].beamDef.componentType == ComponentTypeTN.Rail ||
                    ShipBeam[loop].beamDef.componentType == ComponentTypeTN.AdvRail)
                {
                    ShotsExpended = ShotsExpended + ShipBeam[loop].shotsExpended;
                    ShipBeam[loop].shotsExpended = 0;
                }
            }

            for (int loop = 0; loop < ShipTurret.Count; loop++)
            {
                /// <summary>
                /// Reset the shots expended for gauss turrets.
                /// </summary>
                if (ShipTurret[loop].turretDef.baseBeamWeapon.componentType == ComponentTypeTN.Gauss)
                {
                    ShotsExpended = ShotsExpended + ShipTurret[loop].shotsExpended;
                    ShipTurret[loop].shotsExpended = 0;
                }
            }

            /// <summary>
            /// There are beam weapons that need to be recharged.
            /// </summary>
            if (ShipClass.TotalPowerRequirement != 0)
            {
                /// <summary>
                /// All beam weapons can be recharged.
                /// </summary>
                if (PowerRecharge > ShipClass.TotalPowerRequirement)
                {
                    /// <summary>
                    /// Recharge the regular beams.
                    /// </summary>
                    for (int loop = 0; loop < ShipBeam.Count; loop++)
                    {
                        ushort beamCap = (ushort)(ShipBeam[loop].beamDef.weaponCapacitor * amt);

                        /// <summary>
                        /// This beam can be totally recharged.
                        /// </summary>
                        if (ShipBeam[loop].currentCapacitor + beamCap > ShipBeam[loop].beamDef.powerRequirement)
                        {
                            ShipBeam[loop].currentCapacitor = ShipBeam[loop].beamDef.powerRequirement;

                            PowerRecharge = PowerRecharge - ((float)ShipBeam[loop].beamDef.powerRequirement - ShipBeam[loop].currentCapacitor);
                        }
                        /// <summary>
                        /// This beam can be partially recharged.
                        /// </summary>
                        else
                        {
                            ShipBeam[loop].currentCapacitor = (ushort)(ShipBeam[loop].currentCapacitor + beamCap);
                            PowerRecharge = PowerRecharge - beamCap;
                        }
                    }

                    /// <summary>
                    /// Recharge the turrets.
                    /// </summary>
                    for (int loop = 0; loop < ShipTurret.Count; loop++)
                    {
                        ushort beamCap = (ushort)(ShipTurret[loop].turretDef.baseBeamWeapon.weaponCapacitor * ShipTurret[loop].turretDef.multiplier * amt);
                        if (ShipTurret[loop].currentCapacitor + beamCap > ShipTurret[loop].turretDef.powerRequirement)
                        {
                            ShipTurret[loop].currentCapacitor = ShipTurret[loop].turretDef.powerRequirement;

                            PowerRecharge = PowerRecharge - ((float)ShipTurret[loop].turretDef.powerRequirement - ShipTurret[loop].currentCapacitor);
                        }
                        else
                        {
                            ShipTurret[loop].currentCapacitor = ShipTurret[loop].currentCapacitor + beamCap;
                            PowerRecharge = PowerRecharge - beamCap;
                        }
                    }

                    /// <summary>
                    /// return leftover power. leftover power means that all recharges are complete, which tells the simulation to remove this ship from the recharge list.
                    /// </summary>
                    return (int)PowerRecharge;
                }
                /// <summary>
                /// All beams may not be recharged.
                /// </summary>
                else
                {
                    float AvailablePower = PowerRecharge;

                    /// <summary>
                    /// Regular beams are recharged 1st, not turrets. this is a tough decision, there are pros and cons to any recharge scheme, mainly though this is the easiest and
                    /// least effort requiring way to do this.
                    /// </summary>
                    for (int loop = 0; loop < ShipBeam.Count; loop++)
                    {
                        float WeaponPowerRequirement = (float)ShipBeam[loop].beamDef.powerRequirement - ShipBeam[loop].currentCapacitor;
                        ushort beamCap = (ushort)(ShipBeam[loop].beamDef.weaponCapacitor * amt);

                        /// <summary>
                        /// This weapon can be fully recharged.
                        /// </summary>
                        if (AvailablePower > beamCap)
                        {
                            if (ShipBeam[loop].currentCapacitor + beamCap > ShipBeam[loop].beamDef.powerRequirement)
                            {
                                AvailablePower = AvailablePower - (ShipBeam[loop].beamDef.powerRequirement - ShipBeam[loop].currentCapacitor);
                                ShipBeam[loop].currentCapacitor = ShipBeam[loop].beamDef.powerRequirement;
                            }
                            else
                            {
                                ShipBeam[loop].currentCapacitor = (ushort)(ShipBeam[loop].currentCapacitor + (ushort)beamCap);
                                AvailablePower = AvailablePower - beamCap;
                            }
                        }
                        /// <summary>
                        /// This weapon can be partially recharged, and we're finished recharging weapons.
                        /// </summary>
                        else
                        {
                            /// <summary>
                            /// This weapon was fully recharged as it had power in the capacitor, and didn't require a full beamCap.
                            /// All power may be consumed here, that will be caught on the next go around of the loop.
                            /// </summary>
                            if (ShipBeam[loop].currentCapacitor + AvailablePower > ShipBeam[loop].beamDef.powerRequirement)
                            {
                                AvailablePower = AvailablePower - (ShipBeam[loop].beamDef.powerRequirement - ShipBeam[loop].currentCapacitor);
                                ShipBeam[loop].currentCapacitor = ShipBeam[loop].beamDef.powerRequirement;
                            }
                            /// <summary>
                            /// All power consumed.
                            /// </summary>
                            else
                            {
                                ShipBeam[loop].currentCapacitor = (ushort)(ShipBeam[loop].currentCapacitor + (ushort)AvailablePower);
                                AvailablePower = 0;

                                /// <summary>
                                /// No more beams will be recharged so return here. Turret recharge did not even run.
                                /// </summary>
                                return (int)AvailablePower;
                            }
                        }
                    }

                    /// <summary>
                    /// recharge the turrets, not all of these will finish.
                    /// </summary>
                    for (int loop = 0; loop < ShipTurret.Count; loop++)
                    {
                        float WPR = (float)ShipTurret[loop].turretDef.powerRequirement - ShipTurret[loop].currentCapacitor;
                        ushort beamCap = (ushort)(ShipTurret[loop].turretDef.baseBeamWeapon.weaponCapacitor * ShipTurret[loop].turretDef.multiplier * amt);

                        /// <summary>
                        /// Power is available to recharge this turret.
                        /// </summary>
                        if (AvailablePower > beamCap)
                        {
                            /// <summary>
                            /// Completely
                            /// </summary>
                            if (ShipTurret[loop].currentCapacitor + beamCap > ShipTurret[loop].turretDef.powerRequirement)
                            {
                                AvailablePower = AvailablePower - (ShipTurret[loop].turretDef.powerRequirement - ShipTurret[loop].currentCapacitor);
                                ShipTurret[loop].currentCapacitor = ShipTurret[loop].turretDef.powerRequirement;
                            }
                            /// <summary>
                            /// Partially
                            /// </summary>
                            else
                            {
                                ShipTurret[loop].currentCapacitor = ShipTurret[loop].currentCapacitor + beamCap;
                                AvailablePower = AvailablePower - beamCap;
                            }
                        }
                        /// <summary>
                        /// Power is not available to completely recharge this turret.
                        /// </summary>
                        else
                        {
                            /// <summary>
                            /// Thanks to power in the cap, this turret does not require all AvailablePower to recharge. AP may be 0 now, but that will be caught next loop iteration.
                            /// </summary>
                            if (ShipTurret[loop].currentCapacitor + AvailablePower > ShipTurret[loop].turretDef.powerRequirement)
                            {
                                AvailablePower = AvailablePower - (ShipTurret[loop].turretDef.powerRequirement - ShipTurret[loop].currentCapacitor);
                                ShipTurret[loop].currentCapacitor = ShipTurret[loop].turretDef.powerRequirement;
                            }
                            /// <summary>
                            /// There is not enough power in the cap to add to AvailablePower to completely recharge this beam.
                            /// </summary>
                            else
                            {
                                ShipTurret[loop].currentCapacitor = ShipTurret[loop].currentCapacitor + AvailablePower;
                                AvailablePower = 0;

                                /// <summary>
                                /// no more recharges can be done so return.
                                /// </summary>
                                return (int)AvailablePower;
                            }
                        }
                    }

                    return (int)AvailablePower;
                }
            }
            return (int)PowerRecharge;
        }

        /// <summary>
        /// This function iterates through every fire control, and orders them to attempt to fire.
        /// Sensor detection should be run before this every tick, or else we don't know if we can target a particular ship.
        /// Jump sickness will prevent firing.
        /// </summary>
        /// <param name="CurrentSecond">Tick the ship is ordered to fire.</param>
        /// <param name="RNG">RNG passed from further up the food chain since I can't generate random results except by having a "global" rng.</param>
        public bool ShipFireWeapons(Random RNG)
        {
            /// <summary>
            /// Can we perform this action or will jump sickness interfere?
            /// </summary>
            if (IsJumpSick())
                return false;

            bool fired = false;
            for (int loop = 0; loop < ShipBFC.Count; loop++)
            {
                if (ShipBFC[loop].openFire == true && ShipBFC[loop].isDestroyed == false && ShipBFC[loop].target != null)
                {
                    TargetTN BFCTarget = ShipBFC[loop].target;
                    if (BFCTarget.targetType == StarSystemEntityType.TaskGroup)
                    {
                        /// <summary>
                        /// Sanity Check. Make sure both are in the same system before checking distance.
                        /// </summary>
                        if (ShipsTaskGroup.Contact.Position.System == BFCTarget.ship.ShipsTaskGroup.Contact.Position.System)
                        {
                            float distance;
                            ShipsTaskGroup.Contact.DistTable.GetDistance(BFCTarget.ship.ShipsTaskGroup.Contact, out distance);

                            int track = ShipsFaction.BaseTracking;
                            if (CurrentSpeed > ShipsFaction.BaseTracking)
                            {
                                track = CurrentSpeed;
                            }

                            if (distance < Constants.Units.BEAM_AU_MAX)
                            {
                                float DistKM = (float)Distance.ToKm(distance);

                                fired = ShipBFC[loop].FireWeapons(DistKM, RNG, track, this);
                            }
                        }
                    }
                    else if (BFCTarget.targetType == StarSystemEntityType.Missile)
                    {
                        /// <summary>
                        /// Sanity Check. Make sure both are in the same system before checking distance.
                        /// </summary>
                        if (ShipsTaskGroup.Contact.Position.System == BFCTarget.missileGroup.contact.Position.System)
                        {
                            float distance;
                            ShipsTaskGroup.Contact.DistTable.GetDistance(BFCTarget.missileGroup.contact, out distance);

                            int track = ShipsFaction.BaseTracking;
                            if (CurrentSpeed > ShipsFaction.BaseTracking)
                            {
                                track = CurrentSpeed;
                            }

                            if (distance < Constants.Units.BEAM_AU_MAX)
                            {
                                float DistKM = (float)Distance.ToKm(distance);

                                fired = ShipBFC[loop].FireWeapons(DistKM, RNG, track, this);
                            }
                        }
                    }
                    else if (BFCTarget.targetType == StarSystemEntityType.Population)
                    {
                        /// <summary>
                        /// Sanity Check
                        /// </summary>
                        if (ShipsTaskGroup.Contact.Position.System == BFCTarget.pop.Planet.Position.System)
                        {
                            float distance;
                            ShipsTaskGroup.Contact.DistTable.GetDistance(BFCTarget.pop.Contact, out distance);

                            /// <summary>
                            /// Simple check to see if the maximum beam distance is exceeded by this attempt to fire.
                            if (distance < Constants.Units.BEAM_AU_MAX)
                            {
                                float DistKM = (float)Distance.ToKm(distance);

                                /// <summary>
                                /// Track doesn't matter for planets, but including it here since FireWeapons wants it.
                                /// </summary>
                                int track = ShipsFaction.BaseTracking;
                                if (CurrentSpeed > ShipsFaction.BaseTracking)
                                {
                                    track = CurrentSpeed;
                                }

                                fired = ShipBFC[loop].FireWeapons(DistKM, RNG, track, this);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Missile Fire Control Section
            /// </summary>
            for (int loop = 0; loop < ShipMFC.Count; loop++)
            {
                if (ShipMFC[loop].openFire == true && ShipMFC[loop].isDestroyed == false && ShipMFC[loop].target != null)
                {
                    TargetTN MFCTarget = ShipMFC[loop].target;

                    if (MFCTarget.targetType == StarSystemEntityType.TaskGroup)
                    {
                        /// <summary>
                        /// Sanity Check. Make sure both are in the same system before firing.
                        /// </summary>
                        if (ShipsTaskGroup.Contact.Position.System == MFCTarget.ship.ShipsTaskGroup.Contact.Position.System)
                        {
                            fired = ShipMFC[loop].FireWeapons(ShipsTaskGroup, this);
                        }
                    }
                    else if (MFCTarget.targetType == StarSystemEntityType.Missile)
                    {
                        /// <summary>
                        /// Sanity Check. Make sure both are in the same system before firing.
                        /// </summary>
                        if (ShipsTaskGroup.Contact.Position.System == MFCTarget.missileGroup.contact.Position.System)
                        {
                            fired = ShipMFC[loop].FireWeapons(ShipsTaskGroup, this);
                        }
                    }
                    else if (MFCTarget.targetType == StarSystemEntityType.Population)
                    {
                        /// <summary>
                        /// Sanity Check. Make sure both are in the same system before firing.
                        /// </summary>
                        if (ShipsTaskGroup.Contact.Position.System == MFCTarget.pop.Contact.Position.System)
                        {
                            fired = ShipMFC[loop].FireWeapons(ShipsTaskGroup, this);
                        }
                    }
                    else if (MFCTarget.targetType == StarSystemEntityType.Waypoint)
                    {
                        if (ShipsTaskGroup.Contact.Position.System == MFCTarget.wp.Position.System)
                        {
                            fired = ShipMFC[loop].FireWeapons(ShipsTaskGroup, this);
                        }
                    }
                }
            }
            return fired;
        }

        #endregion

        /// <summary>
        /// Sets the shields to the specified value
        /// </summary>
        /// <param name="Active">Whether shields are active(true), or inactive(false)</param>
        public void SetShields(bool Active)
        {
            if (ShipShield.Count != 0)
            {
                if (Active == false)
                {
                    /// <summary>
                    /// Remove either that this ship wants to recharge its shields, or the ship in total from the recharge list.
                    /// </summary>
                    if (ShipsFaction.RechargeList.ContainsKey(this) == true)
                    {
                        if ((ShipsFaction.RechargeList[this] & (int)Faction.RechargeStatus.Shields) == (int)Faction.RechargeStatus.Shields)
                        {
                            ShipsFaction.RechargeList[this] = ShipsFaction.RechargeList[this] - (int)Faction.RechargeStatus.Shields;

                            if (ShipsFaction.RechargeList[this] == 0)
                            {
                                ShipsFaction.RechargeList.Remove(this);
                            }
                        }
                    }
                }
                else if (Active == true)
                {
                    /// <summary>
                    /// Add to the recharge list.
                    /// </summary>
                    if (ShipsFaction.RechargeList.ContainsKey(this) == false)
                    {
                        ShipsFaction.RechargeList.Add(this, (int)Faction.RechargeStatus.Shields);
                    }
                    else 
                    {
                        if ((ShipsFaction.RechargeList[this] & (int)Faction.RechargeStatus.Shields) != (int)Faction.RechargeStatus.Shields)
                        {
                            ShipsFaction.RechargeList[this] = ShipsFaction.RechargeList[this] + (int)Faction.RechargeStatus.Shields;
                        }
                    }
                }

                /// <summary>
                /// What is the shield state before this order, and what is this shield being set to? If active state changes then the EM signature must be recalculated.
                /// </summary>
                if (ShieldIsActive == true && Active == false)
                {
                    /// <summary>
                    /// Recalculate the EM signature and resort the taskgroup ships based on EM as a result.
                    /// </summary>
                    CurrentEMSignature = CurrentEMSignature - (int)(CurrentShieldPoolMax * 30.0f);
                    ShipsTaskGroup.SortShipBySignature(EMList, ShipsTaskGroup.EMSortList, 1);
                }
                else if (ShieldIsActive == false && Active == true)
                {
                    /// <summary>
                    /// Recalculate the EM signature and resort the taskgroup ships based on EM as a result.
                    /// </summary>
                    CurrentEMSignature = CurrentEMSignature + (int)(CurrentShieldPoolMax * 30.0f);
                    ShipsTaskGroup.SortShipBySignature(EMList, ShipsTaskGroup.EMSortList, 1);
                }

                ShieldIsActive = Active;

                /// <summary>
                /// If the shields are down then zero the current shield pool.
                /// </summary>
                if (ShieldIsActive == false)
                {
                    CurrentShieldPool = 0.0f;
                }
            }
        }

        /// <summary>
        /// Recharges the ships shields, if they are active.
        /// </summary>
        /// <param name="tick">Tick is the value in seconds the sim is being advanced by. 1 day = 86400 seconds. smallest practical value is 5.</param>
        public void RechargeShields(uint tick)
        {
            if (ShieldIsActive == true && CurrentShieldPool != CurrentShieldPoolMax)
            {
                String Charge = "Shield Recharge: ";
                float amt = (float)tick / 5.0f;

                float ShieldRecharge = CurrentShieldGen * amt;

                if (CurrentShieldPool + ShieldRecharge >= CurrentShieldPoolMax)
                {
                    ShieldRecharge = CurrentShieldPoolMax - CurrentShieldPool;
                    CurrentShieldPool = CurrentShieldPoolMax;
                }
                else
                {
                    CurrentShieldPool = CurrentShieldPool + ShieldRecharge;
                }

                Charge = String.Format("{0} {1} points", Charge, ShieldRecharge);

                MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ShieldRecharge, this.ShipsTaskGroup.Contact.Position.System, this.ShipsTaskGroup.Contact, GameState.Instance.GameDateTime,
                                                     GameState.Instance.LastTimestep, Charge);

                ShipsFaction.MessageLog.Add(NMsg);
            }
        }

        #region Ordnance loading and unloading
        /// <summary>
        /// Loads ordnance to this ship from a population.
        /// </summary>
        /// <param name="pop">planetary stockpile to take missiles from</param>
        public void LoadOrdnance(Population pop)
        {
            if (ShipClass.PreferredOrdnanceSize == 0 || CurrentMagazineCapacity == ShipClass.PreferredOrdnanceSize)
            {
                return;
            }
            else
            {
                foreach (KeyValuePair<OrdnanceDefTN, int> SCPair in ShipClass.ShipClassOrdnance)
                {
                    OrdnanceSeriesTN Series = SCPair.Key.ordSeries;

                    if (ContainsMissileOfSeries(Series) == 0)
                    {
                        LoadMissileFromPopulation(Series, pop, SCPair.Value, 0);
                    }
                    else
                    {
                        foreach (KeyValuePair<OrdnanceDefTN, int> SOPair in ShipOrdnance)
                        {
                            if (SOPair.Key.ordSeries == Series)
                            {
                                if (SOPair.Value == SCPair.Value)
                                {
                                    /// <summary>
                                    /// This missile is loaded properly.
                                    /// </summary>
                                    break;
                                }
                                else
                                {
                                    /// <summary>
                                    /// need to load from the population.
                                    /// </summary>
                                    LoadMissileFromPopulation(Series, pop, SCPair.Value, SOPair.Value);

                                } //end else
                            } // end if SOPair.Key.ordSeries
                        } // end foreach
                    }


                }
            }
        }

        /// <summary>
        /// Does this ship have a missile of Series X?
        /// </summary>
        /// <param name="Series">Series to test against.</param>
        /// <returns>Number of missiles of that series.</returns>
        public int ContainsMissileOfSeries(OrdnanceSeriesTN Series)
        {
            if (ShipOrdnance.Count == 0)
            {
                return 0;
            }
            else
            {
                foreach (KeyValuePair<OrdnanceDefTN, int> SOPair in ShipOrdnance)
                {
                    if (SOPair.Key.ordSeries == Series)
                    {
                        return SOPair.Value;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Loads a missile from the specified series on Population pop into the ship.
        /// </summary>
        /// <param name="Series">Series of missile to search through</param>
        /// <param name="pop">Population stockpile.</param>
        /// <param name="SCValue">ShipClass Preferred Ordnance #</param>
        /// <param name="SOValue">Current Ship Ordnance #</param>
        public void LoadMissileFromPopulation(OrdnanceSeriesTN Series, Population pop, int SCValue, int SOValue)
        {
            for (int loop = (Series.missilesInSeries.Count - 1); loop >= 0; loop--)
            {
                if (pop.MissileStockpile.ContainsKey(Series.missilesInSeries[loop]))
                {
                    int SpaceAvailable = CurrentMagazineCapacityMax - CurrentMagazineCapacity;
                    int MissileReq = ((SCValue - SOValue) * (int)Math.Ceiling(Series.missilesInSeries[loop].size));


                    if (MissileReq <= SpaceAvailable)
                    {
                        /// <summary>
                        /// Unload missiles from Stockpile. MissilesAvailable will be less than or equal to MissileReq.
                        /// MissilesAvailable is the number of missiles.
                        /// AvailableSizeTotal is the total size of the available missiles.
                        /// missileSpaceReq is the number of missiles that will fit in the remaining space available.
                        /// </summary>
                        int MissilesAvailable = (int)Math.Floor(pop.LoadMissileToStockpile(Series.missilesInSeries[loop], (float)(MissileReq * -1.0f)));
                        int missileSpaceReq = (int)Math.Floor((double)SpaceAvailable / Math.Ceiling(Series.missilesInSeries[loop].size));
                        int load = 0;
                        if (missileSpaceReq >= MissilesAvailable)
                            load = MissilesAvailable;
                        else
                            load = missileSpaceReq;

                        /// <summary>
                        /// Now load them to shipOrdnance.
                        /// </summary>
                        if (ShipOrdnance.ContainsKey(Series.missilesInSeries[loop]) == true)
                        {
                            ShipOrdnance[Series.missilesInSeries[loop]] = ShipOrdnance[Series.missilesInSeries[loop]] + load;
                        }
                        else
                        {
                            ShipOrdnance.Add(Series.missilesInSeries[loop], load);
                        }

                        CurrentMagazineCapacity = CurrentMagazineCapacity + load;

                        MissilesAvailable = MissilesAvailable - load;

                        if (MissilesAvailable > 0)
                        {
                            /// <summary>
                            /// Magazines are all filled and cannot accomodate remaining missiles. put the extras back in the stockpile.
                            /// </summary>
                            pop.LoadMissileToStockpile(Series.missilesInSeries[loop], MissilesAvailable);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unloads ordnance to a population.
        /// </summary>
        /// <param name="pop">planetary stockpile to put missiles in.</param>
        public void UnloadOrdnance(Population pop)
        {
            foreach (KeyValuePair<OrdnanceDefTN, int> SOPair in ShipOrdnance)
            {
                pop.LoadMissileToStockpile(SOPair.Key, SOPair.Value);
            }

            ShipOrdnance.Clear();
        }

        #endregion

        #region Point Defense and CIWS
        /// <summary>
        /// InterceptMissile tests to see whether a CIWS on this ship can shoot down an incoming missile or not.
        /// </summary>
        /// <param name="RNG">Should be a global RNG</param>
        /// <param name="MissileSpeed">Speed of incoming missile.</param>
        /// <returns>Whether or not intercept happened</returns>
        public bool InterceptMissile(Random RNG, OrdnanceTN Ordnance)
        {
            /// <summary>
            /// Can we perform this action or will jump sickness interfere?
            /// </summary>
            if (IsJumpSick())
                return false;

            /// <summary>
            /// Personal Point defense(CIWS/FDF(Self)/FDF) here
            /// </summary>

            /// <summary>
            /// If this ship has CIWS, they are about to be fired, so add this ship to the faction recharge list.
            /// </summary>
            if (ShipCIWS.Count != 0)
            {
                if (ShipsFaction.RechargeList.ContainsKey(this) == true)
                {
                    /// <summary>
                    /// If our recharge value does not have CIWS in it(bitflag 8 for now), then add it.
                    /// </summary>
                    if ((ShipsFaction.RechargeList[this] & (int)Faction.RechargeStatus.CIWS) != (int)Faction.RechargeStatus.CIWS)
                    {
                        ShipsFaction.RechargeList[this] = (ShipsFaction.RechargeList[this] + (int)Faction.RechargeStatus.CIWS);
                    }
                }
                else
                {
                    ShipsFaction.RechargeList.Add(this, (int)Faction.RechargeStatus.CIWS);
                }
            }
            bool Intercept = false;
            for (int loop2 = ShipCIWSIndex; loop2 < ShipCIWS.Count; loop2++)
            {
                Intercept = ShipCIWS[loop2].InterceptTarget(RNG, Ordnance.missileDef.maxSpeed, ShipsFaction,
                            ShipsTaskGroup.Contact);

                if (Intercept == true)
                {
                    ShipCIWSIndex = loop2;
                    return true;
                }
            }

            if (Intercept == false)
            {
                for (int loop2 = 0; loop2 < ShipBFC.Count; loop2++)
                {
                    if (ShipBFC[loop2].pDState == PointDefenseState.FinalDefensiveFireSelf)
                    {
                        /// <summary>
                        /// Now I need to know whether the beam weapons linked to this PD can fire. for regular beams that is simple enough.
                        /// everything capable of multifire on the other hand is another matter. Gauss, railguns, and turrets will all have multiple shots, and they have to be
                        /// given the opportunity to use those shots against different missiles. I could do a similar thing to ShipCIWSIndex for BFCs but will refrain from doing so for the moment.
                        /// </summary>
                        bool WF = false;
                        Intercept = ShipBFC[loop2].InterceptTarget(RNG, 0, Ordnance, ShipsFaction, ShipsTaskGroup.Contact, this, out WF);

                        if (WF == true)
                        {
                            if (ShipsFaction.RechargeList.ContainsKey(this) == true)
                            {
                                /// <summary>
                                /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                /// </summary>
                                if ((ShipsFaction.RechargeList[this] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                {
                                    ShipsFaction.RechargeList[this] = (ShipsFaction.RechargeList[this] + (int)Faction.RechargeStatus.Weapons);
                                }
                            }
                            else
                            {
                                ShipsFaction.RechargeList.Add(this, (int)Faction.RechargeStatus.Weapons);
                            }
                        }

                        if (Intercept == true)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// reset all the CIWS to be able to fire again.
        /// </summary>
        /// <returns>How many shots were expended, this is important for removing this ship from the recharge list.</returns>
        public int RechargeCIWS()
        {
            ShipCIWSIndex = 0;

            int TotalExpendedShots = 0;

            for (int loop = 0; loop < ShipCIWS.Count; loop++)
            {
                TotalExpendedShots = TotalExpendedShots + ShipCIWS[loop].shotsExpended;
                ShipCIWS[loop].RechargeCIWS();
            }

            return TotalExpendedShots;
        }
        #endregion

        #region Ship jump functionality
        /// <summary>
        /// Reduce the jump sickness of this ship. if it is zero the ship is no longer sick
        /// </summary>
        /// <param name="TimeValue">Time to reduce jump sickness by.</param>
        /// <returns>Whether or not jumpsickness was reduced. if this is false, this ship should be taken out of the jump sick recharge list</returns>
        public bool ReduceSickness(uint TimeValue)
        {
            if (JumpSickness == 0)
                return false;

            JumpSickness = JumpSickness - (int)TimeValue;
            if (JumpSickness < 0)
                JumpSickness = 0;

            return true;
        }

        /// <summary>
        /// Is this craft currently suffering from jump sickness?
        /// </summary>
        /// <returns>bool based on if condition is met(true = ship is jumpsick)</returns>
        public bool IsJumpSick()
        {
            if (JumpSickness > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Set Standard transit jump sickness
        /// </summary>
        public void StandardTransit()
        {
            JumpSickness = Constants.JumpEngineTN.StandardTransitPenalty;
        }

        /// <summary>
        /// Set Squadron transit jump sickness
        /// </summary>
        public void SquadronTransit()
        {
            JumpSickness = Constants.JumpEngineTN.SquadronTransitPenalty;
        }
        #endregion

        #region Ship detection setting and getting
        /// <summary>
        /// Is this ship detected this tick via thermal?
        /// </summary>
        /// <param name="FactionID">by which faction</param>
        /// <param name="tick">current second</param>
        /// <param name="year">current year</param>
        /// <returns>true = yes, false = no</returns>
        public bool IsDetectedThermal(int FactionID, int tick, int year)
        {
            if (ThermalDetection[FactionID] == tick && ThermalYearDetection[FactionID] == year)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Is this ship detected this tick via em?
        /// </summary>
        /// <param name="FactionID">by which faction</param>
        /// <param name="tick">current second</param>
        /// <param name="year">current year</param>
        /// <returns>true = yes, false = no</returns>
        public bool IsDetectedEM(int FactionID, int tick, int year)
        {
            if (EMDetection[FactionID] == tick && EMYearDetection[FactionID] == year)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Is this ship detected this tick via active?
        /// </summary>
        /// <param name="FactionID">by which faction</param>
        /// <param name="tick">current second</param>
        /// <param name="year">current year</param>
        /// <returns>true = yes, false = no</returns>
        public bool IsDetectedActive(int FactionID, int tick, int year)
        {
            if (ActiveDetection[FactionID] == tick && ActiveYearDetection[FactionID] == year)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set this ship as detected via thermal
        /// </summary>
        /// <param name="FactionID">faction detecting</param>
        /// <param name="tick">current second</param>
        /// <param name="year">current year</param>
        public void SetThermalDetection(int FactionID, int tick, int year)
        {
            ThermalDetection[FactionID] = tick;
            ThermalYearDetection[FactionID] = year;
        }

        /// <summary>
        /// Set this ship as detected via em
        /// </summary>
        /// <param name="FactionID">faction detecting</param>
        /// <param name="tick">current second</param>
        /// <param name="year">current year</param>
        public void SetEMDetection(int FactionID, int tick, int year)
        {
            EMDetection[FactionID] = tick;
            EMYearDetection[FactionID] = year;
        }

        /// <summary>
        /// Set this ship as detected via active. really should have made detectableEntity a class that ships, populations, and ordnance groups inherit from.
        /// </summary>
        /// <param name="FactionID">faction detecting</param>
        /// <param name="tick">current second</param>
        /// <param name="year">current year</param>
        public void SetActiveDetection(int FactionID, int tick, int year)
        {
            ActiveDetection[FactionID] = tick;
            ActiveYearDetection[FactionID] = year;
        }
        #endregion
    }
    /// <summary>
    /// End of ShipTN class
    /// </summary>
    /// 

    /// <summary>
    /// This is intended to be part of a larger dictionary that separates these by system. Component and Ship data is stored for every Point defense enabled FC here.
    /// moved from Faction.cs, for want of a better place to put it. 
    /// </summary>
    public class PointDefenseList
    {
        /// <summary>
        /// The component and which ship it is on.
        /// </summary>
        public Dictionary<ComponentTN, ShipTN> PointDefenseFC { get; set; }

        /// <summary>
        /// FCType of the base component, again, no pointer exists to the component def so this must be stored.
        /// as always, false = MFC, true = BFC.
        /// </summary>
        public Dictionary<ComponentTN, bool> PointDefenseType { get; set; }


        /// <summary>
        /// Constructor for PDList.
        /// </summary>
        public PointDefenseList()
        {
            PointDefenseFC = new Dictionary<ComponentTN, ShipTN>();
            PointDefenseType = new Dictionary<ComponentTN, bool>();
        }

        /// <summary>
        /// Handles adding a new FC to the list.
        /// </summary>
        /// <param name="Comp">Fire control component to add</param>
        /// <param name="Ship">Ship the FC is on.</param>
        /// <param name="Type">Type of FC.</param>
        public void AddComponent(ComponentTN Comp, ShipTN Ship, bool Type)
        {
            if (PointDefenseFC.ContainsKey(Comp) == false)
            {
                PointDefenseFC.Add(Comp, Ship);
            }

            if (PointDefenseType.ContainsKey(Comp) == false)
            {
                PointDefenseType.Add(Comp, Type);
            }
        }

        /// <summary>
        /// Handles removing an existing FC from the list.
        /// </summary>
        /// <param name="Comp">Fire control component to remove</param>
        /// <param name="Ship">Ship the FC is on.</param>
        /// <param name="Type">Type of FC.</param>
        public void RemoveComponent(ComponentTN Comp)
        {
            if (PointDefenseFC.ContainsKey(Comp) == true)
            {
                PointDefenseFC.Remove(Comp);
            }

            if (PointDefenseType.ContainsKey(Comp) == true)
            {
                PointDefenseType.Remove(Comp);
            }
        }
    }

}
