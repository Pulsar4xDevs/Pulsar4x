using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
namespace Pulsar4X.Entities
{
    public class Orders
    {
        /// <summary>
        /// What order type is this
        /// </summary>
        private Constants.ShipTN.OrderType TypeOf { get; set; }
        public Constants.ShipTN.OrderType typeOf
        {
            get { return TypeOf; }
        }

        /// <summary>
        /// What entity are those orders pointed at?
        /// </summary>
        private StarSystemEntity Target { get; set; }
        public StarSystemEntity target
        {
            get { return Target; }
        }

        /// <summary>
        /// If SSE is a TG then this is that task group.
        /// </summary>
        private TaskGroupTN TaskGroup { get; set; }
        public TaskGroupTN taskGroup
        {
            get { return TaskGroup; }
        }

        /// <summary>
        /// SSE might be a planet, if so it is here.
        /// </summary>
        private Planet Body { get; set; }
        public Planet body
        {
            get { return Body; }
        }
        /// <summary>
        /// If SSE is a population then this is that population.
        /// </summary>
        private Population Pop { get; set; }
        public Population pop
        {
            get { return Pop; }
        }

        /// <summary>
        /// IF SSE should be a jumppoint then said Jumppoint is stored here.
        /// </summary>
        private JumpPoint JumpPoint { get; set; }
        public JumpPoint jumpPoint
        {
            get { return JumpPoint; }

        }

        private Waypoint WayPoint { get; set; }
        public Waypoint wayPoint 
        {
            get { return WayPoint; }
        }

        /// <summary>
        /// Installation/Ship Component/Troops/Ship as part of taskgroup identifier for load/tractor orders.
        /// </summary>
        private int Secondary { get; set; }
        public int secondary
        {
            get { return Secondary; }
        }

        /// <summary>
        /// Constructor for TaskGroup related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="TargetOrder">StarSystem entity contains location and type of SSE.</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TaskGroupOrder">The TaskGroup in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, StarSystemEntity TargetOrder, int SecondaryOrder, TaskGroupTN TaskGroupOrder)
        {
            TypeOf = TypeOrder;
            Target = TargetOrder;
            Secondary = SecondaryOrder;
            TaskGroup = TaskGroupOrder;
        }

        /// <summary>
        /// Constructor for planet related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="TargetOrder">StarSystem entity contains location and type of SSE.</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="PlanetOrder">The Planet in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, StarSystemEntity TargetOrder, int SecondaryOrder, Planet PlanetOrder)
        {
            TypeOf = TypeOrder;
            Target = TargetOrder;
            Secondary = SecondaryOrder;
            Body = PlanetOrder;
        }

        /// <summary>
        /// Constructor for Population related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="TargetOrder">StarSystem entity contains location and type of SSE.</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="PopOrder">The Population in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, StarSystemEntity TargetOrder, int SecondaryOrder, Population PopOrder)
        {
            TypeOf = TypeOrder;
            Target = TargetOrder;
            Secondary = SecondaryOrder;
            Pop = PopOrder;
        }

        /// <summary>
        /// Constructor for Jump point related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="TargetOrder">StarSystem entity contains location and type of SSE.</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="JPOrder">The Jump Point in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, StarSystemEntity TargetOrder, int SecondaryOrder, JumpPoint JPOrder)
        {
            TypeOf = TypeOrder;
            Target = TargetOrder;
            Secondary = SecondaryOrder;
            JumpPoint = JPOrder;
        }

        /// <summary>
        /// Constructor for way point related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="TargetOrder">StarSystem entity contains location and type of SSE.</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="WPOrder">The Way Point in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, StarSystemEntity TargetOrder, int SecondaryOrder, Waypoint WPOrder)
        {
            TypeOf = TypeOrder;
            Target = TargetOrder;
            Secondary = SecondaryOrder;
            WayPoint = WPOrder;
        }
    }


}
