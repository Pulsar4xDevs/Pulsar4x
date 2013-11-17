using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;
using System.Drawing;

namespace Pulsar4X.Entities
{
    public class FactionContact
    {
        /// <summary>
        /// The detected ship.
        /// </summary>
        public ShipTN Ship{ get; set; }

        /// <summary>
        /// Detected via thermal.
        /// </summary>
        public bool thermal { get; set; }

        /// <summary>
        /// Tick thermal detect event happened on.
        /// </summary>
        public uint thermalTick { get; set; }


        /// <summary>
        /// Detected via EM.
        /// </summary>
        public bool EM { get; set; }

        /// <summary>
        /// Tick detected via EM.
        /// </summary>
        public uint EMTick { get; set; }

        /// <summary>
        /// Detected via Actives.
        /// </summary>
        public bool active { get; set; }

        /// <summary>
        /// Tick active detection event occurred on.
        /// </summary>
        public uint activeTick { get; set; }

        /// <summary>
        /// Initializer for detected ship event. FactionContact is the detector side of what is detected, while ShipTN itself stores the detectee side. 
        /// multiple of these can exist, but only 1 per faction hopefully.
        /// </summary>
        /// <param name="DetectedShip">Ship detected.</param>
        /// <param name="Thermal">Was the detection thermal based?</param>
        /// <param name="em">Detection via EM?</param>
        /// <param name="Active">Active detection?</param>
        /// <param name="tick">What tick did this detection event occur on?</param>
        public FactionContact(ShipTN DetectedShip, bool Thermal, bool em, bool Active, uint tick)
        {
            thermal = Thermal;
            EM = em;
            active = Active;

            if (thermal == true)
                thermalTick = tick;

            if (EM == true)
                EMTick = tick;

            if (active == true)
                activeTick = tick;
        }

        /// <summary>
        /// Each tick every faction contact should be updated on the basis of collected sensor data.
        /// </summary>
        /// <param name="Thermal">Thermal detection?</param>
        /// <param name="Em">Detected on EM?</param>
        /// <param name="Active">Detected by actives?</param>
        /// <param name="tick">Current tick.</param>
        public void updateFactionContact(bool Thermal, bool Em, bool Active, uint tick)
        {
            if (thermal == false && Thermal == true)
            {
                /// <summary>
                /// New thermal detection event, message logic should be here.
                /// </summary>
                thermalTick = tick;
            }
            else if (thermal == true && Thermal == false)
            {
                /// <summary>
                /// Thermal contact lost.
                /// </summary>
            }

            if (EM == false && Em == true)
            {
                /// <summary>
                /// New EM detection event, message logic should be here.
                /// </summary>
                EMTick = tick;
            }
            if (EM == true && Em == false)
            {
                /// <summary>
                /// EM contact lost.
                /// </summary>
            }

            if (active == false && Active == true)
            {
                /// <summary>
                /// New active detection event, message logic should be here.
                /// </summary>
                activeTick = tick;
            }
            if (active == true && Active == false)
            {
                /// <summary>
                /// Active contact lost.
                /// </summary>
            }


            thermal = Thermal;
            EM = Em;
            active = Active;
        }


    }

    public class FactionSystemDetection
    {
        /// <summary>
        /// Faction that owns this list.
        /// </summary>
        public Faction Faction { get; set; }

        /// <summary>
        /// The system these contacts are stored for.
        /// </summary>
        public StarSystem System { get; set; }

        /// <summary>
        /// Last time this contact index was spotted via thermals.
        /// </summary>
        public BindingList<int> Thermal { get; set; }

        /// <summary>
        /// Last time this contact index was spotted via EM.
        /// </summary>
        public BindingList<int> EM { get; set; }

        /// <summary>
        /// Last time this contact index was spotted via Active.
        /// </summary>
        public BindingList<int> Active { get; set; }

        /// <summary>
        /// Creates a faction contact list for the specified system.
        /// </summary>
        /// <param name="system">System indicated.</param>
        public FactionSystemDetection(Faction Fact,StarSystem system)
        {
            Faction = Fact;
            System = system;

            Thermal = new BindingList<int>();
            EM = new BindingList<int>();
            Active = new BindingList<int>();

            Thermal.RaiseListChangedEvents = false;
            EM.RaiseListChangedEvents = false;
            Active.RaiseListChangedEvents = false;

            for (int loop = 0; loop < system.SystemContactList.Count; loop++)
            {
                Thermal.Add(0);
                EM.Add(0);
                Active.Add(0);
            }

            Thermal.RaiseListChangedEvents = true;
            EM.RaiseListChangedEvents = true;
            Active.RaiseListChangedEvents = true;

            Thermal.ResetBindings();
            EM.ResetBindings();
            Active.ResetBindings();
        }

        /// <summary>
        /// pushes a contact onto the end of the list since that is where all new contacts will be added.
        /// </summary>
        public void AddContact()
        {
            Thermal.Add(0);
            EM.Add(0);
            Active.Add(0);
        }

        /// <summary>
        /// Removes a contact at the specified index.
        /// </summary>
        /// <param name="RemIndex">index to be removed.</param>
        public void RemoveContact(int RemIndex)
        {
            Thermal.RemoveAt(RemIndex);
            EM.RemoveAt(RemIndex);
            Active.RemoveAt(RemIndex);
        }
    }

    public class MessageEntry
    {
        /// <summary>
        /// which starsystem does this message occur in.
        /// </summary>
        public StarSystem Location { get; set; }

        /// <summary>
        /// Which tg/planet/pop are we referencing?
        /// </summary>
        public StarSystemEntity entity { get; set; }

        /// <summary>
        /// When was this message sent?
        /// </summary>
        public DateTime TimeOfMessage { get; set; }

        /// <summary>
        /// How long since the last time increment?
        /// </summary>
        
        public int TimeSlice { get; set; }
        /// <summary>
        /// Text of the message for the log.
        /// </summary>
        public String Text { get; set; }


        /// <summary>
        /// MessageEntry constructs a specific message with the relevant location, time, and entity(if applicable). 
        /// </summary>
        /// <param name="Loc">Starsystem message is from.</param>
        /// <param name="Ref">Starsystementity the message refers to.</param>
        /// <param name="Time">Game time of message.</param>
        /// <param name="timeSlice">Time since last increment.</param>
        /// <param name="text">text of the message.</param>
        public MessageEntry(StarSystem Loc, StarSystemEntity Ref, DateTime Time, int timeSlice, string text)
        {
            Location = Loc;
            entity = Ref;
            TimeOfMessage = Time;
            TimeSlice = timeSlice;
            Text = text;
        }

    }


    public class Faction : GameEntity
    {
        /// <summary>
        /// Factions have to know their own ID. this should be a 0 based index of the factionList where ever that ends up being.
        /// </summary>
        public int FactionID;

        public string Title { get; set; }
        public Species Species { get; set; }

        public FactionTheme FactionTheme { get; set; }
        public FactionCommanderTheme CommanderTheme { get; set; }

        public BindingList<StarSystem> KnownSystems { get; set; }
        public BindingList<TaskForce> TaskForces { get; set; }
        public BindingList<Commander> Commanders { get; set; }
        public BindingList<Population> Populations { get; set; }

        /// <summary>
        /// List of all components the faction has defined.
        /// </summary>
        public ComponentDefListTN ComponentList { get; set; }
        
        /// <summary>
        /// The faction's ship designs are stored here.
        /// </summary>
        public BindingList<ShipClassTN> ShipDesigns { get; set; }

        /// <summary>
        /// Taskgroups for this faction are here.
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroups { get; set; }

        /// <summary>
        /// A list of every ship this faction has is stored here.
        /// </summary>
        public BindingList<ShipTN> Ships { get; set; }

        /// <summary>
        /// I'll just store every contact in every system potentially here right now.
        /// </summary>
        public Dictionary<StarSystem,FactionSystemDetection> SystemContacts { get; set; }

        /// <summary>
        /// here is where only the specifically detected contacts are placed.
        /// </summary>
        public Dictionary<ShipTN,FactionContact> DetectedContacts { get; set; }

        /// <summary>
        /// Just a list of the available installation types for this faction.
        /// </summary>
        public BindingList<Installation> InstallationTypes { get; set; }

        public BindingList<MessageEntry> MessageLog { get; set; }

        public Color FactionColor { get; set; }

        /// <summary>
        /// This may go away in future revisions.
        /// </summary>
        public BindingList<OrdnanceGroupTN> MissileGroups { get; set; }

        /// <summary>
        /// Faction base tracking that all ships will use. This may eventually get moved to the tech department
        /// </summary>
        public int BaseTracking { get; set; }

        /// <summary>
        /// These are Firecontrols set to open fire.
        /// </summary>
        public Dictionary<ComponentTN,ShipTN> OpenFireFC { get; set; }
        public Dictionary<ComponentTN, bool> OpenFireFCType { get; set; }

        /// <summary>
        /// List of ships that need various combat related functionality. the int is a status word.
        /// </summary>
        public Dictionary<ShipTN,int> RechargeList { get; set; }

        /// <summary>
        /// Bitwise status flag enumerator.
        /// </summary>
        public enum RechargeStatus
        {
            Shields=1,
            Weapons=2,
            Destroyed=4,
            Count=8
        }

        /// <summary>
        /// Total faction tech levels.
        /// </summary>
        public BindingList<SByte> FactionTechLevel { get; set; }

        /// <summary>
        /// Techs start at -1(undiscovered). some techs have multiple levels, others have only 1.
        /// </summary>
        public enum FactionTechnology
        {
            /// <summary>
            /// Biology Techs. Genome Sequence is the precursor for all biology related techs(excepting terraforming and rate)
            /// </summary>
            GenomeSequence,
            BaseGravityPlus,
            BaseGravityMinus,
            BaseOxygenPlus,
            BaseOxygenMinus,
            BaseTemperaturePlus,
            BaseTemperatureMinus,
            TemperatureRange,
            TerraformingModule,
            TerraformRate,
            BiologyCount,

            /// <summary>
            /// Construction & Production Techs. TNTech is the precursor for all TN related techs. Only a few techs are not TN.
            /// </summary>
            AsteroidModule,
            ConstructionProdRate,
            ExpandCivilianEconomy,
            FighterProdRate,
            FuelProdRate,
            JumpGateModule,
            MiningProdRate,
            ResearchProdRate,
            ShipProdRate,
            ShipOps,
            SmallJumpGateModule,
            SoriumModule,
            TransNewtonianTech,
            ConstructionCount,

            /// <summary>
            /// Defense Techs. techs that hide ship signatures or increase resistance to damage. Cloak theory is required for cloaking devices.
            /// </summary>
            AbsorptionShieldStrength,
            AbsorptionShieldRadiateRate,
            ArmourProtection,
            CloakSensorReduction,
            CloakEfficiency,
            CloakTheory,
            CloakMinimumSize,
            DamageControl,
            StandardShieldStrength,
            StandardShieldRegen,
            ThermalReduction,
            DefenseCount,

            /// <summary>
            /// Energy weapon techs. These don't need to carry ammunition of any sort, though reactor power may be required.
            /// </summary>
            AdvancedLaserFocal,
            LaserFocal,
            MesonFocal,
            MicrowaveFocal,
            PlasmaCarronadeCalibre,
            AdvancedPlasmaCarronadeCalibre,
            ParticleBeamStrength,
            AdvancedParticleBeamStrength,
            SpinalMount,
            ReducedSizeLasers,
            LaserWarhead,
            LaserWavelength,
            MesonFocusing,
            MicrowaveFocusing,
            ParticleBeamRange,
            PlasmaTorpedoStrength,
            PlasmaTorpedoSpeed,
            PlasmaTorpedoIntegrity,
            PlasmaTorpedoRechargeRate,
            TurretTracking,
            EnergyCount,

            /// <summary>
            /// Logistics and Ground Combat. many one offs here.
            /// </summary>
            MaintStorage,
            CargoHandlingSystems,
            BoatBays,
            Hangars,
            ReplacementBat,
            Garrison,
            ConstructionBrigade,
            CombatEngineer,
            MobileInfantry,
            AssaultInfantry,
            MarineBat,
            MarineCompany,
            HeavyAssaultBat,
            BrigadeHQ,
            DivisionHQ,
            GroundCombatStrength,
            ColonyCostReduction,
            CombatDropBat,
            CombatDropComp,
            CryoCombatDropBat,
            CryoCombatDropComp,
            SmallCrewQuarters,
            CryoTransport,
            SmallEngineeringSection,
            FlagBridge,
            FuelStorage,
            MaintModule,
            OrbHabitat,
            RecModule,
            SalvageModule,
            TroopTransport,
            LogisticsCount,

            /// <summary>
            /// Missile and Kinetic weapons technologies. Railguns require energy, Missiles require ordnance, and gauss requires neither.
            /// </summary>
            Railgun,
            AdvancedRailgun,
            RailgunVelocity,
            GaussCannonVelocity,
            GaussCannonROF,
            WarheadStrength,
            EnhancedRadiationWarhead,
            MissileAgility,
            MagazineEjectionSystem,
            MagazineFeedEfficiency,
            LauncherReloadRate,
            ReducedSizeLaunchers,
            OrdnanceProdRate,
            MissileCount,

            /// <summary>
            /// Power and Propulsion Technologies, Engines, Jump Engines, and power plants.
            /// </summary>
            EngineBaseTech,
            CapacitorChargeRate,
            FuelConsumption,
            HyperdriveSizeMod,
            MaxEnginePowerMod,
            MinEnginePowerMod,
            JumpPointTheory,
            JumpEngineEfficiency,
            MaxSquadJumpSize,
            MaxSquadJumpRadius,
            MinJumpEngineSize,
            ReactorPowerBoost,
            ReactorBaseTech,
            PowerCount,

            /// <summary>
            /// Sensors and Fire Control Technologies.
            /// </summary>
            ActiveSensorStrength,
            BeamFireControlRange,
            BeamFireControlTracking,
            EMSensorSensitivity,
            ThermalSensorSensitivity,
            Hardening,
            GeoSensor,
            GravSensor,
            ElectronicWarfare,
            ECM,
            ECCM,
            CompactECM,
            CompactECCM,
            SmallECM,
            SmallECCM,
            MaxTrackVsMissiles,
            MissileECM,
            DSTSSensorStrength,

            /// <summary>
            /// Overall Count and Sensor Count
            /// </summary>
            Count
        }

        /// <summary>
        /// Need tech names somewhere
        /// Reactor Technologies:
        /// "Pressurised Water(1.5k)", "Pebble Bed(3k)", "Gas Cooled Fast(6k)", "Stellarator Fusion(12k)", "Tokamak Fusion(24k)", "Magnetic Confinement Fusion(45k)"
        /// "Inertial Confinement Fusion(90k)","Solid-Core Antimatter(180k)", "Gas-Core Antimatter(375k)","Plasma-Core Antimatter(750k)","Beam-Core Antimatter(1.5M)","Vacuum Energy(3M)"
        /// Shield:
        /// //Alpha(1K) Beta(2K) Gamma(4K) Delta(8K) Epsilon(15K) Theta(30K) Xi(60K) Omicron(120K) Sigma(250K) Tau(500K) Psi(1M) Omega(2M)
        /// Engines:
        /// Nuclear Thermal(2500) Nuclear Pulse(5000) Ion(10000) Magneto-Plasma(20k) Internal Confinement Fusion(40k) Magnetic Confinement Fusion(80k) Inertial Confinement fusion(150k)
        ///   Solid-Core Antimatter(300k) Gas-Core Antimatter(600k) Plasma-Core Antimatter(1.25M) Beam-Core Antimater(2.5M) Photonic(5M)
        /// Laser Wavelengths:
        /// Infared, Visible Light, Near Ultraviolet, Ultraviolet, Far Ultraviolet, Soft X-Ray(30), X-Ray(60), Far X-Ray(125), Extreme X-Ray(250), Near Gamma Ray(500), Gamma Ray(1M), Far Gamma Ray(2M)
        /// </summary>


        /// <summary>
        /// Constructor for basic faction.
        /// </summary>
        /// <param name="ID">placement of this faction in the factionsystemdetection lists. must be in order.</param>
        public Faction(int ID)
        {
            Name = "Human Federation";
            Species = new Species(); // go with the default Species!

            KnownSystems = new BindingList<StarSystem>();
            TaskForces = new BindingList<TaskForce>();
            Commanders = new BindingList<Commander>();
            Populations = new BindingList<Population>();

            ComponentList = new ComponentDefListTN();
            ShipDesigns = new BindingList<ShipClassTN>();
            TaskGroups = new BindingList<TaskGroupTN>();
            Ships = new BindingList<ShipTN>();
            ComponentList.AddInitialComponents();

            SystemContacts = new Dictionary<StarSystem,FactionSystemDetection>();
            DetectedContacts = new Dictionary<ShipTN, FactionContact>();

            FactionID = ID;

            InstallationTypes = new BindingList<Installation>();
            for (int loop = 0; loop < (int)Installation.InstallationType.InstallationCount; loop++)
            {
                Installation NewInst = new Installation((Installation.InstallationType)loop);
                InstallationTypes.Add(NewInst);
            }

            MessageLog = new BindingList<MessageEntry>();

            MissileGroups = new BindingList<OrdnanceGroupTN>();

            BaseTracking = 1250;

            OpenFireFC = new Dictionary<ComponentTN, ShipTN>();
            OpenFireFCType = new Dictionary<ComponentTN,bool>();

            RechargeList = new Dictionary<ShipTN,int>();

            FactionTechLevel = new BindingList<SByte>();

            for (int loop = 0; loop < (int)FactionTechnology.Count; loop++)
            {
                FactionTechLevel.Add(-1);
            }

            /// <summary>
            /// Hardening is a special case that must start at zero and not negative 1.
            /// </summary>
            FactionTechLevel[(int)Faction.FactionTechnology.Hardening] = 0;

            /// <summary>
            /// These are conventional tech starts, each conventional faction starts with them researched.
            /// If anyone has a better idea about how these should be organized feel free, but note that changing this will have repercussions in Component design(specifically components.cs)
            /// </summary>
            FactionTechLevel[(int)Faction.FactionTechnology.ThermalSensorSensitivity] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.FuelConsumption] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.ThermalReduction] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.EngineBaseTech] = 0;
        }

        /// <summary>
        /// String for faction with specified name and species.
        /// </summary>
        /// <param name="a_oName">Faction name</param>
        /// <param name="a_oSpecies">Faction species</param>
        /// <param name="ID">Faction placement in its order(0th faction is 0, 1st faction is 1, and so on)</param>
        public Faction(string a_oName, Species a_oSpecies, int ID)
        {
            Name = a_oName;
            Species = a_oSpecies;

            KnownSystems = new BindingList<StarSystem>();
            TaskForces = new BindingList<TaskForce>();
            Commanders = new BindingList<Commander>();
            Populations = new BindingList<Population>();

            ComponentList = new ComponentDefListTN();
            ShipDesigns = new BindingList<ShipClassTN>();
            TaskGroups = new BindingList<TaskGroupTN>();
            Ships = new BindingList<ShipTN>();
            ComponentList.AddInitialComponents();

            SystemContacts = new Dictionary<StarSystem, FactionSystemDetection>();
            DetectedContacts = new Dictionary<ShipTN, FactionContact>();

            FactionID = ID;

            InstallationTypes = new BindingList<Installation>();
            for (int loop = 0; loop < (int)Installation.InstallationType.InstallationCount; loop++)
            {
                Installation NewInst = new Installation((Installation.InstallationType)loop);
                InstallationTypes.Add(NewInst);
            }

            MessageLog = new BindingList<MessageEntry>();

            MissileGroups = new BindingList<OrdnanceGroupTN>();

            BaseTracking = 1250;

            OpenFireFC = new Dictionary<ComponentTN, ShipTN>();
            OpenFireFCType = new Dictionary<ComponentTN, bool>();

            RechargeList = new Dictionary<ShipTN, int>();

            FactionTechLevel = new BindingList<SByte>();
            for (int loop = 0; loop < (int)FactionTechnology.Count; loop++)
            {
                FactionTechLevel.Add(-1);
            }

            /// <summary>
            /// Hardening is a special case that must start at zero and not negative 1.
            /// </summary>
            FactionTechLevel[(int)Faction.FactionTechnology.Hardening] = 0;

            /// <summary>
            /// These are conventional tech starts, each conventional faction starts with them researched.
            /// If anyone has a better idea about how these should be organized feel free, but note that changing this will have repercussions in Component design(specifically components.cs)
            /// </summary>
            FactionTechLevel[(int)Faction.FactionTechnology.ThermalSensorSensitivity] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.FuelConsumption] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.ThermalReduction] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.EngineBaseTech] = 0;
        }

        /// <summary>
        /// Adds a new taskgroup to the taskgroups list at location StartBody in System StartSystem.
        /// </summary>
        /// <param name="Title">Name.</param>
        /// <param name="StartBody">Body with population that built the ship that will be put into the TG.</param>
        /// <param name="StartSystem">System in which the TG starts in.</param>
        public void AddNewTaskGroup(String Title,OrbitingEntity StartBody, StarSystem StartSystem)
        {
            TaskGroupTN TG = new TaskGroupTN(Title, this, StartBody, StartSystem);
            TaskGroups.Add(TG);
        }

        public void AddNewShipDesign(String Title)
        {
            ShipClassTN Ship = new ShipClassTN(Title,this);
            ShipDesigns.Add(Ship);
        }

        /// <summary>
        /// Adds a list of contacts to the faction.
        /// </summary>
        /// <param name="system">Starsystem for the contacts.</param>
        public void AddNewContactList(StarSystem system)
        {
            FactionSystemDetection NewContact = new FactionSystemDetection(this,system);
            system.FactionDetectionLists.Add(NewContact);
            SystemContacts.Add(system,NewContact);
        
        }

        /// <summary>
        /// Removes a list of contacts from the faction and from the system lists.
        /// </summary>
        /// <param name="ContactList">List to be removed</param>
        public void RemoveContactList(FactionSystemDetection ContactList)
        {
            ContactList.System.FactionDetectionLists.Remove(ContactList);
            SystemContacts.Remove(ContactList.System);
        }


        #region Sensor Sweep Code

        /// <summary>
        /// Galaxy wide sensor sweep of all systems in which this faction has a presence. Here is where things start to get complicated.
        /// </summary>
        /// <param name="YearTickValue">The second count for the current year.</param>
        public void SensorSweep(int YearTickValue)
        {
            /// <summary>
            /// Loop through all DSTS. ***
            /// </summary>
            
            /// <summary>
            /// Loop through all faction taskgroups.
            /// </summary>
            for (int loop = 0; loop < TaskGroups.Count; loop++)
            {

                StarSystem System = TaskGroups[loop].Contact.CurrentSystem;
                /// <summary>
                /// Loop through the global contacts list for the system. thermal.Count is equal to SystemContacts.Count. or should be.
                /// </summary>
                for (int loop2 = 0; loop2 < System.FactionDetectionLists[FactionID].Thermal.Count; loop2++)
                {
                    /// <summary>
                    /// I don't own loop2, and it hasn't been fully detected yet.
                    /// </summary>
                    if (this != System.SystemContactList[loop2].faction && System.FactionDetectionLists[FactionID].Thermal[loop2] != YearTickValue &&
                        System.FactionDetectionLists[FactionID].EM[loop2] != YearTickValue && System.FactionDetectionLists[FactionID].Active[loop2] != YearTickValue)
                    {
                        float dist = -1.0f;
                        if (TaskGroups[loop].Contact.DistanceUpdate[loop2] == YearTickValue)
                        {
                            dist = TaskGroups[loop].Contact.DistanceTable[loop2];
                        }
                        else
                        {
                            float distX = (float)(TaskGroups[loop].Contact.XSystem - System.SystemContactList[loop2].XSystem);
                            float distY = (float)(TaskGroups[loop].Contact.YSystem - System.SystemContactList[loop2].YSystem);
                            dist = (float)Math.Sqrt((double)((distX * distX) + (distY * distY)));

                            TaskGroups[loop].Contact.DistanceTable[loop2] = dist;
                            TaskGroups[loop].Contact.DistanceUpdate[loop2] = YearTickValue;

                            int TGID = System.SystemContactList.IndexOf(TaskGroups[loop].Contact);

                            System.SystemContactList[loop2].DistanceTable[TGID] = dist;
                            System.SystemContactList[loop2].DistanceUpdate[TGID] = YearTickValue;
                        }

                        /// <summary>
                        /// Now to find the biggest thermal signature in the contact. The biggest for planets is just the planetary pop itself since
                        /// multiple colonies really shouldn't happen.
                        /// </summary>
                        int sig = -1;
                        int detection = -1;

                        /// <summary>
                        /// Handle population detection
                        /// </summary>
                        if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.Population)
                        {
                            sig = System.SystemContactList[loop2].Pop.ThermalSignature;
                            detection = TaskGroups[loop].BestThermal.pSensorDef.GetPassiveDetectionRange(sig);

                            /// <summary>
                            /// LargeDetection handles determining if dist or detection go beyond INTMAX and acts accordingly.
                            /// </summary>
                            bool det = LargeDetection(System, dist, detection);

                            /// <summary>
                            /// Mark this contact as detected for this time slice via thermal for both the contact, and for the faction as a whole.
                            /// </summary>
                            if (det == true)
                            {
                                System.SystemContactList[loop2].Pop.ThermalDetection[FactionID] = YearTickValue;
                                System.FactionDetectionLists[FactionID].Thermal[loop2] = YearTickValue;
                            }

                            sig = System.SystemContactList[loop2].Pop.EMSignature;
                            detection = TaskGroups[loop].BestEM.pSensorDef.GetPassiveDetectionRange(sig);

                            det = LargeDetection(System, dist, detection);

                            if (det == true)
                            {
                                System.SystemContactList[loop2].Pop.EMDetection[FactionID] = YearTickValue;
                                System.FactionDetectionLists[FactionID].EM[loop2] = YearTickValue;
                            }

                            sig = Constants.ShipTN.ResolutionMax - 1;
                            /// <summary>
                            /// The -1 is because a planet is most certainly not a missile.
                            /// </summary>
                            detection = TaskGroups[loop].ActiveSensorQue[ TaskGroups[loop].TaskGroupLookUpST[ sig ]].aSensorDef.GetActiveDetectionRange(sig,-1);

                            /// <summary>
                            /// Do detection calculations here.
                            /// </summary>
                            det = LargeDetection(System, dist, detection);

                            if (det == true)
                            {
                                System.SystemContactList[loop2].Pop.ActiveDetection[FactionID] = YearTickValue;
                                System.FactionDetectionLists[FactionID].Active[loop2] = YearTickValue;
                            }

                        }
                        else if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.TaskGroup && System.SystemContactList[loop2].TaskGroup.Ships.Count != 0)
                        {

                            /// <summary>
                            /// Taskgroups have multiple signatures, so noDetection and allDetection become important.
                            /// </summary>
                            
                            bool noDetection = false;
                            bool allDetection = false;

                            #region Ship Thermal Detection Code

                            if (System.FactionDetectionLists[FactionID].Thermal[loop2] != YearTickValue)
                            {

                                /// <summary>
                                /// Get the best detection range for thermal signatures in loop.
                                /// </summary>
                                int ShipID = System.SystemContactList[loop2].TaskGroup.ThermalSortList.Last();
                                ShipTN scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                sig = scratch.CurrentThermalSignature;

                                /// <summary>
                                /// Check to make sure the taskgroup has a thermal sensor available, otherwise use the default.
                                /// </summary>
                                if (TaskGroups[loop].BestThermalCount != 0)
                                {
                                    detection = TaskGroups[loop].BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                                }
                                else
                                {
                                    detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                }

                                /// <summary>
                                /// Test the biggest signature against the best sensor.
                                /// </summary>
                                bool det = LargeDetection(System, dist, detection);

                                /// <summary>
                                /// Good case, none of the ships are detected.
                                /// </summary>
                                if (det == false)
                                {
                                    noDetection = true;
                                }

                                /// <summary>
                                /// Atleast the biggest ship is detected.
                                /// </summary
                                if (noDetection == false)
                                {
                                    ShipID = System.SystemContactList[loop2].TaskGroup.ThermalSortList.First();
                                    scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentThermalSignature;

                                    /// <summary>
                                    /// Check to make sure the taskgroup has a thermal sensor available, otherwise use the default.
                                    /// </summary>
                                    if (TaskGroups[loop].BestThermalCount != 0)
                                    {
                                        detection = TaskGroups[loop].BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                                    }
                                    else
                                    {
                                        detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                    }

                                    /// <summary>
                                    /// Now for the smallest vs the best.
                                    /// </summary>
                                    det = LargeDetection(System, dist, detection);

                                    /// <summary>
                                    /// Best case, everything is detected.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        allDetection = true;

                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            System.SystemContactList[loop2].TaskGroup.Ships[loop3].ThermalDetection[FactionID] = YearTickValue;
                                        }
                                        System.FactionDetectionLists[FactionID].Thermal[loop2] = YearTickValue;
                                    }
                                    else if (noDetection == false && allDetection == false)
                                    {
                                        /// <summary>
                                        /// Worst case. some are detected, some aren't.
                                        /// </summary>

                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            LinkedListNode<int> node = System.SystemContactList[loop2].TaskGroup.ThermalSortList.Last;
                                            bool done = false;

                                            while (!done)
                                            {
                                                scratch = System.SystemContactList[loop2].TaskGroup.Ships[node.Value];

                                                if (scratch.ThermalDetection[FactionID] != YearTickValue)
                                                {
                                                    sig = scratch.CurrentThermalSignature;
                                                    if (TaskGroups[loop].BestThermalCount != 0)
                                                    {
                                                        detection = TaskGroups[loop].BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                                                    }
                                                    else
                                                    {
                                                        detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                                    }

                                                    /// <summary>
                                                    /// Test each ship until I get to one I don't see.
                                                    /// </summary>
                                                    det = LargeDetection(System, dist, detection);

                                                    if (det == true)
                                                    {
                                                        scratch.ThermalDetection[FactionID] = YearTickValue;
                                                    }
                                                    else
                                                    {
                                                        done = true;
                                                        break;
                                                    }
                                                }
                                                if (node == System.SystemContactList[loop2].TaskGroup.ThermalSortList.First)
                                                {
                                                    /// <summary>
                                                    /// This should not happen.
                                                    /// </summary>
                                                    Console.WriteLine("Line 638 Faction.cs, partial detect looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                    done = true;
                                                    break;
                                                }
                                                node = node.Previous;
                                            }
                                        }
                                    }
                                    /// <summary>
                                    /// End else
                                    /// </summary>
                                }
                            }
                            #endregion

                            #region Ship EM Detection Code

                            if (System.FactionDetectionLists[FactionID].EM[loop2] != YearTickValue)
                            {
                                noDetection = false;
                                allDetection = false;

                                /// <summary>
                                /// Get the best detection range for EM signatures in loop.
                                /// </summary>
                                int ShipID = System.SystemContactList[loop2].TaskGroup.EMSortList.Last();
                                ShipTN scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                sig = scratch.CurrentEMSignature;

                                /// <summary>
                                /// Check to see if the taskgroup has an em sensor, and that said em sensor is not destroyed.
                                /// otherwise use the default passive detection range.
                                /// </summary>
                                if (TaskGroups[loop].BestEMCount > 0)
                                {
                                    detection = TaskGroups[loop].BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                                }
                                else
                                {
                                    detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                }

                                bool det = LargeDetection(System, dist, detection);

                                /// <summary>
                                /// Good case, none of the ships are detected.
                                /// </summary>
                                if (det == false)
                                {
                                    noDetection = true;
                                }

                                /// <summary>
                                /// Atleast the biggest ship is detected.
                                /// </summary
                                if (noDetection == false)
                                {
                                    ShipID = System.SystemContactList[loop2].TaskGroup.EMSortList.First();
                                    scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentEMSignature;

                                    /// <summary>
                                    /// once again we must check here to make sure that the taskgroup does have a passive suite, or else use the default one.
                                    /// </summary>
                                    if (TaskGroups[loop].BestEMCount > 0)
                                    {
                                        detection = TaskGroups[loop].BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                                    }
                                    else
                                    {
                                        detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                    }

                                    det = LargeDetection(System, dist, detection);

                                    /// <summary>
                                    /// Best case, everything is detected.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        allDetection = true;

                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            System.SystemContactList[loop2].TaskGroup.Ships[loop3].EMDetection[FactionID] = YearTickValue;
                                        }
                                        System.FactionDetectionLists[FactionID].EM[loop2] = YearTickValue;
                                    }
                                    else if (noDetection == false && allDetection == false)
                                    {
                                        /// <summary>
                                        /// Worst case. some are detected, some aren't.
                                        /// </summary>


                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            LinkedListNode<int> node = System.SystemContactList[loop2].TaskGroup.EMSortList.Last;
                                            bool done = false;

                                            while (!done)
                                            {
                                                scratch = System.SystemContactList[loop2].TaskGroup.Ships[node.Value];

                                                if (scratch.EMDetection[FactionID] != YearTickValue)
                                                {
                                                    sig = scratch.CurrentEMSignature;

                                                    /// <summary>
                                                    /// here is where EM detection differs from Thermal detection:
                                                    /// If a ship has a signature of 0 by this point(and we didn't already hit noDetection above,
                                                    /// it means that one ship is emitting a signature, but that no other ships are.
                                                    /// Mark the group as totally detected, but not the ships, this serves to tell me that the ships are undetectable
                                                    /// in this case.
                                                    /// </summary>
                                                    if (sig == 0)
                                                    {
                                                        /// <summary>
                                                        /// The last signature we looked at was the ship emitting an EM sig, and this one is not.
                                                        /// Mark the entire group as "spotted" because no other detection will occur.
                                                        /// </summary>
                                                        if (System.SystemContactList[loop2].TaskGroup.Ships[node.Next.Value].EMDetection[FactionID] == YearTickValue)
                                                        {
                                                            System.FactionDetectionLists[FactionID].EM[loop2] = YearTickValue;
                                                        }
                                                        break;
                                                    }

                                                    if (TaskGroups[loop].BestEMCount > 0)
                                                    {
                                                        detection = TaskGroups[loop].BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                                                    }
                                                    else
                                                    {
                                                        detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                                    }

                                                    det = LargeDetection(System, dist, detection);

                                                    if (det == true)
                                                    {
                                                        scratch.EMDetection[FactionID] = YearTickValue;
                                                    }
                                                    else
                                                    {
                                                        done = true;
                                                        break;
                                                    }
                                                }
                                                if (node == System.SystemContactList[loop2].TaskGroup.EMSortList.First)
                                                {
                                                    /// <summary>
                                                    /// This should not happen.
                                                    /// </summary>
                                                    Console.WriteLine("Line 787, partial detect looped through every ship.");
                                                    done = true;
                                                    break;
                                                }
                                                node = node.Previous;
                                            }
                                        }
                                    }
                                    /// <summary>
                                    /// End else
                                    /// </summary>
                                }
                            }
                            #endregion

                            #region Ship Active Detection Code

                            if (System.FactionDetectionLists[FactionID].Active[loop2] != YearTickValue && TaskGroups[loop].ActiveSensorQue.Count > 0)
                            {
                                noDetection = false;
                                allDetection = false;

                                /// <summary>
                                /// Get the best detection range for thermal signatures in loop.
                                /// </summary>
                                int ShipID = System.SystemContactList[loop2].TaskGroup.ActiveSortList.Last();
                                ShipTN scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                sig = scratch.TotalCrossSection - 1;

                                if (sig > Constants.ShipTN.ResolutionMax - 1)
                                    sig = Constants.ShipTN.ResolutionMax - 1;

                                detection = TaskGroups[loop].ActiveSensorQue[ TaskGroups[loop].TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig,-1);


                                bool det = LargeDetection(System, dist, detection);

                                /// <summary>
                                /// Good case, none of the ships are detected.
                                /// </summary>
                                if (det == false)
                                {
                                    noDetection = true;
                                }

                                /// <summary>
                                /// Atleast the biggest ship is detected.
                                /// </summary
                                if (noDetection == false)
                                {
                                    ShipID = System.SystemContactList[loop2].TaskGroup.ActiveSortList.First();
                                    scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                    sig = scratch.TotalCrossSection - 1;

                                    if (sig > Constants.ShipTN.ResolutionMax - 1)
                                        sig = Constants.ShipTN.ResolutionMax - 1;

                                    detection = TaskGroups[loop].ActiveSensorQue[TaskGroups[loop].TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                                    det = LargeDetection(System, dist, detection);

                                    /// <summary>
                                    /// Best case, everything is detected.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        allDetection = true;

                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            System.SystemContactList[loop2].TaskGroup.Ships[loop3].ActiveDetection[FactionID] = YearTickValue;
                                        }
                                        /// <summary>
                                        /// FactionSystemDetection entry. I hope to deprecate this at some point.
                                        /// Be sure to erase the factionDetectionSystem entry first, to track down everywhere this overbloated thing is.
                                        /// </summary>
                                        System.FactionDetectionLists[FactionID].Active[loop2] = YearTickValue;
                                    }
                                    else if (noDetection == false && allDetection == false)
                                    {
                                        /// <summary>
                                        /// Worst case. some are detected, some aren't.
                                        /// </summary>


                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            LinkedListNode<int> node = System.SystemContactList[loop2].TaskGroup.ActiveSortList.Last;
                                            bool done = false;

                                            while (!done)
                                            {
                                                scratch = System.SystemContactList[loop2].TaskGroup.Ships[node.Value];

                                                if (scratch.ActiveDetection[FactionID] != YearTickValue)
                                                {
                                                    sig = scratch.TotalCrossSection - 1;

                                                    if (sig > Constants.ShipTN.ResolutionMax - 1)
                                                        sig = Constants.ShipTN.ResolutionMax - 1;

                                                    detection = TaskGroups[loop].ActiveSensorQue[TaskGroups[loop].TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                                                    det = LargeDetection(System, dist, detection);

                                                    if (det == true)
                                                    {
                                                        scratch.ActiveDetection[FactionID] = YearTickValue;
                                                    }
                                                    else
                                                    {
                                                        done = true;
                                                        break;
                                                    }
                                                }
                                                if (node == System.SystemContactList[loop2].TaskGroup.ActiveSortList.First)
                                                {
                                                    /// <summary>
                                                    /// This should not happen.
                                                    /// </summary>
                                                    Console.WriteLine("Line 900, partial detect looped through every ship.");
                                                    done = true;
                                                    break;
                                                }
                                                node = node.Previous;
                                            }
                                        }
                                    }
                                    /// <summary>
                                    /// End else
                                    /// </summary>
                                }
                            }
                            #endregion

                            /// <summary>
                            /// Detected contacts logic. If a ship has been newly detected this tick, create a contact entry for it.
                            /// Otherwise update the existing one. Messages to the message log should be handled there(at the top of this very file.
                            /// if a ship is no longer detected this tick then remove it from the detected contacts list.
                            /// 
                            /// This can actually be improved by turning detectedContact into a linked list, and putting updated contacts in front.
                            /// this way unupdated contacts would be at the end, and I would not have to loop through all ships here.
                            /// </summary>
                            for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                            {
                                ShipTN detectedShip = System.SystemContactList[loop2].TaskGroup.Ships[loop3];

                                /// <summary>
                                /// Sanity check to keep allied ships out of the DetectedContacts list.
                                /// </summary>
                                if (detectedShip.ShipsFaction != this)
                                {

                                    bool inDict = DetectedContacts.ContainsKey(detectedShip);
                                    bool th = (detectedShip.ThermalDetection[FactionID] == YearTickValue);
                                    bool em = (detectedShip.EMDetection[FactionID] == YearTickValue);
                                    bool ac = (detectedShip.ActiveDetection[FactionID] == YearTickValue);

                                    if (inDict == true)
                                    {
                                        DetectedContacts[detectedShip].updateFactionContact(th, em, ac, (uint)YearTickValue);

                                        if (th == false && em == false && ac == false)
                                        {
                                            DetectedContacts.Remove(detectedShip);
                                        }
                                    }
                                    else if (inDict == false && (th == true || em == true || ac == true))
                                    {
                                        FactionContact newContact = new FactionContact(detectedShip, th, em, ac, (uint)YearTickValue);
                                        DetectedContacts.Add(detectedShip, newContact);
                                    }
                                }
                            }
                        }
                        /// <summary>
                        /// End if planet or Task Group
                        /// </sumamry>
                    }
                    /// <summary>
                    /// End if not globally detected.
                    /// </summary>

                }
                /// <summary>
                /// End for Faction Contact Lists
                /// </summary>

            }
            /// <summary>
            /// End for Faction TaskGroups.
            /// </summary>
        }
        /// <summary>
        /// End SensorSweep()
        /// </summary>

        /// <summary>
        /// ActiveLargeDetection handles potentially greater than MAX distance in KM detection for actives.
        /// </summary>
        /// <param name="System">Starsystem this takes place in</param>
        /// <param name="dist">distance in AU</param>
        /// <param name="detection">Detection factor, KM / 10,000</param>
        /// <returns>Whether or not detection has occured.</returns>
        public bool LargeDetection(StarSystem System, float dist, int detection)
        {
            /// <summary>
            /// Then I need to use the large distance detection model.
            /// </summary>
            if (detection > 214748)
            {
                double factor = Constants.Units.KM_PER_AU / 10000.0;
                double AUDetection = (double)detection / factor;

                /// <summary>
                /// Distance is in AU. If dist is greater, then no detection.
                /// </summary>
                if (dist < (float)AUDetection)
                {
                    return true;
                }
            }
            else
            {
                /// <summary>
                /// Due to this else, we know that our sensor cannot spot objects beyond MAX_KM_IN_AU at this point, so no detection if not true.
                /// </summary>
                if (dist < Constants.Units.MAX_KM_IN_AU)
                {
                    float distKM = (dist * (float)Constants.Units.KM_PER_AU) / 10000.0f;

                    /// <summary>
                    /// if distKM is less than detection(KM) then detection occurs.
                    /// </summary>
                    if (distKM < (float)detection)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion


        /// <summary>
        /// Sets all tech levels to above maximum(usually around 12). Sanity checking will be elsewhere.
        /// </summary>
        public void GiveAllTechs()
        {
            for (int loop = 0; loop < (int)FactionTechnology.Count; loop++)
            {
                FactionTechLevel[loop] = 100;
            }

            BaseTracking = 25000;
        }

    }
}
