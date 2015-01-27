using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Pulsar4X.Entities.Components;

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
        /// where the contact was on the last tick.
        /// </summary>
        public SystemPosition LastPosition;

        /// <summary>
        /// If this contact is a planetary population it will be here.
        /// </summary>
        public Population Pop { get; set; }

        /// <summary>
        /// if the contact is a taskgroup it will be stored here.
        /// </summary>
        public TaskGroupTN TaskGroup { get; set; }

        /// <summary>
        /// If the contact is a missile, it goes here.
        /// </summary>
        public OrdnanceGroupTN MissileGroup { get; set; }

        /// <summary>
        /// Distance between this contact and the other contacts in the system in AU.
        /// </summary>
        public BindingList<float> DistanceTable { get; set; }

        /// <summary>
        /// Last timeslice the distance Table was updated.
        /// </summary>
        public BindingList<int> DistanceUpdate { get; set; }

        /// <summary>
        /// Contact element state.
        /// </summary>
        public enum CEState
        {
            NotCreated,
            Created,
            Delete,
            TypeCount
        }

        /// <summary>
        /// Kludge to determine when contacts should be created or deleted.
        /// </summary>
        public CEState ContactElementCreated { get; set; }


        /// <summary>
        /// Creates a new system contact.
        /// </summary>
        /// <param name="Fact">Faction of contact.</param>
        /// <param name="body">Type of contact.</param>
        public SystemContact(Faction Fact, Population pop)
        {
            Id = Guid.NewGuid();
            faction = Fact;
            Position.X = pop.Planet.Position.X;
            Position.Y = pop.Planet.Position.Y;
            LastPosition.X = Position.X;
            LastPosition.Y = Position.Y;

            Pop = pop;
            SSEntity = StarSystemEntityType.Population;

            DistanceTable = new BindingList<float>();
            DistanceUpdate = new BindingList<int>();

            ContactElementCreated = CEState.NotCreated;
        }

        /// <summary>
        /// Creates a new system contact.
        /// </summary>
        /// <param name="Fact">Faction of contact.</param>
        /// <param name="TG">Type of contact.</param>
        public SystemContact(Faction Fact, TaskGroupTN TG)
        {
            Id = Guid.NewGuid();
            faction = Fact;
            Position.X = TG.Position.X;
            Position.Y = TG.Position.Y;
            LastPosition.X = Position.X;
            LastPosition.Y = Position.Y;

            TaskGroup = TG;
            SSEntity = StarSystemEntityType.TaskGroup;
            DistanceTable = new BindingList<float>();
            DistanceUpdate = new BindingList<int>();

            ContactElementCreated = CEState.NotCreated;
        }

        /// <summary>
        /// Creates a new system contact.
        /// </summary>
        /// <param name="Fact">Faction of contact.</param>
        /// <param name="MG">Type of contact.</param>
        public SystemContact(Faction Fact, OrdnanceGroupTN MG)
        {
            Id = Guid.NewGuid();
            faction = Fact;
            Position.X = MG.Position.X;
            Position.Y = MG.Position.Y;
            LastPosition.X = Position.X;
            LastPosition.Y = Position.Y;

            MissileGroup = MG;
            SSEntity = StarSystemEntityType.Missile;
            DistanceTable = new BindingList<float>();
            DistanceUpdate = new BindingList<int>();

            ContactElementCreated = CEState.NotCreated;
        }

        /// <summary>
        /// Updates the location of the contact.
        /// </summary>
        /// <param name="X">X position in AU.</param>
        /// <param name="Y">Y Position in AU.</param>
        public void UpdateLocationInSystem(double X, double Y)
        {
            LastPosition.X = Position.X;
            LastPosition.Y = Position.Y;
            Position.X = X;
            Position.Y = Y;
        }

        /// <summary>
        /// Updates the contact after transiting a jump point, LastPosition.X needs to be set to current position for the travel line.
        /// </summary>
        /// <param name="X">X position in AU in the new system</param>
        /// <param name="Y">Y position in AU in the new system</param>
        public void UpdateLocationAfterTransit(double X, double Y)
        {
            LastPosition.X = X;
            LastPosition.Y = Y;
            Position.X = X;
            Position.Y = Y;
        }

        /// <summary>
        /// Updates the system location of this contact. The 4 blocks of updates to the lists will, I hope, facilitate efficient updating of the binding list.
        /// </summary>
        /// <param name="system">new System.</param>
        public void UpdateSystem(StarSystem system)
        {
            Position.System = system;

            DistanceTable.Clear();
            DistanceUpdate.Clear();

            DistanceTable.RaiseListChangedEvents = false;
            DistanceUpdate.RaiseListChangedEvents = false;

            for (int loop = 0; loop < Position.System.SystemContactList.Count; loop++)
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
