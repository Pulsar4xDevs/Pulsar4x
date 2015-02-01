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

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

namespace Pulsar4X.Entities
{
    public class FactionContact
    {
        /// <summary>
        /// The detected ship.
        /// </summary>
        public ShipTN ship { get; set; }

        /// <summary>
        /// Or it could be a detected missile.
        /// </summary>
        public OrdnanceGroupTN missileGroup { get; set; }

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
        public FactionContact(Faction CurrentFaction, ShipTN DetectedShip, bool Thermal, bool em, bool Active, uint tick)
        {
            ship = DetectedShip;
            missileGroup = null;
            thermal = Thermal;
            EM = em;
            active = Active;

            String Contact = "New contact detected:";

            if (thermal == true)
            {
                thermalTick = tick;
                Contact = String.Format("{0} Thermal Signature {1}", Contact, DetectedShip.CurrentThermalSignature);
            }

            if (EM == true)
            {
                EMTick = tick;
                Contact = String.Format("{0} EM Signature {1}", Contact, DetectedShip.CurrentEMSignature);
            }

            if (active == true)
            {
                activeTick = tick;
                Contact = String.Format("{0} TCS {1}", Contact, DetectedShip.TotalCrossSection);
            }

            /// <summary>
            /// Print to the message log.
            /// </summary>
            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ContactNew, DetectedShip.ShipsTaskGroup.Contact.Position.System, DetectedShip.ShipsTaskGroup.Contact,
                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Contact);

            CurrentFaction.MessageLog.Add(NMsg);

            /// <summary>
            /// Inform SimEntity.
            /// </summary>
            GameState.SE.SetInterrupt(InterruptType.NewSensorContact);

        }

        /// <summary>
        /// Initializer for detected missile event. FactionContact is the detector side of what is detected, while OrdnanceTN itself stores the detectee side. 
        /// multiple of these can exist, but only 1 per faction hopefully.
        /// </summary>
        /// <param name="DetectedMissile">Missile detected.</param>
        /// <param name="Thermal">Was the detection thermal based?</param>
        /// <param name="em">Detection via EM?</param>
        /// <param name="Active">Active detection?</param>
        /// <param name="tick">What tick did this detection event occur on?</param>
        public FactionContact(Faction CurrentFaction, OrdnanceGroupTN DetectedMissileGroup, bool Thermal, bool em, bool Active, uint tick)
        {
            missileGroup = DetectedMissileGroup;
            ship = null;
            thermal = Thermal;
            EM = em;
            active = Active;
            MessageEntry NMsg;

            String Contact = "New contact detected:";

            if (thermal == true)
            {
                thermalTick = tick;
                Contact = String.Format("{0} Thermal Signature {1} x{2}", Contact, (int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.totalThermalSignature), DetectedMissileGroup.missiles.Count);
            }

            if (EM == true)
            {
                EMTick = tick;
                if (DetectedMissileGroup.missiles[0].missileDef.aSD != null)
                {
                    Contact = String.Format("{0} EM Signature {1} x{2}", Contact, DetectedMissileGroup.missiles[0].missileDef.aSD.gps, DetectedMissileGroup.missiles.Count);
                }
                else
                {
                    Contact = String.Format("Error with {0} : has EM signature but no active sensor.", Contact);
                    NMsg = new MessageEntry(MessageEntry.MessageType.Error, DetectedMissileGroup.contact.Position.System, DetectedMissileGroup.contact,
                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Contact);

                    CurrentFaction.MessageLog.Add(NMsg);
                }

            }

            if (active == true)
            {
                activeTick = tick;
                Contact = String.Format("{0} TCS {1} x{2}", Contact, (int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size), DetectedMissileGroup.missiles.Count);
            }

            /// <summary>
            /// print to the message log.
            /// </summary>
            NMsg = new MessageEntry(MessageEntry.MessageType.ContactNew, DetectedMissileGroup.contact.Position.System, DetectedMissileGroup.contact,
                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Contact);

            CurrentFaction.MessageLog.Add(NMsg);

            /// <summary>
            /// Inform SimEntity.
            /// </summary>
            GameState.SE.SetInterrupt(InterruptType.NewSensorContact);
        }



        /// <summary>
        /// Each tick every faction contact should be updated on the basis of collected sensor data.
        /// </summary>
        /// <param name="Thermal">Thermal detection?</param>
        /// <param name="Em">Detected on EM?</param>
        /// <param name="Active">Detected by actives?</param>
        /// <param name="tick">Current tick.</param>
        public void updateFactionContact(Faction CurrentFaction, bool Thermal, bool Em, bool Active, uint tick)
        {
            if (thermal == Thermal && EM == Em && active == Active)
            {
                return;
            }

            String Contact = "N/A";
            MessageEntry.MessageType type = MessageEntry.MessageType.Count;

            if (Thermal == false && Em == false && Active == false)
            {
                Contact = "Existing contact lost";
                type = MessageEntry.MessageType.ContactLost;
            }
            else
            {
                Contact = "Update on existing contact:";
                type = MessageEntry.MessageType.ContactUpdate;

                if (thermal == false && Thermal == true)
                {
                    /// <summary>
                    /// New thermal detection event, message logic should be here.
                    /// </summary>
                    thermalTick = tick;

                    if (ship != null)
                        Contact = String.Format("{0} Thermal Signature {1}", Contact, ship.CurrentThermalSignature);
                    else if (missileGroup != null)
                        Contact = String.Format("{0} Thermal Signature {1} x{2}", Contact, (int)Math.Ceiling(missileGroup.missiles[0].missileDef.totalThermalSignature), missileGroup.missiles.Count);
                    else
                    {
                        type = MessageEntry.MessageType.Error;
                        Contact = "Error: Both ship and missile are null in UpdateFactionContact.";
                    }
                }
                else if (thermal == true && Thermal == false)
                {
                    /// <summary>
                    /// Thermal contact lost.
                    /// </summary>
                    Contact = String.Format("{0} Thermal contact lost", Contact);
                }

                if (EM == false && Em == true)
                {
                    /// <summary>
                    /// New EM detection event, message logic should be here.
                    /// </summary>
                    EMTick = tick;


                    if (ship != null)
                        Contact = String.Format("{0} EM Signature {1}", Contact, ship.CurrentEMSignature);
                    else if (missileGroup != null)
                    {
                        if (missileGroup.missiles[0].missileDef.aSD != null)
                        {
                            Contact = String.Format("{0} EM Signature {1} x{2}", Contact, missileGroup.missiles[0].missileDef.aSD.gps, missileGroup.missiles.Count);
                        }
                        else
                        {
                            type = MessageEntry.MessageType.Error;
                            Contact = "Error: Missile lacks a sensor, but is emitting EM in UpdateFactionContact.";
                        }
                    }
                    else
                    {
                        type = MessageEntry.MessageType.Error;
                        Contact = "Error: Both ship and missile are null in UpdateFactionContact.";
                    }
                }
                if (EM == true && Em == false)
                {
                    /// <summary>
                    /// EM contact lost.
                    /// </summary>
                    Contact = String.Format("{0} EM contact lost", Contact);
                }

                if (active == false && Active == true)
                {
                    /// <summary>
                    /// New active detection event, message logic should be here.
                    /// </summary>
                    activeTick = tick;



                    if (ship != null)
                        Contact = String.Format("{0} TCS {1}", Contact, ship.TotalCrossSection);
                    else if (missileGroup != null)
                        Contact = String.Format("{0} TCS_MSP {1} x{2}", Contact, (int)Math.Ceiling(missileGroup.missiles[0].missileDef.size), missileGroup.missiles.Count);
                    else
                    {
                        type = MessageEntry.MessageType.Error;
                        Contact = "Error: Both ship and missile are null in UpdateFactionContact.";
                    }
                }
                if (active == true && Active == false)
                {
                    /// <summary>
                    /// Active contact lost.
                    /// </summary>

                    Contact = String.Format("{0} Active contact lost", Contact);
                }

            }

            SystemContact SysCon = null;

            if (ship == null)
                SysCon = missileGroup.contact;
            else if (missileGroup == null)
                SysCon = ship.ShipsTaskGroup.Contact;

            MessageEntry NMsg = new MessageEntry(type, SysCon.Position.System, SysCon,
                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Contact);

            CurrentFaction.MessageLog.Add(NMsg);

            thermal = Thermal;
            EM = Em;
            active = Active;
        }
    }

    /// <summary>
    /// I need a new class to store a detected contacts list for every system.
    /// </summary>
    public class DetectedContactsList
    {
        /// <summary>
        /// This was the original faction detected contacts list, now moved to this class container so that I can put this in a dictionary with System.
        /// </summary>
        public Dictionary<ShipTN, FactionContact> DetectedContacts { get; set; }

        /// <summary>
        /// A DetectionEntity subclass would have come in handy perhaps. This is the detected contact list that is specific to missiles.
        /// </summary>
        public Dictionary<OrdnanceGroupTN, FactionContact> DetectedMissileContacts { get; set; }

        /// <summary>
        /// Constructor for this new list.
        /// </summary>
        public DetectedContactsList()
        {
            DetectedContacts = new Dictionary<ShipTN, FactionContact>();
            DetectedMissileContacts = new Dictionary<OrdnanceGroupTN, FactionContact>();
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
        public FactionSystemDetection(Faction Fact, StarSystem system)
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
        /// Message types that will be printed to the event log.
        /// </summary>
        public enum MessageType
        {
            ColonyLacksCI,
            ColonyLacksMinerals,

            ContactNew,
            ContactUpdate,
            ContactLost,

            Firing,
            FiringHit,
            FiringMissed,
            FiringNoAvailableOrdnance,
            FiringNoLoadedOrdnance,
            FiringRecharging,
            FiringZeroHitChance,

            LaunchTubeReloaded,
            LaunchTubeNoOrdnanceToReload,

            MissileHit,
            MissileIntercepted,
            MissileInterceptEvent,
            MissileLostFireControl,
            MissileLostTracking,
            MissileMissed,
            MissileOutOfFuel,

            OrdersCompleted,
            OrdersNotCompleted,

            PotentialFleetInterception,

            ShieldRecharge,

            ShipDamage,
            ShipDamageReport,

            Error,
            Count
        }

#if LOG4NET_ENABLED
        /// <summary>
        /// Ship Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(MessageEntry));
#endif

        /// <summary>
        /// What specific type of message is this?
        /// </summary>
        public MessageType TypeOf { get; set; }

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
        public MessageEntry(MessageType Type, StarSystem Loc, StarSystemEntity Ref, DateTime Time, int timeSlice, string text)
        {
            TypeOf = Type;
            Location = Loc;
            entity = Ref;
            TimeOfMessage = Time;
            TimeSlice = timeSlice;
            Text = text;


#if LOG4NET_ENABLED
            if (TypeOf == MessageType.Error)
            {
                String Entry = String.Format("Faction Message Logging Error: Type:{0} | Time:{1} | Location:{2} - TimeSlice:{3}: Text:{4}\n", TypeOf, TimeOfMessage, Location, TimeSlice, Text);
                logger.Debug(Entry);
            }
#endif
        }
    }

    /// <summary>
    /// This is intended to be part of a larger dictionary that separates these by system. Component and Ship data is stored for every Point defense enabled FC here.
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


#warning When jump transits are fully implemented, PointDefenseFC listings will have to be moved as appropriate, be sure to handle that.

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
        public Planet Capitol { get; set; }

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
            Count = 16
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

            ShipBPTotal = Constants.GameSettings.FactionStartingShipBP;
            PDCBPTotal = Constants.GameSettings.FactionStartingPDCBP;


            FactionWealth = Constants.Faction.StartingWealth;
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

            ShipBPTotal = Constants.GameSettings.FactionStartingShipBP;
            PDCBPTotal = Constants.GameSettings.FactionStartingPDCBP;

            FactionWealth = Constants.Faction.StartingWealth;
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
            FactionSystemDetection NewContact = new FactionSystemDetection(this, system);
            system.FactionDetectionLists.Add(NewContact);
            SystemContacts.Add(system, NewContact);

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
        /// <param name="CurrentSecond">The second count for the current year.</param>
        public void SensorSweep()
        {
            /// <summary>
            /// clear the fleet preempt list.
            /// </summary>
            GameState.SE.ClearFleetPreemptList();

            /// <summary>
            /// Ships and missiles are added to these two binding lists. this is for later to help the detected contact list.
            /// </summary>
            BindingList<ShipTN> DetShipList = new BindingList<ShipTN>();
            BindingList<OrdnanceGroupTN> DetMissileList = new BindingList<OrdnanceGroupTN>();

            /// <summary>
            /// Loop through all DSTS. ***
            /// </summary>

            /// <summary>
            /// Loop through all faction taskgroups.
            /// </summary>
            #region Faction Taskgroup Loop
            foreach(TaskGroupTN CurrentTaskGroup in TaskGroups)
            {
                StarSystem System = CurrentTaskGroup.Contact.Position.System;
                /// <summary>
                /// Loop through the global contacts list for the system. thermal.Count is equal to SystemContacts.Count. or should be.
                /// </summary>
                for (int detListIterator = 0; detListIterator < System.FactionDetectionLists[FactionID].Thermal.Count; detListIterator++)
                {
                    /// <summary>
                    /// I don't own loop2, and it hasn't been fully detected yet.
                    /// TODO: CHECK: ^ What does this mean?
                    /// </summary>
                    if (this != System.SystemContactList[detListIterator].faction && System.FactionDetectionLists[FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond &&
                        System.FactionDetectionLists[FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond && System.FactionDetectionLists[FactionID].Active[detListIterator] != GameState.Instance.CurrentSecond)
                    {
                        float dist;

                        // Check to see if our distance table is updated for this contact.
                        if (!CurrentTaskGroup.Contact.DistTable.GetDistance(System.SystemContactList[detListIterator], out dist))
                        {
                            /// <summary>
                            /// Handle fleet interception check here.
                            /// </summary>
                            if (GameState.SE.FleetInterceptionPreemptTick != GameState.Instance.CurrentSecond && System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.TaskGroup)
                            {
                                TaskGroupTN TaskGroup = System.SystemContactList[detListIterator].Entity as TaskGroupTN;
                                /// <summary>
                                /// player created empty taskgroups should not get checked here.
                                /// </summary>
                                if (TaskGroup.Ships.Count == 0)
                                    continue;

                                /// <summary>
                                /// how far could this TG travel within a single day?
                                /// </summary>
                                float TaskGroupDistance = (CurrentTaskGroup.CurrentSpeed / (float)Constants.Units.KM_PER_AU) * Constants.TimeInSeconds.Day;



#warning fleet intercept preemption magic number here, if less than 5 days travel time currently.
#warning fleet intercept needs to only process for hostile or unknown taskgroups, not all taskgroups.

                                int ShipID = TaskGroup.ActiveSortList.Last();
                                ShipTN LargestContactTCS = TaskGroup.Ships[ShipID];

                                /// <summary>
                                /// If this Taskgroup isn't already detected, and the distance is short enough, put it in the fleet intercept preempt list.
                                /// </summary>
                                if (TaskGroupDistance >= (dist / 5.0) && (DetectedContactLists.ContainsKey(System) == false ||
                                    (DetectedContactLists.ContainsKey(System) == true && (!DetectedContactLists[System].DetectedContacts.ContainsKey(LargestContactTCS) ||
                                     DetectedContactLists[System].DetectedContacts[LargestContactTCS].active == false))))
                                {
#warning Update this fleet intercept list for planets/populations
                                    GameState.SE.FleetInterceptionPreemptTick = GameState.Instance.CurrentSecond;

                                    GameState.SE.AddFleetToPreemptList(CurrentTaskGroup);
                                    if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.TaskGroup)
                                    {

                                        GameState.SE.AddFleetToPreemptList(TaskGroup);
                                    }
                                }

                            } // /Fleet Interception Check
                        } // distance Table Update.

                        /// <summary>
                        /// Now to find the biggest signature in the contact. The biggest for planets is just the planetary pop itself since
                        /// multiple colonies really shouldn't happen.
                        /// </summary>
                        int sig = -1;
                        int detection = -1;

                        /// <summary>
                        /// Handle population detection
                        /// </summary>
                        if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.Population)
                        {
                            Population Pop = System.SystemContactList[detListIterator].Entity as Population;
                            sig = Pop.ThermalSignature;
                            detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(sig);

                            /// <summary>
                            /// LargeDetection handles determining if dist or detection go beyond INTMAX and acts accordingly.
                            /// </summary>
                            bool det = LargeDetection(dist, detection);

                            /// <summary>
                            /// Mark this contact as detected for this time slice via thermal for both the contact, and for the faction as a whole.
                            /// </summary>
                            if (det == true)
                            {
                                Pop.ThermalDetection[FactionID] = GameState.Instance.CurrentSecond;
                                System.FactionDetectionLists[FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;
                            }

                            sig = Pop.EMSignature;
                            detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(sig);

                            det = LargeDetection(dist, detection);

                            if (det == true)
                            {
                                Pop.EMDetection[FactionID] = GameState.Instance.CurrentSecond;
                                System.FactionDetectionLists[FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                            }

                            sig = Constants.ShipTN.ResolutionMax - 1;
                            /// <summary>
                            /// The -1 is because a planet is most certainly not a missile.
                            /// </summary>
                            detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                            /// <summary>
                            /// Do detection calculations here.
                            /// </summary>
                            det = LargeDetection(dist, detection);

                            if (det == true)
                            {
                                Pop.ActiveDetection[FactionID] = GameState.Instance.CurrentSecond;
                                System.FactionDetectionLists[FactionID].Active[detListIterator] = GameState.Instance.CurrentSecond;
                            }
                        }
                        else if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.TaskGroup)
                        {
                            TaskGroupTN TaskGroup = System.SystemContactList[detListIterator].Entity as TaskGroupTN;

                            if (TaskGroup.Ships.Count != 0)
                            {
                                /// <summary>
                                /// Taskgroups have multiple signatures, so noDetection and allDetection become important.
                                /// </summary>

                                bool noDetection = false;
                                bool allDetection = false;

                                #region Ship Thermal Detection Code

                                if (System.FactionDetectionLists[FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond)
                                {

                                    /// <summary>
                                    /// Get the best detection range for thermal signatures in loop.
                                    /// </summary>
                                    int ShipID = TaskGroup.ThermalSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentThermalSignature;

                                    /// <summary>
                                    /// Check to make sure the taskgroup has a thermal sensor available, otherwise use the default.
                                    /// </summary>
                                    if (CurrentTaskGroup.BestThermalCount != 0)
                                    {
                                        detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                                    }
                                    else
                                    {
                                        detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                    }

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = LargeDetection(dist, detection);

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
                                        ShipID = TaskGroup.ThermalSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.CurrentThermalSignature;

                                        /// <summary>
                                        /// Check to make sure the taskgroup has a thermal sensor available, otherwise use the default.
                                        /// </summary>
                                        if (CurrentTaskGroup.BestThermalCount != 0)
                                        {
                                            detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                                        }
                                        else
                                        {
                                            detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                        }

                                        /// <summary>
                                        /// Now for the smallest vs the best.
                                        /// </summary>
                                        det = LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].ThermalDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                if (DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;


                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.ThermalSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.ThermalDetection[FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.CurrentThermalSignature;
                                                        if (CurrentTaskGroup.BestThermalCount != 0)
                                                        {
                                                            detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                                                        }
                                                        else
                                                        {
                                                            detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                                        }

                                                        /// <summary>
                                                        /// Test each ship until I get to one I don't see.
                                                        /// </summary>
                                                        det = LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.ThermalDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                            if (DetShipList.Contains(scratch) == false)
                                                            {
                                                                DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.ThermalSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial Thermal detect for TGs looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, CurrentTaskGroup.Contact.Position.System, CurrentTaskGroup.Contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        MessageLog.Add(NMsg);
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

                                if (System.FactionDetectionLists[FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    noDetection = false;
                                    allDetection = false;

                                    /// <summary>
                                    /// Get the best detection range for EM signatures in loop.
                                    /// </summary>
                                    int ShipID = TaskGroup.EMSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentEMSignature;

                                    /// <summary>
                                    /// Check to see if the taskgroup has an em sensor, and that said em sensor is not destroyed.
                                    /// otherwise use the default passive detection range.
                                    /// </summary>
                                    if (CurrentTaskGroup.BestEMCount > 0)
                                    {
                                        detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                                    }
                                    else
                                    {
                                        detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                    }

                                    bool det = LargeDetection(dist, detection);

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
                                        ShipID = TaskGroup.EMSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.CurrentEMSignature;

                                        /// <summary>
                                        /// once again we must check here to make sure that the taskgroup does have a passive suite, or else use the default one.
                                        /// </summary>
                                        if (CurrentTaskGroup.BestEMCount > 0)
                                        {
                                            detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                                        }
                                        else
                                        {
                                            detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                        }

                                        det = LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].EMDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                if (DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>


                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.EMSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.EMDetection[FactionID] != GameState.Instance.CurrentSecond)
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
                                                            if (TaskGroup.Ships[node.Next.Value].EMDetection[FactionID] == GameState.Instance.CurrentSecond)
                                                            {
                                                                System.FactionDetectionLists[FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                                            }
                                                            break;
                                                        }

                                                        if (CurrentTaskGroup.BestEMCount > 0)
                                                        {
                                                            detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                                                        }
                                                        else
                                                        {
                                                            detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                                        }

                                                        det = LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.EMDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                            if (DetShipList.Contains(scratch) == false)
                                                            {
                                                                DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.EMSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial EM detect for TGs looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);

                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, CurrentTaskGroup.Contact.Position.System, CurrentTaskGroup.Contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        MessageLog.Add(NMsg);
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

                                if (System.FactionDetectionLists[FactionID].Active[detListIterator] != GameState.Instance.CurrentSecond && CurrentTaskGroup.ActiveSensorQue.Count > 0)
                                {
                                    noDetection = false;
                                    allDetection = false;

                                    /// <summary>
                                    /// Get the best detection range for thermal signatures in loop.
                                    /// </summary>
                                    int ShipID = TaskGroup.ActiveSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.TotalCrossSection - 1;

                                    if (sig > Constants.ShipTN.ResolutionMax - 1)
                                        sig = Constants.ShipTN.ResolutionMax - 1;

                                    detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);


                                    bool det = LargeDetection(dist, detection);

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
                                        ShipID = TaskGroup.ActiveSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.TotalCrossSection - 1;

                                        if (sig > Constants.ShipTN.ResolutionMax - 1)
                                            sig = Constants.ShipTN.ResolutionMax - 1;

                                        detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                                        det = LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].ActiveDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                if (DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            /// <summary>
                                            /// FactionSystemDetection entry. I hope to deprecate this at some point.
                                            /// Be sure to erase the factionDetectionSystem entry first, to track down everywhere this overbloated thing is.
                                            /// update, not happening. FactionDetectionList is too important.
                                            /// </summary>
                                            System.FactionDetectionLists[FactionID].Active[detListIterator] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>


                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.ActiveSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.ActiveDetection[FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.TotalCrossSection - 1;

                                                        if (sig > Constants.ShipTN.ResolutionMax - 1)
                                                            sig = Constants.ShipTN.ResolutionMax - 1;

                                                        detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                                                        det = LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.ActiveDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                            if (DetShipList.Contains(scratch) == false)
                                                            {
                                                                DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.ActiveSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial Active detect for TGs looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);

                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, CurrentTaskGroup.Contact.Position.System, CurrentTaskGroup.Contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        MessageLog.Add(NMsg);
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
                            }
                            #endregion

                        }
                        else if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.Missile)
                        {
                            OrdnanceGroupTN MissileGroup = System.SystemContactList[detListIterator].Entity as OrdnanceGroupTN;

                            if (MissileGroup.missiles.Count != 0)
                            {
                                /// <summary>
                                /// Do Missile Detection here:
                                /// </summary>
                                #region Missile Detection
                                OrdnanceTN Missile = MissileGroup.missiles[0];
                                if (System.FactionDetectionLists[FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    int ThermalSignature = (int)Math.Ceiling(Missile.missileDef.totalThermalSignature);
                                    detection = -1;

                                    /// <summary>
                                    /// Check to make sure the taskgroup has a thermal sensor available, otherwise use the default.
                                    /// </summary>
                                    if (CurrentTaskGroup.BestThermalCount != 0)
                                    {
                                        detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(ThermalSignature);
                                    }
                                    else
                                    {
                                        detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(ThermalSignature);
                                    }

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = LargeDetection(dist, detection);

                                    /// <summary>
                                    /// If one missile is detected, all are.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ThermalDetection[FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;
                                    }
                                }

                                if (System.FactionDetectionLists[FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    int EMSignature = 0;
                                    if (Missile.missileDef.activeStr != 0.0f)
                                    {
                                        EMSignature = Missile.missileDef.aSD.gps;
                                    }

                                    if (EMSignature != 0)
                                    {
                                        /// <summary>
                                        /// Check to see if the taskgroup has an em sensor, and that said em sensor is not destroyed.
                                        /// otherwise use the default passive detection range.
                                        /// </summary>
                                        if (CurrentTaskGroup.BestEMCount > 0)
                                        {
                                            detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(EMSignature);
                                        }
                                        else
                                        {
                                            detection = ComponentList.DefaultPassives.GetPassiveDetectionRange(EMSignature);
                                        }

                                        bool det = LargeDetection(dist, detection);

                                        /// <summary>
                                        /// If one missile is detected, all are.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                            {
                                                MissileGroup.missiles[loop3].EMDetection[FactionID] = GameState.Instance.CurrentSecond;
                                            }

                                            if (DetMissileList.Contains(MissileGroup) == false)
                                            {
                                                DetMissileList.Add(MissileGroup);
                                            }


                                            System.FactionDetectionLists[FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                        }
                                    }
                                }

                                if (System.FactionDetectionLists[FactionID].Active[detListIterator] != GameState.Instance.CurrentSecond && CurrentTaskGroup.ActiveSensorQue.Count > 0)
                                {
                                    int TotalCrossSection_MSP = (int)Math.Ceiling(Missile.missileDef.size);
                                    sig = -1;
                                    detection = -1;

                                    if (TotalCrossSection_MSP < ((Constants.OrdnanceTN.MissileResolutionMaximum + 6) + 1))
                                    {
                                        if (TotalCrossSection_MSP <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                                        {
                                            sig = Constants.OrdnanceTN.MissileResolutionMinimum;
                                        }
                                        else if (TotalCrossSection_MSP <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                                        {
                                            sig = TotalCrossSection_MSP - 6;
                                        }
                                        detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(0, sig);
                                    }
                                    else
                                    {
                                        /// <summary>
                                        /// Big missiles will be treated in HS terms: 21-40 MSP = 2 HS, 41-60 = 3 HS, 61-80 = 4 HS, 81-100 = 5 HS. The same should hold true for greater than 100 sized missiles.
                                        /// but those are impossible to build.
                                        /// </summary>
                                        sig = (int)Math.Ceiling((float)TotalCrossSection_MSP / 20.0f);

                                        detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);
                                    }

                                    bool det = LargeDetection(dist, detection);

                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ActiveDetection[FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[FactionID].Active[detListIterator] = GameState.Instance.CurrentSecond;
                                    }
                                }
                                #endregion
                            }
                        }
                        /// <summary>
                        /// End if planet,Task Group, or Missile
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
            #endregion
            /// <summary>
            /// End for Faction TaskGroups.
            /// </summary>


            /// <summary>
            /// Loop through all missile groups.
            /// </summary>
            for (int loop = 0; loop < MissileGroups.Count; loop++)
            #region Faction Missile Loop
            {
                /// <summary>
                /// Hopefully I won't get into this situation ever. 
                /// </summary>
                if (MissileGroups[loop].missiles.Count == 0)
                {
                    String ErrorMessage = string.Format("Missile group {0} has no missiles and is still in the list of missile groups.", MissileGroups[loop].Name);
                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, MissileGroups[loop].contact.Position.System, MissileGroups[loop].contact,
                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);

                    MessageLog.Add(NMsg);
                    continue;
                }

                /// <summary>
                /// Missile groups have homogeneous missile complements, so will only have 1 of each sensor to check against.
                /// </summary>
                StarSystem System = MissileGroups[loop].contact.Position.System;
                OrdnanceTN Missile = MissileGroups[loop].missiles[0];

                /// <summary>
                /// If this missile has no sensors don't process all this.
                /// </summary>
                if (Missile.missileDef.tHD == null && Missile.missileDef.eMD == null && Missile.missileDef.aSD == null)
                    continue;

                /// <summary>
                /// Loop through the global contacts list for the system. thermal.Count is equal to SystemContacts.Count. or should be.
                /// </summary>
                for (int loop2 = 0; loop2 < System.FactionDetectionLists[FactionID].Thermal.Count; loop2++)
                {
                    /// <summary>
                    /// I don't own loop2, and it hasn't been fully detected yet. And this missile can actually detect things.
                    /// </summary>
                    if (this != System.SystemContactList[loop2].faction && System.FactionDetectionLists[FactionID].Thermal[loop2] != GameState.Instance.CurrentSecond &&
                        System.FactionDetectionLists[FactionID].EM[loop2] != GameState.Instance.CurrentSecond && System.FactionDetectionLists[FactionID].Active[loop2] != GameState.Instance.CurrentSecond &&
                        (Missile.missileDef.thermalStr != 0.0f || Missile.missileDef.eMStr != 0.0f || Missile.missileDef.activeStr != 0.0f))
                    {
                        float dist;
                        TaskGroups[loop].Contact.DistTable.GetDistance(System.SystemContactList[loop2], out dist);

                        /// <summary>
                        /// Now to find the biggest thermal signature in the contact. The biggest for planets is just the planetary pop itself since
                        /// multiple colonies really shouldn't happen.
                        /// </summary>
                        int sig = -1;
                        int detection = -1;

                        /// <summary>
                        /// Check for each detection method whether or not this missile can actually detect things, then copy the taskgroup detection code here.
                        /// Update detected contacts as needed:
                        /// </summary>
                        if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.Population)
                        {
                            #region Planetary detection by missiles
                            Population Pop = System.SystemContactList[loop2].Entity as Population;
                            /// <summary>
                            /// Does this missile have a thermal sensor suite? by default the answer is no, this is different from Aurora which gives them the basic ship suite.
                            /// </summary>
                            if (Missile.missileDef.tHD != null)
                            {
                                sig = Pop.ThermalSignature;
                                detection = Missile.missileDef.tHD.GetPassiveDetectionRange(sig);

                                bool det = LargeDetection(dist, detection);

                                /// <summary>
                                /// Mark this contact as detected for this time slice via thermal for both the contact, and for the faction as a whole.
                                /// </summary>
                                if (det == true)
                                {
                                    Pop.ThermalDetection[FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[FactionID].Thermal[loop2] = GameState.Instance.CurrentSecond;
                                }
                            }

                            /// <summary>
                            /// Does this missile have an EM sensor suite? by default again the answer is no, which is again different from Aurora.
                            /// </summary>
                            if (Missile.missileDef.eMD != null)
                            {
                                sig = Pop.EMSignature;
                                detection = Missile.missileDef.eMD.GetPassiveDetectionRange(sig);

                                bool det = LargeDetection(dist, detection);

                                /// <summary>
                                /// Mark this contact as detected for this time slice via EM for both the contact, and for the faction as a whole.
                                /// </summary>
                                if (det == true)
                                {
                                    Pop.EMDetection[FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[FactionID].EM[loop2] = GameState.Instance.CurrentSecond;
                                }
                            }

                            /// <summary>
                            /// Lastly does this missile have an active sensor?
                            /// </summary>
                            if (Missile.missileDef.aSD != null)
                            {
                                sig = Constants.ShipTN.ResolutionMax - 1;
                                detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);

                                bool det = LargeDetection(dist, detection);

                                /// <summary>
                                /// Mark this contact as detected for this time slice via Active for both the contact, and for the faction as a whole.
                                /// </summary>
                                if (det == true)
                                {
                                    Pop.ActiveDetection[FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[FactionID].Active[loop2] = GameState.Instance.CurrentSecond;
                                }
                            }
                            #endregion
                        }
                        else if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.TaskGroup)
                        {
                            TaskGroupTN TaskGroup = System.SystemContactList[loop2].Entity as TaskGroupTN;

                            if (TaskGroup.Ships.Count != 0)
                            {
                                #region Taskgroup Detection by Missiles
                                /// <summary>
                                /// Taskgroups have multiple signatures, so noDetection and allDetection become important.
                                /// </summary>

                                bool noDetection = false;
                                bool allDetection = false;

                                #region Ship Thermal Detection Code
                                if (System.FactionDetectionLists[FactionID].Thermal[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.tHD != null)
                                {
                                    int ShipID = TaskGroup.ThermalSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentThermalSignature;

                                    detection = Missile.missileDef.tHD.GetPassiveDetectionRange(sig);

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = LargeDetection(dist, detection);

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
                                        ShipID = TaskGroup.ThermalSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.CurrentThermalSignature;

                                        /// <summary>
                                        /// Now for the smallest vs the best.
                                        /// </summary>
                                        detection = Missile.missileDef.tHD.GetPassiveDetectionRange(sig);
                                        det = LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].ThermalDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                if (DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[FactionID].Thermal[loop2] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>
                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.ThermalSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.ThermalDetection[FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.CurrentThermalSignature;
                                                        detection = Missile.missileDef.tHD.GetPassiveDetectionRange(sig);

                                                        /// <summary>
                                                        /// Test each ship until I get to one I don't see.
                                                        /// </summary>
                                                        det = LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.ThermalDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                            if (DetShipList.Contains(scratch) == false)
                                                            {
                                                                DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.ThermalSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial Thermal detect for missiles looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, MissileGroups[loop].contact.Position.System, MissileGroups[loop].contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        MessageLog.Add(NMsg);
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }//end while
                                            }//end for
                                        }//end else if (noDetection == false && allDetection == false)
                                    }//end if noDetection == false
                                }//end if not detected and missile can detect thermally
                                #endregion

                                #region Ship EM Detection Code
                                if (System.FactionDetectionLists[FactionID].EM[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.eMD != null)
                                {
                                    int ShipID = TaskGroup.EMSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentEMSignature;

                                    detection = Missile.missileDef.eMD.GetPassiveDetectionRange(sig);

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = LargeDetection(dist, detection);

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
                                        ShipID = TaskGroup.EMSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.CurrentEMSignature;

                                        /// <summary>
                                        /// Now for the smallest vs the best.
                                        /// </summary>
                                        detection = Missile.missileDef.eMD.GetPassiveDetectionRange(sig);
                                        det = LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].EMDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                if (DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[FactionID].EM[loop2] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>
                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.EMSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.EMDetection[FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.CurrentEMSignature;

                                                        /// <summary>
                                                        /// here is where EM detection differs from Thermal detection:
                                                        /// If a ship has a signature of 0 by this point(and we didn't already hit noDetection above,
                                                        /// it means that one ship is emitting a signature, but that no other ships are.
                                                        /// Mark the group as totally detected, but not the ships, this serves to tell me that the ships are undetectable
                                                        /// in this case.
                                                        /// Also, sig will never be 0 for the first iteration of the loop, that has already been tested, so I don't need to worry about
                                                        /// node.Next.Value blowing up on me.
                                                        /// </summary>
                                                        if (sig == 0)
                                                        {
                                                            /// <summary>
                                                            /// The last signature we looked at was the ship emitting an EM sig, and this one is not.
                                                            /// Mark the entire group as "spotted" because no other detection will occur.
                                                            /// </summary>
                                                            if (TaskGroup.Ships[node.Next.Value].EMDetection[FactionID] == GameState.Instance.CurrentSecond)
                                                            {
                                                                System.FactionDetectionLists[FactionID].EM[loop2] = GameState.Instance.CurrentSecond;
                                                            }
                                                            break;
                                                        }


                                                        detection = Missile.missileDef.eMD.GetPassiveDetectionRange(sig);

                                                        /// <summary>
                                                        /// Test each ship until I get to one I don't see.
                                                        /// </summary>
                                                        det = LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.EMDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                            if (DetShipList.Contains(scratch) == false)
                                                            {
                                                                DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.EMSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial EM detect for missiles looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, MissileGroups[loop].contact.Position.System, MissileGroups[loop].contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        MessageLog.Add(NMsg);
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }//end while
                                            }//end for
                                        }//end else if (noDetection == false && allDetection == false)
                                    }//end if noDetection == false
                                }//end if not detected and missile can detect EM
                                #endregion

                                #region Ship Active Detection Code
                                if (System.FactionDetectionLists[FactionID].Active[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.aSD != null)
                                {
                                    int ShipID = TaskGroup.ActiveSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.TotalCrossSection;

                                    detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = LargeDetection(dist, detection);

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
                                        ShipID = TaskGroup.ActiveSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.TotalCrossSection;

                                        /// <summary>
                                        /// Now for the smallest vs the best.
                                        /// </summary>
                                        detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);
                                        det = LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].ActiveDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                if (DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[FactionID].Active[loop2] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>
                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.ActiveSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.ActiveDetection[FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.TotalCrossSection;
                                                        detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);

                                                        /// <summary>
                                                        /// Test each ship until I get to one I don't see.
                                                        /// </summary>
                                                        det = LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.ActiveDetection[FactionID] = GameState.Instance.CurrentSecond;

                                                            if (DetShipList.Contains(scratch) == false)
                                                            {
                                                                DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.ActiveSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial Active detect for missiles looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, MissileGroups[loop].contact.Position.System, MissileGroups[loop].contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        MessageLog.Add(NMsg);
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }//end while
                                            }//end for
                                        }//end else if (noDetection == false && allDetection == false)
                                    }//end if noDetection == false
                                }//end if not detected and missile can detect TCS
                                #endregion
                                #endregion
                            }
                        }
                        else if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.Missile)
                        {
                            OrdnanceGroupTN MissileGroup = System.SystemContactList[loop2].Entity as OrdnanceGroupTN;

                            if (MissileGroup.missiles.Count != 0)
                            {

                                /// <summary>
                                /// Do Missile Detection here:
                                /// </summary>
                                #region Missile Detection
                                OrdnanceTN MissileTarget = MissileGroup.missiles[0];
                                if (System.FactionDetectionLists[FactionID].Thermal[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.tHD != null)
                                {
                                    int ThermalSignature = (int)Math.Ceiling(MissileTarget.missileDef.totalThermalSignature);

                                    detection = Missile.missileDef.tHD.GetPassiveDetectionRange(ThermalSignature);

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = LargeDetection(dist, detection);

                                    /// <summary>
                                    /// If one missile is detected, all are.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ThermalDetection[FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[FactionID].Thermal[loop2] = GameState.Instance.CurrentSecond;
                                    }
                                }

                                if (System.FactionDetectionLists[FactionID].EM[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.eMD != null)
                                {
                                    int EMSignature = 0;
                                    if (MissileTarget.missileDef.activeStr != 0.0f && MissileTarget.missileDef.aSD != null)
                                    {
                                        EMSignature = MissileTarget.missileDef.aSD.gps;
                                    }

                                    if (EMSignature != 0)
                                    {
                                        detection = Missile.missileDef.eMD.GetPassiveDetectionRange(EMSignature);

                                        bool det = LargeDetection(dist, detection);

                                        /// <summary>
                                        /// If one missile is detected, all are.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                            {
                                                MissileGroup.missiles[loop3].EMDetection[FactionID] = GameState.Instance.CurrentSecond;
                                            }

                                            if (DetMissileList.Contains(MissileGroup) == false)
                                            {
                                                DetMissileList.Add(MissileGroup);
                                            }

                                            System.FactionDetectionLists[FactionID].EM[loop2] = GameState.Instance.CurrentSecond;
                                        }
                                    }
                                }

                                if (System.FactionDetectionLists[FactionID].Active[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.aSD != null)
                                {
                                    int TotalCrossSection_MSP = (int)Math.Ceiling(MissileTarget.missileDef.size);
                                    sig = -1;
                                    detection = -1;

                                    if (TotalCrossSection_MSP < ((Constants.OrdnanceTN.MissileResolutionMaximum + 6) + 1))
                                    {
                                        if (TotalCrossSection_MSP <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                                        {
                                            sig = Constants.OrdnanceTN.MissileResolutionMinimum;
                                        }
                                        if (TotalCrossSection_MSP <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                                        {
                                            sig = TotalCrossSection_MSP - 6;
                                        }
                                        detection = Missile.missileDef.aSD.GetActiveDetectionRange(0, sig);
                                    }
                                    else
                                    {
                                        /// <summary>
                                        /// Big missiles will be treated in HS terms: 21-40 MSP = 2 HS, 41-60 = 3 HS, 61-80 = 4 HS, 81-100 = 5 HS. The same should hold true for greater than 100 sized missiles.
                                        /// but those are impossible to build.
                                        /// </summary>
                                        sig = (int)Math.Ceiling((float)TotalCrossSection_MSP / 20.0f);
                                        detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);
                                    }

                                    bool det = LargeDetection(dist, detection);

                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ActiveDetection[FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[FactionID].Active[loop2] = GameState.Instance.CurrentSecond;
                                    }
                                }
                                #endregion
                            }
                        }//end else if SSE = missile && ordnance group has missiles in it
                    }//end if contact not detected and can be detected
                }//end for faction detection list contacts
            }//end for missile groups
            #endregion



            /// <summary>
            /// Detected contacts logic. If a ship has been newly detected this tick, create a contact entry for it.
            /// Otherwise update the existing one. Messages to the message log should be handled there(at the top of this very file.
            /// if a ship is no longer detected this tick then remove it from the detected contacts list.
            /// 
            /// This can actually be improved by turning detectedContact into a linked list, and putting updated contacts in front.
            /// this way unupdated contacts would be at the end, and I would not have to loop through all ships here.
            /// </summary>
            for (int loop3 = 0; loop3 < DetShipList.Count; loop3++)
            {
                ShipTN detectedShip = DetShipList[loop3];
                StarSystem System = detectedShip.ShipsTaskGroup.Contact.Position.System;

                /// <summary>
                /// Sanity check to keep allied ships out of the DetectedContacts list.
                /// </summary>
                if (detectedShip.ShipsFaction != this)
                {
                    bool inDict = DetectedContactLists.ContainsKey(System);

                    if (inDict == false)
                    {
                        DetectedContactsList newDCL = new DetectedContactsList();
                        DetectedContactLists.Add(System, newDCL);
                    }

                    inDict = DetectedContactLists[System].DetectedContacts.ContainsKey(detectedShip);

                    bool th = (detectedShip.ThermalDetection[FactionID] == GameState.Instance.CurrentSecond);
                    bool em = (detectedShip.EMDetection[FactionID] == GameState.Instance.CurrentSecond);
                    bool ac = (detectedShip.ActiveDetection[FactionID] == GameState.Instance.CurrentSecond);

                    if (inDict == true)
                    {
                        DetectedContactLists[System].DetectedContacts[detectedShip].updateFactionContact(this, th, em, ac, (uint)GameState.Instance.CurrentSecond);

                        if (th == false && em == false && ac == false)
                        {
                            DetectedContactLists[System].DetectedContacts.Remove(detectedShip);
                        }
                    }
                    else if (inDict == false && (th == true || em == true || ac == true))
                    {
                        FactionContact newContact = new FactionContact(this, detectedShip, th, em, ac, (uint)GameState.Instance.CurrentSecond);
                        DetectedContactLists[System].DetectedContacts.Add(detectedShip, newContact);
                    }
                }
            }

            for (int loop3 = 0; loop3 < DetMissileList.Count; loop3++)
            {
                OrdnanceTN Missile = DetMissileList[loop3].missiles[0];
                StarSystem System = DetMissileList[loop3].contact.Position.System;

                /// <summary>
                /// Sanity check to keep allied missiles out of the DetectedContacts list.
                /// </summary>
                if (Missile.missileGroup.ordnanceGroupFaction != this)
                {
                    bool inDict = DetectedContactLists.ContainsKey(System);

                    if (inDict == false)
                    {
                        DetectedContactsList newDCL = new DetectedContactsList();
                        DetectedContactLists.Add(System, newDCL);
                    }

                    inDict = DetectedContactLists[System].DetectedMissileContacts.ContainsKey(Missile.missileGroup);

                    bool th = (Missile.ThermalDetection[FactionID] == GameState.Instance.CurrentSecond);
                    bool em = (Missile.EMDetection[FactionID] == GameState.Instance.CurrentSecond);
                    bool ac = (Missile.ActiveDetection[FactionID] == GameState.Instance.CurrentSecond);

                    if (inDict == true)
                    {
                        DetectedContactLists[System].DetectedMissileContacts[Missile.missileGroup].updateFactionContact(this, th, em, ac, (uint)GameState.Instance.CurrentSecond);

                        if (th == false && em == false && ac == false)
                        {
                            DetectedContactLists[System].DetectedMissileContacts.Remove(Missile.missileGroup);
                        }
                    }
                    else if (inDict == false && (th == true || em == true || ac == true))
                    {
                        FactionContact newContact = new FactionContact(this, Missile.missileGroup, th, em, ac, (uint)GameState.Instance.CurrentSecond);
                        DetectedContactLists[System].DetectedMissileContacts.Add(Missile.missileGroup, newContact);
                    }
                }
            }

        }
        /// <summary>
        /// End SensorSweep()
        /// </summary>

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
                double factor = Constants.Units.KM_PER_AU / 10000.0;
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
#warning baseTracking is a magic number here.
            BaseTracking = 25000;
        }
    }
}
