using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Pulsar4X.Entities.Components;
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
        /// EM signature
        /// </summary>
        public int EMSignature { get; set; }

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
        public FactionContact(Faction CurrentFaction, ShipTN DetectedShip, bool Thermal, bool em, int EMSig, bool Active, uint tick)
        {
            ship = DetectedShip;
            missileGroup = null;
            thermal = Thermal;
            EM = em;
            EMSignature = EMSig;
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
                Contact = String.Format("{0} EM Signature {1}", Contact, EMSignature);
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
        public void updateFactionContact(Faction CurrentFaction, bool Thermal, bool Em, int EMSig, bool Active, uint tick)
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

                    EMSignature = EMSig;


                    if (ship != null)
                        Contact = String.Format("{0} EM Signature {1}", Contact, EMSignature);
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
                    EMSignature = -1;
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




}
