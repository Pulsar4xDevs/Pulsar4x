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
using Pulsar4X.Helpers.GameMath;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

namespace Pulsar4X.Entities
{
    public class Faction : GameEntity
    {
        /// <summary>
        /// Factions have to know their own ID. this should be a 0 based index of the factionList where ever that ends up being.
        /// </summary>
        public int FactionID;

        public string Title { get; set; }
        public Species Species { get; set; }

        /// <summary>
        /// For now this is a stub for where ships will be created.
        /// </summary>
        public Population Capitol { get; set; }

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
        public Dictionary<StarSystem, FactionSystemDetection> SystemContacts { get; set; }

        /// <summary>
        /// here is where only the specifically detected contacts are placed.
        /// </summary>
        public Dictionary<StarSystem, DetectedContactsList> DetectedContactLists { get; set; }

        /// <summary>
        /// Just a list of the available installation types for this faction.
        /// </summary>
        public BindingList<Installation> InstallationTypes { get; set; }

        /// <summary>
        /// List of messages this faction has
        /// </summary>
        public BindingList<MessageEntry> MessageLog { get; set; }

        /// <summary>
        /// Color for this faction. may eventually be moved out of here.
        /// </summary>
        public Color FactionColor { get; set; }

        /// <summary>
        /// Missilegroups belonging to this faction. This may go away in future revisions.
        /// </summary>
        public BindingList<OrdnanceGroupTN> MissileGroups { get; set; }

        /// <summary>
        /// Ordnance series that ordnance definitions can belong to.
        /// </summary>
        public BindingList<OrdnanceSeriesTN> OrdnanceSeries { get; set; }

        /// <summary>
        /// Faction base tracking that all ships will use. This may eventually get moved to the tech department
        /// </summary>
        public int BaseTracking { get; set; }

        /// <summary>
        /// These are Firecontrols set to open fire.
        /// </summary>
        public Dictionary<ComponentTN, ShipTN> OpenFireFC { get; set; }
        public Dictionary<ComponentTN, bool> OpenFireFCType { get; set; }

        /// <summary>
        /// These are fire controls set to point defense.
        /// </summary>
        public Dictionary<StarSystem, PointDefenseList> PointDefense { get; set; }

        /// <summary>
        /// These missile groups belong to this faction, and either have some or all of their missiles removed.
        /// </summary>
        public BindingList<OrdnanceGroupTN> MissileRemoveList { get; set; }

        /// <summary>
        /// List of ships that need various combat related functionality. the int is a status word.
        /// </summary>
        public Dictionary<ShipTN, int> RechargeList { get; set; }

        /// <summary>
        /// Bitwise status flag enumerator.
        /// </summary>
        public enum RechargeStatus
        {
            Shields = 1,
            Weapons = 2,
            Destroyed = 4,
            CIWS = 8,
            JumpRecharge = 16,
            JumpStandardSickness = 32,
            JumpSquadronSickness = 64,
            Count = 128
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
            ImprovedCommandAndControl,
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
            MissileKineticCount,

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
        /// Armour:
        /// Conventional(0),Duranium(500),High Density Duranium(2500),Composite(5K),Ceramic Composite(10K),Laminate Composite(20K),Compressed Carbon(40K),Biphased Carbide(80K),Crystaline Composite(150K),
        /// Superdense(300K),Bonded Superdense(600K),Coherent Superdense(1.25M),Collapsium(2.5M);
        /// </summary>



        /// <summary>
        /// Ship BP this faction starts with.
        /// </summary>
        public decimal ShipBPTotal { get; set; }

        /// <summary>
        /// PDC BP this faction starts with.
        /// </summary>
        public decimal PDCBPTotal { get; set; }

        /// <summary>
        /// How much cash this faction has on hand, this can go negative.
        /// </summary>
        public decimal FactionWealth { get; set; }


        /// <summary>
        /// These lists are persistent between sensor sweeps, as everything that has been detected will have to be checked again later for if it is no longer detected.
        /// </summary>
        public BindingList<ShipTN> DetShipList { get; set; }

        /// <summary>
        /// Persistent missile detection list.
        /// </summary>
        public BindingList<OrdnanceGroupTN> DetMissileList { get; set; }

        /// <summary>
        /// Persistent population detection list.
        /// </summary>
        public BindingList<Population> DetPopList { get; set; }


        /// <summary>
        /// Constructor for basic faction.
        /// </summary>
        /// <param name="ID">placement of this faction in the factionsystemdetection lists. must be in order.</param>
        public Faction(int ID, string name = "Human Federation", Species species = null)
            : base()
        {
            Name = name;
            if (species == null)
                Species = new Species(); // go with the default Species!
            else
                Species = species;

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
            DetectedContactLists = new Dictionary<StarSystem, DetectedContactsList>();

            FactionID = ID;

            InstallationTypes = new BindingList<Installation>();
            for (int loop = 0; loop < (int)Installation.InstallationType.InstallationCount; loop++)
            {
                Installation NewInst = new Installation((Installation.InstallationType)loop);
                InstallationTypes.Add(NewInst);
            }

            MessageLog = new BindingList<MessageEntry>();

            MissileGroups = new BindingList<OrdnanceGroupTN>();

            OrdnanceSeries = new BindingList<OrdnanceSeriesTN>();
            OrdnanceSeriesTN NewOrd = new OrdnanceSeriesTN("No Series Selected");
            OrdnanceSeries.Add(NewOrd);

            BaseTracking = Constants.GameSettings.FactionBaseTrackingSpeed;

            OpenFireFC = new Dictionary<ComponentTN, ShipTN>();
            OpenFireFCType = new Dictionary<ComponentTN, bool>();

            PointDefense = new Dictionary<StarSystem, PointDefenseList>();

            MissileRemoveList = new BindingList<OrdnanceGroupTN>();

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
            FactionTechLevel[(int)Faction.FactionTechnology.ReducedSizeLaunchers] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.ArmourProtection] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.WarheadStrength] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.MissileAgility] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.TurretTracking] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.ECCM] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.DSTSSensorStrength] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.MinJumpEngineSize] = 0;
            FactionTechLevel[(int)Faction.FactionTechnology.ShipProdRate] = 0;

            ShipBPTotal = Constants.GameSettings.FactionStartingShipBP;
            PDCBPTotal = Constants.GameSettings.FactionStartingPDCBP;


            FactionWealth = Constants.Faction.StartingWealth;

            /// <summary>
            /// Ships and missiles are added to these two binding lists. this is for later to help the detected contact list.
            /// </summary>
            DetShipList = new BindingList<ShipTN>();
            DetMissileList = new BindingList<OrdnanceGroupTN>();
            DetPopList = new BindingList<Population>();

            GameState.Instance.Factions.Add(this);

            foreach (StarSystem system in GameState.Instance.StarSystems)
            {
                AddNewContactList(system);
            }
        }

        /// <summary>
        /// Adds a new taskgroup to the taskgroups list at location StartBody in System StartSystem.
        /// </summary>
        /// <param name="Title">Name.</param>
        /// <param name="StartBody">Body with population that built the ship that will be put into the TG.</param>
        /// <param name="StartSystem">System in which the TG starts in.</param>
        public void AddNewTaskGroup(String Title, OrbitingEntity StartBody, StarSystem StartSystem)
        {
            TaskGroupTN TG = new TaskGroupTN(Title, this, StartBody, StartSystem);
            TaskGroups.Add(TG);
        }

        public void AddNewShipDesign(String Title)
        {
            ShipClassTN Ship = new ShipClassTN(Title, this);
            ShipDesigns.Add(Ship);
        }

        /// <summary>
        /// Adds a list of contacts to the faction.
        /// </summary>
        /// <param name="system">Starsystem for the contacts.</param>
        public void AddNewContactList(StarSystem system)
        {
            if (SystemContacts.ContainsKey(system) == false)
            {
                FactionSystemDetection NewContact = new FactionSystemDetection(this, system);
                system.FactionDetectionLists.Add(NewContact);
                SystemContacts.Add(system, NewContact);
            }

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


        #region Sensor Sweep Code now just large detection. this wasn't moved as it is used elsewhere I believe.

        /// <summary>
        /// ActiveLargeDetection handles potentially greater than MAX distance in KM detection for actives.
        /// consider making this a static function, I am calling it from all over the place.
        /// </summary>
        /// <param name="dist">distance in AU</param>
        /// <param name="detection">Detection factor, KM / 10,000</param>
        /// <returns>Whether or not detection has occured.</returns>
        public bool LargeDetection(float dist, int detection)
        {
#warning 10,000 here is a magic number related to the 10K km unit that AuroraTN uses.
            /// <summary>
            /// Then I need to use the large distance detection model.
            /// </summary>
            if (detection > (int)Constants.Units.TEN_KM_MAX)
            {
                double factor = Constants.Units.KmPerAu / 10000.0;
                double AUDetection = (double)detection / factor;

                /// <summary>
                /// distance is in AU. If dist is greater, then no detection.
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
                    float distKM = (float)Distance.ToKm(dist) / 10000.0f;

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
#warning baseTracking is a magic number here.
            BaseTracking = 25000;

            foreach (Population CurrentPopulation in Populations)
            {
                if (CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number >= 1.0f)
                {
                    foreach (Installation.ShipyardInformation SYInfo in CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo)
                    {
                        SYInfo.UpdateModRate(this);
                    }
                }

                if (CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number >= 1.0f)
                {
                    foreach (Installation.ShipyardInformation SYInfo in CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo)
                    {
                        SYInfo.UpdateModRate(this);
                    }
                }
            }
        }
    }
    /// <summary>
    /// End Faction Class
    /// </summary>
}
