using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;


//using log4net.Config;
//using log4net;

namespace Pulsar4X.Entities
{
    public class StarSystem : GameEntity
    {
        public BindingList<Star> Stars { get; set; }

        /// <summary>
        /// Each starsystem has its own list of waypoints. These probably need to be faction specific?
        /// </summary>
        public BindingList<Waypoint> Waypoints { get; set; }

        /// <summary>
        /// Each system has links to other systems.
        /// </summary>
        public BindingList<JumpPoint> JumpPoints { get; set; }

        /// <summary>
        /// Global List of all contacts within the system.
        /// </summary>
        public BindingList<SystemContact> SystemContactList { get; set; }

        /// <summary>
        /// List of contacts that need to be created, this will be used by the system map/sceen display.
        /// </summary>
        public BindingList<SystemContact> ContactCreateList { get; set; }

        /// <summary>
        /// List of contacts that need to be deleted. this will be used by the system map/sceen display.
        /// </summary>
        public BindingList<SystemContact> ContactDeleteList { get; set; }

        /// <summary>
        /// List of faction contact lists. Here is where context starts getting confusing. This is a list of the last time the SystemContactList was pinged.
        /// SystemContactList stores Location and pointers to Pop/TG signatures. These must be arrayed in order from Faction[0] to Faction[Max] Corresponding to
        /// FactionContactLists[0] - [max]
        /// </summary>
        public BindingList<FactionSystemDetection> FactionDetectionLists { get; set; }


        public int Seed { get; set; }

        //public static readonly ILog logger = LogManager.GetLogger(typeof(StarSystem));

        public StarSystem()
            : this(string.Empty)
        {
        }

        public StarSystem(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Stars = new BindingList<Star>();

            Waypoints = new BindingList<Waypoint>();
            JumpPoints = new BindingList<JumpPoint>();
            SystemContactList = new BindingList<SystemContact>();
            FactionDetectionLists = new BindingList<FactionSystemDetection>();

            ContactCreateList = new BindingList<SystemContact>();
            ContactDeleteList = new BindingList<SystemContact>();
        }

        /// <summary>
        /// This function adds a waypoint to the system waypoint list, it is called by SystemMap.cs and connects the UI waypoint to the back end waypoints.
        /// </summary>
        /// <param name="XSystemAU">System Position X in AU</param>
        /// <param name="YSystemAU">System Position Y in AU</param>
        public void AddWaypoint(String Title, double XSystemAU, double YSystemAU, int FactionID)
        {
            Waypoint NewWP = new Waypoint(Title,this,XSystemAU, YSystemAU, FactionID);
            Waypoints.Add(NewWP);
            //logger.Info("Waypoint added.");
            //logger.Info(XSystemAU.ToString());
            //logger.Info(YSystemAU.ToString());
        }

        /// <summary>
        /// This function removes a waypoint from the system waypoint list, it is called in SystemMap.cs and connects the UI to the backend.
        /// </summary>
        /// <param name="Remove"></param>
        public void RemoveWaypoint(Waypoint Remove)
        {
            //logger.Info("Waypoint Removed.");
            //logger.Info(Remove.XSystem.ToString());
            //logger.Info(Remove.YSystem.ToString());
            if (Waypoints.Count == 1)
                Waypoints.Clear();
            else
                Waypoints.Remove(Remove);
        }

        /// <summary>
        /// Adds a new jump point to the system. Since JPs can't be destroyed there is no corresponding remove function. Perhaps there should be.
        /// </summary>
        /// <param name="XSystemAU">X Location in AU of JP.</param>
        /// <param name="YSystemAU">Y Location in AU of JP.</param>
        public void AddJumpPoint(double XSystemAU, double YSystemAU)
        {
            JumpPoint NewJP = new JumpPoint(this,XSystemAU,YSystemAU);
            JumpPoints.Add(NewJP);
        }


        /// <summary>
        /// Systems have to store a global(or perhaps system wide) list of contacts. This function adds a contact in the event one is generated.
        /// Generation events include construction, hangar launches, missile launches, and Jump Point Entry into the System.
        /// </summary>
        /// <param name="Contact">Contact to be added.</param>
        public void AddContact(SystemContact Contact)
        {
            /// <summary>
            /// Add a new entry to every distance table for every contact.
            /// </summary>
            for (int loop = 0; loop < SystemContactList.Count; loop++)
            {
                SystemContactList[loop].DistanceTable.Add(0.0f);
                SystemContactList[loop].DistanceUpdate.Add(-1);
            }


            SystemContactList.Add(Contact);
            Contact.UpdateSystem(this);

            /// <summary>
            /// Update all the faction contact lists with the new contact.
            /// </summary>
            for (int loop = 0; loop < FactionDetectionLists.Count; loop++)
            {
                FactionDetectionLists[loop].AddContact();
            }

            /// <summary>
            /// Inform the systemmap/sceen that a new contact needs to be created.
            /// </summary>
            ContactCreateList.Add(Contact);

            /// <summary>
            /// In the event that this contact is in the delete list, it probably means that this contact travelled through this system, left, and is back in the system, without being drawn.
            /// If so put it in the contactCreateList and take it out of the contactDeleteList.
            /// </summary>
            if (ContactDeleteList.Contains(Contact) == true)
                ContactDeleteList.Remove(Contact);
        }


        /// <summary>
        /// This function removes contacts from the system wide contact list when a contact deletion event occurs.
        /// This happens whenever a ship is scrapped or otherwise destroyed, ships/fighters land on a hangar, missiles hit their target or run out of endurance, and jump point exits.
        /// </summary>
        /// <param name="Contact">Contact to be removed.</param>
        public void RemoveContact(SystemContact Contact)
        {
            int index = SystemContactList.IndexOf(Contact);

           if (index != -1)
           {
               /// <summary>
               /// Remove the contact from each of the faction contact lists as well as the System contact list.
               /// </summary>
               for (int loop = 0; loop < FactionDetectionLists.Count; loop++)
               {
                   FactionDetectionLists[loop].RemoveContact(index);
               }

               SystemContactList.Remove(Contact);

               /// <summary>
               /// Distance Table is updated every tick, and doesn't care about last tick's info. so deleting simply the last entry
               /// causes no issues with distance calculations.
               /// </summary>
               for (int loop = 0; loop < SystemContactList.Count; loop++)
               {
                   SystemContactList[loop].DistanceTable.RemoveAt(SystemContactList.Count - 1);
                   SystemContactList[loop].DistanceUpdate.RemoveAt(SystemContactList.Count - 1);
               }

               /// <summary>
               /// inform the display that this contact needs to be deleted.
               /// </summary>
               ContactDeleteList.Add(Contact);

               /// <summary>
               /// also clean up the contact create list if this contact hasn't been created yet by the display.
               /// </summary>
               if (ContactCreateList.Contains(Contact) == true)
                   ContactCreateList.Remove(Contact);
           }
           else
           {
               String Entry = String.Format("Index for the system contact list is {0} for system {1}", index, Name);
               MessageEntry Entry2 = new MessageEntry(MessageEntry.MessageType.Error, Contact.CurrentSystem, Contact,
                                                      GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
               GameState.Instance.Factions[0].MessageLog.Add(Entry2);
           }
        }

        /// <summary>
        /// Get the PPV level for this faction in this system.
        /// </summary>
        /// <param name="fact">Faction to find PPV for</param>
        /// <returns>PPV value</returns>
        public int GetProtectionLevel(Faction fact)
        {
            int PPV = 0;
            foreach (SystemContact Contact in SystemContactList)
            {
                if (Contact.SSEntity == StarSystemEntityType.TaskGroup)
                {
                    if (Contact.TaskGroup.TaskGroupFaction == fact)
                    {
                        foreach (ShipTN Ship in Contact.TaskGroup.Ships)
                        {
                            PPV = PPV + Ship.ShipClass.PlanetaryProtectionValue;
                        }
                    }
                }
            }

            return PPV;
        }
    }
}
