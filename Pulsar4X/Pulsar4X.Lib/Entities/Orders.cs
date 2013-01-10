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
        /// Order limits or perhaps delay time.
        /// </summary>
        private int Tertiary { get; set; }
        public int tertiary
        {
            get { return Tertiary; }
        }

        /// <summary>
        /// Time required to perform any non movement related portions of this order.
        /// </summary>
        private int OrderTimeRequirement { get; set; }
        public int orderTimeRequirement
        {
            get { return OrderTimeRequirement; }
            set { OrderTimeRequirement = value; }
        }

        /// <summary>
        /// Constructor for TaskGroup related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits or delay time offsets.</param>
        /// <param name="TaskGroupOrder">The TaskGroup in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, TaskGroupTN TaskGroupOrder)
        {
            TypeOf = TypeOrder;
            Target = TaskGroupOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            TaskGroup = TaskGroupOrder;

            OrderTimeRequirement = -1;
        }

        /// <summary>
        /// Constructor for planet related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits or delay time offsets.</param>
        /// <param name="PlanetOrder">The Planet in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, Planet PlanetOrder)
        {
            TypeOf = TypeOrder;
            Target = PlanetOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            Body = PlanetOrder;

            OrderTimeRequirement = -1;
        }

        /// <summary>
        /// Constructor for Population related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits or delay time offsets.</param>
        /// <param name="PopOrder">The Population in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, Population PopOrder)
        {
            TypeOf = TypeOrder;
            Target = PopOrder.Planet;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            Pop = PopOrder;

            OrderTimeRequirement = -1;
        }

        /// <summary>
        /// Constructor for Jump point related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits or delay time offsets.</param>
        /// <param name="JPOrder">The Jump Point in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, JumpPoint JPOrder)
        {
            TypeOf = TypeOrder;
            Target = JPOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            JumpPoint = JPOrder;

            OrderTimeRequirement = -1;
        }

        /// <summary>
        /// Constructor for way point related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits or delay time offsets.</param>
        /// <param name="WPOrder">The Way Point in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, Waypoint WPOrder)
        {
            TypeOf = TypeOrder;
            Target = WPOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            WayPoint = WPOrder;

            OrderTimeRequirement = -1;
        }
    }


}
