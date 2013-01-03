using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;
using log4net.Config;
using log4net;

namespace Pulsar4X.Entities
{
    public class StarSystem : GameEntity
    {
        public BindingList<Star> Stars { get; set; }

        /// <summary>
        /// Each starsystem has its own list of waypoints.
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
        }

        /// <summary>
        /// This function adds a waypoint to the system waypoint list, it is called by SystemMap.cs and connects the UI waypoint to the back end waypoints.
        /// </summary>
        /// <param name="XSystemAU">System Position X in AU</param>
        /// <param name="YSystemAU">System Position Y in AU</param>
        public void AddWaypoint(double XSystemAU, double YSystemAU)
        {
            Waypoint NewWP = new Waypoint(XSystemAU, YSystemAU);
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
            Waypoints.Remove(Remove);
        }

        /// <summary>
        /// Adds a new jump point to the system. Since JPs can't be destroyed there is no corresponding remove function.
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
        }


        /// <summary>
        /// This function removes contacts from the system wide contact list when a contact deletion event occurs.
        /// This happens whenever a ship is scrapped or otherwise destroyed, ships/fighters land on a hangar, missiles hit their target or run out of endurance, and jump point exits.
        /// </summary>
        /// <param name="Contact">Contact to be removed.</param>
        public void RemoveContact(SystemContact Contact)
        {
            int index = SystemContactList.IndexOf(Contact);

            /// <summary>
            /// Remove the contact from each of the faction contact lists as well as the System contact list.
            /// </summary>
            for (int loop = 0; loop < FactionDetectionLists.Count; loop++)
            {
                FactionDetectionLists[loop].RemoveContact(index);
            }

            SystemContactList.Remove(Contact);

            for (int loop = 0; loop < SystemContactList.Count; loop++)
            {
                SystemContactList[loop].DistanceTable.RemoveAt(SystemContactList.Count - 1);
                SystemContactList[loop].DistanceUpdate.RemoveAt(SystemContactList.Count - 1);
            }
        }
    }
}
