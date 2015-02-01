using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;


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
        /// A list of TaskGroups currently inside this system.
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroups { get; set; }

        /// <summary>
        /// A list of Populations currently inside this system.
        /// </summary>
        public BindingList<Population> Populations { get; set; }

        /// <summary>
        /// A list of OrdnanceGroups (Missile Groups) currently inside this system.
        /// </summary>
        public BindingList<OrdnanceGroupTN> OrdnanceGroups { get; set; }

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

        public int SystemIndex;

        /// <summary>
        /// Random generation seed used to generate this system.
        /// </summary>
        public int Seed { get; set; }

        private bool contactsChanged;

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

            TaskGroups = new BindingList<TaskGroupTN>();
            Populations = new BindingList<Population>();
            OrdnanceGroups = new BindingList<OrdnanceGroupTN>();

            // Subscribe to change events.
            TaskGroups.ListChanged += ContactsChanged;
            Populations.ListChanged += ContactsChanged;
            OrdnanceGroups.ListChanged += ContactsChanged;

            ContactCreateList = new BindingList<SystemContact>();
            ContactDeleteList = new BindingList<SystemContact>();
        }

        /// <summary>
        /// This function adds a waypoint to the system waypoint list, it is called by SystemMap.cs and connects the UI waypoint to the back end waypoints.
        /// </summary>
        /// <param name="X">System Position X in AU</param>
        /// <param name="Y">System Position Y in AU</param>
        public void AddWaypoint(String Title, double X, double Y, int FactionID)
        {
            Waypoint NewWP = new Waypoint(Title, this, X, Y, FactionID);
            Waypoints.Add(NewWP);
        }

        /// <summary>
        /// This function removes a waypoint from the system waypoint list, it is called in SystemMap.cs and connects the UI to the backend.
        /// </summary>
        /// <param name="Remove"></param>
        public void RemoveWaypoint(Waypoint Remove)
        {
            if (Waypoints.Count == 1)
                Waypoints.Clear();
            else
                Waypoints.Remove(Remove);
        }

        /// <summary>
        /// Adds a new jump point to the system. Since JPs can't be destroyed there is no corresponding remove function. Perhaps there should be.
        /// </summary>
        /// <param name="parentStar">Star to attach this JP to.</param>
        /// <param name="Position.XAU">X offset from Star Position</param>
        /// <param name="Position.YAU">Y offset from Star Position.</param>
        /// <returns>Newly Created Jumpoint</returns>
        public JumpPoint CreateJumpPoint(Star parentStar, double XOffsetAU, double YOffsetAU)
        {
            JumpPoint NewJP = new JumpPoint(this, parentStar, XOffsetAU, YOffsetAU);
            JumpPoints.Add(NewJP);
            return NewJP;
        }

        /// <summary>
        /// Systems have to store a global(or perhaps system wide) list of contacts. This function adds a contact in the event one is generated.
        /// Generation events include construction, hangar launches, missile launches, and Jump Point Entry into the System.
        /// </summary>
        /// <param name="Contact">Contact to be added.</param>
        public void AddContact(SystemContact Contact)
        {
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
                /// distance Table is updated every tick, and doesn't care about last tick's info. so deleting simply the last entry
                /// causes no issues with distance calculations.
                /// </summary>
                for (int loop = 0; loop < SystemContactList.Count; loop++)
                {
                    SystemContactList[loop].DistTable.Remove(Contact);
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
                MessageEntry Entry2 = new MessageEntry(MessageEntry.MessageType.Error, Contact.Position.System, Contact,
                                                       GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Entry);
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
            foreach (TaskGroupTN TaskGroup in TaskGroups)
            {
                if (TaskGroup.TaskGroupFaction == fact)
                {
                    foreach (ShipTN Ship in TaskGroup.Ships)
                    {
                        PPV = PPV + Ship.ShipClass.PlanetaryProtectionValue;
                    }
                }
            }

            return PPV;
        }

        /// <summary>
        /// Event raised when TaskGroups, Populations, or OrdnanceGroups are
        /// added/removed from the system.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContactsChanged(object sender, ListChangedEventArgs e)
        {
            contactsChanged = true;
        }

        /// <summary>
        /// Updates this StarSystem for the new time.
        /// </summary>
        /// <param name="deltaSeconds">Change in seconds since last update.</param>
        public void Update(int deltaSeconds)
        {
            // Update the position of all planets. This should probably be in something like the construction tick in Aurora.
            foreach (Star CurrentStar in Stars)
            {
                // The system primary will cause a divide by zero error currently as it has no orbit.
                if (CurrentStar != Stars[0])
                {
                    CurrentStar.UpdatePosition(deltaSeconds);

                    // Since the star moved, update the JumpPoint position.
                    foreach (JumpPoint CurrentJumpPoint in JumpPoints)
                    {
                        CurrentJumpPoint.UpdatePosition();
                    }
                }

                foreach (Planet CurrentPlanet in CurrentStar.Planets)
                {
                    CurrentPlanet.UpdatePosition(deltaSeconds);
                }
            }
        }
    }
}
