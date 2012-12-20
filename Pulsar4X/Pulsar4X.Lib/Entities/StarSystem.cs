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
        }

        public void AddWaypoint(double XSystemAU, double YSystemAU)
        {
            Waypoint NewWP = new Waypoint(XSystemAU, YSystemAU);
            Waypoints.Add(NewWP);


            //logger.Info("Waypoint added.");
            //logger.Info(XSystemAU.ToString());
            //logger.Info(YSystemAU.ToString());
        }

        public void RemoveWaypoint(Waypoint Remove)
        {
            //logger.Info("Waypoint Removed.");
            //logger.Info(Remove.XSystem.ToString());
            //logger.Info(Remove.YSystem.ToString());
            Waypoints.Remove(Remove);
        }
    }
}
