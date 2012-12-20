using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class StarSystem : GameEntity
    {
        public BindingList<Star> Stars { get; set; }

        /// <summary>
        /// Each starsystem has its own list of waypoints.
        /// </summary>
        public BindingList<Waypoint> Waypoints { get; set; }

        public int Seed { get; set; }

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
        }

        public void AddWaypoint(double XSystemAU, double YSystemAU)
        {
            Waypoint NewWP = new Waypoint(XSystemAU, YSystemAU);
            Waypoints.Add(NewWP);
        }

        public void RemoveWaypoint(Waypoint Remove)
        {
            Waypoints.Remove(Remove);
        }
    }
}
