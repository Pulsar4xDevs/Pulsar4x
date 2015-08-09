using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;
using Pulsar4X.Helpers;


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
        public VerboseBindingList<SystemContact> SystemContactList { get; set; }

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
        private int m_seed;
        public int Seed { get { return m_seed; } }

        public StarSystem(string name, int seed)
            : base()
        {
            Name = name;
            Stars = new BindingList<Star>();

            Waypoints = new BindingList<Waypoint>();
            JumpPoints = new BindingList<JumpPoint>();
            SystemContactList = new VerboseBindingList<SystemContact>();
            FactionDetectionLists = new BindingList<FactionSystemDetection>();

            TaskGroups = new BindingList<TaskGroupTN>();
            Populations = new BindingList<Population>();
            OrdnanceGroups = new BindingList<OrdnanceGroupTN>();

            m_seed = seed;

            // Subscribe to change events.
            SystemContactList.ListChanged += SystemContactList_ListChanged;

            // Create the faciton contact information for each faction.
            foreach (Faction f in GameState.Instance.Factions)
            {
                f.AddNewContactList(this);
            }
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

        private void SystemContactList_ListChanged(object sender, ListChangedEventArgs e)
        {
            BindingList<SystemContact> list = sender as BindingList<SystemContact>;

            switch (e.ListChangedType)
            {
                    ///< @todo Find a better place to update the FactionDetectionLists
                case ListChangedType.ItemAdded:
                    // Update all the faction contact lists with the new contact.
                    for (int loop = 0; loop < FactionDetectionLists.Count; loop++)
                    {
                        FactionDetectionLists[loop].AddContact();
                    }
                    break;
                case ListChangedType.ItemDeleted:
                    // Remove the contact from each of the faction contact lists as well as the System contact list.
                    for (int loop = 0; loop < FactionDetectionLists.Count; loop++)
                    {
                        FactionDetectionLists[loop].RemoveContact(e.NewIndex);
                    }
                    break;
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
        /// Updates this StarSystem for the new time.
        /// </summary>
        /// <param name="deltaSeconds">Change in seconds since last update.</param>
        public void Update(int deltaSeconds)
        {
            // Update the position of all planets. This should probably be in something like the construction tick in Aurora.
            foreach (Star CurrentStar in Stars)
            {
                CurrentStar.UpdatePosition(deltaSeconds);

                // Since the star moved, update the JumpPoint position.
                foreach (JumpPoint CurrentJumpPoint in JumpPoints)
                {
                    CurrentJumpPoint.UpdatePosition();
                }
            }
        }

        /// <summary>
        /// The cost for Gravitational survey points as per Aurora is: square root ( System Primary's solar masses ) * 400 
        /// </summary>
        /// <returns>Number of survey points that must be generated to survey each gravitational survey point.</returns>
        public int GetSurveyCost()
        {
            return (int)Math.Floor((float)Math.Sqrt(Stars[0].Orbit.MassRelativeToSol) * 400.0f);
        }
    }
}
