using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// Creates a new system contact.
        /// </summary>
        /// <param name="Fact">Faction of contact.</param>
        /// <param name="body">Type of contact.</param>
        public SystemContact(Faction Fact, Population pop)
        {
            faction = Fact;
            XSystem = pop.Planet.XSystem;
            YSystem = pop.Planet.YSystem;

            SystemKmX = (float)(XSystem * Constants.Units.KM_PER_AU);
            SystemKmY = (float)(YSystem * Constants.Units.KM_PER_AU);

            Pop = pop;
            SSEntity = StarSystemEntityType.Population;
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

            SystemKmX = (float)(XSystem * Constants.Units.KM_PER_AU);
            SystemKmY = (float)(YSystem * Constants.Units.KM_PER_AU);

            TaskGroup = TG;
            SSEntity = TG.SSEntity;
        }

        /// <summary>
        /// Updates the location of the contact.
        /// </summary>
        /// <param name="X">X position in AU.</param>
        /// <param name="Y">Y Position in AU.</param>
        public void UpdateLocation(double X, double Y)
        {
            XSystem = X;
            YSystem = Y;

            SystemKmX = (float)(XSystem * Constants.Units.KM_PER_AU);
            SystemKmY = (float)(YSystem * Constants.Units.KM_PER_AU);
        }

        /// <summary>
        /// Updates the system location of this contact.
        /// </summary>
        /// <param name="system">new System.</param>
        public void UpdateSystem(StarSystem system)
        {
            CurrentSystem = system;
        }
    }
}
