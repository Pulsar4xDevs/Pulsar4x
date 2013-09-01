using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

/// <summary>
/// I am concerned that the distances here are going to get too massive without some bounds checking, that will have to be added in at some point.
/// </summary>

namespace Pulsar4X.Entities
{
    public class SystemContact : StarSystemEntity
    {
        /// <summary>
        /// To which faction does this contact belong?
        /// </summary>
        public Faction faction { get; set; }

        /// <summary>
        /// Which system is this contact in?
        /// </summary>
        public StarSystem CurrentSystem { get; set; }

        /// <summary>
        /// While SSEntity contains XSystem, X position in AU, this is X position in KM 
        /// </summary>
        public float SystemKmX { get; set; }

        /// <summary>
        /// Corresponding Y position in system.
        /// </summary>
        public float SystemKmY { get; set; }

        /// <summary>
        /// where the contact was on the last tick.
        /// </summary>
        public double LastXSystem { get; set; }

        /// <summary>
        /// Where the contact was on the last tick.
        /// </summary>
        public double LastYSystem { get; set; }

        /// <summary>
        /// Utterly useless Mass value included due to compiler demanding it.
        /// </summary>
        public override double Mass
        {
            get { return 0.0; }
            set { value = 0.0; }
        }

        /// <summary>
        /// If this contact is a planetary population it will be here.
        /// </summary>
        public Population Pop { get; set; }

        /// <summary>
        /// if the contact is a taskgroup it will be stored here.
        /// </summary>
        public TaskGroupTN TaskGroup { get; set; }

        /// <summary>
        /// Distance between this contact and the other contacts in the system.
        /// </summary>
        public BindingList<float> DistanceTable { get; set; }

        /// <summary>
        /// Last timeslice the distance Table was updated.
        /// </summary>
        public BindingList<int> DistanceUpdate { get; set; }


        /// <summary>
        /// Creates a new system contact.
        /// </summary>
        /// <param name="Fact">Faction of contact.</param>
        /// <param name="body">Type of contact.</param>
        public SystemContact(Faction Fact, Population pop)
        {
            faction = Fact;
            XSystem = pop.Planet.XSystem;
            YSystem = pop.Planet.YSystem;
            LastXSystem = XSystem;
            LastYSystem = YSystem;

            SystemKmX = (float)(XSystem * Constants.Units.KM_PER_AU);
            SystemKmY = (float)(YSystem * Constants.Units.KM_PER_AU);

            Pop = pop;
            SSEntity = StarSystemEntityType.Population;

            DistanceTable = new BindingList<float>();
            DistanceUpdate = new BindingList<int>();
        }

        /// <summary>
        /// Creates a new system contact.
        /// </summary>
        /// <param name="Fact">Faction of contact.</param>
        /// <param name="TG">Type of contact.</param>
        public SystemContact(Faction Fact, TaskGroupTN TG)
        {
            faction = Fact;
            XSystem = TG.XSystem;
            YSystem = TG.YSystem;
            LastXSystem = XSystem;
            LastYSystem = YSystem;

            SystemKmX = (float)(XSystem * Constants.Units.KM_PER_AU);
            SystemKmY = (float)(YSystem * Constants.Units.KM_PER_AU);

            TaskGroup = TG;
            SSEntity = StarSystemEntityType.TaskGroup;
            DistanceTable = new BindingList<float>();
            DistanceUpdate = new BindingList<int>();
        }

        /// <summary>
        /// Updates the location of the contact.
        /// </summary>
        /// <param name="X">X position in AU.</param>
        /// <param name="Y">Y Position in AU.</param>
        public void UpdateLocationInSystem(double X, double Y)
        {
            LastXSystem = XSystem;
            LastYSystem = YSystem;
            XSystem = X;
            YSystem = Y;

            SystemKmX = (float)(XSystem * Constants.Units.KM_PER_AU);
            SystemKmY = (float)(YSystem * Constants.Units.KM_PER_AU);
        }

        /// <summary>
        /// Updates the system location of this contact. The 4 blocks of updates to the lists will, I hope, facilitate efficient updating of the binding list.
        /// </summary>
        /// <param name="system">new System.</param>
        public void UpdateSystem(StarSystem system)
        {
            CurrentSystem = system;

            DistanceTable.Clear();
            DistanceUpdate.Clear();

            DistanceTable.RaiseListChangedEvents = false;
            DistanceUpdate.RaiseListChangedEvents = false;

            for (int loop = 0; loop < CurrentSystem.SystemContactList.Count; loop++)
            {
                DistanceTable.Add(0.0f);
                DistanceUpdate.Add(-1);
            }

            DistanceTable.RaiseListChangedEvents = true;
            DistanceUpdate.RaiseListChangedEvents = true;

            /// <summary>
            /// BindingLists apparently do a bunch of events every time they are changed.
            /// This has hopefully circumvented that, with just 1 event.
            /// </summary>
            DistanceTable.ResetBindings();
            DistanceUpdate.ResetBindings();
        }
    }
}
