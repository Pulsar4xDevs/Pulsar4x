using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;




namespace Pulsar4X.Entities
{
    public class ShipTN : GameEntity
    {
        public Faction Faction { get; set; }

        /// <summary>
        /// Class of this ship.
        /// </summary>
        public ShipClassTN ShipClass { get; set; }

        /// <summary>
        /// Taskgroup the ship is part of.
        /// </summary>
        public TaskGroupTN ShipsTaskGroup { get; set; }

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
        public int CurrentCrew;

        /// <summary>
        /// How many crew/POWs are in cryo stasis.
        /// </summary>
        public int CurrentCryoStorage;

        /// <summary>
        /// How long has the ship been out on patrol. 1.0 = Max deployment time.
        /// </summary>
        public float CurrentDeployment;

        /// <summary>
        /// Fuel Tanks, Small Fuel Tanks, Tiny Fuel Tanks, Large Fuel Tanks, Very Large Fuel Tanks, Ultra Large Fuel Tanks.
        /// </summary>
        public BindingList<GeneralComponentTN> FuelTanks { get; set; }

        /// <summary>
        /// How much fuel in total does the ship have. it will be divided equally among all tanks for convenience.
        /// </summary>
        public float CurrentFuel { get; set; }

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
        /// Engine related ship statistics. Maximum values are in ship class.
        /// </summary>
        public int CurrentEnginePower { get; set; }
        public int CurrentThermalSignature { get; set; }
        public int CurrentSpeed { get; set; }
        public float CurrentFuelUsePerHour { get; set; }

        /// <summary>
        /// Ships can have several types of cargo holds and multiple of each.
        /// </summary>
        public BindingList<CargoTN> ShipCargo { get; set; }

        /// <summary>
        /// Just how much is this specific ship holding.
        /// </summary>
        public int CurrentCargoTonnage{ get; set; }

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
        /// Each ship will store its placement in the overall taskgroup.
        /// </summary>
        public LinkedListNode<int> ThermalList;
        public LinkedListNode<int> EMList;
        public LinkedListNode<int> ActiveList;


        /// <summary>
        /// ShipTN creates a ship of classDefinition in Index ShipIndex for the taskgroup ship list.
        /// </summary>
        /// <param name="ClassDefinition">Definition of the ship.</param>
        /// <param name="ShipIndex">Its index within the shiplist of the taskgroup.</param>
        public ShipTN(ShipClassTN ClassDefinition, int ShipIndex, int CurrentTimeSlice)
        {
            /// <summary>
            /// Set the class definition
            /// </summary>
            ShipClass = ClassDefinition;

            /// <summary>
            /// Inform the class that it has a new member.
            /// </summary>
            ClassDefinition.ShipsInClass.Add(this);

            /// <summary>
            /// All ships will have armor, and all ship defs should have armor before this point.
            /// </summary.
            ShipArmor = new ArmorTN(ClassDefinition.ShipArmorDef);

            /// <summary>
            /// Crew Quarters don't strictly have to be present, but will be in almost all designs.
            /// </summary>
            CrewQuarters = new BindingList<GeneralComponentTN>();
            AddComponents(CrewQuarters, ClassDefinition.CrewQuarters, ClassDefinition.CrewQuartersCount);
            CurrentCrew = 0;
            CurrentCryoStorage = 0;
            CurrentDeployment = 0.0f;

            /// <summary>
            ///Fuel Tanks don't have to be present, but will be in most designs.
            /// </summary>
            FuelTanks = new BindingList<GeneralComponentTN>();
            AddComponents(FuelTanks, ClassDefinition.FuelTanks, ClassDefinition.FuelTanksCount);
            CurrentFuel = 0.0f;
            FuelCounter = 0;

            /// <summary>
            /// Engineering spaces must be on civ designs(atleast 1), but can be absent from military designs.
            /// </summary>
            EngineeringBays = new BindingList<GeneralComponentTN>();
            AddComponents(EngineeringBays, ClassDefinition.EngineeringBays, ClassDefinition.EngineeringBaysCount);
            CurrentDamageControlRating = ClassDefinition.MaxDamageControlRating;
            CurrentMSP = ShipClass.TotalMSPCapacity;

            /// <summary>
            /// All remaining components that are of a more specialized nature. These do not have to be present, except bridges on ships bigger than 1K tons.
            /// </summary>
            OtherComponents = new BindingList<GeneralComponentTN>();
            AddComponents(OtherComponents, ClassDefinition.OtherComponents, ClassDefinition.OtherComponentsCount);

            /// <summary>
            /// All mobile ships need engines, orbitals and PDCs don't however.
            /// </summary>
            ShipEngine = new BindingList<EngineTN>();
            for (int loop = 0; loop < ClassDefinition.ShipEngineCount; loop++)
            {
                EngineTN Engine = new EngineTN(ClassDefinition.ShipEngineDef);
                Engine.componentIndex = ShipEngine.Count;
                ShipEngine.Add(Engine);
            }
            CurrentEnginePower = ClassDefinition.MaxEnginePower;
            CurrentThermalSignature = ClassDefinition.MaxThermalSignature;
            CurrentSpeed = ClassDefinition.MaxSpeed;
            CurrentFuelUsePerHour = ClassDefinition.MaxFuelUsePerHour;


            /// <summary>
            /// Usually only cargo ships and salvagers will have cargo holds.
            /// </summary>
            ShipCargo = new BindingList<CargoTN>();
            for (int loop = 0; loop < ClassDefinition.ShipCargoDef.Count; loop++)
            {
                for (int loop2 = 0; loop2 < ClassDefinition.ShipCargoCount[loop]; loop2++)
                {
                    CargoTN cargo = new CargoTN(ClassDefinition.ShipCargoDef[loop]);
                    cargo.componentIndex = ShipCargo.Count;
                    ShipCargo.Add(cargo);
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
                for (int loop2 = 0; loop2 < ClassDefinition.ShipColonyCount[loop]; loop2++)
                {
                    ColonyTN colony = new ColonyTN(ClassDefinition.ShipColonyDef[loop]);
                    colony.componentIndex = ShipColony.Count;
                    ShipColony.Add(colony);
                }
            }
            CurrentCryoStorage = 0;

            /// <summary>
            /// Any ship with cargo holds, troop bays, cryo berths, or drop pods will benefit from a cargohandling system. though droppods benefit from the CHSes on other vessels as well.
            /// </summary>
            ShipCHS = new BindingList<CargoHandlingTN>();
            for (int loop = 0; loop < ClassDefinition.ShipCHSDef.Count; loop++)
            {
                for (int loop2 = 0; loop2 < ClassDefinition.ShipCHSCount[loop]; loop2++)
                {
                    CargoHandlingTN CHS = new CargoHandlingTN(ClassDefinition.ShipCHSDef[loop]);
                    CHS.componentIndex = ShipCHS.Count;
                    ShipCHS.Add(CHS);
                }
            }



            /// <summary>
            /// Every ship will have a passive sensor rating, but very few will have specialized passive sensors.
            /// </summary>
            ShipPSensor = new BindingList<PassiveSensorTN>();
            for (int loop = 0; loop < ClassDefinition.ShipPSensorDef.Count; loop++)
            {
                for (int loop2 = 0; loop2 < ClassDefinition.ShipPSensorCount[loop]; loop2++)
                {
                    PassiveSensorTN PSensor = new PassiveSensorTN(ClassDefinition.ShipPSensorDef[loop]);
                    PSensor.componentIndex = ShipPSensor.Count;
                    ShipPSensor.Add(PSensor);
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
                for (int loop2 = 0; loop2 < ClassDefinition.ShipASensorCount[loop]; loop2++)
                {
                    ActiveSensorTN ASensor = new ActiveSensorTN(ClassDefinition.ShipASensorDef[loop]);
                    ASensor.componentIndex = ShipASensor.Count;
                    ShipASensor.Add(ASensor);
                }
            }
            /// <summary>
            /// This won't change, but it should be here for convenience during sensor sweeps.
            /// </summary>
            TotalCrossSection = ClassDefinition.TotalCrossSection;
            CurrentEMSignature = 0;

            ThermalList = new LinkedListNode<int>(ShipIndex);
            EMList = new LinkedListNode<int>(ShipIndex);
            ActiveList = new LinkedListNode<int>(ShipIndex);

            ThermalDetection = new BindingList<int>();
            EMDetection = new BindingList<int>();
            ActiveDetection = new BindingList<int>();

            for (int loop = 0; loop < Constants.Faction.FactionMax; loop++)
            {
                ThermalDetection.Add(CurrentTimeSlice);
                EMDetection.Add(CurrentTimeSlice);
                ActiveDetection.Add(CurrentTimeSlice);
            }

            ShipCommanded = false;
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
                
                for (int loop2 = 0; loop2 < countList[loop]; loop2++)
                {
                    GeneralComponentTN NewComponent = new GeneralComponentTN(fromList[loop]);
                    NewComponent.componentIndex = AddList.Count;
                    AddList.Add(NewComponent);
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
            }
            else
            {
                CurrentCrew = CurrentCrew + CrewAvailable;
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
        public void SetSensor(ActiveSensorTN Sensor,bool active)
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
        public void SetSpeed(int Speed)
        {
            if (ShipClass.MaxSpeed < Speed)
                CurrentSpeed = ShipClass.MaxSpeed;
            else
                CurrentSpeed = Speed;

            float fraction = (float)CurrentSpeed / (float)ShipClass.MaxSpeed;

            CurrentEnginePower = (int)((float)ShipClass.MaxEnginePower * fraction);
            CurrentThermalSignature = (int)((float)ShipClass.MaxThermalSignature * fraction);
            CurrentFuelUsePerHour = ShipClass.MaxFuelUsePerHour * fraction;
        }

        /// <summary>
        /// Handle the consequences of a ship destruction.
        /// Class, Taskgroup, and the new wreck all need to be dealt with.
        /// </summary>
        public void OnDestroyed()
        {
            ShipClass.ShipsInClass.Remove(this);
            ShipsTaskGroup.Ships.Remove(this);

            /// <summary>
            /// A new wreck needs to be created with the surviving components, if any, and some fraction of the cost of the ship.
            /// </summary>
        }


    }
    /// <summary>
    /// End of ShipTN class
    /// </summary>
}
