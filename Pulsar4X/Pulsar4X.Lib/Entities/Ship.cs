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
        /// How many additional berths this vessel has. negative numbers indicate overloaded crew quarters and chance of environmental failures.
        /// </summary>
        public int SpareBerths;

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
        /// Ships with CHS systems have a tractor multiplier that reduces load time for cargo, but these can be damaged.
        /// </summary>
        public int CurrentTractorMultiplier { get; set; }

        /// <summary>
        /// List of ship components for DAC/OnDamage/Wreck functionality
        /// </summary>
        public BindingList<ComponentTN> ShipComponents { get; set; }

        /// <summary>
        /// In shipclass is a member ListOfComponentDefs. This will store the starting index of each of these in ShipComponents.
        /// </summary>
        public BindingList<ushort> ComponentDefIndex { get; set; }

        /// <summary>
        /// List of destroyed components for damage control.
        /// </summary>
        public BindingList<ComponentTN> DestroyedComponents { get; set; }

        /// <summary>
        /// Remaining hit to kill of the ship.
        /// </summary>
        public int ShipHTK { get; set; }

        /// <summary>
        /// Component currently being repaired by damage control.
        /// </summary>
        public ComponentTN DamageControlTarget { get; set; }

        /// <summary>
        /// List of components to be repaired by damage control in the order specified by player. 0 first, count last.
        /// </summary>
        public BindingList<ComponentTN> DamageControlQue { get; set; }

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
            int index;

            /// <summary>
            /// Set the class definition
            /// </summary>
            ShipClass = ClassDefinition;

            /// <summary>
            /// Inform the class that it has a new member.
            /// </summary>
            ClassDefinition.ShipsInClass.Add(this);

            /// <summary>
            /// Make sure to initialize this important variable that everything uses.
            /// </summary>
            ShipComponents = new BindingList<ComponentTN>();

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
            DestroyedComponents = new BindingList<ComponentTN>();

            /// <summary>
            /// How much damage can the ship take?
            /// </summary>
            ShipHTK = ShipClass.TotalHTK;

            /// <summary>
            /// When the destroyed components list is populated it can be selected from to put components here to be repaired.
            /// </summary>
            DamageControlQue = new BindingList<ComponentTN>();

            /// <summary>
            /// Not yet set.
            /// </summary>
            DamageControlTarget = null;


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
                    ShipASensor.Add(ASensor);
                    ShipComponents.Add(ASensor);
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

        /// <summary>
        /// Damage goes through a 3 part process, 1st shields subtract damage, then armor blocks damage, then internals take the hits.
        /// if 20 rolls happen without an internal in the list being targeted then call OnDestroyed(); Mesons skip to the internal damage section.
        /// Microwaves do shield damage, then move to the special electronic only DAC.
        /// </summary>
        /// <param name="Type">Type of damage, for armor penetration.</param>
        /// <param name="Value">How much damage is being done.</param>
        /// <param name="HitLocation">Where Armor damage is inflicted. Temporary argument for the time being. remove these when rngs are resolved.</param>
        public void OnDamaged(DamageTypeTN Type, ushort Value, ushort HitLocation)
        {
            ushort Damage = Value;
            ushort internalDamage = 0;
            /// <summary>
            /// Handle Shield Damage.
            /// </summary>
            

            /// <summary>
            /// Armor Penetration.
            /// </summary>
            ushort Columns = ShipArmor.armorDef.cNum;
            short left, right;

            ushort ImpactLevel = ShipArmor.armorDef.depth;
            if(ShipArmor.isDamaged == true)
                ImpactLevel = ShipArmor.armorColumns[HitLocation];

            DamageTableTN Table;
            switch(Type)
            {
                case DamageTypeTN.Beam : Table = DamageValuesTN.EnergyTable[Damage-1];
                break;
                case DamageTypeTN.Kinetic : Table = DamageValuesTN.KineticTable[Damage-1];
                break;
                case DamageTypeTN.Missile : Table = DamageValuesTN.MissileTable[Damage-1];
                break;
                case DamageTypeTN.Plasma : Table = DamageValuesTN.PlasmaTable[Damage-1];
                break;
                default :
                    Table = DamageValuesTN.MissileTable[Damage-1];
                break;
            }
            left = (short)(HitLocation - 1);
            right = (short)(HitLocation + 1);
            internalDamage = (ushort)ShipArmor.SetDamage(Columns, ShipArmor.armorDef.depth, HitLocation, Table.damageTemplate[Table.hitPoint]);
            if (Type == DamageTypeTN.Plasma)
            {
                internalDamage = (ushort)((ushort)internalDamage + (ushort)ShipArmor.SetDamage(Columns, ShipArmor.armorDef.depth, (ushort)(HitLocation + 1), Table.damageTemplate[Table.hitPoint + 1]));
                right++;
            }

            for (int loop = 1; loop <= Table.halfSpread; loop++)
            {
                if (left < 0)
                {
                    left = (short)(Columns-1);
                }
                if (right >= Columns)
                {
                    right = 0;
                }

                /// <summary>
                /// side impact damage doesn't always reduce armor, the principle hitpoint should be the site of the deepest armor penetration. Damage can be wasted in this manner.
                /// </summary>
                if(ImpactLevel - Table.damageTemplate[Table.hitPoint - loop] < ShipArmor.armorColumns[left])
                    internalDamage = (ushort)((ushort)internalDamage + (ushort)ShipArmor.SetDamage(Columns, ShipArmor.armorDef.depth, (ushort)left, Table.damageTemplate[Table.hitPoint - loop]));
                if(ImpactLevel - Table.damageTemplate[Table.hitPoint + loop] < ShipArmor.armorColumns[right])
                    internalDamage = (ushort)((ushort)internalDamage + (ushort)ShipArmor.SetDamage(Columns, ShipArmor.armorDef.depth, (ushort)right, Table.damageTemplate[Table.hitPoint + loop]));

                left--;
                right++;
            }    
            

            /// <summary>
            /// Internal Component Damage. Each component with an HTK >0 can take atleast 1 hit. a random number is rolled over the entire dac. the selected component's HTK
            /// is tested against the internal damage value, and if greater than the damage value the component has a chance of surviving. otherwise, the component is destroyed, damage
            /// is reduced, and the next component is chosen.
            /// DAC Should be redone as a binary tree at some later date.
            /// </summary>
            int Attempts = 0;
            Random DacRNG = new Random(HitLocation);

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

                        destroy = (int)Math.Floor(((float)(DACHit - previousDAC)/(float)size));

                        /// <summary>
                        /// By this point total should definitely be >= destroy. destroy is the HS of the group being hit.
                        /// Should I try to find the exact component hit, or merely loop through all of them?
                        /// internalDamage: Damage done to all internals
                        /// destroy: component to destroy from shipClass.ListOfComponentDefs
                        /// ComponentDefIndex[loop] where in ShipComponents this definition group is.
                        /// </summary>

                        int DamageDone = DestroyComponent(ShipClass.ListOfComponentDefs[loop].componentType, loop, internalDamage, destroy, DacRNG);

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
                OnDestroyed();
            }
        }

        /// <summary>
        /// Destroy component handles destroying individual components of a ship, updating the ships data, and taskgroup data as needed.
        /// crew destruction and logging remain to be done.
        /// </summary>
        /// <param name="Type">Type of component destroyed</param>
        /// <param name="componentIndex">Where in the shipClass.ListOfComponentDefs this component resides</param>
        /// <param name="Damage">How much damage is to be applied in the attempt to destroy the component</param>
        /// <param name="ComponentIndex">Which </param>
        /// <param name="DacRNG"></param>
        /// <returns>Damage remaining after component destruction, or special message indicating no component to destroy(-1)/ship is totally destroyed(-2)</returns>
        public int DestroyComponent(ComponentTypeTN Type, int ComponentListDefIndex, int Damage, int ComponentIndex, Random DacRNG)
        {
            if (Damage >= ShipHTK)
                return -2;

            int ID = ComponentDefIndex[ComponentListDefIndex] + ComponentIndex;

            if(ShipComponents[ID].isDestroyed == true)
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
                    DamageReturn = Damage - ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk;
                }
                else
                {
                    int htkTest = DacRNG.Next(1, ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk);

                    if (htkTest == ShipClass.ListOfComponentDefs[ComponentListDefIndex].htk)
                    {
                        ShipComponents[ID].isDestroyed = true;
                        DamageReturn = 0;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            /// <summary>
            /// Copy the component over to the destroyed components list and decrement the ships remaining HTK value.
            DestroyedComponents.Add(ShipComponents[ID]);
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
                    CurrentMSPCapacity = CurrentMSPCapacity - (int)((float)ShipClass.BuildPointCost * (( MSP / ShipClass.SizeHS ) / 0.08f));
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
                    CurrentMaxEnginePower = CurrentMaxEnginePower - ShipEngine[0].engineDef.enginePower;
                    CurrentMaxThermalSignature = CurrentMaxThermalSignature - ShipEngine[0].engineDef.thermalSignature;
                    CurrentMaxFuelUsePerHour = CurrentMaxFuelUsePerHour - ShipEngine[0].engineDef.fuelUsePerHour;
                    CurrentMaxSpeed = (int)((1000.0f / (float)ShipClass.TotalCrossSection) * (float)CurrentMaxEnginePower);
                    if (CurrentSpeed > CurrentMaxSpeed)
                        SetSpeed(CurrentMaxSpeed);
                break;

                case ComponentTypeTN.PassiveSensor:
                    /// <summary>
                    /// Performance could be improved here by storing a sorted linked list of all passive sensors if need be.
                    /// I don't believe that sensor destruction events will be common enough to necessitate that however.
                    /// </summary>
                    if (ShipPSensor[ShipComponents[ID].componentIndex].pSensorDef.thermalOrEM == PassiveSensorType.EM)
                    {
                        if (ShipPSensor[ShipComponents[ID].componentIndex].pSensorDef.rating == BestEMRating)
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
                                            if (ShipsTaskGroup.BestEMCount == 0 || ShipsTaskGroup.Ships[loop].ShipPSensor[loop2].pSensorDef.rating > ShipsTaskGroup.BestEM.pSensorDef.rating )
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
                        if (ShipPSensor[ShipComponents[ID].componentIndex].pSensorDef.rating == BestThermalRating)
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
            }
            return DamageReturn;
        }

        public void RepairComponent(ComponentTypeTN Type, int ComponentIndex)
        {
            /// <summary>
            /// Subtract MSPs in damage control function, not necessary here.
            /// </summary>
            /// 
            ShipComponents[ComponentIndex].isDestroyed = false;
            switch(Type)
            {
                case ComponentTypeTN.Crew :
                    SpareBerths = SpareBerths + (int)(CrewQuarters[ShipComponents[ComponentIndex].componentIndex].genCompDef.size / ShipClass.TonsPerMan);
                break;

                case ComponentTypeTN.Fuel :
                    CurrentFuelCapacity = CurrentFuelCapacity + (FuelTanks[ShipComponents[ComponentIndex].componentIndex].genCompDef.size * 50000.0f);
                break;

                case ComponentTypeTN.Engineering :
                    float MSP = EngineeringBays[ShipComponents[ComponentIndex].componentIndex].genCompDef.size;
                    CurrentMSPCapacity = CurrentMSPCapacity + (int)((float)ShipClass.BuildPointCost * (( MSP / ShipClass.SizeHS ) / 0.08f));
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
                    CurrentMaxEnginePower = CurrentMaxEnginePower + ShipEngine[0].engineDef.enginePower;
                    CurrentMaxThermalSignature = CurrentMaxThermalSignature + ShipEngine[0].engineDef.thermalSignature;
                    CurrentMaxFuelUsePerHour = CurrentMaxFuelUsePerHour + ShipEngine[0].engineDef.fuelUsePerHour;
                    CurrentMaxSpeed = (int)((1000.0f / (float)ShipClass.TotalCrossSection) * (float)CurrentMaxEnginePower);
                        SetSpeed(CurrentMaxSpeed);
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
            }
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


        /// <summary>
        /// Links specified weapon to the selected BFC. Sanity check to make sure both are on the same ship?
        /// </summary>
        /// <param name="BFC">Beam Fire Controller</param>
        /// <param name="Weapon">Beam Weapon</param>
        public void LinkWeaponToBeamFC(BeamFireControlTN BFC, BeamTN Weapon)
        {
            if (BFC.linkedWeapons.IndexOf(Weapon) == -1)
            {
                BFC.linkedWeapons.Add(Weapon);
                Weapon.fireController = BFC;
            }
        }
    }
    /// <summary>
    /// End of ShipTN class
    /// </summary>
}
